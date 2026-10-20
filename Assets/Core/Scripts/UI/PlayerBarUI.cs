using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Gavi.Audio;

namespace Gavi
{
    /// <summary>
    /// Bottom player bar: song and view controls, pause, restart, volume and a progress display.
    /// All buttons call the same public methods the hotkeys use.
    /// </summary>
    public class PlayerBarUI : MonoBehaviour
    {
        [Header("--- Buttons ---")]
        [SerializeField] private Button _buttonPreviousSong;
        [SerializeField] private Button _buttonNextSong;
        [SerializeField] private Button _buttonPause;
        [SerializeField] private Button _buttonPreviousView;
        [SerializeField] private Button _buttonNextView;
        [SerializeField] private Button _buttonRestart;

        [Header("--- Displays ---")]
        [SerializeField] private TextMeshProUGUI _textSong;
        [SerializeField] private TextMeshProUGUI _textPause;
        [Tooltip("Optional. Shows the play or pause icon instead of the text.")]
        [SerializeField] private Image _iconPause;
        [SerializeField] private Sprite _spritePlay;
        [SerializeField] private Sprite _spritePauseIcon;
        [SerializeField] private TextMeshProUGUI _textView;
        [SerializeField] private TextMeshProUGUI _textTime;
        [SerializeField] private Slider _sliderProgress;
        [SerializeField] private Slider _sliderVolume;
        [Tooltip("Thin progress line that stays visible at the bottom edge while the bar is hidden. Its right anchor follows the playback position.")]
        [SerializeField] private RectTransform _cueProgressFill;

        [Header("--- Microphone ---")]
        [Tooltip("Shown instead of the time while the microphone is the audio source.")]
        [SerializeField] private GameObject _micRoot;
        [Tooltip("Microphone sensitivity. It changes the overall amplitude modifier of the microphone clip.")]
        [SerializeField] private Slider _sliderMic;
        [SerializeField] private TextMeshProUGUI _textMic;
        [SerializeField] private float _micSensitivityMax = 100f;

        private AudioClipContainer _micContainer;

        private void Start()
        {
            AddListener(_buttonPreviousSong, () => SimpleControl.Instance?.PreviousAudio());
            AddListener(_buttonNextSong, () => SimpleControl.Instance?.NextAudio());
            AddListener(_buttonPause, () => SimpleControl.Instance?.TogglePause());
            AddListener(_buttonPreviousView, () => ViewManager.Instance?.PreviousView());
            AddListener(_buttonNextView, () => ViewManager.Instance?.NextView());
            AddListener(_buttonRestart, () => ViewManager.Instance?.RestartView());

            if (_sliderVolume != null)
            {
                _sliderVolume.minValue = 0f;
                _sliderVolume.maxValue = 1f;
                _sliderVolume.onValueChanged.AddListener(v => SimpleControl.Instance?.SetVolume(SimpleControl.SliderToVolume(v)));
            }
            if (_sliderProgress != null)
                _sliderProgress.interactable = false;

            if (_sliderMic != null)
            {
                _sliderMic.minValue = 0f;
                _sliderMic.maxValue = _micSensitivityMax;
                _sliderMic.onValueChanged.AddListener(OnMicSensitivityChanged);
            }
            if (_micRoot != null)
                _micRoot.SetActive(false);
        }

        private void OnMicSensitivityChanged(float value)
        {
            if (_micContainer != null && _micContainer.Settings != null)
                _micContainer.Settings.AmplitudeModifierOverall = value;
            UpdateMicText(value);
        }

        private void UpdateMicText(float value)
        {
            if (_textMic != null)
                _textMic.text = "Mic " + Mathf.RoundToInt(value);
        }

        /// <summary>The time makes way for the microphone sensitivity slider while the microphone is playing.</summary>
        private void UpdateMicControl(AudioClipContainer container)
        {
            bool isMic = container != null && container.UseMic && container.Settings != null;

            if (_micRoot != null)
                _micRoot.SetActive(isMic);
            if (_textTime != null)
                _textTime.gameObject.SetActive(!isMic);

            if (!isMic)
            {
                _micContainer = null;
                return;
            }

            if (container != _micContainer)
            {
                _micContainer = container;
                float value = container.Settings.AmplitudeModifierOverall;
                if (_sliderMic != null)
                    _sliderMic.SetValueWithoutNotify(value);
                UpdateMicText(value);
            }
        }

        private void Update()
        {
            AudioPlayer player = AudioPlayer.Instance;
            if (player != null)
            {
                if (_textSong != null)
                    _textSong.text = GetSongName(player.CurrentlyPlayingClip);
                if (_textPause != null)
                    _textPause.text = player.IsPlaying ? "Pause" : "Play";
                if (_iconPause != null && _spritePlay != null && _spritePauseIcon != null)
                    _iconPause.sprite = player.IsPlaying ? _spritePauseIcon : _spritePlay;
                if (_sliderVolume != null && !Mathf.Approximately(_sliderVolume.value, SimpleControl.VolumeToSlider(player.Volume)))
                    _sliderVolume.SetValueWithoutNotify(SimpleControl.VolumeToSlider(player.Volume));
                UpdateProgress(player);
            }

            if (_textView != null && ViewManager.Instance != null)
                _textView.text = GetViewName();
        }

        private void UpdateProgress(AudioPlayer player)
        {
            AudioClipContainer container = player.CurrentlyPlayingClip;
            bool hasClip = container != null && !container.UseMic && container.Clip != null;
            float length = hasClip ? player.CurrentClipLength : -1f;
            bool hasLength = length > 0;

            if (_sliderProgress != null)
            {
                _sliderProgress.gameObject.SetActive(hasLength);
                if (hasLength)
                    _sliderProgress.SetValueWithoutNotify(Mathf.Clamp01(player.CurrentPlaybackTime / length));
            }
            if (_cueProgressFill != null)
            {
                Vector2 anchorMax = _cueProgressFill.anchorMax;
                anchorMax.x = hasLength ? Mathf.Clamp01(player.CurrentPlaybackTime / length) : 0f;
                _cueProgressFill.anchorMax = anchorMax;
            }
            if (_textTime != null)
                _textTime.text = hasLength ? FormatTime(player.CurrentPlaybackTime) + " / " + FormatTime(length) : "live";
            UpdateMicControl(container);
        }

        private static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return (total / 60) + ":" + (total % 60).ToString("00");
        }

        private static string GetSongName(AudioClipContainer container)
        {
            if (container == null)
                return "-";
            if (container.UseMic)
                return "Microphone";
            return container.Clip != null ? container.Clip.name.Replace(" [NCS Release]", "") : container.name;
        }

        private static string GetViewName()
        {
            try
            {
                string viewName = ViewManager.Instance.CurrentView.name;
                return viewName.StartsWith("View ") ? viewName.Substring(5) : viewName;
            }
            catch (System.Exception)
            {
                return "-";
            }
        }

        private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
                button.onClick.AddListener(action);
        }
    }
}
