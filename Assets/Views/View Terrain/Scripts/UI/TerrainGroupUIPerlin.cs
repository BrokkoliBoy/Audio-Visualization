using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gavi
{
    /// <summary>Noise pattern of the terrain height (Perlin and others).</summary>
    public class TerrainGroupUIPerlin : TerrainGroupUI
    {
        [SerializeField] private Toggle _toggleApplyPerlin;
        [SerializeField] private TMP_Dropdown _dropdownPattern;
        [SerializeField] private Button _buttonPatternPrevious;
        [SerializeField] private Button _buttonPatternNext;
        [SerializeField] private Toggle _toggleInvert;
        [Tooltip("Optional: turns the whole noise (all pattern layers mixed) upside down.")]
        [SerializeField] private Toggle _toggleInvertAll;
        [SerializeField] private SliderG _sliderPerlinDepth;
        [SerializeField] private SliderG _sliderPerlinZoom;
        [Tooltip("Optional: how strongly the terrain height follows the zoom (0 = off), so zooming out keeps the shapes instead of making them steeper.")]
        [SerializeField] private SliderG _sliderHeightZoom;
        [SerializeField] private SliderG _sliderPerlinSpeed;
        [Tooltip("Optional: the pattern is turned upside down on the beat every this many beats, 0 = off.")]
        [SerializeField] private SliderG _sliderFlipBeats;
        [Tooltip("Direction of the noise movement. The speed slider is the length of this direction (radial patterns move outward with a negative y).")]
        [SerializeField] private Vector2 _speedDirection = new Vector2(0f, -1f);

        /// <summary>Speed of the pattern as the slider shows it, 0 while the pattern is off.</summary>
        public float ActiveSpeed => (_toggleApplyPerlin == null || _toggleApplyPerlin.isOn) && _sliderPerlinSpeed != null ? _sliderPerlinSpeed.Value : 0f;
        public SliderG SpeedSlider => _sliderPerlinSpeed;
        public bool SpeedOn => _toggleApplyPerlin == null || _toggleApplyPerlin.isOn;

        private void Awake()
        {
            _toggleApplyPerlin.onValueChanged.AddListener(OnUIValueChanged);
            _sliderPerlinDepth.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderPerlinZoom.OnValueChangedFloat.AddListener(OnUIValueChanged);
            if (_sliderHeightZoom != null)
                _sliderHeightZoom.OnValueChangedFloat.AddListener(OnUIValueChanged);
            if (_sliderPerlinSpeed != null)
                _sliderPerlinSpeed.OnValueChangedFloat.AddListener(OnUIValueChanged);
            if (_sliderFlipBeats != null)
                _sliderFlipBeats.OnValueChangedFloat.AddListener(OnUIValueChanged);
            if (_toggleInvert != null)
                _toggleInvert.onValueChanged.AddListener(OnInvertChanged);
            if (_toggleInvertAll != null)
                _toggleInvertAll.onValueChanged.AddListener(OnUIValueChanged);

            if (_dropdownPattern != null)
            {
                _dropdownPattern.ClearOptions();
                _dropdownPattern.AddOptions(new List<string>(TerrainNoise.Names));
                _dropdownPattern.onValueChanged.AddListener(OnPatternChanged);
            }
            if (_buttonPatternPrevious != null)
                _buttonPatternPrevious.onClick.AddListener(() => StepPattern(-1));
            if (_buttonPatternNext != null)
                _buttonPatternNext.onClick.AddListener(() => StepPattern(1));

            // The group can be switched off (closed tab) while the menu is filled with the profile. The dropdown has no options then,
            // so the profile is shown again now.
            _ready = true;
            if (_shownProfile != null)
                ShowProfile(_shownProfile);
        }

        private bool _ready;
        private TerrainSettingsProfile _shownProfile;

        // before the group woke up the dropdown and the tick box are empty, the profile knows
        public override string LayerLabel
        {
            get
            {
                bool on = _ready || _shownProfile == null ? _toggleApplyPerlin.isOn : _shownProfile.ApplyPerlin;
                int pattern = _ready || _shownProfile == null ? _dropdownPattern.value : _shownProfile.NoisePattern;
                return on ? TerrainNoise.Names[Mathf.Clamp(pattern, 0, TerrainNoise.Names.Length - 1)] : LayerOffLabel;
            }
        }

        /// <summary>The invert option belongs to the pattern: it is remembered per pattern while the menu is open, off at first.</summary>
        private void OnInvertChanged(bool isOn)
        {
            _invertByPattern[_shownPattern] = isOn;
            OnUIValueChanged(isOn);
        }

        private void OnPatternChanged(int pattern)
        {
            _shownPattern = Mathf.Clamp(pattern, 0, _invertByPattern.Length - 1);
            TerrainNoise.ApplyZoomScale(_sliderPerlinZoom, _shownPattern, true);
            TerrainGenerator.HeightZoomReference = _sliderPerlinZoom.ValueAtPosition(0.5f);
            if (_toggleInvert != null)
                _toggleInvert.SetIsOnWithoutNotify(_invertByPattern[_shownPattern]);
            OnUIValueChanged(pattern);
        }

        private readonly bool[] _invertByPattern = new bool[TerrainNoise.Names.Length];
        private int _shownPattern;

        /// <summary>Selects the next or previous pattern and wraps around at the ends of the list.</summary>
        private void StepPattern(int direction)
        {
            if (_dropdownPattern == null)
                return;
            int count = TerrainNoise.Names.Length;
            _dropdownPattern.value = (_dropdownPattern.value + direction + count) % count;
        }

        public override void ExtractProfile(TerrainSettingsProfile profile)
        {
            // before the first Awake the dropdown is empty, its value says nothing
            if (!_ready)
                return;
            base.ExtractProfile(profile);
            profile.ApplyPerlin = _toggleApplyPerlin.isOn;
            profile.PerlinDepth = _sliderPerlinDepth.Value;
            profile.PerlinZoom = _sliderPerlinZoom.Value;
            if (_sliderHeightZoom != null)
                profile.HeightZoomStrength = _sliderHeightZoom.Value;
            float speed = _sliderPerlinSpeed != null ? _sliderPerlinSpeed.Value : 1f;
            profile.PerlinSpeedX = _speedDirection.x * speed;
            profile.PerlinSpeedY = _speedDirection.y * speed;
            if (_dropdownPattern != null)
                profile.NoisePattern = _dropdownPattern.value;
            if (_toggleInvert != null)
                profile.NoiseInvert = _toggleInvert.isOn;
            if (_toggleInvertAll != null)
                profile.NoiseInvertAll = _toggleInvertAll.isOn;
            if (_sliderFlipBeats != null)
                profile.NoiseFlipBeats = _sliderFlipBeats.ValueInt;
        }

        public override void ShowProfile(TerrainSettingsProfile profile)
        {
            _shownProfile = profile;
            if (!_ready)
                return;
            base.ShowProfile(profile);
            _toggleApplyPerlin.isOn = profile.ApplyPerlin;
            _sliderPerlinDepth.Value = profile.PerlinDepth;
            if (_sliderHeightZoom != null)
                _sliderHeightZoom.Value = profile.HeightZoomStrength;
            if (_sliderPerlinSpeed != null)
            {
                // the length of the stored speed along the direction of the menu
                float length = Mathf.Max(0.0001f, _speedDirection.magnitude);
                _sliderPerlinSpeed.Value = Mathf.Max(0f, Vector2.Dot(new Vector2(profile.PerlinSpeedX, profile.PerlinSpeedY), _speedDirection) / (length * length));
            }
            _shownPattern = Mathf.Clamp(profile.NoisePattern, 0, TerrainNoise.Names.Length - 1);
            if (_dropdownPattern != null)
            {
                _dropdownPattern.SetValueWithoutNotify(_shownPattern);
                _dropdownPattern.RefreshShownValue();
            }
            TerrainNoise.ApplyZoomScale(_sliderPerlinZoom, _shownPattern, false);
            TerrainGenerator.HeightZoomReference = _sliderPerlinZoom.ValueAtPosition(0.5f);
            _sliderPerlinZoom.Value = profile.PerlinZoom;
            // the profile only knows the invert option of its own pattern
            _invertByPattern[_shownPattern] = profile.NoiseInvert;
            if (_toggleInvert != null)
                _toggleInvert.isOn = profile.NoiseInvert;
            if (_toggleInvertAll != null)
                _toggleInvertAll.isOn = profile.NoiseInvertAll;
            if (_sliderFlipBeats != null)
                _sliderFlipBeats.Value = profile.NoiseFlipBeats;
        }
    }
}
