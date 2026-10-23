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
        [Tooltip("The noise is calculated again every this many mesh updates.")]
        [SerializeField] private SliderG _sliderNoiseInterval;
        [Tooltip("The noise work is spread over the updates instead of done all at once every n-th update.")]
        [SerializeField] private Toggle _toggleNoiseSpread;
        [Tooltip("Frame rate limit of the whole app, from 30 up to the refresh rate of the monitor. Saved on this computer, not part of a profile.")]
        [SerializeField] private SliderG _sliderMaxFps;
        [Tooltip("Seconds the pointer must rest on a folded menu group before it opens. Saved on this computer, not part of a profile.")]
        [SerializeField] private SliderG _sliderMenuOpenDelay;
        [Tooltip("The same when the pointer comes from another group opened by hovering. 0 = switch straight through the groups.")]
        [SerializeField] private SliderG _sliderMenuSwitchDelay;
        [SerializeField] private TextMeshProUGUI _textReadout;
        [Tooltip("Seconds over which the frame rate is averaged.")]
        [SerializeField] private float _measureSeconds = 0.5f;

        private float _elapsed;

        private void Awake()
        {
            _sliderNoiseInterval.OnValueChangedInt.AddListener(OnUIValueChanged);
            _toggleNoiseSpread.onValueChanged.AddListener(OnUIValueChanged);

            // The limit belongs to the computer, so it does not go through the profile.
            _sliderMaxFps.SetRange(FrameRateSettings.MinFps, FrameRateSettings.MonitorHz);
            _sliderMaxFps.ValueInt = FrameRateSettings.MaxFps;
            _sliderMaxFps.OnValueChangedInt.AddListener(value => FrameRateSettings.MaxFps = value);

            _sliderMenuOpenDelay.SetRange(0f, MenuHoverSettings.MaxDelay);
            _sliderMenuOpenDelay.Value = MenuHoverSettings.OpenDelay;
            _sliderMenuOpenDelay.OnValueChangedFloat.AddListener(value => MenuHoverSettings.OpenDelay = value);
            _sliderMenuSwitchDelay.SetRange(0f, MenuHoverSettings.MaxDelay);
            _sliderMenuSwitchDelay.Value = MenuHoverSettings.SwitchDelay;
            _sliderMenuSwitchDelay.OnValueChangedFloat.AddListener(value => MenuHoverSettings.SwitchDelay = value);
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
            profile.NoiseInterval = _sliderNoiseInterval.ValueInt;
            profile.NoiseSpread = _toggleNoiseSpread.isOn;
        }

        public override void ShowProfile(TerrainSettingsProfile profile)
        {
            base.ShowProfile(profile);
            _sliderNoiseInterval.ValueInt = profile.NoiseInterval;
            _toggleNoiseSpread.isOn = profile.NoiseSpread;
        }
    }
}
