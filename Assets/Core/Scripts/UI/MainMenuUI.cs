using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Gavi
{
    /// <summary>
    /// Small popup menu next to the player bar: HDR output toggle, hotkey overview and quit.
    /// Opens while the pointer is on the Menu button or the popup, or when pinned with M / Esc or a click on the Menu button.
    /// Only one of this menu and the Terrain settings panel is open at a time.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        public static MainMenuUI Instance { get; private set; }

        [Header("--- Hotkeys ---")]
        [SerializeField] private KeyCode _keyCodeToggleMenu = KeyCode.M;
        [SerializeField] private KeyCode _keyCodeToggleMenuAlternative = KeyCode.Escape;

        [Header("--- Popup ---")]
        [SerializeField] private UiHoverReveal _reveal;

        [Header("--- Buttons ---")]
        [SerializeField] private Button _buttonToggle;
        [SerializeField] private Button _buttonQuit;

        [Header("--- HDR ---")]
        [Tooltip("Optional. Toggles HDR display output. Disabled when the display or Windows HDR setting does not support it.")]
        [SerializeField] private Toggle _toggleHdr;
        [SerializeField] private TextMeshProUGUI _textHdr;
        [Tooltip("If true and HDR output is available, it is switched on when the app starts.")]
        [SerializeField] private bool _enableHdrOnStart = true;

        public bool IsOpen => _reveal != null && _reveal.IsRevealed;


        #region Mono
        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (_reveal != null)
                _reveal.Changed += OnRevealChanged;

            if (_buttonToggle != null)
                _buttonToggle.onClick.AddListener(TogglePinned);
            if (_buttonQuit != null)
                _buttonQuit.onClick.AddListener(Quit);

            SetupHdrToggle();
        }

        private void OnDestroy()
        {
            if (_reveal != null)
                _reveal.Changed -= OnRevealChanged;
        }

        private void Update()
        {
            if (!UiPointer.IsTyping && (Input.GetKeyDown(_keyCodeToggleMenu) || Input.GetKeyDown(_keyCodeToggleMenuAlternative)))
                ToggleMenu();
        }
        #endregion


        #region Menu
        /// <summary>Hotkey: closes the menu when it is open, otherwise opens it and keeps it open.</summary>
        public void ToggleMenu()
        {
            if (_reveal != null)
                _reveal.ToggleByKey();
        }

        /// <summary>Menu button click: keeps the menu open even when the pointer leaves, or lets go of it again.</summary>
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

        public void Quit()
        {
            Application.Quit();
        }

        private void OnRevealChanged(bool open)
        {
            // Only one of the menus should be open at a time.
            if (open && TerrainViewMenuUI.Instance != null && TerrainViewMenuUI.Instance.isActiveAndEnabled && TerrainViewMenuUI.Instance.MenuIsActive)
                TerrainViewMenuUI.Instance.CloseMenu();
            if (open && CameraMenuUI.Instance != null)
                CameraMenuUI.Instance.CloseMenu();
        }
        #endregion


        #region HDR
        private void SetupHdrToggle()
        {
            if (_toggleHdr == null)
                return;

            HDROutputSettings hdr = HDROutputSettings.main;
            bool supported = hdr != null && hdr.available;
            _toggleHdr.interactable = supported;

            if (supported && _enableHdrOnStart && !hdr.active)
                hdr.RequestHDRModeChange(true);

            _toggleHdr.SetIsOnWithoutNotify(supported && (hdr.active || _enableHdrOnStart));
            _toggleHdr.onValueChanged.AddListener(OnHdrToggled);

            if (_textHdr != null)
                _textHdr.text = supported ? "HDR output" : "HDR output (not available)";
        }

        private void OnHdrToggled(bool on)
        {
            HDROutputSettings hdr = HDROutputSettings.main;
            if (hdr != null && hdr.available)
                hdr.RequestHDRModeChange(on);
        }
        #endregion
    }
}
