using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private float _volumeChangePerSecond = 0.3f;

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

        if (Input.GetKey(_keyCodeVolumeUp))
            _audioPlayer.Volume = Mathf.Clamp01(_audioPlayer.Volume + Time.deltaTime * _volumeChangePerSecond);
        if (Input.GetKey(_keyCodeVolumeDown))
            _audioPlayer.Volume = Mathf.Clamp01(_audioPlayer.Volume - Time.deltaTime * _volumeChangePerSecond);
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
