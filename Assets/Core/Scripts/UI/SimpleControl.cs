using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Gavi.Audio;

public class SimpleControl : MonoBehaviour
{
    [Header("--- Hotkeys ---")]
    [SerializeField] private KeyCode _keyCodeNextAudio = KeyCode.RightArrow;
    [SerializeField] private KeyCode _keyCodePrevAudio = KeyCode.LeftArrow;
    [SerializeField] private List<KeyCode> _keyCodesPause = new List<KeyCode> {KeyCode.Space, KeyCode.P};
    [SerializeField] private KeyCode _keyCodeVolumeUp = KeyCode.UpArrow;
    [SerializeField] private KeyCode _keyCodeVolumeDown = KeyCode.DownArrow;
    [Tooltip("Saves the picture as a PNG to the Screenshots folder in the persistent data path.")]
    [SerializeField] private KeyCode _keyCodeScreenshot = KeyCode.F2;
    [Tooltip("How fast the volume slider moves with the volume keys, in slider units per second.")]
    [SerializeField] private float _volumeChangePerSecond = 0.3f;

    /// <summary>The volume slider is not linear: slider position p gives volume p^2, so quiet volumes get more room.</summary>
    public const float VolumeCurve = 2f;
    private const string ScreenshotFolder = "Screenshots";
    /// <summary>Where the screenshot key saves its pictures.</summary>
    public static string ScreenshotPath => Path.Combine(Application.persistentDataPath, ScreenshotFolder);
    public static float SliderToVolume(float slider) { return Mathf.Pow(Mathf.Clamp01(slider), VolumeCurve); }
    public static float VolumeToSlider(float volume) { return Mathf.Pow(Mathf.Clamp01(volume), 1f / VolumeCurve); }

    [Header("--- References ---")]
    [SerializeField] private AudioPlayer _audioPlayer;
    [SerializeField] private List<AudioClipContainer> _audioClips;
    [SerializeField] private int _audioStartIndex;

    private int _currentAudioIndex;

    public static SimpleControl Instance { get; private set; }
    public AudioPlayer Player => _audioPlayer;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (_audioClips.Count <= 0)
            return;

        _currentAudioIndex = Mathf.Clamp(_audioStartIndex, 0, _audioClips.Count - 1);
        _audioPlayer.Play(_audioClips[_currentAudioIndex]);
    }

    private void Update()
    {
        if (Gavi.UiPointer.IsTyping)
            return;

        if (Input.GetKeyDown(_keyCodeNextAudio))
            NextAudio();
        else if (Input.GetKeyDown(_keyCodePrevAudio))
            PreviousAudio();

        foreach (KeyCode keyCode in _keyCodesPause)
        {
            if (Input.GetKeyDown(keyCode))
            {
                TogglePause();
                break;
            }
        }

        if (Input.GetKeyDown(_keyCodeScreenshot))
            TakeScreenshot(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

        if (Input.GetKey(_keyCodeVolumeUp))
            _audioPlayer.Volume = SliderToVolume(VolumeToSlider(_audioPlayer.Volume) + Time.deltaTime * _volumeChangePerSecond);
        if (Input.GetKey(_keyCodeVolumeDown))
            _audioPlayer.Volume = SliderToVolume(VolumeToSlider(_audioPlayer.Volume) - Time.deltaTime * _volumeChangePerSecond);
    }

    #region Public controls
    public void NextAudio()
    {
        PlayIndex(_currentAudioIndex + 1);
    }

    public void PreviousAudio()
    {
        PlayIndex(_currentAudioIndex - 1);
    }

    public void TogglePause()
    {
        if (_audioPlayer.IsPlaying)
            _audioPlayer.Pause();
        else
            _audioPlayer.UnPause();
    }

    public void SetVolume(float volume)
    {
        _audioPlayer.Volume = Mathf.Clamp01(volume);
    }

    /// <summary>Saves the picture of the camera to the Screenshots folder in the persistent data path, named after the loaded preset and the time. With withMenu the menu is in the picture and the name ends in _Menu.</summary>
    public void TakeScreenshot(bool withMenu = false)
    {
        string folder = ScreenshotPath;
        Directory.CreateDirectory(folder);

        string name = Gavi.TerrainGroupUIPresets.Instance != null ? Gavi.TerrainGroupUIPresets.Instance.BaselinePresetName : null;
        if (string.IsNullOrEmpty(name))
            name = "Screenshot";
        foreach (char invalid in Path.GetInvalidFileNameChars())
            name = name.Replace(invalid, '_');

        string path = Path.Combine(folder, name + "_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + (withMenu ? "_Menu" : "") + ".png");
        StartCoroutine(SaveScreenshot(path, withMenu));
    }

    /// <summary>
    /// Takes the picture at the end of the frame and writes the PNG. Unity's own screen capture
    /// (ScreenCapture.CaptureScreenshot and CaptureScreenshotAsTexture) fails with the HDRP back buffer in the build,
    /// so the camera view is rendered into a texture instead. The menu is a screen overlay and is only in that picture when withMenu is set.
    /// </summary>
    private IEnumerator SaveScreenshot(string path, bool withMenu)
    {
        yield return new WaitForEndOfFrame();

        Texture2D picture = RenderCameraView(withMenu);
        if (picture == null)
        {
            Debug.LogWarning("Screenshot failed: no camera view to render.");
            yield break;
        }

        File.WriteAllBytes(path, picture.EncodeToPNG());
        Destroy(picture);
        Debug.Log("Screenshot saved to " + path + (HDROutputSettings.main != null && HDROutputSettings.main.active ? " (HDR output is on, the colors are not right)" : ""));
    }

    /// <summary>The picture of the main camera in screen size, without alpha. Null without a main camera.</summary>
    private static Texture2D RenderCameraView(bool withMenu)
    {
        Camera view = Camera.main;
        if (view == null)
            return null;

        // A screen overlay is drawn after the camera, so for this one render the overlay canvases hang on the camera and are put back after it.
        List<Canvas> overlays = new List<Canvas>();
        if (withMenu)
        {
            foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay || !canvas.isActiveAndEnabled)
                    continue;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = view;
                canvas.planeDistance = view.nearClipPlane + 0.5f;
                overlays.Add(canvas);
            }
            Canvas.ForceUpdateCanvases();
        }

        int width = Screen.width;
        int height = Screen.height;
        // sRGB: HDRP writes the linear picture and the target encodes it. A linear target kept the picture linear and far too dark (tested with HDR output off).
        // With HDR output on, HDRP renders for the HDR display and the picture comes out pale, so the PNG is only right with HDR output off.
        RenderTexture target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        RenderTexture oldTarget = view.targetTexture;
        RenderTexture oldActive = RenderTexture.active;

        view.targetTexture = target;
        view.Render();
        view.targetTexture = oldTarget;

        foreach (Canvas canvas in overlays)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = null;
        }

        RenderTexture.active = target;
        Texture2D picture = new Texture2D(width, height, TextureFormat.RGB24, false);
        picture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
        picture.Apply();

        RenderTexture.active = oldActive;
        RenderTexture.ReleaseTemporary(target);
        return picture;
    }

    private void PlayIndex(int index)
    {
        if (_audioClips.Count <= 0)
            return;

        _currentAudioIndex = (index % _audioClips.Count + _audioClips.Count) % _audioClips.Count;

        _audioPlayer.Stop();
        _audioPlayer.Play(_audioClips[_currentAudioIndex]);

        Gavi.Events.Events.OnClipChanged?.Invoke();
    }
    #endregion
}
