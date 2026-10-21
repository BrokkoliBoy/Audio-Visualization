using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi
{
    public class TerrainViewMenuUI : MonoBehaviour
    {
        public static TerrainViewMenuUI Instance;
        
        [Header("--- Menu Toggle ---")]
        [Tooltip("Opens the menu when the pointer comes near the handle and closes it when the pointer leaves.")]
        [SerializeField] private UiHoverReveal _hoverReveal;
        [Tooltip("Pins the menu open or closes it.")]
        [SerializeField] private KeyCode _keyCodeToggleMenu = KeyCode.Tab;
        [SerializeField] private float _lerpSpeed = 0.1f;
        
        [Header("- Menu Transforms -")]
        [SerializeField] private RectTransform _menuTransform;
        [SerializeField] private RectTransform _menuActiveMenuTransform;
        [SerializeField] private RectTransform _menuInactiveMenuTransform;
        
        [Header("- Camera Transforms -")]
        [Tooltip("If true, the camera moves aside while the menu is open. Off by default so the terrain stays where it is.")]
        [SerializeField] private bool _moveCameraWithMenu = false;
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private Transform _menuActiveCameraTransform;
        [SerializeField] private Transform _menuInactiveCameraTransform;
        
        [Header("- Button Texts -")]
        [SerializeField] private List<RectTransform> _textsButtonToggleMenu;

        public bool MenuIsActive => _menuIsActive;
        private bool _menuIsActive;
        private Coroutine _coroutineMoveCamera;
        private Coroutine _coroutineMoveMenu;
        private Coroutine _coroutineRotateButtonText;


        #region Mono
        private void Awake()
        {
            if (Instance != null)
                Utility.Debugger.LogInstanceError(GetType());
            Instance = this;
        }

        private void OnEnable()
        {
            if (_hoverReveal != null)
                _hoverReveal.Changed += SetMenuActive;
        }

        private void OnDisable()
        {
            if (_hoverReveal != null)
                _hoverReveal.Changed -= SetMenuActive;
        }

        private void Update()
        {
            if (!UiPointer.IsTyping && Input.GetKeyDown(_keyCodeToggleMenu))
                ToggleMenu();
        }
        #endregion


        #region Menu
        /// <summary>Hotkey: closes the menu when it is open, otherwise opens it and keeps it open.</summary>
        public void ToggleMenu()
        {
            if (_hoverReveal == null)
                SetMenuActive(!_menuIsActive);
            else if (_hoverReveal.isActiveAndEnabled)
                _hoverReveal.ToggleByKey();
        }

        /// <summary>Handle click: keeps the menu open even when the pointer leaves, or lets go of it again.</summary>
        public void TogglePinned()
        {
            if (_hoverReveal == null)
                SetMenuActive(!_menuIsActive);
            else if (_hoverReveal.isActiveAndEnabled)
                _hoverReveal.TogglePinned();
        }

        public void CloseMenu()
        {
            if (_hoverReveal == null)
                SetMenuActive(false);
            else if (_hoverReveal.IsRevealed)
                _hoverReveal.Dismiss();
        }

        private void SetMenuActive(bool active)
        {
            if (_menuIsActive == active)
                return;

            _menuIsActive = active;

            if (_menuIsActive && MainMenuUI.Instance != null && MainMenuUI.Instance.IsOpen)
                MainMenuUI.Instance.CloseMenu();
            if (_menuIsActive && CameraMenuUI.Instance != null && CameraMenuUI.Instance.IsOpen)
                CameraMenuUI.Instance.CloseMenu();

            if (_coroutineMoveCamera != null)
                StopCoroutine(_coroutineMoveCamera);
            if (_moveCameraWithMenu && _cameraTransform != null && _menuActiveCameraTransform != null && _menuInactiveCameraTransform != null)
                _coroutineMoveCamera = StartCoroutine(MoveCamera(_menuIsActive));
            
            if (_coroutineMoveMenu != null)
                StopCoroutine(_coroutineMoveMenu);
            _coroutineMoveMenu = StartCoroutine(MoveMenuUI(_menuIsActive));
            
            if (_coroutineRotateButtonText != null)
                StopCoroutine(_coroutineRotateButtonText);
            _coroutineRotateButtonText = StartCoroutine(RotateToggleButtonText(_menuIsActive));
        }

        private IEnumerator MoveCamera(bool menuIsActive)
        {
            Vector3 targetPosition = menuIsActive ? _menuActiveCameraTransform.position : _menuInactiveCameraTransform.position;
            while ((targetPosition - _cameraTransform.position).magnitude > 0.1f)
            {
                _cameraTransform.position = Vector3.Lerp(_cameraTransform.position, targetPosition, Utility.Utility.Smooth(_lerpSpeed, Utility.Utility.GetFpsEqualizer()));
                yield return null;
            }

            _coroutineMoveCamera = null;
        }
        
        private IEnumerator MoveMenuUI(bool menuIsActive)
        {
            Vector3 targetPosition = menuIsActive ? _menuActiveMenuTransform.position : _menuInactiveMenuTransform.position;

            while ((targetPosition - _menuTransform.position).magnitude > 0.1f)
            {
                _menuTransform.position = Vector3.Lerp(_menuTransform.position, targetPosition, Utility.Utility.Smooth(_lerpSpeed, Utility.Utility.GetFpsEqualizer()));
                yield return null;
            }

            _coroutineMoveMenu = null;
        }

        private IEnumerator RotateToggleButtonText(bool menuIsActive)
        {
            if (_textsButtonToggleMenu == null || _textsButtonToggleMenu.Count == 0)
            {
                _coroutineRotateButtonText = null;
                yield break;
            }
            
            Quaternion targetRotation = Quaternion.Euler(0, 0, menuIsActive ? -180 : -361);

            while (Quaternion.Angle(targetRotation, _textsButtonToggleMenu[0].rotation) > 1)
            {
                for (int i = 0; i < _textsButtonToggleMenu.Count; i++)
                    _textsButtonToggleMenu[i].rotation = Quaternion.LerpUnclamped(_textsButtonToggleMenu[i].rotation, targetRotation, Utility.Utility.Smooth(_lerpSpeed, Utility.Utility.GetFpsEqualizer()));
                yield return null;
            }

            if (!_menuIsActive)
            {
                for (int i = 0; i < _textsButtonToggleMenu.Count; i++)
                    _textsButtonToggleMenu[i].rotation = Quaternion.Euler(0, 0, menuIsActive ? -180 : 0);
            }
            
            _coroutineRotateButtonText = null;
        }
        #endregion
    }
}
