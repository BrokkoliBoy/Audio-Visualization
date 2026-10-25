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

        public override void ExtractProfile(TerrainSettingsProfile profile)
        {
            base.ExtractProfile(profile);
            profile.ColorBandHue = _sliderHue.Value;
            profile.ColorBandBrightness = _sliderBrightness.Value;
            profile.ColorBandGlow = _sliderGlow.Value;
            profile.ColorPatternHue = _sliderPatternHue.Value;
            profile.ColorPatternBrightness = _sliderPatternBrightness.Value;
            profile.ColorPatternGlow = _sliderPatternGlow.Value;
        }

        public override void ShowProfile(TerrainSettingsProfile profile)
        {
            base.ShowProfile(profile);
            _sliderHue.Value = profile.ColorBandHue;
            _sliderBrightness.Value = profile.ColorBandBrightness;
            _sliderGlow.Value = profile.ColorBandGlow;
            _sliderPatternHue.Value = profile.ColorPatternHue;
            _sliderPatternBrightness.Value = profile.ColorPatternBrightness;
            _sliderPatternGlow.Value = profile.ColorPatternGlow;
        }
    }
}
