using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gavi
{
    public class TerrainGroupUIOutOfBounds : TerrainGroupUI
    {
        [SerializeField] private TMPro.TMP_Dropdown _dropdownOperation;
        [SerializeField] private SliderG _sliderMultiplier;
        [SerializeField] private Toggle _toggleNeverTopHighestBand;

        private void Awake()
        {
            _dropdownOperation.onValueChanged.AddListener(OnUIValueChanged);
            _sliderMultiplier.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _toggleNeverTopHighestBand.onValueChanged.AddListener(OnUIValueChanged);
        }

        public override void ExtractProfile(TerrainSettingsProfile profile)
        {
            base.ExtractProfile(profile);
            profile.OutOfBoundsOperation = _dropdownOperation.value;
            profile.OutOfBoundsMultiplier = _sliderMultiplier.Value;
            profile.NeverTopHighestBand = _toggleNeverTopHighestBand.isOn;
        }

        public override void ShowProfile(TerrainSettingsProfile profile)
        {
            base.ShowProfile(profile);
            _dropdownOperation.value = profile.OutOfBoundsOperation;
            _sliderMultiplier.Value = profile.OutOfBoundsMultiplier;
            _toggleNeverTopHighestBand.isOn = profile.NeverTopHighestBand;
        }
    }
}
