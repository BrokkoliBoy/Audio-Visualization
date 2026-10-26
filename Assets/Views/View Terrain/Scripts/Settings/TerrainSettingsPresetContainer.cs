using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi
{
    public class TerrainSettingsPresetContainer : MonoBehaviour
    {
        [SerializeField] private bool _export;
        
        [SerializeField] private TerrainSettingsPreset _preset;

        private void OnDrawGizmosSelected()
        {
            if (!_export)
                return;
            _export = false;

            if (_preset == null)
                return;
            TerrainSettingsPresetManager.Instance.ExportPresetsFromJson(_preset);
        }
    }
}
