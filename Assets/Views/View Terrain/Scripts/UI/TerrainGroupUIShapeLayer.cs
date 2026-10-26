using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gavi
{
    /// <summary>One of the three audio shape layers: its shape, strength, zoom, invert, how it is mixed with the layers before it and whether the inner bands are broadened.</summary>
    public class TerrainGroupUIShapeLayer : TerrainGroupUI
    {
        public const int LayerCount = 3;

        [Tooltip("0 = layer 1, 1 = layer 2, 2 = layer 3.")]
        [SerializeField] private int _layerIndex;
        [SerializeField] private Toggle _toggleEnabled;
        [SerializeField] private TMP_Dropdown _dropdownShape;
        [SerializeField] private Button _buttonShapePrevious;
        [SerializeField] private Button _buttonShapeNext;
        [SerializeField] private SliderG _sliderStrength;
        [SerializeField] private SliderG _sliderZoom;
        [SerializeField] private Toggle _toggleInvert;
        [SerializeField] private TMP_Dropdown _dropdownOperation;
        [SerializeField] private Toggle _toggleBroadenInnerBands;

        private bool _ready;
        private TerrainSettingsPreset _shownPreset;

        // before the group woke up the dropdown is empty, the preset knows
        public override string LayerLabel
        {
            get
            {
                int shape = _dropdownShape.value;
                if (!_ready && _shownPreset != null && _shownPreset.Shapes != null && _layerIndex < _shownPreset.Shapes.Count)
                    shape = _shownPreset.Shapes[_layerIndex].Shape;
                return _toggleEnabled.isOn ? TerrainShape.ShapeNames[Mathf.Clamp(shape, 0, TerrainShape.ShapeNames.Length - 1)] : LayerOffLabel;
            }
        }

        private void Awake()
        {
            _toggleEnabled.onValueChanged.AddListener(OnUIValueChanged);
            _toggleInvert.onValueChanged.AddListener(OnUIValueChanged);
            _toggleBroadenInnerBands.onValueChanged.AddListener(OnUIValueChanged);
            _sliderStrength.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderZoom.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _dropdownOperation.onValueChanged.AddListener(OnUIValueChanged);

            _dropdownShape.ClearOptions();
            _dropdownShape.AddOptions(new List<string>(TerrainShape.ShapeNames));
            _dropdownShape.onValueChanged.AddListener(OnUIValueChanged);

            _buttonShapePrevious.onClick.AddListener(() => StepShape(-1));
            _buttonShapeNext.onClick.AddListener(() => StepShape(1));

            // The group can be switched off (closed tab) while the menu is filled with the preset. The dropdown has no options then,
            // so the preset is shown again now.
            _ready = true;
            if (_shownPreset != null)
                ShowPreset(_shownPreset);
        }

        /// <summary>Selects the next or previous shape and wraps around at the ends of the list.</summary>
        private void StepShape(int direction)
        {
            int count = TerrainShape.ShapeNames.Length;
            _dropdownShape.value = (_dropdownShape.value + direction + count) % count;
        }

        public override void ExtractPreset(TerrainSettingsPreset preset)
        {
            // before the first Awake the dropdown is empty, its value says nothing
            if (!_ready)
                return;
            base.ExtractPreset(preset);
            preset.Shapes ??= new List<TerrainShapeData>();
            // layers that were never used are off; a preset from before the layers can hold more shapes than the menu shows
            while (preset.Shapes.Count <= _layerIndex)
                preset.Shapes.Add(new TerrainShapeData { ApplyShape = false });
            if (preset.Shapes.Count > LayerCount)
                preset.Shapes.RemoveRange(LayerCount, preset.Shapes.Count - LayerCount);
            TerrainShapeData shape = preset.Shapes[_layerIndex];
            shape.ApplyShape = _toggleEnabled.isOn;
            shape.Shape = _dropdownShape.value;
            shape.Strength = _sliderStrength.Value;
            shape.Zoom = _sliderZoom.Value;
            shape.Invert = _toggleInvert.isOn;
            shape.Operation = _dropdownOperation.value;
            shape.BroadenInnerBands = _toggleBroadenInnerBands.isOn;
        }

        public override void ShowPreset(TerrainSettingsPreset preset)
        {
            _shownPreset = preset;
            // the on switch is shown even before the group woke up, the tab of the layer is dimmed by it
            if (!_ready)
            {
                _toggleEnabled.SetIsOnWithoutNotify(preset.Shapes != null && _layerIndex < preset.Shapes.Count && preset.Shapes[_layerIndex].ApplyShape);
                return;
            }
            base.ShowPreset(preset);
            bool stored = preset.Shapes != null && _layerIndex < preset.Shapes.Count;
            TerrainShapeData shape = stored ? preset.Shapes[_layerIndex] : new TerrainShapeData { ApplyShape = false };
            _toggleEnabled.isOn = shape.ApplyShape;
            _dropdownShape.SetValueWithoutNotify(Mathf.Clamp(shape.Shape, 0, TerrainShape.ShapeNames.Length - 1));
            _dropdownShape.RefreshShownValue();
            _sliderStrength.Value = shape.Strength;
            _sliderZoom.Value = shape.Zoom;
            _toggleInvert.isOn = shape.Invert;
            _dropdownOperation.SetValueWithoutNotify(Mathf.Clamp(shape.Operation, 0, _dropdownOperation.options.Count - 1));
            _dropdownOperation.RefreshShownValue();
            _toggleBroadenInnerBands.isOn = shape.BroadenInnerBands;
        }
    }
}
