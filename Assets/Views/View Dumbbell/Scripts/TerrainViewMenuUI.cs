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
        [SerializeField] private KeyCode _keyCodeToggleMenu = KeyCode.Tab;
        [SerializeField] private float _lerpSpeed = 0.1f;
        
        [Header("- Menu Transforms -")]
        [SerializeField] private RectTransform _menuTransform;
        [SerializeField] private RectTransform _menuActiveMenuTransform;
        [SerializeField] private RectTransform _menuInactiveMenuTransform;
        
        [Header("- Camera Transforms -")]
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

        private void Update()
        {
            if (Input.GetKeyDown(_keyCodeToggleMenu))
                ToggleMenu();
        }
        #endregion
        
        
        #region Menu
        public void ToggleMenu()
        {
            if (_cameraTransform == null || _menuActiveCameraTransform == null)
                return;
            
            _menuIsActive = !_menuIsActive;

            if (_menuIsActive && MainMenuUI.Instance != null && MainMenuUI.Instance.IsOpen)
                MainMenuUI.Instance.ToggleMenu();

            if (_coroutineMoveCamera != null)
                StopCoroutine(_coroutineMoveCamera);
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
                _cameraTransform.position = Vector3.Lerp(_cameraTransform.position, targetPosition, _lerpSpeed * Utility.Utility.GetFpsEqualizer());
                yield return null;
            }

            _coroutineMoveCamera = null;
        }
        
        private IEnumerator MoveMenuUI(bool menuIsActive)
        {
            Vector3 targetPosition = menuIsActive ? _menuActiveMenuTransform.position : _menuInactiveMenuTransform.position;

            while ((targetPosition - _menuTransform.position).magnitude > 0.1f)
            {
                _menuTransform.position = Vector3.Lerp(_menuTransform.position, targetPosition, _lerpSpeed * Utility.Utility.GetFpsEqualizer());
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
                    _textsButtonToggleMenu[i].rotation = Quaternion.LerpUnclamped(_textsButtonToggleMenu[i].rotation, targetRotation, _lerpSpeed * Utility.Utility.GetFpsEqualizer());
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
