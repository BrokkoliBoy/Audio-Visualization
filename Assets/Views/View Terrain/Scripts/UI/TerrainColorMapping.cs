using UnityEngine;

namespace Gavi
{
    /// <summary>
    /// Maps the color menu values (hue, saturation, intensity) to HDR colors.
    /// Menu intensity 3 is the default Unity color intensity of 0 (color x 1). Menu 0 is <see cref="MinIntensity"/> (nearly black),
    /// menu 10 is <see cref="MaxIntensity"/>. Unity intensity is an exposure value: color x 2^intensity.
    /// Before palette and preset format version 2 the default sat at menu 1 and menu 0 was intensity -1,
    /// <see cref="ConvertLegacyMenu"/> turns such values into the current scale.
    /// </summary>
    public static class TerrainColorMapping
    {
        public const float MenuIntensityMax = 10f;
        public const float MenuIntensityDefault = 3f;
        /// <summary>Unity intensity (exposure value) that menu intensity 0 stands for.</summary>
        public const float MinIntensity = -5f;
        /// <summary>Unity intensity (exposure value) that menu intensity 10 stands for.</summary>
        public const float MaxIntensity = 6f;

        public static float MenuToIntensity(float menu)
        {
            if (menu <= MenuIntensityDefault)
                return (menu - MenuIntensityDefault) / MenuIntensityDefault * -MinIntensity;
            return (menu - MenuIntensityDefault) / (MenuIntensityMax - MenuIntensityDefault) * MaxIntensity;
        }

        public static float IntensityToMenu(float intensity)
        {
            if (intensity <= 0f)
                return Mathf.Max(0f, MenuIntensityDefault + intensity / -MinIntensity * MenuIntensityDefault);
            return Mathf.Min(MenuIntensityMax, MenuIntensityDefault + intensity / MaxIntensity * (MenuIntensityMax - MenuIntensityDefault));
        }

        /// <summary>Menu value of the old scale (format version 1: default at 1, menu 0 = intensity -1, menu 10 = +6) in the current scale. Same color.</summary>
        public static float ConvertLegacyMenu(float legacyMenu)
        {
            float intensity = legacyMenu <= 1f ? legacyMenu - 1f : (legacyMenu - 1f) / 9f * MaxIntensity;
            return IntensityToMenu(intensity);
        }

        /// <summary>
        /// Exponent of the saturation curve. HDR colors with a high intensity turn white quickly, because the weak
        /// channels get multiplied as well. The curve keeps the colors strong at the top of the slider and loses
        /// them faster toward the bottom: slider 0.5 gives 0.97, 0.25 gives 0.76, 0.1 gives 0.41, 0 stays 0.
        /// </summary>
        public const float SaturationCurve = 5f;

        /// <summary>
        /// Weakly saturated colors are brightened less: a white (saturation 0) with the same exposure looks much brighter
        /// than a saturated color, because all three channels are on. At saturation 0 the color gets this share of the brightness.
        /// </summary>
        private const float WhiteBrightness = 0.6f;

        private static float BrightnessOf(float effectiveSaturation)
        {
            return Mathf.Lerp(WhiteBrightness, 1f, effectiveSaturation);
        }

        /// <summary>Saturation of the color for a slider value.</summary>
        public static float EffectiveSaturation(float slider)
        {
            return 1f - Mathf.Pow(1f - Mathf.Clamp01(slider), SaturationCurve);
        }

        /// <summary>Slider value that gives this saturation, the inverse of <see cref="EffectiveSaturation"/>.</summary>
        public static float SliderSaturation(float effective)
        {
            return 1f - Mathf.Pow(1f - Mathf.Clamp01(effective), 1f / SaturationCurve);
        }

        /// <summary>HDR color: full value hue with the given saturation (slider value), multiplied by 2^intensity.</summary>
        public static Color ToHdrColor(float hueDegrees, float saturation, float intensity)
        {
            float effective = EffectiveSaturation(saturation);
            Color color = Color.HSVToRGB(Mathf.Repeat(hueDegrees / 360f, 1f), effective, 1f);
            float factor = Mathf.Pow(2f, intensity) * BrightnessOf(effective);
            return new Color(color.r * factor, color.g * factor, color.b * factor, 1f);
        }

        /// <summary>Splits a stored HDR color into hue (degrees), saturation and intensity (exposure value).</summary>
        public static void Decompose(Color color, out float hueDegrees, out float saturation, out float intensity)
        {
            float max = Mathf.Max(color.r, color.g, color.b, 0.0001f);
            Color.RGBToHSV(new Color(color.r / max, color.g / max, color.b / max), out float h, out float s, out float v);
            hueDegrees = h * 360f;
            saturation = SliderSaturation(s);
            intensity = Mathf.Log(max / BrightnessOf(s), 2f);
        }

        /// <summary>Low dynamic range preview for the menu: dimmer below menu intensity 1, shifting toward white above it.</summary>
        public static Color Preview(float hueDegrees, float saturation, float menuIntensity)
        {
            Color color = Color.HSVToRGB(Mathf.Repeat(hueDegrees / 360f, 1f), EffectiveSaturation(saturation), 1f);
            if (menuIntensity <= MenuIntensityDefault)
            {
                float dim = 0.35f + 0.65f * Mathf.Clamp01(menuIntensity / MenuIntensityDefault);
                return new Color(color.r * dim, color.g * dim, color.b * dim, 1f);
            }

            float hot = Mathf.Clamp01((menuIntensity - MenuIntensityDefault) / (MenuIntensityMax - MenuIntensityDefault));
            return Color.Lerp(color, Color.white, 0.55f * hot);
        }
    }
}
