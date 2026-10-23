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
        [Tooltip("Noise pattern is calculated only every n-th mesh update, in between it blends from the last result.")]
        [SerializeField] private SliderG _sliderNoiseInterval;
        [Tooltip("Optional (dropped from the menu).")]
        [SerializeField] private Toggle _toggleNoiseSpread;
        [Tooltip("Copy of the detail slider of the Terrain window (same settings). Both always show the same value.")]
        [SerializeField] private SliderG _sliderDetail;
        [SerializeField] private SliderG _sliderDetailMain;
        [Tooltip("Frame rate limit of the whole app, from 30 up to the refresh rate of the monitor. Saved on this computer, not part of a profile.")]
        [SerializeField] private SliderG _sliderMaxFps;
        [SerializeField] private TextMeshProUGUI _textReadout;
        [Tooltip("Says up to which Noise every the fastest pattern speed still looks right, turns orange when the slider is above that.")]
        [SerializeField] private TextMeshProUGUI _textSpeedHint;
        [Tooltip("Seconds over which the frame rate is averaged.")]
        [SerializeField] private float _measureSeconds = 0.5f;

        private float _elapsed;
        private TerrainGroupUIPerlin _perlin;
        private TerrainGroupUINoiseLayer[] _layers;

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

            UpdateSpeedHint();

            TerrainGenerator terrain = TerrainSettingsManager.Instance != null ? TerrainSettingsManager.Instance.Terrain : null;
            if (_textReadout == null || terrain == null)
                return;
            _textReadout.text = "Mesh: " + terrain.FaceCountX + " x " + terrain.FaceCountX + " faces, update " + terrain.UpdateMilliseconds.ToString("F1") + " ms";
        }

        /// <summary>Largest Noise every that still looks right for a pattern speed: moving patterns lag behind with a long interval.</summary>
        private static int MaxIntervalForSpeed(float speed)
        {
            if (speed >= 4f) return 1;
            if (speed >= 3f) return 2;
            if (speed >= 2f) return 3;
            return TerrainGenerator.MaxNoiseInterval;
        }

        private void UpdateSpeedHint()
        {
            if (_textSpeedHint == null || _sliderNoiseInterval == null)
                return;
            if (_perlin == null)
                _perlin = FindFirstObjectByType<TerrainGroupUIPerlin>(FindObjectsInactive.Include);
            if (_layers == null)
                _layers = FindObjectsByType<TerrainGroupUINoiseLayer>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            float speed = _perlin != null ? _perlin.ActiveSpeed : 0f;
            foreach (TerrainGroupUINoiseLayer layer in _layers)
                speed = Mathf.Max(speed, layer.ActiveSpeed);

            int allowed = MaxIntervalForSpeed(speed);
            bool tooHigh = _sliderNoiseInterval.ValueInt > allowed;
            _textSpeedHint.text = "Fastest pattern speed " + speed.ToString("0.#") + ": noise every up to " + allowed;
            _textSpeedHint.color = tooHigh ? new Color(1f, 0.65f, 0.2f) : new Color(1f, 1f, 1f, 0.45f);
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
