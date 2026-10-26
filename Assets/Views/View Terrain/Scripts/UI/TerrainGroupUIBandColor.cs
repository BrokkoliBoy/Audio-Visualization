using UnityEngine;

namespace Gavi
{
    /// <summary>
    /// Window "Color mapping": per property (place in the gradient, brightness, glow) how much it comes from the activity of the
    /// vertex's frequency band (first three sliders) or from the strength of the noise pattern at the vertex (second three)
    /// instead of its height. 0 = from the height as before, 1 = from the band or the pattern.
    /// </summary>
    public class TerrainGroupUIBandColor : TerrainGroupUI
    {
        [SerializeField] private SliderG _sliderHue;
        [SerializeField] private SliderG _sliderBrightness;
        [SerializeField] private SliderG _sliderGlow;

        [Header("--- Color by pattern ---")]
        [SerializeField] private SliderG _sliderPatternHue;
        [SerializeField] private SliderG _sliderPatternBrightness;
        [SerializeField] private SliderG _sliderPatternGlow;

        private void Awake()
        {
            _sliderHue.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderBrightness.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderGlow.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderPatternHue.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderPatternBrightness.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderPatternGlow.OnValueChangedFloat.AddListener(OnUIValueChanged);
        }

        public override void ExtractPreset(TerrainSettingsPreset preset)
        {
            base.ExtractPreset(preset);
            preset.ColorBandHue = _sliderHue.Value;
            preset.ColorBandBrightness = _sliderBrightness.Value;
            preset.ColorBandGlow = _sliderGlow.Value;
            preset.ColorPatternHue = _sliderPatternHue.Value;
            preset.ColorPatternBrightness = _sliderPatternBrightness.Value;
            preset.ColorPatternGlow = _sliderPatternGlow.Value;
        }

        public override void ShowPreset(TerrainSettingsPreset preset)
        {
            base.ShowPreset(preset);
            _sliderHue.Value = preset.ColorBandHue;
            _sliderBrightness.Value = preset.ColorBandBrightness;
            _sliderGlow.Value = preset.ColorBandGlow;
            _sliderPatternHue.Value = preset.ColorPatternHue;
            _sliderPatternBrightness.Value = preset.ColorPatternBrightness;
            _sliderPatternGlow.Value = preset.ColorPatternGlow;
        }
    }
}
