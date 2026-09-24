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
        }
        #endregion


        #region Profiles
        #endregion
    }
}
