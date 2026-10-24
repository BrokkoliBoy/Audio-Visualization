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
        private TerrainSettingsProfile _shownProfile;

        // before the group woke up the dropdown is empty, the profile knows
        public override string LayerLabel
        {
            get
            {
                int shape = _dropdownShape.value;
                if (!_ready && _shownProfile != null && _shownProfile.Shapes != null && _layerIndex < _shownProfile.Shapes.Count)
                    shape = _shownProfile.Shapes[_layerIndex].Shape;
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

            // The group can be switched off (closed tab) while the menu is filled with the profile. The dropdown has no options then,
            // so the profile is shown again now.
            _ready = true;
            if (_shownProfile != null)
                ShowProfile(_shownProfile);
        }

        /// <summary>Selects the next or previous shape and wraps around at the ends of the list.</summary>
        private void StepShape(int direction)
        {
            int count = TerrainShape.ShapeNames.Length;
            _dropdownShape.value = (_dropdownShape.value + direction + count) % count;
        }

        public override void ExtractProfile(TerrainSettingsProfile profile)
        {
            // before the first Awake the dropdown is empty, its value says nothing
            if (!_ready)
                return;
            base.ExtractProfile(profile);
            profile.Shapes ??= new List<TerrainShapeData>();
            // layers that were never used are off; a profile from before the layers can hold more shapes than the menu shows
            while (profile.Shapes.Count <= _layerIndex)
                profile.Shapes.Add(new TerrainShapeData { ApplyShape = false });
            if (profile.Shapes.Count > LayerCount)
                profile.Shapes.RemoveRange(LayerCount, profile.Shapes.Count - LayerCount);
            TerrainShapeData shape = profile.Shapes[_layerIndex];
            shape.ApplyShape = _toggleEnabled.isOn;
            shape.Shape = _dropdownShape.value;
            shape.Strength = _sliderStrength.Value;
            shape.Zoom = _sliderZoom.Value;
            shape.Invert = _toggleInvert.isOn;
            shape.Operation = _dropdownOperation.value;
            shape.BroadenInnerBands = _toggleBroadenInnerBands.isOn;
        }

        public override void ShowProfile(TerrainSettingsProfile profile)
        {
            _shownProfile = profile;
            // the on switch is shown even before the group woke up, the tab of the layer is dimmed by it
            if (!_ready)
            {
                _toggleEnabled.SetIsOnWithoutNotify(profile.Shapes != null && _layerIndex < profile.Shapes.Count && profile.Shapes[_layerIndex].ApplyShape);
                return;
            }
            base.ShowProfile(profile);
            bool stored = profile.Shapes != null && _layerIndex < profile.Shapes.Count;
            TerrainShapeData shape = stored ? profile.Shapes[_layerIndex] : new TerrainShapeData { ApplyShape = false };
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
