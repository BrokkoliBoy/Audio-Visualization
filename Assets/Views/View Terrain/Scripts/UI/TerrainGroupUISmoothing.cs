using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gavi
{
    public class TerrainGroupUISmoothing : TerrainGroupUI
    {
        [SerializeField] private Toggle _toggleApplySmoothing;

        private void Awake()
        {
            _toggleApplySmoothing.onValueChanged.AddListener(OnUIValueChanged);
        }

        public override void ExtractPreset(TerrainSettingsPreset preset)
        {
            base.ExtractPreset(preset);
            preset.ApplySmoothing = _toggleApplySmoothing.isOn;
        }

        public override void ShowPreset(TerrainSettingsPreset preset)
        {
            base.ShowPreset(preset);
            _toggleApplySmoothing.isOn = preset.ApplySmoothing;
        }
    }
}
