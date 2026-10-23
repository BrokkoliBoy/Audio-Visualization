using Gavi.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gavi
{
    /// <summary>
    /// Speed settings of the terrain in the General tab: the frame rate limit and what the mesh update costs. The text
    /// shows what the mesh update costs right now, so a setting can be judged by its saving and by eye.
    /// </summary>
    public class TerrainGroupUIPerformance : TerrainGroupUI
    {
        [Tooltip("Optional (dropped from the menu).")]
        [SerializeField] private SliderG _sliderNoiseInterval;
        [Tooltip("Optional (dropped from the menu).")]
        [SerializeField] private Toggle _toggleNoiseSpread;
        [Tooltip("Frame rate limit of the whole app, from 30 up to the refresh rate of the monitor. Saved on this computer, not part of a profile.")]
        [SerializeField] private SliderG _sliderMaxFps;
        [SerializeField] private TextMeshProUGUI _textReadout;
        [Tooltip("Seconds over which the frame rate is averaged.")]
        [SerializeField] private float _measureSeconds = 0.5f;

        private float _elapsed;

        private void Awake()
        {
            if (_sliderNoiseInterval != null)
                _sliderNoiseInterval.OnValueChangedInt.AddListener(OnUIValueChanged);
            if (_toggleNoiseSpread != null)
                _toggleNoiseSpread.onValueChanged.AddListener(OnUIValueChanged);

            // The limit belongs to the computer, so it does not go through the profile.
            _sliderMaxFps.SetRange(FrameRateSettings.MinFps, FrameRateSettings.MonitorHz);
            _sliderMaxFps.ValueInt = FrameRateSettings.MaxFps;
            _sliderMaxFps.OnValueChangedInt.AddListener(value => FrameRateSettings.MaxFps = value);
        }

        private void OnEnable()
        {
            _elapsed = 0f;
        }

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed < _measureSeconds)
                return;
            _elapsed = 0f;

            TerrainGenerator terrain = TerrainSettingsManager.Instance != null ? TerrainSettingsManager.Instance.Terrain : null;
            if (_textReadout == null || terrain == null)
                return;
            _textReadout.text = "Mesh update: " + terrain.UpdateMilliseconds.ToString("F1") + " ms";
        }

        public override void ExtractProfile(TerrainSettingsProfile profile)
        {
            base.ExtractProfile(profile);
            if (_sliderNoiseInterval != null)
                profile.NoiseInterval = _sliderNoiseInterval.ValueInt;
            if (_toggleNoiseSpread != null)
                profile.NoiseSpread = _toggleNoiseSpread.isOn;
        }

        public override void ShowProfile(TerrainSettingsProfile profile)
        {
            base.ShowProfile(profile);
            if (_sliderNoiseInterval != null)
                _sliderNoiseInterval.ValueInt = profile.NoiseInterval;
            if (_toggleNoiseSpread != null)
                _toggleNoiseSpread.isOn = profile.NoiseSpread;
        }
    }
}
