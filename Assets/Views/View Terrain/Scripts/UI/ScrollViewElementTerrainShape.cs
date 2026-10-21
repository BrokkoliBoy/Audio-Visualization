using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gavi
{
    public class ScrollViewElementTerrainShape : ScrollViewElement
    {
        [Header("- Data Elements -")]
        [SerializeField] private Toggle _toggleApplyShape;
        [SerializeField] private TMP_Dropdown _dropdownShape;
        [SerializeField] private SliderG _sliderStrength;
        [SerializeField] private SliderG _sliderZoom;
        [SerializeField] private Toggle _toggleInvert;
        [SerializeField] private TMP_Dropdown _dropdownOperation;
        [SerializeField] private Toggle _toggleBroadenLowerBands;

        [Header("- Buttons -")]
        [SerializeField] private Button _buttonMoveUp;
        [SerializeField] private Button _buttonMoveDown;
        [SerializeField] private Button _buttonAdd;
        [SerializeField] private Button _buttonRemove;

        private void Awake()
        {
            _dropdownShape.ClearOptions();
            _dropdownShape.AddOptions(new List<string>(TerrainShape.ShapeNames));
        }

        public void ExtractProfile(TerrainShapeData shapeData)
        {
            shapeData ??= new TerrainShapeData();
            shapeData.ApplyShape = _toggleApplyShape.isOn;
            shapeData.Shape = _dropdownShape.value;
            shapeData.Strength = _sliderStrength.Value;
            shapeData.Zoom = _sliderZoom.Value;
            shapeData.Invert = _toggleInvert.isOn;
            shapeData.Operation = _dropdownOperation.value;
            shapeData.BroadenInnerBands = _toggleBroadenLowerBands.isOn;
        }

        public void ShowProfile(TerrainShapeData shapeData)
        {
            if (shapeData == null)
                return;
            _toggleApplyShape.isOn = shapeData.ApplyShape;
            _dropdownShape.value = shapeData.Shape;
            _sliderStrength.Value = shapeData.Strength;
            _sliderZoom.Value = shapeData.Zoom;
            _toggleInvert.isOn = shapeData.Invert;
            _dropdownOperation.value = shapeData.Operation;
            _toggleBroadenLowerBands.isOn = shapeData.BroadenInnerBands;
        }
        
        private void OnEnable()
        {
            _toggleApplyShape.onValueChanged.AddListener(TerrainGroupUIShapes.Instance.OnUIValueChanged);
            _dropdownShape.onValueChanged.AddListener(TerrainGroupUIShapes.Instance.OnUIValueChanged);
            _sliderStrength.OnValueChangedFloat.AddListener(TerrainGroupUIShapes.Instance.OnUIValueChanged);
            _sliderZoom.OnValueChangedFloat.AddListener(TerrainGroupUIShapes.Instance.OnUIValueChanged);
            _toggleInvert.onValueChanged.AddListener(TerrainGroupUIShapes.Instance.OnUIValueChanged);
            _dropdownOperation.onValueChanged.AddListener(TerrainGroupUIShapes.Instance.OnUIValueChanged);
            _toggleBroadenLowerBands.onValueChanged.AddListener(TerrainGroupUIShapes.Instance.OnUIValueChanged);
            
            _buttonMoveUp.onClick.AddListener(OnPressedMoveUp);
            _buttonMoveDown.onClick.AddListener(OnPressedMoveDown);
            _buttonAdd.onClick.AddListener(OnPressedAdd);
            _buttonRemove.onClick.AddListener(OnPressedRemove);
        }
        
        private void OnDisable()
        {
            _toggleApplyShape.onValueChanged.RemoveListener(TerrainGroupUIShapes.Instance.OnUIValueChanged);
            _dropdownShape.onValueChanged.RemoveListener(TerrainGroupUIShapes.Instance.OnUIValueChanged);
            _sliderStrength.OnValueChangedFloat.RemoveListener(TerrainGroupUIShapes.Instance.OnUIValueChanged);
            _sliderZoom.OnValueChangedFloat.RemoveListener(TerrainGroupUIShapes.Instance.OnUIValueChanged);
            _toggleInvert.onValueChanged.RemoveListener(TerrainGroupUIShapes.Instance.OnUIValueChanged);
            _dropdownOperation.onValueChanged.RemoveListener(TerrainGroupUIShapes.Instance.OnUIValueChanged);
            _toggleBroadenLowerBands.onValueChanged.RemoveListener(TerrainGroupUIShapes.Instance.OnUIValueChanged);
            
            _buttonMoveUp.onClick.RemoveListener(OnPressedMoveUp);
            _buttonMoveDown.onClick.RemoveListener(OnPressedMoveDown);
            _buttonAdd.onClick.RemoveListener(OnPressedAdd);
            _buttonRemove.onClick.RemoveListener(OnPressedRemove);
        }

        private void OnPressedMoveUp()
        {
            TerrainGroupUIShapes.Instance.OnPressedMoveUp(this);
        }

        private void OnPressedMoveDown()
        {
            TerrainGroupUIShapes.Instance.OnPressedMoveDown(this);
        }

        private void OnPressedAdd()
        {
            TerrainGroupUIShapes.Instance.OnPressedAdd(this);
        }

        private void OnPressedRemove()
        {
            TerrainGroupUIShapes.Instance.OnPressedRemove(this);
        }
    }
}
