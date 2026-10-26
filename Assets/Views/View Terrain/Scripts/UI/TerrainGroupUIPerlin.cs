using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gavi
{
    /// <summary>Noise pattern of the terrain height (Perlin and others).</summary>
    public class TerrainGroupUIPerlin : TerrainGroupUI, IPatternSpeed
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
        [Tooltip("Speed along x and along y, from -10 to 10 with 0 in the middle (see TerrainPatternSpeed).")]
        [SerializeField] private SliderG _sliderSpeedX;
        [SerializeField] private SliderG _sliderSpeedY;
        [Tooltip("Optional: the pattern is turned upside down on the beat every this many beats, 0 = off.")]
        [SerializeField] private SliderG _sliderFlipBeats;

        /// <summary>Length of the speed as the sliders show it, 0 while the pattern is off.</summary>
        public float ActiveSpeed => SpeedOn ? SpeedLength : 0f;
        public bool SpeedOn => SpeedInPreset ? _shownPreset.ApplyPerlin : _toggleApplyPerlin == null || _toggleApplyPerlin.isOn;
        // before the group woke up (closed tab) its sliders show nothing, the preset knows the speed
        private bool SpeedInPreset => !_ready && _shownPreset != null;
        public float SpeedLength => SpeedInPreset ? new Vector2(_shownPreset.PerlinSpeedX, _shownPreset.PerlinSpeedY).magnitude : TerrainPatternSpeed.Length(_sliderSpeedX, _sliderSpeedY);

        public void SetSpeedLength(float length)
        {
            if (!SpeedInPreset)
            {
                TerrainPatternSpeed.SetLength(_sliderSpeedX, _sliderSpeedY, length);
                return;
            }
            Vector2 speed = TerrainPatternSpeed.WithLength(new Vector2(_shownPreset.PerlinSpeedX, _shownPreset.PerlinSpeedY), length);
            _shownPreset.PerlinSpeedX = speed.x;
            _shownPreset.PerlinSpeedY = speed.y;
            OnSpeedChanged(length);
        }
        public event System.Action SpeedChanged;

        private void Awake()
        {
            _toggleApplyPerlin.onValueChanged.AddListener(OnUIValueChanged);
            _sliderPerlinDepth.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderPerlinZoom.OnValueChangedFloat.AddListener(OnUIValueChanged);
            if (_sliderHeightZoom != null)
                _sliderHeightZoom.OnValueChangedFloat.AddListener(OnUIValueChanged);
            if (_sliderSpeedX != null)
                _sliderSpeedX.OnValueChangedFloat.AddListener(OnSpeedChanged);
            if (_sliderSpeedY != null)
                _sliderSpeedY.OnValueChangedFloat.AddListener(OnSpeedChanged);
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

            // The group can be switched off (closed tab) while the menu is filled with the preset. The dropdown has no options then,
            // so the preset is shown again now.
            _ready = true;
            if (_shownPreset != null)
                ShowPreset(_shownPreset);
        }

        private bool _ready;
        private TerrainSettingsPreset _shownPreset;

        // before the group woke up the dropdown and the tick box are empty, the preset knows
        public override string LayerLabel
        {
            get
            {
                bool on = _ready || _shownPreset == null ? _toggleApplyPerlin.isOn : _shownPreset.ApplyPerlin;
                int pattern = _ready || _shownPreset == null ? _dropdownPattern.value : _shownPreset.NoisePattern;
                return on ? TerrainNoise.Names[Mathf.Clamp(pattern, 0, TerrainNoise.Names.Length - 1)] : LayerOffLabel;
            }
        }

        private void OnSpeedChanged(float _)
        {
            OnUIValueChanged();
            SpeedChanged?.Invoke();
        }

        /// <summary>The invert option belongs to the pattern: it is remembered per pattern while the menu is open, off at first.</summary>
        private void OnInvertChanged(bool isOn)
        {
            _invertByPattern[_shownPattern] = isOn;
            OnUIValueChanged(isOn);
        }

        private void OnPatternChanged(int pattern)
        {
            int previous = _shownPattern;
            _shownPattern = Mathf.Clamp(pattern, 0, _invertByPattern.Length - 1);
            _zoomMemory.Switch(_sliderPerlinZoom, previous, _shownPattern);
            if (_toggleInvert != null)
                _toggleInvert.SetIsOnWithoutNotify(_invertByPattern[_shownPattern]);
            OnUIValueChanged(pattern);
        }

        private readonly bool[] _invertByPattern = new bool[TerrainNoise.Names.Length];
        private readonly TerrainNoise.ZoomMemory _zoomMemory = new TerrainNoise.ZoomMemory();
        private int _shownPattern;

        /// <summary>Selects the next or previous pattern and wraps around at the ends of the list.</summary>
        private void StepPattern(int direction)
        {
            if (_dropdownPattern == null)
                return;
            int count = TerrainNoise.Names.Length;
            _dropdownPattern.value = (_dropdownPattern.value + direction + count) % count;
        }

        public override void ExtractPreset(TerrainSettingsPreset preset)
        {
            // before the first Awake the dropdown is empty, its value says nothing
            if (!_ready)
                return;
            base.ExtractPreset(preset);
            preset.ApplyPerlin = _toggleApplyPerlin.isOn;
            preset.PerlinDepth = _sliderPerlinDepth.Value;
            preset.PerlinZoom = _sliderPerlinZoom.Value;
            preset.HeightZoomReference = _sliderPerlinZoom.ValueAtPosition(0.5f);
            if (_sliderHeightZoom != null)
                preset.HeightZoomStrength = _sliderHeightZoom.Value;
            Vector2 speed = TerrainPatternSpeed.Stored(_sliderSpeedX, _sliderSpeedY);
            preset.PerlinSpeedX = speed.x;
            preset.PerlinSpeedY = speed.y;
            if (_dropdownPattern != null)
                preset.NoisePattern = _dropdownPattern.value;
            if (_toggleInvert != null)
                preset.NoiseInvert = _toggleInvert.isOn;
            if (_toggleInvertAll != null)
                preset.NoiseInvertAll = _toggleInvertAll.isOn;
            if (_sliderFlipBeats != null)
                preset.NoiseFlipBeats = _sliderFlipBeats.ValueInt;
        }

        public override void ShowPreset(TerrainSettingsPreset preset)
        {
            _shownPreset = preset;
            if (!_ready)
                return;
            base.ShowPreset(preset);
            _toggleApplyPerlin.isOn = preset.ApplyPerlin;
            _sliderPerlinDepth.Value = preset.PerlinDepth;
            if (_sliderHeightZoom != null)
                _sliderHeightZoom.Value = preset.HeightZoomStrength;
            TerrainPatternSpeed.Show(_sliderSpeedX, _sliderSpeedY, preset.PerlinSpeedX, preset.PerlinSpeedY);
            _shownPattern = Mathf.Clamp(preset.NoisePattern, 0, TerrainNoise.Names.Length - 1);
            if (_dropdownPattern != null)
            {
                _dropdownPattern.SetValueWithoutNotify(_shownPattern);
                _dropdownPattern.RefreshShownValue();
            }
            TerrainNoise.ApplyZoomScale(_sliderPerlinZoom, _shownPattern, false);
            _sliderPerlinZoom.Value = preset.PerlinZoom;
            _zoomMemory.Remember(_shownPattern, _sliderPerlinZoom.Value);
            // the preset only knows the invert option of its own pattern
            _invertByPattern[_shownPattern] = preset.NoiseInvert;
            if (_toggleInvert != null)
                _toggleInvert.isOn = preset.NoiseInvert;
            if (_toggleInvertAll != null)
                _toggleInvertAll.isOn = preset.NoiseInvertAll;
            if (_sliderFlipBeats != null)
                _sliderFlipBeats.Value = preset.NoiseFlipBeats;
        }
    }
}
