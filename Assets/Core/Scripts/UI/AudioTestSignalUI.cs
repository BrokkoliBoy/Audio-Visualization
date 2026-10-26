using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Gavi.Audio;

namespace Gavi
{
    /// <summary>
    /// Menu group of the <see cref="AudioTestSignal"/>. The settings of the signal (which signal, level, beat strength, beat from the song,
    /// freeze noise) are part of the terrain preset. Whether the signal is on is not: it is a tool to look at the effects and always starts switched off.
    /// The signal itself is the source of the values, so the preset can be read and shown while the group is closed.
    /// </summary>
    public class AudioTestSignalUI : TerrainGroupUI
    {
        [SerializeField] private AudioTestSignal _signal;
        [SerializeField] private Toggle _toggleEnabled;
        [SerializeField] private TMP_Dropdown _dropdownSignal;
        [SerializeField] private SliderG _sliderLevel;
        [SerializeField] private SliderG _sliderBeatStrength;
        [SerializeField] private Toggle _toggleBeatFromSong;
        [SerializeField] private Toggle _toggleFreezeNoise;
        [SerializeField] private Toggle _toggleEditBands;

        private bool _started;

        private void Awake()
        {
            _dropdownSignal.ClearOptions();
            _dropdownSignal.AddOptions(new List<string>(AudioTestSignal.SignalNames));

            _toggleEnabled.onValueChanged.AddListener(isOn => _signal.Enabled = isOn);
            _dropdownSignal.onValueChanged.AddListener(index => { _signal.SignalIndex = index; OnUIValueChanged(index); });
            _sliderLevel.OnValueChangedFloat.AddListener(value => { _signal.Level = value; OnUIValueChanged(value); });
            _sliderBeatStrength.OnValueChangedFloat.AddListener(value => { _signal.BeatStrength = value; OnUIValueChanged(value); });
            _toggleBeatFromSong.onValueChanged.AddListener(isOn => { _signal.BeatStrengthFromSong = isOn; OnUIValueChanged(isOn); });
            _toggleFreezeNoise.onValueChanged.AddListener(isOn => { _signal.FreezeNoise = isOn; OnUIValueChanged(isOn); });
            _toggleEditBands.onValueChanged.AddListener(isOn => _signal.EditBands = isOn);
        }

        private void Update()
        {
            // the hotkey switches the signal too, the toggle follows it
            if (_toggleEnabled.isOn != _signal.Enabled)
                _toggleEnabled.SetIsOnWithoutNotify(_signal.Enabled);
        }

        private void Start()
        {
            _started = true;
            ShowSignal();
        }

        // a preset can have been shown while the group was closed
        private void OnEnable()
        {
            if (_started)
                ShowSignal();
        }

        /// <summary>The menu shows what the signal is set to.</summary>
        private void ShowSignal()
        {
            _toggleEnabled.SetIsOnWithoutNotify(_signal.Enabled);
            _dropdownSignal.SetValueWithoutNotify(_signal.SignalIndex);
            _dropdownSignal.RefreshShownValue();
            _sliderLevel.Value = _signal.Level;
            _sliderBeatStrength.Value = _signal.BeatStrength;
            _toggleBeatFromSong.SetIsOnWithoutNotify(_signal.BeatStrengthFromSong);
            _toggleFreezeNoise.SetIsOnWithoutNotify(_signal.FreezeNoise);
            _toggleEditBands.SetIsOnWithoutNotify(_signal.EditBands);
        }

        public override void ExtractPreset(TerrainSettingsPreset preset)
        {
            if (_signal == null)
                return;
            base.ExtractPreset(preset);
            preset.TestSignal = _signal.SignalIndex;
            preset.TestSignalLevel = _signal.Level;
            preset.TestSignalBeatStrength = _signal.BeatStrength;
            preset.TestSignalBeatFromSong = _signal.BeatStrengthFromSong;
            preset.TestSignalFreezeNoise = _signal.FreezeNoise;
        }

        public override void ShowPreset(TerrainSettingsPreset preset)
        {
            if (_signal == null)
                return;
            base.ShowPreset(preset);
            _signal.SignalIndex = preset.TestSignal;
            _signal.Level = preset.TestSignalLevel;
            _signal.BeatStrength = preset.TestSignalBeatStrength;
            _signal.BeatStrengthFromSong = preset.TestSignalBeatFromSong;
            _signal.FreezeNoise = preset.TestSignalFreezeNoise;
            // before the group woke up (or while it is closed) the menu shows the signal later
            if (_started && isActiveAndEnabled)
                ShowSignal();
        }
    }
}
