using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gavi
{
    /// <summary>An extra pattern layer on top of the main pattern: its own pattern, zoom, speed and invert, and how it is mixed with the layers below.</summary>
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
        [SerializeField] private SliderG _sliderDepth;
        [SerializeField] private SliderG _sliderZoom;
        [SerializeField] private SliderG _sliderSpeed;
        [Tooltip("Direction of the pattern movement. The speed slider is the length of this direction.")]
        [SerializeField] private Vector2 _speedDirection = new Vector2(0f, -1f);

        /// <summary>Speed of the pattern as the slider shows it, 0 while the layer is off.</summary>
        public float ActiveSpeed => _toggleEnabled != null && _toggleEnabled.isOn && _sliderSpeed != null ? _sliderSpeed.Value : 0f;

        private void Awake()
        {
            _toggleEnabled.onValueChanged.AddListener(OnUIValueChanged);
            _toggleInvert.onValueChanged.AddListener(OnUIValueChanged);
            _sliderDepth.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderZoom.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderSpeed.OnValueChangedFloat.AddListener(OnUIValueChanged);

            _dropdownPattern.ClearOptions();
            _dropdownPattern.AddOptions(new List<string>(TerrainNoise.Names));
            _dropdownPattern.onValueChanged.AddListener(OnUIValueChanged);
            _dropdownOperation.ClearOptions();
            _dropdownOperation.AddOptions(new List<string>(TerrainNoise.OperationNames));
            _dropdownOperation.onValueChanged.AddListener(OnUIValueChanged);

            _buttonPatternPrevious.onClick.AddListener(() => StepPattern(-1));
            _buttonPatternNext.onClick.AddListener(() => StepPattern(1));

            // The group can be switched off (closed tab) while the menu is filled with the profile. The dropdowns have no options then,
            // so the profile is shown again now.
            _ready = true;
            if (_shownProfile != null)
                ShowProfile(_shownProfile);
        }

        private bool _ready;
        private TerrainSettingsProfile _shownProfile;

        /// <summary>Selects the next or previous pattern and wraps around at the ends of the list.</summary>
        private void StepPattern(int direction)
        {
            int count = TerrainNoise.Names.Length;
            _dropdownPattern.value = (_dropdownPattern.value + direction + count) % count;
        }

        private TerrainNoiseLayerData Layer(TerrainSettingsProfile profile)
        {
            return _layerIndex == 0 ? profile.NoiseLayer2 : profile.NoiseLayer3;
        }

        public override void ExtractProfile(TerrainSettingsProfile profile)
        {
            // before the first Awake the dropdowns are empty, their values say nothing
            if (!_ready)
                return;
            base.ExtractProfile(profile);
            TerrainNoiseLayerData layer = Layer(profile);
            layer.Enabled = _toggleEnabled.isOn;
            layer.Pattern = _dropdownPattern.value;
            layer.Operation = _dropdownOperation.value;
            layer.Invert = _toggleInvert.isOn;
            layer.Depth = _sliderDepth.Value;
            layer.Zoom = _sliderZoom.Value;
            layer.SpeedX = _speedDirection.x * _sliderSpeed.Value;
            layer.SpeedY = _speedDirection.y * _sliderSpeed.Value;
        }

        public override void ShowProfile(TerrainSettingsProfile profile)
        {
            _shownProfile = profile;
            if (!_ready)
                return;
            base.ShowProfile(profile);
            TerrainNoiseLayerData layer = Layer(profile);
            _toggleEnabled.isOn = layer.Enabled;
            _dropdownPattern.SetValueWithoutNotify(Mathf.Clamp(layer.Pattern, 0, TerrainNoise.Names.Length - 1));
            _dropdownPattern.RefreshShownValue();
            _dropdownOperation.SetValueWithoutNotify(Mathf.Clamp(layer.Operation, 0, TerrainNoise.OperationNames.Length - 1));
            _dropdownOperation.RefreshShownValue();
            _toggleInvert.isOn = layer.Invert;
            _sliderDepth.Value = layer.Depth;
            _sliderZoom.Value = layer.Zoom;
            // the length of the stored speed along the direction of the menu
            float length = Mathf.Max(0.0001f, _speedDirection.magnitude);
            _sliderSpeed.Value = Mathf.Max(0f, Vector2.Dot(new Vector2(layer.SpeedX, layer.SpeedY), _speedDirection) / (length * length));
        }
    }
}
