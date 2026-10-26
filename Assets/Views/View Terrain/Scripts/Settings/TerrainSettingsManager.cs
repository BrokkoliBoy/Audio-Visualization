using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi
{
    public class TerrainSettingsManager : MonoBehaviour
    {
        public static TerrainSettingsManager Instance;
        
        
        [SerializeField] private TerrainGenerator _terrain;
        public TerrainGenerator Terrain => _terrain;
        
        [Header("- Groups -")]
        [SerializeField] private List<TerrainGroupUI> _groups;
        public List<TerrainGroupUI> Groups => _groups;

        public TerrainSettingsPreset CurrentPreset => _currentPreset;
        private TerrainSettingsPreset _currentPreset = new TerrainSettingsPreset();


        #region Mono
        private void Awake()
        {
            if (Instance != null)
                Utility.Debugger.LogInstanceError(GetType());
            Instance = this;
        }

        private void Start()
        {
            _currentPreset.CopyFromTerrainSettings();
            _currentPreset.ApplyPresetToUI();
        }

        private void LateUpdate()
        {
            if (!_uiChanged)
                return;
            _uiChanged = false;
            _currentPreset.ExtractPresetFromUI();
            _currentPreset.ApplyPresetToTerrain();
        }
        #endregion


        #region UI changes
        private bool _uiChanged;

        /// <summary>A menu value changed. The terrain follows at the end of the frame, once for all changes of this frame.</summary>
        public void MarkUIChanged() => _uiChanged = true;
        #endregion


        #region Presets
        #endregion
    }
}
