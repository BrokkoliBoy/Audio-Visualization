using Gavi.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gavi
{
    /// <summary>
    /// Speed settings of the terrain: how often the noise and the mesh are calculated and in what detail. The text shows what the
    /// mesh update costs right now, so a setting can be judged by its saving and by eye.
    /// </summary>
    public class TerrainGroupUIPerformance : TerrainGroupUI
    {
        [Tooltip("The noise is calculated again every this many mesh updates.")]
        [SerializeField] private SliderG _sliderNoiseInterval;
        [Tooltip("The noise is calculated for every this-many-th vertex.")]
        [SerializeField] private SliderG _sliderNoiseStep;
        [Tooltip("The mesh is updated this often per second at most.")]
        [SerializeField] private SliderG _sliderMeshRate;
        [Tooltip("The vertices between the noise points follow a smooth curve instead of straight lines (only with Noise step above 1).")]
        [SerializeField] private Toggle _toggleNoiseSmooth;
        [Tooltip("The noise work is spread over the updates instead of done all at once every n-th update.")]
        [SerializeField] private Toggle _toggleNoiseSpread;
        [Tooltip("Frame rate limit of the whole app, from 30 up to the refresh rate of the monitor. Saved on this computer, not part of a profile.")]
        [SerializeField] private SliderG _sliderMaxFps;
        [SerializeField] private TextMeshProUGUI _textReadout;
        [Tooltip("Seconds over which the frame rate is averaged.")]
        [SerializeField] private float _measureSeconds = 0.5f;

        private int _frames;
        private float _elapsed;

        private void Awake()
        {
            _sliderNoiseInterval.OnValueChangedInt.AddListener(OnUIValueChanged);
            _sliderNoiseStep.OnValueChangedInt.AddListener(OnUIValueChanged);
            _sliderMeshRate.OnValueChangedInt.AddListener(OnUIValueChanged);
            _toggleNoiseSmooth.onValueChanged.AddListener(OnUIValueChanged);
            _toggleNoiseSpread.onValueChanged.AddListener(OnUIValueChanged);

            // The limit belongs to the computer, so it does not go through the profile.
            _sliderMaxFps.SetRange(FrameRateSettings.MinFps, FrameRateSettings.MonitorHz);
            _sliderMaxFps.ValueInt = FrameRateSettings.MaxFps;
            _sliderMaxFps.OnValueChangedInt.AddListener(value => FrameRateSettings.MaxFps = value);
        }

        private void OnEnable()
        {
            _frames = 0;
            _elapsed = 0f;
        }

        private void Update()
        {
            _frames++;
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed < _measureSeconds)
                return;

            float fps = _frames / _elapsed;
            _frames = 0;
            _elapsed = 0f;

            TerrainGenerator terrain = TerrainSettingsManager.Instance != null ? TerrainSettingsManager.Instance.Terrain : null;
            if (_textReadout == null || terrain == null)
                return;
            _textReadout.text = Mathf.RoundToInt(fps) + " FPS | mesh " + terrain.UpdateMilliseconds.ToString("F1") + " ms";
        }

        public override void ExtractProfile(TerrainSettingsProfile profile)
        {
            base.ExtractProfile(profile);
            profile.NoiseInterval = _sliderNoiseInterval.ValueInt;
            profile.NoiseStep = _sliderNoiseStep.ValueInt;
            profile.MeshUpdateRate = _sliderMeshRate.ValueInt;
            profile.NoiseSmooth = _toggleNoiseSmooth.isOn;
            profile.NoiseSpread = _toggleNoiseSpread.isOn;
        }

        public override void ShowProfile(TerrainSettingsProfile profile)
        {
            base.ShowProfile(profile);
            _sliderNoiseInterval.ValueInt = profile.NoiseInterval;
            _sliderNoiseStep.ValueInt = profile.NoiseStep;
            _sliderMeshRate.ValueInt = profile.MeshUpdateRate;
            _toggleNoiseSmooth.isOn = profile.NoiseSmooth;
            _toggleNoiseSpread.isOn = profile.NoiseSpread;
        }
    }
}
