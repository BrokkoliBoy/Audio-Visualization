using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gavi
{
    /// <summary>
    /// Shows a color gradient as the track of a slider and tints its handle.
    /// The track has no empty or full part, the gradient replaces it.
    /// </summary>
    public class UiSliderGradient : MonoBehaviour
    {
        [SerializeField] private RawImage _gradientImage;
        [SerializeField] private Image _handle;
        [Tooltip("Optional. Disabled, because a gradient track has no filled part.")]
        [SerializeField] private Image _fill;
        [SerializeField] private int _resolution = 64;

        private Texture2D _texture;
        private Color[] _pixels;

        private void Awake()
        {
            if (_fill != null)
                _fill.enabled = false;
        }

        private void OnDestroy()
        {
            if (_texture != null)
                Destroy(_texture);
        }

        /// <summary>Fills the track with sampler(0..1) and tints the handle.</summary>
        public void Apply(Func<float, Color> sampler, Color handleColor)
        {
            if (_gradientImage != null && sampler != null)
            {
                if (_texture == null)
                {
                    _texture = new Texture2D(_resolution, 1, TextureFormat.RGBA32, false)
                    {
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Bilinear,
                        name = "Slider Gradient"
                    };
                    _pixels = new Color[_resolution];
                    _gradientImage.texture = _texture;
                }

                for (int i = 0; i < _resolution; i++)
                {
                    Color color = sampler(i / (float)(_resolution - 1));
                    color.a = 1f;
                    _pixels[i] = color;
                }
                _texture.SetPixels(_pixels);
                _texture.Apply(false);
            }

            if (_handle != null)
                _handle.color = handleColor;
        }
    }
}
