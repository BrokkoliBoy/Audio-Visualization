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
        [Tooltip("Upper limit of Noise every: the noise pattern is calculated only every n-th mesh update, in between it blends from the last result. The interval used is lowered on its own for fast patterns.")]
        [SerializeField] private SliderG _sliderNoiseInterval;
        [Tooltip("Optional (dropped from the menu).")]
        [SerializeField] private Toggle _toggleNoiseSpread;
        [Tooltip("Copy of the detail slider of the Terrain window (same settings). Both always show the same value.")]
        [SerializeField] private SliderG _sliderDetail;
        [SerializeField] private SliderG _sliderDetailMain;
        [Tooltip("Frame rate limit of the whole app, from 30 up to the refresh rate of the monitor. Saved on this computer, not part of a profile.")]
        [SerializeField] private SliderG _sliderMaxFps;
        [SerializeField] private TextMeshProUGUI _textReadout;
        [Tooltip("Seconds over which the update time is refreshed.")]
        [SerializeField] private float _measureSeconds = 0.5f;

        private float _elapsed;
        private bool _readoutDirty;

        private void Awake()
        {
            if (_sliderNoiseInterval != null)
                _sliderNoiseInterval.OnValueChangedInt.AddListener(OnUIValueChanged);
            if (_toggleNoiseSpread != null)
                _toggleNoiseSpread.onValueChanged.AddListener(OnUIValueChanged);

            // The detail slider exists twice; setting the same value again changes nothing, so the two settle at once.
            if (_sliderDetail != null && _sliderDetailMain != null)
            {
                _sliderDetail.ValueInt = _sliderDetailMain.ValueInt;
                _sliderDetail.OnValueChangedInt.AddListener(value => _sliderDetailMain.ValueInt = value);
                _sliderDetailMain.OnValueChangedInt.AddListener(value => _sliderDetail.ValueInt = value);
            }
            // The face count in the text follows a detail change at once, not with the next refresh.
            if (_sliderDetail != null)
                _sliderDetail.OnValueChangedInt.AddListener(_ => _readoutDirty = true);
            if (_sliderDetailMain != null)
                _sliderDetailMain.OnValueChangedInt.AddListener(_ => _readoutDirty = true);

            // The limit belongs to the computer, so it does not go through the profile.
            _sliderMaxFps.SetRange(FrameRateSettings.MinFps, FrameRateSettings.MonitorHz);
            _sliderMaxFps.ValueInt = FrameRateSettings.MaxFps;
            _sliderMaxFps.OnValueChangedInt.AddListener(value => FrameRateSettings.MaxFps = value);
        }

        private void OnEnable()
        {
            _elapsed = 0f;
            _readoutDirty = true;
        }

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed < _measureSeconds)
                return;
            _elapsed = 0f;
            UpdateReadout();
        }

        // After the other scripts of this frame, so the new face count is already applied to the terrain.
        private void LateUpdate()
        {
            if (!_readoutDirty)
                return;
            _readoutDirty = false;
            UpdateReadout();
        }

        private void UpdateReadout()
        {
            TerrainGenerator terrain = TerrainSettingsManager.Instance != null ? TerrainSettingsManager.Instance.Terrain : null;
            if (_textReadout == null || terrain == null)
                return;
            _textReadout.text = "Mesh: " + terrain.FaceCountX + " x " + terrain.FaceCountX + " faces, " + terrain.UpdateMilliseconds.ToString("F1") + " ms";
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
