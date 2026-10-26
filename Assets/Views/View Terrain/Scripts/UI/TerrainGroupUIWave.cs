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

        public override void ExtractPreset(TerrainSettingsPreset preset)
        {
            base.ExtractPreset(preset);
            preset.ApplyWave = _toggleApplyWave.isOn;
            preset.WaveStrength = _sliderWaveHeight.Value;
            preset.WaveSpeed = _sliderWaveSpeed.Value;
            preset.WaveWidth = _sliderWaveWidth.Value;
            preset.WaveFade = _sliderWaveFade.Value;
            if (_sliderEveryNthBeat != null)
                preset.WaveEveryNthBeat = _sliderEveryNthBeat.ValueInt;
            preset.WaveMode = _dropdownWaveMode.value;
            preset.WaveOrigin = _dropdownWaveOrigin.value;
            if (_toggleUseNoise != null)
                preset.WaveUseNoise = _toggleUseNoise.isOn;
        }

        public override void ShowPreset(TerrainSettingsPreset preset)
        {
            base.ShowPreset(preset);
            _toggleApplyWave.isOn = preset.ApplyWave;
            _sliderWaveHeight.Value = preset.WaveStrength;
            _sliderWaveSpeed.Value = preset.WaveSpeed;
            _sliderWaveWidth.Value = preset.WaveWidth;
            _sliderWaveFade.Value = preset.WaveFade;
            if (_sliderEveryNthBeat != null)
                _sliderEveryNthBeat.Value = preset.WaveEveryNthBeat;
            ShowDropdown(_dropdownWaveMode, preset.WaveMode, TerrainBeatWave.ModeNames.Length);
            ShowDropdown(_dropdownWaveOrigin, preset.WaveOrigin, TerrainBeatWave.OriginNames.Length);
            if (_toggleUseNoise != null)
                _toggleUseNoise.isOn = preset.WaveUseNoise;
        }

        private void ShowDropdown(TMP_Dropdown dropdown, int value, int optionCount)
        {
            dropdown.SetValueWithoutNotify(Mathf.Clamp(value, 0, optionCount - 1));
            dropdown.RefreshShownValue();
        }
    }
}
