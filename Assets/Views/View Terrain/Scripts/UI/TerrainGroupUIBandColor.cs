using UnityEngine;

namespace Gavi
{
    /// <summary>
    /// Color tab, part "Color by band": per property (place in the gradient, brightness, glow) how much it comes from the
    /// activity of the vertex's frequency band instead of its height. 0 = from the height as before, 1 = from the band.
    /// </summary>
    public class TerrainGroupUIBandColor : TerrainGroupUI
    {
        [SerializeField] private SliderG _sliderHue;
        [SerializeField] private SliderG _sliderBrightness;
        [SerializeField] private SliderG _sliderGlow;

        private void Awake()
        {
            _sliderHue.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderBrightness.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderGlow.OnValueChangedFloat.AddListener(OnUIValueChanged);
        }

        public override void ExtractProfile(TerrainSettingsProfile profile)
        {
            base.ExtractProfile(profile);
            profile.ColorBandHue = _sliderHue.Value;
            profile.ColorBandBrightness = _sliderBrightness.Value;
            profile.ColorBandGlow = _sliderGlow.Value;
        }

        public override void ShowProfile(TerrainSettingsProfile profile)
        {
            base.ShowProfile(profile);
            _sliderHue.Value = profile.ColorBandHue;
            _sliderBrightness.Value = profile.ColorBandBrightness;
            _sliderGlow.Value = profile.ColorBandGlow;
        }
    }
}
