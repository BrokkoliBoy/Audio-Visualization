using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Gavi.Utility;

namespace Gavi.Player
{
    public class BasicUi : MonoBehaviour
    {
        public static BasicUi Instance;

        [SerializeField] private PanelType _startPanel;
        
        [Header("--- Cursor ---")]
        [SerializeField] private CursorLockMode _cursorLockMode = CursorLockMode.Confined;
        [Tooltip("Time in seconds after the cursor should stop showing after not moving.")]
        [SerializeField] private float _toggleCursorTime = 3f;

        [Header("--- References ---")]
        [SerializeField] private GameObject _menuPanel;
        [SerializeField] private GameObject _hudPanel;

        /// <summary>Seconds without mouse movement after which the cursor is hidden.</summary>
        public float CursorHideSeconds => _toggleCursorTime;

        public bool IsInMenuPanel => (_menuPanel != null && _menuPanel.activeSelf) || (MainMenuUI.Instance != null && MainMenuUI.Instance.IsOpen) || (CameraMenuUI.Instance != null && CameraMenuUI.Instance.IsOpen);
        public bool IsInGamePanel => !IsInMenuPanel;
        private enum PanelType { Game, Menu }
        
        private float _lastCursorMovement;
        private Vector2 _lastCursorPosition;
            
        
        #region Mono
        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Debugger.LogInstanceError(typeof(BasicUi));

            Cursor.lockState = _cursorLockMode;

            SetPanel(_startPanel);
        }

        private void Update()
        {
            CheckCursorToggle();
        }
        #endregion


        #region Panels
        private void SetPanel(PanelType panel)
        {
            if (_hudPanel != null)
                _hudPanel.SetActive(panel == PanelType.Game);
            if (_menuPanel != null)
                _menuPanel.SetActive(panel == PanelType.Menu);
        }
        #endregion

        
        #region Cursor
        private void CheckCursorToggle()
        {
            Vector2 currentCursorPosition = Input.mousePosition;
            if (currentCursorPosition != _lastCursorPosition)
                _lastCursorMovement = Time.time;
            
            bool isInTerrainAndHasMenuActive = 
                ViewManager.Instance != null &&
                ViewManager.Instance.CurrentView.ViewType == View.ViewTypeEnum.Terrain &&
                TerrainViewMenuUI.Instance != null &&
                TerrainViewMenuUI.Instance.MenuIsActive;

            bool showCursor = 
                (isInTerrainAndHasMenuActive ||
                IsInMenuPanel ||
                currentCursorPosition != _lastCursorPosition ||
                _lastCursorMovement + _toggleCursorTime > Time.time);
            
            // The cursor is hidden while the mouse steers the camera.
            Cursor.visible = showCursor && !UiPointer.IsLocked;
            _lastCursorPosition = currentCursorPosition;
        }
        #endregion
    }
}