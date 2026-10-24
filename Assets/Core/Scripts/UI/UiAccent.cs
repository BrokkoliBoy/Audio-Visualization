using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gavi
{
    /// <summary>
    /// Carries the accent color of a window (the stripe at its edge) into its controls: sliders, the step buttons next to dropdowns
    /// and the fold arrow get a tone of that color, and the bar under a menu tab becomes a gradient of the accents of its windows.
    /// Controls that carry color information themselves (gradient sliders) are left alone.
    /// </summary>
    public static class UiAccent
    {
        private const string AccentName = "Image - Accent";

        /// <summary>The accent color of a window (without transparency), false if the window has no accent stripe.</summary>
        public static bool TryGetAccent(GameObject group, out Color accent)
        {
            accent = Color.white;
            if (group == null)
                return false;
            Transform stripe = group.transform.Find(AccentName);
            Image image = stripe != null ? stripe.GetComponent<Image>() : null;
            if (image == null)
                return false;
            accent = image.color;
            accent.a = 1f;
            return true;
        }

        /// <summary>Gives the accent stripe of a window another hue, saturation and brightness stay.</summary>
        public static void SetHue(GameObject group, float hue)
        {
            Transform stripe = group != null ? group.transform.Find(AccentName) : null;
            Image image = stripe != null ? stripe.GetComponent<Image>() : null;
            if (image == null)
                return;
            Color.RGBToHSV(image.color, out _, out float s, out float v);
            Color color = Color.HSVToRGB(Mathf.Repeat(hue, 1f), s, v);
            color.a = image.color.a;
            image.color = color;
        }

        /// <summary>The same hue with a different brightness, a bit less saturated so dark tones do not get garish.</summary>
        public static Color Tone(Color accent, float saturation, float value)
        {
            Color.RGBToHSV(accent, out float h, out float s, out _);
            return Color.HSVToRGB(h, s * saturation, value);
        }

        // Filled part of a slider: pale or dark accents (pink, blue) are brought up to the level of the vivid ones.
        private static Color Fill(Color accent)
        {
            Color.RGBToHSV(accent, out float h, out float s, out _);
            float value = Mathf.Clamp(0.68f + (0.55f - accent.grayscale) * 0.5f, 0.62f, 0.85f);
            return Color.HSVToRGB(h, Mathf.Clamp(Mathf.Max(s, 0.72f) * 0.95f, 0f, 0.9f), value);
        }

        /// <summary>Tints the controls of one window with its accent color. Safe to call on an inactive window.</summary>
        public static void TintGroup(GameObject group)
        {
            if (!TryGetAccent(group, out Color accent))
                return;

            foreach (Slider slider in group.GetComponentsInChildren<Slider>(true))
            {
                // gradient sliders show their own colors
                if (slider.GetComponentInParent<UiSliderGradient>(true) != null)
                    continue;
                TintImage(slider.fillRect, Fill(accent));
                TintImage(slider.handleRect, Tone(accent, 0.2f, 0.94f));
                Transform background = slider.transform.Find("Background");
                if (background != null)
                    TintImage(background as RectTransform, Tone(accent, 0.6f, 0.8f));
            }

            // subheadings take a pale tone of the accent
            foreach (TMP_Text text in group.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.name != "Subheading")
                    continue;
                Color.RGBToHSV(accent, out float h, out float s, out _);
                Color pale = Color.HSVToRGB(h, s * 0.55f, 1f);
                pale.a = 0.9f;
                text.color = pale;
            }

            foreach (Button button in group.GetComponentsInChildren<Button>(true))
            {
                if (!button.name.StartsWith("Button - Previous") && !button.name.StartsWith("Button - Next"))
                    continue;
                ColorBlock colors = button.colors;
                colors.normalColor = Tone(accent, 0.7f, 0.36f);
                colors.highlightedColor = Tone(accent, 0.65f, 0.5f);
                colors.selectedColor = colors.highlightedColor;
                colors.pressedColor = Tone(accent, 0.7f, 0.25f);
                button.colors = colors;
            }

            UiGroupFold fold = group.GetComponent<UiGroupFold>();
            Transform arrow = fold != null && fold.Arrow != null ? fold.Arrow : group.transform.Find("Toggle - Header/Button - Fold/Image - Arrow");
            Image arrowImage = arrow != null ? arrow.GetComponent<Image>() : null;
            if (arrowImage != null)
            {
                Color tint = Tone(accent, 0.45f, 0.95f);
                tint.a = arrowImage.color.a;
                arrowImage.color = tint;
            }
        }

        /// <summary>A one pixel high texture sprite that runs through the colors, for the bar of a tab.</summary>
        public static Sprite GradientSprite(IList<Color> colors)
        {
            const int width = 32;
            Texture2D texture = new Texture2D(width, 1, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            for (int x = 0; x < width; x++)
            {
                float position = colors.Count > 1 ? x / (float)(width - 1) * (colors.Count - 1) : 0f;
                int from = Mathf.Min((int)position, colors.Count - 1);
                int to = Mathf.Min(from + 1, colors.Count - 1);
                texture.SetPixel(x, 0, Color.Lerp(colors[from], colors[to], position - from));
            }
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, width, 1f), new Vector2(0.5f, 0.5f), 100f);
        }

        // keeps the transparency the control already has
        private static void TintImage(RectTransform rect, Color tone)
        {
            Image image = rect != null ? rect.GetComponent<Image>() : null;
            if (image == null)
                return;
            tone.a = image.color.a;
            image.color = tone;
        }
    }
}
