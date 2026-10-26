using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gavi
{
    public class TerrainGroupUITerraces : TerrainGroupUI
    {
        [SerializeField] private Toggle _toggleApplyTerraces;
        [SerializeField] private SliderG _sliderTerraceCount;
        [SerializeField] private TMPro.TMP_Dropdown _dropdownExecutionPoint;

        private void Awake()
        {
            _toggleApplyTerraces.onValueChanged.AddListener(OnUIValueChanged);
            _sliderTerraceCount.OnValueChangedInt.AddListener(OnUIValueChanged);
            _dropdownExecutionPoint.onValueChanged.AddListener(OnUIValueChanged);
        }

        public override void ExtractPreset(TerrainSettingsPreset preset)
        {
            base.ExtractPreset(preset);
            preset.ApplyTerrace = _toggleApplyTerraces.isOn;
            preset.TerraceCount = _sliderTerraceCount.ValueInt;
            preset.TerraceExecutionPoint = _dropdownExecutionPoint.value;
        }

        public override void ShowPreset(TerrainSettingsPreset preset)
        {
            base.ShowPreset(preset);
            _toggleApplyTerraces.isOn = preset.ApplyTerrace;
            _sliderTerraceCount.ValueInt = preset.TerraceCount;
            _dropdownExecutionPoint.value = preset.TerraceExecutionPoint;
        }
    }
}
