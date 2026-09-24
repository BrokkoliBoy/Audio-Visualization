using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi
{
    public class TerrainSettingsProfileContainer : MonoBehaviour
    {
        [SerializeField] private bool _export;
        
        [SerializeField] private TerrainSettingsProfile _profile;

        private void OnDrawGizmosSelected()
        {
            if (!_export)
                return;
            _export = false;

            if (_profile == null)
                return;
            TerrainSettingsProfileManager.Instance.ExportProfilesFromJson(_profile);
        }
    }
}
