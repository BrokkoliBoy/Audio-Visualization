using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gavi
{
    public class TerrainGroupUIDampening : TerrainGroupUI
    {
        [SerializeField] private Toggle _toggleApplyDampening;
        [SerializeField] private SliderG _sliderDampeningUp;
        [SerializeField] private SliderG _sliderDampeningDown;

        private void Awake()
        {
            _toggleApplyDampening.onValueChanged.AddListener(OnUIValueChanged);
            _sliderDampeningUp.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderDampeningDown.OnValueChangedFloat.AddListener(OnUIValueChanged);
        }

        public override void ExtractProfile(TerrainSettingsProfile profile)
        {
            base.ExtractProfile(profile);
            profile.ApplyDampening = _toggleApplyDampening.isOn;
            profile.DampenUp = _sliderDampeningUp.Value;
            profile.DampenDown = _sliderDampeningDown.Value;
        }

        public override void ShowProfile(TerrainSettingsProfile profile)
        {
            base.ShowProfile(profile);
            _toggleApplyDampening.isOn = profile.ApplyDampening;
            _sliderDampeningUp.Value = profile.DampenUp;
            _sliderDampeningDown.Value = profile.DampenDown;
        }
    }
}
