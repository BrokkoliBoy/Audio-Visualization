using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gavi
{
    /// <summary>
    /// One color of the terrain gradient in the color menu: hue, saturation and intensity sliders with a preview swatch.
    /// The slider tracks show the color the slider would give, depending on the other two sliders.
    /// </summary>
    public class TerrainColorEntryUI : MonoBehaviour
    {
        [SerializeField] private SliderG _hue;
        [SerializeField] private SliderG _saturation;
        [SerializeField] private SliderG _intensity;
        [SerializeField] private Image _swatch;
        [SerializeField] private UiSliderGradient _hueGradient;
        [SerializeField] private UiSliderGradient _saturationGradient;
        [SerializeField] private UiSliderGradient _intensityGradient;

        public float Hue => _hue.Value;
        public float Saturation => _saturation.Value;
        public float MenuIntensity => _intensity.Value;

        /// <summary>Raised when the user moves one of the sliders.</summary>
        public event Action Changed;

        private bool _suppress;
        private bool _hasListeners;


        #region Mono
        private void Awake()
        {
            AddListeners();
        }

        private void Start()
        {
            RefreshVisuals();
        }
        #endregion


        public void SetValues(float hue, float saturation, float menuIntensity)
        {
            AddListeners();
            _suppress = true;
            _hue.Value = hue;
            _saturation.Value = saturation;
            _intensity.Value = menuIntensity;
            _suppress = false;
            RefreshVisuals();
        }

        private void AddListeners()
        {
            if (_hasListeners)
                return;
            _hasListeners = true;
            _hue.OnValueChangedFloat.AddListener(OnSliderChanged);
            _saturation.OnValueChangedFloat.AddListener(OnSliderChanged);
            _intensity.OnValueChangedFloat.AddListener(OnSliderChanged);
        }

        private void OnSliderChanged(float value)
        {
            RefreshVisuals();
            if (!_suppress)
                Changed?.Invoke();
        }

        private void RefreshVisuals()
        {
            float hue = _hue.Value;
            float sat = _saturation.Value;
            float intensity = _intensity.Value;
            Color current = TerrainColorMapping.Preview(hue, sat, intensity);

            if (_swatch != null)
                _swatch.color = current;
            if (_hueGradient != null)
                _hueGradient.Apply(t => TerrainColorMapping.Preview(t * 360f, sat, intensity), current);
            if (_saturationGradient != null)
                _saturationGradient.Apply(t => TerrainColorMapping.Preview(hue, t, intensity), current);
            if (_intensityGradient != null)
                _intensityGradient.Apply(t => TerrainColorMapping.Preview(hue, sat, t * TerrainColorMapping.MenuIntensityMax), current);
        }
    }
}
