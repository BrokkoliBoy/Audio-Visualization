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

        public TerrainSettingsProfile CurrentProfile => _currentProfile;
        private TerrainSettingsProfile _currentProfile = new TerrainSettingsProfile();


        #region Mono
        private void Awake()
        {
            if (Instance != null)
                Utility.Debugger.LogInstanceError(GetType());
            Instance = this;
        }

        private void Start()
        {
            _currentProfile.CopyFromTerrainSettings();
            _currentProfile.ApplyProfileToUI();
            foreach (TerrainGroupUI group in _groups)
                if (group is TerrainColorGroupUI colorGroup)
                    _colorGroup = colorGroup;
        }

        private TerrainColorGroupUI _colorGroup;

        private void Update()
        {
            // The color group is switched off while another menu tab is open, so its own Update does not run then.
            if (_colorGroup != null)
                _colorGroup.PollColorCurve();
        }
        #endregion


        #region Profiles
        #endregion
    }
}
