using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Gavi.Audio;

namespace Gavi
{
    /// <summary>
    /// Slide-in control panel (bottom right) that exposes everything the hotkeys can do as buttons.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        public static MainMenuUI Instance { get; private set; }

        [Header("--- Hotkeys ---")]
        [SerializeField] private KeyCode _keyCodeToggleMenu = KeyCode.M;
        [SerializeField] private KeyCode _keyCodeToggleMenuAlternative = KeyCode.Escape;

        [Header("--- Slide ---")]
        [SerializeField] private RectTransform _panel;
        [SerializeField] private float _hiddenOffset = 290f;
        [SerializeField] private float _slideSpeed = 12f;
        [SerializeField] private bool _startOpen = false;
        [SerializeField] private TextMeshProUGUI _toggleArrowText;

        [Header("--- Buttons ---")]
        [SerializeField] private Button _buttonToggle;
        [SerializeField] private Button _buttonPreviousSong;
        [SerializeField] private Button _buttonNextSong;
        [SerializeField] private Button _buttonPreviousView;
        [SerializeField] private Button _buttonNextView;
        [SerializeField] private Button _buttonRestart;
        [SerializeField] private Button _buttonPause;
        [SerializeField] private Button _buttonQuit;

        [Header("--- Displays ---")]
        [SerializeField] private Slider _sliderVolume;
        [SerializeField] private TextMeshProUGUI _textSong;
        [SerializeField] private TextMeshProUGUI _textView;
        [SerializeField] private TextMeshProUGUI _textPause;

        public bool IsOpen => _isOpen;
        private bool _isOpen;
        private Vector2 _shownPosition;
        private Coroutine _coroutineSlide;


        #region Mono
        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            _shownPosition = _panel.anchoredPosition;
            _isOpen = _startOpen;
            _panel.anchoredPosition = GetTargetPosition(_isOpen);
            UpdateArrow();

            AddListener(_buttonToggle, ToggleMenu);
            AddListener(_buttonPreviousSong, () => SimpleControl.Instance?.PreviousAudio());
            AddListener(_buttonNextSong, () => SimpleControl.Instance?.NextAudio());
            AddListener(_buttonPreviousView, () => ViewManager.Instance?.PreviousView());
            AddListener(_buttonNextView, () => ViewManager.Instance?.NextView());
            AddListener(_buttonRestart, () => ViewManager.Instance?.RestartView());
            AddListener(_buttonPause, () => SimpleControl.Instance?.TogglePause());
            AddListener(_buttonQuit, Quit);

            if (_sliderVolume != null)
            {
                _sliderVolume.minValue = 0f;
                _sliderVolume.maxValue = 1f;
                _sliderVolume.onValueChanged.AddListener(OnVolumeSliderChanged);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(_keyCodeToggleMenu) || Input.GetKeyDown(_keyCodeToggleMenuAlternative))
                ToggleMenu();

            RefreshDisplays();
        }
        #endregion


        #region Menu
        public void ToggleMenu()
        {
            _isOpen = !_isOpen;
            UpdateArrow();

            // Only one of the two menus should be open at a time.
            if (_isOpen && TerrainViewMenuUI.Instance != null && TerrainViewMenuUI.Instance.isActiveAndEnabled && TerrainViewMenuUI.Instance.MenuIsActive)
                TerrainViewMenuUI.Instance.ToggleMenu();

            if (_coroutineSlide != null)
                StopCoroutine(_coroutineSlide);
            _coroutineSlide = StartCoroutine(Slide());
        }

        public void Quit()
        {
            Application.Quit();
        }

        private IEnumerator Slide()
        {
            Vector2 target = GetTargetPosition(_isOpen);
            while ((_panel.anchoredPosition - target).sqrMagnitude > 0.25f)
            {
                _panel.anchoredPosition = Vector2.Lerp(_panel.anchoredPosition, target, 1f - Mathf.Exp(-_slideSpeed * Time.unscaledDeltaTime));
                yield return null;
            }

            _panel.anchoredPosition = target;
            _coroutineSlide = null;
        }

        private Vector2 GetTargetPosition(bool open)
        {
            return open ? _shownPosition : _shownPosition + new Vector2(_hiddenOffset, 0f);
        }

        private void UpdateArrow()
        {
            if (_toggleArrowText != null)
                _toggleArrowText.text = _isOpen ? ">" : "<";
        }
        #endregion


        #region Displays
        private void RefreshDisplays()
        {
            AudioPlayer player = AudioPlayer.Instance;
            if (player != null)
            {
                if (_textSong != null)
                    _textSong.text = GetSongName(player.CurrentlyPlayingClip);
                if (_textPause != null)
                    _textPause.text = player.IsPlaying ? "Pause  [Space / P]" : "Play  [Space / P]";
                if (_sliderVolume != null && !Mathf.Approximately(_sliderVolume.value, player.Volume))
                    _sliderVolume.SetValueWithoutNotify(player.Volume);
            }

            if (_textView != null && ViewManager.Instance != null)
                _textView.text = GetViewName();
        }

        private void OnVolumeSliderChanged(float value)
        {
            SimpleControl.Instance?.SetVolume(value);
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
        #endregion
    }
}
