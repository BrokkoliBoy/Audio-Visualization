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

        [Tooltip("Optional. Tracks of the two sliders: dim and slow on the left, bright and snappy on the right.")]
        [SerializeField] private UiSliderGradient _gradientUp;
        [SerializeField] private UiSliderGradient _gradientDown;

        private void Awake()
        {
            Color slow = new Color(0.16f, 0.2f, 0.3f);
            Color fast = new Color(0.8f, 0.9f, 1f);
            if (_gradientUp != null)
                _gradientUp.Bind(_sliderDampeningUp, t => Color.Lerp(slow, fast, t));
            if (_gradientDown != null)
                _gradientDown.Bind(_sliderDampeningDown, t => Color.Lerp(slow, fast, t));

            _toggleApplyDampening.onValueChanged.AddListener(OnUIValueChanged);
            _sliderDampeningUp.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderDampeningDown.OnValueChangedFloat.AddListener(OnUIValueChanged);
        }

        public override void ExtractPreset(TerrainSettingsPreset preset)
        {
            base.ExtractPreset(preset);
            preset.ApplyDampening = _toggleApplyDampening.isOn;
            preset.DampenUp = _sliderDampeningUp.Value;
            preset.DampenDown = _sliderDampeningDown.Value;
        }

        public override void ShowPreset(TerrainSettingsPreset preset)
        {
            base.ShowPreset(preset);
            _toggleApplyDampening.isOn = preset.ApplyDampening;
            _sliderDampeningUp.Value = preset.DampenUp;
            _sliderDampeningDown.Value = preset.DampenDown;
        }
    }
}
