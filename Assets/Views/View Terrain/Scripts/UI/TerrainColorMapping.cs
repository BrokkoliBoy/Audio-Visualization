using UnityEngine;

namespace Gavi
{
    /// <summary>
    /// Maps the color menu values (hue, saturation, intensity) to HDR colors.
    /// Menu intensity 1 is the default Unity color intensity of 0 (color x 1). Menu 0 is intensity -1,
    /// menu 10 is the configurable maximum intensity. Unity intensity is an exposure value: color x 2^intensity.
    /// </summary>
    public static class TerrainColorMapping
    {
        public const float MenuIntensityMax = 10f;
        public const float MenuIntensityDefault = 1f;

        public static float MenuToIntensity(float menu, float maxIntensity)
        {
            if (menu <= MenuIntensityDefault)
                return menu - MenuIntensityDefault;
            return (menu - MenuIntensityDefault) / (MenuIntensityMax - MenuIntensityDefault) * maxIntensity;
        }

        public static float IntensityToMenu(float intensity, float maxIntensity)
        {
            if (intensity <= 0f)
                return Mathf.Max(0f, MenuIntensityDefault + intensity);
            return Mathf.Min(MenuIntensityMax, MenuIntensityDefault + intensity / Mathf.Max(0.0001f, maxIntensity) * (MenuIntensityMax - MenuIntensityDefault));
        }

        /// <summary>HDR color: full value hue with the given saturation, multiplied by 2^intensity.</summary>
        public static Color ToHdrColor(float hueDegrees, float saturation, float intensity)
        {
            Color color = Color.HSVToRGB(Mathf.Repeat(hueDegrees / 360f, 1f), Mathf.Clamp01(saturation), 1f);
            float factor = Mathf.Pow(2f, intensity);
            return new Color(color.r * factor, color.g * factor, color.b * factor, 1f);
        }

        /// <summary>Splits a stored HDR color into hue (degrees), saturation and intensity (exposure value).</summary>
        public static void Decompose(Color color, out float hueDegrees, out float saturation, out float intensity)
        {
            float max = Mathf.Max(color.r, color.g, color.b, 0.0001f);
            Color.RGBToHSV(new Color(color.r / max, color.g / max, color.b / max), out float h, out float s, out float v);
            hueDegrees = h * 360f;
            saturation = s;
            intensity = Mathf.Log(max, 2f);
        }

        /// <summary>Low dynamic range preview for the menu: dimmer below menu intensity 1, shifting toward white above it.</summary>
        public static Color Preview(float hueDegrees, float saturation, float menuIntensity)
        {
            Color color = Color.HSVToRGB(Mathf.Repeat(hueDegrees / 360f, 1f), Mathf.Clamp01(saturation), 1f);
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
