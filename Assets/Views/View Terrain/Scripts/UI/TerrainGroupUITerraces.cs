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

        public override void ExtractProfile(TerrainSettingsProfile profile)
        {
            base.ExtractProfile(profile);
            profile.ApplyTerrace = _toggleApplyTerraces.isOn;
            profile.TerraceCount = _sliderTerraceCount.ValueInt;
            profile.TerraceExecutionPoint = _dropdownExecutionPoint.value;
        }

        public override void ShowProfile(TerrainSettingsProfile profile)
        {
            base.ShowProfile(profile);
            _toggleApplyTerraces.isOn = profile.ApplyTerrace;
            _sliderTerraceCount.ValueInt = profile.TerraceCount;
            _dropdownExecutionPoint.value = profile.TerraceExecutionPoint;
        }
    }
}
