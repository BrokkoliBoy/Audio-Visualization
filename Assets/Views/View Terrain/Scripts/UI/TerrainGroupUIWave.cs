using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gavi
{
    /// <summary>Waves that run over the terrain on the beat. The whole module can be switched off with the header toggle.</summary>
    public class TerrainGroupUIWave : TerrainGroupUI
    {
        [SerializeField] private Toggle _toggleApplyWave;
        [SerializeField] private SliderG _sliderWaveHeight;
        [SerializeField] private SliderG _sliderWaveSpeed;
        [SerializeField] private SliderG _sliderWaveWidth;
        [SerializeField] private SliderG _sliderWaveFade;
        [SerializeField] private TMP_Dropdown _dropdownWaveMode;
        [SerializeField] private TMP_Dropdown _dropdownWaveOrigin;
        [SerializeField] private Toggle _toggleUseNoise;
        [SerializeField] private SliderG _sliderEveryNthBeat;

        private void Awake()
        {
            if (_toggleUseNoise != null)
                _toggleUseNoise.onValueChanged.AddListener(OnUIValueChanged);
            _toggleApplyWave.onValueChanged.AddListener(OnUIValueChanged);
            _sliderWaveHeight.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderWaveSpeed.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderWaveWidth.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderWaveFade.OnValueChangedFloat.AddListener(OnUIValueChanged);
            if (_sliderEveryNthBeat != null)
                _sliderEveryNthBeat.OnValueChangedFloat.AddListener(OnUIValueChanged);
            SetupDropdown(_dropdownWaveMode, TerrainBeatWave.ModeNames);
            SetupDropdown(_dropdownWaveOrigin, TerrainBeatWave.OriginNames);
        }

        private void SetupDropdown(TMP_Dropdown dropdown, string[] names)
        {
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(names));
            dropdown.onValueChanged.AddListener(OnUIValueChanged);
        }

        public override void ExtractProfile(TerrainSettingsProfile profile)
        {
            base.ExtractProfile(profile);
            profile.ApplyWave = _toggleApplyWave.isOn;
            profile.WaveStrength = _sliderWaveHeight.Value;
            profile.WaveSpeed = _sliderWaveSpeed.Value;
            profile.WaveWidth = _sliderWaveWidth.Value;
            profile.WaveFade = _sliderWaveFade.Value;
            if (_sliderEveryNthBeat != null)
                profile.WaveEveryNthBeat = _sliderEveryNthBeat.ValueInt;
            profile.WaveMode = _dropdownWaveMode.value;
            profile.WaveOrigin = _dropdownWaveOrigin.value;
            if (_toggleUseNoise != null)
                profile.WaveUseNoise = _toggleUseNoise.isOn;
        }

        public override void ShowProfile(TerrainSettingsProfile profile)
        {
            base.ShowProfile(profile);
            _toggleApplyWave.isOn = profile.ApplyWave;
            _sliderWaveHeight.Value = profile.WaveStrength;
            _sliderWaveSpeed.Value = profile.WaveSpeed;
            _sliderWaveWidth.Value = profile.WaveWidth;
            _sliderWaveFade.Value = profile.WaveFade;
            if (_sliderEveryNthBeat != null)
                _sliderEveryNthBeat.Value = profile.WaveEveryNthBeat;
            ShowDropdown(_dropdownWaveMode, profile.WaveMode, TerrainBeatWave.ModeNames.Length);
            ShowDropdown(_dropdownWaveOrigin, profile.WaveOrigin, TerrainBeatWave.OriginNames.Length);
            if (_toggleUseNoise != null)
                _toggleUseNoise.isOn = profile.WaveUseNoise;
        }

        private void ShowDropdown(TMP_Dropdown dropdown, int value, int optionCount)
        {
            dropdown.SetValueWithoutNotify(Mathf.Clamp(value, 0, optionCount - 1));
            dropdown.RefreshShownValue();
        }
    }
}
