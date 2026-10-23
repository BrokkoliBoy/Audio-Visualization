using UnityEngine;
using UnityEngine.UI;
using Gavi.Modifiables;

namespace Gavi
{
    /// <summary>
    /// Small popup next to the player bar with an overview of the mouse and keyboard camera control.
    /// Opens while the pointer is on the Camera button or the popup, or when pinned with a click on the Camera button.
    /// Only one of the popups and the Terrain settings panel is open at a time.
    /// The reset button and the switches for the camera sway and the beat zoom sit in the Camera group of the
    /// General tab (this class wires them up, so they work while that tab is closed). The switches are remembered between sessions.
    /// </summary>
    public class CameraMenuUI : MonoBehaviour
    {
        public static CameraMenuUI Instance { get; private set; }

        [Header("--- Popup ---")]
        [SerializeField] private UiHoverReveal _reveal;
        [SerializeField] private Button _buttonToggle;

        [Header("--- Controls ---")]
        [SerializeField] private Button _buttonReset;
        [SerializeField] private Toggle _toggleSway;
        [SerializeField] private Toggle _toggleBeatZoom;

        private const string KeySway = "Camera.Sway";
        private const string KeyBeatZoom = "Camera.BeatZoom";

        private CameraControl _control;

        public bool IsOpen => _reveal != null && _reveal.IsRevealed;


        #region Mono
        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            _control = FindFirstObjectByType<CameraControl>();

            if (_reveal != null)
                _reveal.Changed += OnRevealChanged;
            if (_buttonToggle != null)
                _buttonToggle.onClick.AddListener(TogglePinned);
            if (_buttonReset != null)
                _buttonReset.onClick.AddListener(ResetCamera);

            SetupSway();
            SetupBeatZoom();
        }

        private void OnDestroy()
        {
            if (_reveal != null)
                _reveal.Changed -= OnRevealChanged;
            if (Instance == this)
                Instance = null;
        }
        #endregion


        #region Menu
        /// <summary>Click on the Camera button: keeps the menu open even when the pointer leaves, or lets go of it again.</summary>
        public void TogglePinned()
        {
            if (_reveal != null)
                _reveal.TogglePinned();
        }

        public void CloseMenu()
        {
            if (_reveal != null && _reveal.IsRevealed)
                _reveal.Dismiss();
        }

        public void ResetCamera()
        {
            if (_control != null)
                _control.ResetSmoothly();
        }

        private void OnRevealChanged(bool open)
        {
            if (!open)
                return;
            // Only one of the menus should be open at a time.
            if (MainMenuUI.Instance != null)
                MainMenuUI.Instance.CloseMenu();
            if (TerrainViewMenuUI.Instance != null && TerrainViewMenuUI.Instance.isActiveAndEnabled && TerrainViewMenuUI.Instance.MenuIsActive)
                TerrainViewMenuUI.Instance.CloseMenu();
        }
        #endregion


        #region Settings
        private void SetupSway()
        {
            if (_toggleSway == null)
                return;

            bool on = PlayerPrefs.GetInt(KeySway, _control == null || _control.KeepSway ? 1 : 0) == 1;
            _toggleSway.SetIsOnWithoutNotify(on);
            if (_control != null)
                _control.KeepSway = on;
            _toggleSway.onValueChanged.AddListener(v =>
            {
                if (_control != null)
                    _control.KeepSway = v;
                PlayerPrefs.SetInt(KeySway, v ? 1 : 0);
            });
        }

        private void SetupBeatZoom()
        {
            if (_toggleBeatZoom == null)
                return;

            bool on = PlayerPrefs.GetInt(KeyBeatZoom, 1) == 1;
            _toggleBeatZoom.SetIsOnWithoutNotify(on);
            CameraModifiable.BeatZoomEnabled = on;
            _toggleBeatZoom.onValueChanged.AddListener(v =>
            {
                CameraModifiable.BeatZoomEnabled = v;
                PlayerPrefs.SetInt(KeyBeatZoom, v ? 1 : 0);
            });
        }
        #endregion
    }
}
