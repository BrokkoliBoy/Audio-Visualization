using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Gavi.Audio;

namespace Gavi
{
    /// <summary>
    /// Menu group of the <see cref="AudioTestSignal"/>. It is not part of the terrain profile, the test signal is only
    /// a tool to look at the effects and always starts switched off.
    /// </summary>
    public class AudioTestSignalUI : MonoBehaviour
    {
        [SerializeField] private AudioTestSignal _signal;
        [SerializeField] private Toggle _toggleEnabled;
        [SerializeField] private TMP_Dropdown _dropdownSignal;
        [SerializeField] private SliderG _sliderLevel;
        [SerializeField] private Toggle _toggleBeat;
        [SerializeField] private SliderG _sliderBpm;
        [SerializeField] private Toggle _toggleFreezeNoise;

        private void Awake()
        {
            _dropdownSignal.ClearOptions();
            _dropdownSignal.AddOptions(new List<string>(AudioTestSignal.SignalNames));

            _toggleEnabled.onValueChanged.AddListener(isOn => _signal.Enabled = isOn);
            _dropdownSignal.onValueChanged.AddListener(index => _signal.SignalIndex = index);
            _sliderLevel.OnValueChangedFloat.AddListener(value => _signal.Level = value);
            _toggleBeat.onValueChanged.AddListener(isOn => _signal.Beat = isOn);
            _sliderBpm.OnValueChangedFloat.AddListener(value => _signal.Bpm = value);
            _toggleFreezeNoise.onValueChanged.AddListener(isOn => _signal.FreezeNoise = isOn);
        }

        private void Start()
        {
            // the menu shows what the signal starts with
            _toggleEnabled.SetIsOnWithoutNotify(_signal.Enabled);
            _dropdownSignal.SetValueWithoutNotify(_signal.SignalIndex);
            _dropdownSignal.RefreshShownValue();
            _sliderLevel.Value = _signal.Level;
            _toggleBeat.SetIsOnWithoutNotify(_signal.Beat);
            _sliderBpm.Value = _signal.Bpm;
            _toggleFreezeNoise.SetIsOnWithoutNotify(_signal.FreezeNoise);
        }
    }
}
