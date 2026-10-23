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

        private SliderG _boundSlider;
        private Func<float, Color> _boundSampler;

        /// <summary>
        /// Keeps the track on sampler(position 0..1) and the handle on the color at the slider's position.
        /// The sampler may read other values, call <see cref="Refresh"/> when they change.
        /// </summary>
        public void Bind(SliderG slider, Func<float, Color> sampler)
        {
            if (_boundSlider == null && slider != null)
                slider.OnValueChangedFloat.AddListener(_ => Refresh());
            _boundSlider = slider;
            _boundSampler = sampler;
            Refresh();
        }

        public void Refresh()
        {
            if (_boundSlider == null || _boundSampler == null)
                return;
            Apply(_boundSampler, _boundSampler(_boundSlider.Position));
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

                // The handle centre only travels between half a handle from each end, so the gradient is stretched to
                // match: the end colours then sit exactly where the handle stops.
                float width = _gradientImage.rectTransform.rect.width;
                float half = _handle != null ? _handle.rectTransform.rect.width * 0.5f : 0f;
                if (width > half * 2f + 1f)
                {
                    float travel = width - half * 2f;
                    _gradientImage.uvRect = new Rect(-half / travel, 0f, width / travel, 1f);
                }
            }

            if (_handle != null)
                _handle.color = handleColor;
        }
    }
}
