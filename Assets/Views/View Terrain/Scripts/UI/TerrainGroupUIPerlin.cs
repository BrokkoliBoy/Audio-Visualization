using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gavi
{
    /// <summary>Noise pattern of the terrain height (Perlin and others) and the wave that runs outward on each beat.</summary>
    public class TerrainGroupUIPerlin : TerrainGroupUI
    {
        [SerializeField] private Toggle _toggleApplyPerlin;
        [SerializeField] private TMP_Dropdown _dropdownPattern;
        [SerializeField] private SliderG _sliderPerlinDepth;
        [SerializeField] private SliderG _sliderPerlinZoom;
        [SerializeField] private SliderG _sliderPerlinSpeedX;
        [SerializeField] private SliderG _sliderWaveStrength;
        [SerializeField] private SliderG _sliderWaveSpeed;
        [Tooltip("The speed on the second axis is not editable in the menu and always uses this value.")]
        [SerializeField] private float _fixedPerlinSpeedY = -1f;

        private void Awake()
        {
            _toggleApplyPerlin.onValueChanged.AddListener(OnUIValueChanged);
            _sliderPerlinDepth.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderPerlinZoom.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderPerlinSpeedX.OnValueChangedFloat.AddListener(OnUIValueChanged);

            if (_dropdownPattern != null)
            {
                _dropdownPattern.ClearOptions();
                _dropdownPattern.AddOptions(new List<string>(TerrainNoise.Names));
                _dropdownPattern.onValueChanged.AddListener(OnUIValueChanged);
            }
            if (_sliderWaveStrength != null)
                _sliderWaveStrength.OnValueChangedFloat.AddListener(OnUIValueChanged);
            if (_sliderWaveSpeed != null)
                _sliderWaveSpeed.OnValueChangedFloat.AddListener(OnUIValueChanged);
        }

        public override void ExtractProfile(TerrainSettingsProfile profile)
        {
            base.ExtractProfile(profile);
            profile.ApplyPerlin = _toggleApplyPerlin.isOn;
            profile.PerlinDepth = _sliderPerlinDepth.Value;
            profile.PerlinZoom = _sliderPerlinZoom.Value;
            profile.PerlinSpeedX = _sliderPerlinSpeedX.Value;
            profile.PerlinSpeedY = _fixedPerlinSpeedY;
            if (_dropdownPattern != null)
                profile.NoisePattern = _dropdownPattern.value;
            if (_sliderWaveStrength != null)
                profile.WaveStrength = _sliderWaveStrength.Value;
            if (_sliderWaveSpeed != null)
                profile.WaveSpeed = _sliderWaveSpeed.Value;
        }

        public override void ShowProfile(TerrainSettingsProfile profile)
        {
            base.ShowProfile(profile);
            _toggleApplyPerlin.isOn = profile.ApplyPerlin;
            _sliderPerlinDepth.Value = profile.PerlinDepth;
            _sliderPerlinZoom.Value = profile.PerlinZoom;
            _sliderPerlinSpeedX.Value = profile.PerlinSpeedX;
            if (_dropdownPattern != null)
            {
                _dropdownPattern.SetValueWithoutNotify(Mathf.Clamp(profile.NoisePattern, 0, TerrainNoise.Names.Length - 1));
                _dropdownPattern.RefreshShownValue();
            }
            if (_sliderWaveStrength != null)
                _sliderWaveStrength.Value = profile.WaveStrength;
            if (_sliderWaveSpeed != null)
                _sliderWaveSpeed.Value = profile.WaveSpeed;
        }
    }
}
