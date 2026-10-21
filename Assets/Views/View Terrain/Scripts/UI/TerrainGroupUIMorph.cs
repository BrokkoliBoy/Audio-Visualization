using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gavi
{
    /// <summary>Bends the whole mesh (bowl, planet, tube ...). The header toggle switches the morph on and off.</summary>
    public class TerrainGroupUIMorph : TerrainGroupUI
    {
        [SerializeField] private Toggle _toggleApplyMorph;
        [SerializeField] private TMP_Dropdown _dropdownShape;
        [SerializeField] private SliderG _sliderAmount;
        [SerializeField] private SliderG _sliderBeat;

        private void Awake()
        {
            _dropdownShape.ClearOptions();
            _dropdownShape.AddOptions(new List<string>(TerrainGenerator.MorphNames));
            _dropdownShape.onValueChanged.AddListener(OnUIValueChanged);
            _toggleApplyMorph.onValueChanged.AddListener(OnUIValueChanged);
            _sliderAmount.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderBeat.OnValueChangedFloat.AddListener(OnUIValueChanged);
        }

        public override void ExtractProfile(TerrainSettingsProfile profile)
        {
            base.ExtractProfile(profile);
            profile.ApplyMorph = _toggleApplyMorph.isOn;
            profile.MorphShape = _dropdownShape.value;
            profile.MorphAmount = _sliderAmount.Value;
            profile.MorphBeat = _sliderBeat.Value;
        }

        public override void ShowProfile(TerrainSettingsProfile profile)
        {
            base.ShowProfile(profile);
            _toggleApplyMorph.isOn = profile.ApplyMorph;
            _dropdownShape.SetValueWithoutNotify(Mathf.Clamp(profile.MorphShape, 0, TerrainGenerator.MorphNames.Length - 1));
            _dropdownShape.RefreshShownValue();
            _sliderAmount.Value = profile.MorphAmount;
            _sliderBeat.Value = profile.MorphBeat;
        }
    }
}
