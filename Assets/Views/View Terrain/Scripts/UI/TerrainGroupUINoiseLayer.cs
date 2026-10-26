using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gavi
{
    /// <summary>An extra pattern layer on top of the main pattern: its own pattern, depth, zoom, speed and invert, and how it is mixed (operation and mix) with the layers below.</summary>
    public class TerrainGroupUINoiseLayer : TerrainGroupUI
    {
        [Tooltip("0 = layer 2, 1 = layer 3.")]
        [SerializeField] private int _layerIndex;
        [SerializeField] private Toggle _toggleEnabled;
        [SerializeField] private TMP_Dropdown _dropdownPattern;
        [SerializeField] private Button _buttonPatternPrevious;
        [SerializeField] private Button _buttonPatternNext;
        [SerializeField] private TMP_Dropdown _dropdownOperation;
        [SerializeField] private Toggle _toggleInvert;
        [Tooltip("How far the lowest point of the layer lies under the terrain height.")]
        [SerializeField] private SliderG _sliderDepth;
        [Tooltip("How much of the mixed result is used, 0 = the layers below stay as they are.")]
        [SerializeField] private SliderG _sliderMix;
        [SerializeField] private SliderG _sliderZoom;
        [SerializeField] private SliderG _sliderSpeed;
        [Tooltip("Direction of the pattern movement. The speed slider is the length of this direction.")]
        [SerializeField] private Vector2 _speedDirection = new Vector2(0f, -1f);

        /// <summary>Speed of the pattern as the slider shows it, 0 while the layer is off.</summary>
        public float ActiveSpeed => _toggleEnabled != null && _toggleEnabled.isOn && _sliderSpeed != null ? _sliderSpeed.Value : 0f;
        public SliderG SpeedSlider => _sliderSpeed;
        public bool SpeedOn => _toggleEnabled != null && _toggleEnabled.isOn;

        private void Awake()
        {
            _toggleEnabled.onValueChanged.AddListener(OnUIValueChanged);
            _toggleInvert.onValueChanged.AddListener(OnUIValueChanged);
            _sliderDepth.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderMix.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderZoom.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderSpeed.OnValueChangedFloat.AddListener(OnUIValueChanged);

            _dropdownPattern.ClearOptions();
            _dropdownPattern.AddOptions(new List<string>(TerrainNoise.Names));
            _dropdownPattern.onValueChanged.AddListener(OnPatternChanged);
            _dropdownOperation.ClearOptions();
            _dropdownOperation.AddOptions(new List<string>(TerrainNoise.OperationNames));
            _dropdownOperation.onValueChanged.AddListener(OnUIValueChanged);

            _buttonPatternPrevious.onClick.AddListener(() => StepPattern(-1));
            _buttonPatternNext.onClick.AddListener(() => StepPattern(1));

            // The group can be switched off (closed tab) while the menu is filled with the preset. The dropdowns have no options then,
            // so the preset is shown again now.
            _ready = true;
            if (_shownPreset != null)
                ShowPreset(_shownPreset);
        }

        private bool _ready;
        private TerrainSettingsPreset _shownPreset;

        // before the group woke up the dropdown is empty, the preset knows
        public override string LayerLabel
        {
            get
            {
                int pattern = _ready || _shownPreset == null ? _dropdownPattern.value : Layer(_shownPreset).Pattern;
                return _toggleEnabled.isOn ? TerrainNoise.Names[Mathf.Clamp(pattern, 0, TerrainNoise.Names.Length - 1)] : LayerOffLabel;
            }
        }

        private void OnPatternChanged(int pattern)
        {
            // each pattern has its own zoom range, a new pattern starts at its default zoom
            TerrainNoise.ApplyZoomScale(_sliderZoom, pattern, true);
            OnUIValueChanged(pattern);
        }

        /// <summary>Selects the next or previous pattern and wraps around at the ends of the list.</summary>
        private void StepPattern(int direction)
        {
            int count = TerrainNoise.Names.Length;
            _dropdownPattern.value = (_dropdownPattern.value + direction + count) % count;
        }

        private TerrainNoiseLayerData Layer(TerrainSettingsPreset preset)
        {
            return _layerIndex == 0 ? preset.NoiseLayer2 : preset.NoiseLayer3;
        }

        public override void ExtractPreset(TerrainSettingsPreset preset)
        {
            // before the first Awake the dropdowns are empty, their values say nothing
            if (!_ready)
                return;
            base.ExtractPreset(preset);
            TerrainNoiseLayerData layer = Layer(preset);
            layer.Enabled = _toggleEnabled.isOn;
            layer.Pattern = _dropdownPattern.value;
            layer.Operation = _dropdownOperation.value;
            layer.Invert = _toggleInvert.isOn;
            layer.Depth = _sliderDepth.Value;
            layer.Mix = _sliderMix.Value;
            layer.Zoom = _sliderZoom.Value;
            layer.SpeedX = _speedDirection.x * _sliderSpeed.Value;
            layer.SpeedY = _speedDirection.y * _sliderSpeed.Value;
        }

        public override void ShowPreset(TerrainSettingsPreset preset)
        {
            _shownPreset = preset;
            // the on switch is shown even before the group woke up, the tab of the layer is dimmed by it
            if (!_ready)
            {
                _toggleEnabled.SetIsOnWithoutNotify(Layer(preset).Enabled);
                return;
            }
            base.ShowPreset(preset);
            TerrainNoiseLayerData layer = Layer(preset);
            _toggleEnabled.isOn = layer.Enabled;
            _dropdownPattern.SetValueWithoutNotify(Mathf.Clamp(layer.Pattern, 0, TerrainNoise.Names.Length - 1));
            _dropdownPattern.RefreshShownValue();
            TerrainNoise.ApplyZoomScale(_sliderZoom, layer.Pattern, false);
            _sliderZoom.Value = layer.Zoom;
            _dropdownOperation.SetValueWithoutNotify(Mathf.Clamp(layer.Operation, 0, TerrainNoise.OperationNames.Length - 1));
            _dropdownOperation.RefreshShownValue();
            _toggleInvert.isOn = layer.Invert;
            _sliderDepth.Value = layer.Depth;
            _sliderMix.Value = layer.Mix;
            // the length of the stored speed along the direction of the menu
            float length = Mathf.Max(0.0001f, _speedDirection.magnitude);
            _sliderSpeed.Value = Mathf.Max(0f, Vector2.Dot(new Vector2(layer.SpeedX, layer.SpeedY), _speedDirection) / (length * length));
        }
    }
}
