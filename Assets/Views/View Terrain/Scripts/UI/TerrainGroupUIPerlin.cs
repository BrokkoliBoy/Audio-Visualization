using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gavi
{
    public class TerrainGroupUIPerlin : TerrainGroupUI
    {
        [SerializeField] private Toggle _toggleApplyPerlin;
        [SerializeField] private SliderG _sliderPerlinDepth;
        [SerializeField] private SliderG _sliderPerlinZoom;
        [SerializeField] private SliderG _sliderPerlinSpeedX;
        [SerializeField] private SliderG _sliderPerlinSpeedY;

        private void Awake()
        {
            _toggleApplyPerlin.onValueChanged.AddListener(OnUIValueChanged);
            _sliderPerlinDepth.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderPerlinZoom.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderPerlinSpeedX.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderPerlinSpeedY.OnValueChangedFloat.AddListener(OnUIValueChanged);
        }

        public override void ExtractProfile(TerrainSettingsProfile profile)
        {
            base.ExtractProfile(profile);
            profile.ApplyPerlin = _toggleApplyPerlin.isOn;
            profile.PerlinDepth = _sliderPerlinDepth.Value;
            profile.PerlinZoom = _sliderPerlinZoom.Value;
            profile.PerlinSpeedX = _sliderPerlinSpeedX.Value;
            profile.PerlinSpeedY = _sliderPerlinSpeedY.Value;
        }

        public override void ShowProfile(TerrainSettingsProfile profile)
        {
            base.ShowProfile(profile);
            _toggleApplyPerlin.isOn = profile.ApplyPerlin;
            _sliderPerlinDepth.Value = profile.PerlinDepth;
            _sliderPerlinZoom.Value = profile.PerlinZoom;
            _sliderPerlinSpeedX.Value = profile.PerlinSpeedX;
            _sliderPerlinSpeedY.Value = profile.PerlinSpeedY;
        }
    }
}
