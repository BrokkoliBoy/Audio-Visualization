using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gavi
{
    /// <summary>Bends the whole mesh (dome, planet, tube ...). The header toggle switches the morph on and off.</summary>
    public class TerrainGroupUIMorph : TerrainGroupUI
    {
        [SerializeField] private Toggle _toggleApplyMorph;
        [SerializeField] private TMP_Dropdown _dropdownShape;
        [SerializeField] private Button _buttonShapePrevious;
        [SerializeField] private Button _buttonShapeNext;
        [SerializeField] private SliderG _sliderAmount;

        private void Awake()
        {
            _dropdownShape.ClearOptions();
            _dropdownShape.AddOptions(new List<string>(TerrainGenerator.MorphNames));
            _dropdownShape.onValueChanged.AddListener(OnUIValueChanged);
            if (_buttonShapePrevious != null)
                _buttonShapePrevious.onClick.AddListener(() => StepShape(-1));
            if (_buttonShapeNext != null)
                _buttonShapeNext.onClick.AddListener(() => StepShape(1));
            _toggleApplyMorph.onValueChanged.AddListener(OnUIValueChanged);
            _sliderAmount.OnValueChangedFloat.AddListener(OnUIValueChanged);
        }

        /// <summary>Selects the next or previous shape and wraps around at the ends of the list.</summary>
        private void StepShape(int direction)
        {
            int count = TerrainGenerator.MorphNames.Length;
            _dropdownShape.value = (_dropdownShape.value + direction + count) % count;
        }

        public override void ExtractProfile(TerrainSettingsProfile profile)
        {
            base.ExtractProfile(profile);
            profile.ApplyMorph = _toggleApplyMorph.isOn;
            profile.MorphShape = (int)TerrainGenerator.MorphOrder[_dropdownShape.value];
            profile.MorphAmount = _sliderAmount.Value;
        }

        public override void ShowProfile(TerrainSettingsProfile profile)
        {
            base.ShowProfile(profile);
            _toggleApplyMorph.isOn = profile.ApplyMorph;
            // a profile of a removed shape shows the dome, which is also what the terrain draws
            int index = System.Array.IndexOf(TerrainGenerator.MorphOrder, (TerrainGenerator.MorphShape)profile.MorphShape);
            if (index < 0)
                index = System.Array.IndexOf(TerrainGenerator.MorphOrder, TerrainGenerator.MorphShape.Dome);
            _dropdownShape.SetValueWithoutNotify(index);
            _dropdownShape.RefreshShownValue();
            _sliderAmount.Value = profile.MorphAmount;
        }
    }
}
