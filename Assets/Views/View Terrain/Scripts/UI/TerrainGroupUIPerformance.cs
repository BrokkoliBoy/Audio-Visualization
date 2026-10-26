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
        [Tooltip("Frame rate limit of the whole app, from 30 up to the refresh rate of the monitor. Saved on this computer, not part of a preset.")]
        [SerializeField] private SliderG _sliderMaxFps;
        [SerializeField] private TextMeshProUGUI _textReadout;

        [Header("--- Speed of the patterns (copies of the speed sliders) ---")]
        [Tooltip("The pattern speeds next to the settings that decide the frame rate, so it is clear which speeds matter. Index 0 is the main pattern, then the layers.")]
        [SerializeField] private TerrainGroupUIPerlin _perlin;
        [SerializeField] private TerrainGroupUINoiseLayer[] _layers;
        [Tooltip("One copy per pattern, in the same order. A copy only shows while its pattern is on.")]
        [SerializeField] private SliderG[] _speedMirrors;
        [Tooltip("Rows below the speed copies; they move down for every copy shown.")]
        [SerializeField] private RectTransform[] _rowsBelow;
        [SerializeField] private UiGroupFold _fold;
        [SerializeField] private float _rowHeight = 25f;
        [SerializeField] private float _slideSeconds = 0.2f;

        [Header("--- Cost colors ---")]
        [Tooltip("Gradient of the detail copy, the interval slider and the speed copies: green is cheap, red is expensive.")]
        [SerializeField] private UiSliderGradient _gradientDetail;
        [SerializeField] private UiSliderGradient _gradientNoiseInterval;
        [SerializeField] private UiSliderGradient _gradientMaxFps;
        [SerializeField] private UiSliderGradient[] _gradientSpeeds;
        [SerializeField] private Color _cheapColor = new Color(0.08f, 0.62f, 0.22f);
        [SerializeField] private Color _midColor = new Color(0.92f, 0.66f, 0.04f);
        [SerializeField] private Color _costlyColor = new Color(0.84f, 0.17f, 0.1f);
        [Tooltip("End color of the detail slider: it is the setting that costs the most, so its red is deeper.")]
        [SerializeField] private Color _costlyDetailColor = new Color(0.5f, 0.02f, 0.05f);
        [Tooltip("Mesh detail from where it starts to cost frames, and where it is as costly as it gets.")]
        [SerializeField] private Vector2 _detailRange = new Vector2(50f, 650f);
        [Tooltip("Max FPS where the gradient is at its yellow peak, and from where it is fully red. Below the peak it fades up from green.")]
        [SerializeField] private Vector2 _fpsRange = new Vector2(120f, 180f);
        [Tooltip("Seconds over which the update time is refreshed.")]
        [SerializeField] private float _measureSeconds = 0.5f;

        private float _elapsed;
        private bool _readoutDirty;
        private float[] _shown;
        private float[] _shownVelocity;
        private float[] _rowBaseY;
        private float[] _belowBaseY;
        private CanvasGroup[] _mirrorFade;
        private bool _syncing;

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

            // The limit belongs to the computer, so it does not go through the preset.
            _sliderMaxFps.SetRange(FrameRateSettings.MinFps, FrameRateSettings.MonitorHz);
            _sliderMaxFps.ValueInt = FrameRateSettings.MaxFps;
            _sliderMaxFps.OnValueChangedInt.AddListener(value => FrameRateSettings.MaxFps = value);

            SetUpSpeedMirrors();
            SetUpGradients();
        }

        private void OnEnable()
        {
            _elapsed = 0f;
            _readoutDirty = true;
            SyncMirrorsFromSources();
            UpdateSpeedRows(true);
        }

        private void Update()
        {
            UpdateSpeedRows(false);
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
            _textReadout.text = "Mesh: " + (terrain.FaceCountX * terrain.FaceCountZ).ToString("N0") + " faces, " + terrain.UpdateMilliseconds.ToString("F1") + " ms";
        }

        #region Speed copies and cost colors
        private int PatternCount => 1 + (_layers != null ? _layers.Length : 0);

        private SliderG SourceSlider(int index) => index == 0 ? (_perlin != null ? _perlin.SpeedSlider : null) : _layers[index - 1].SpeedSlider;
        private bool SourceOn(int index) => index == 0 ? _perlin != null && _perlin.SpeedOn : _layers[index - 1].SpeedOn;

        private void SetUpSpeedMirrors()
        {
            if (_speedMirrors == null)
                return;
            int count = Mathf.Min(_speedMirrors.Length, PatternCount);
            _shown = new float[count];
            _shownVelocity = new float[count];
            _rowBaseY = new float[count];
            _mirrorFade = new CanvasGroup[count];
            for (int i = 0; i < count; i++)
            {
                SliderG mirror = _speedMirrors[i];
                SliderG source = SourceSlider(i);
                if (mirror == null || source == null)
                    continue;
                _rowBaseY[i] = ((RectTransform)mirror.transform).anchoredPosition.y;
                _mirrorFade[i] = mirror.GetComponent<CanvasGroup>();

                // Both sliders have the same range, setting the value the other one already has changes nothing, so the pair settles at once.
                mirror.OnValueChangedFloat.AddListener(value => CopyValue(value, source));
                source.OnValueChangedFloat.AddListener(value => CopyValue(value, mirror));
            }
            _belowBaseY = new float[_rowsBelow != null ? _rowsBelow.Length : 0];
            for (int i = 0; i < _belowBaseY.Length; i++)
                _belowBaseY[i] = _rowsBelow[i].anchoredPosition.y;
        }

        private void CopyValue(float value, SliderG target)
        {
            if (_syncing || Mathf.Approximately(target.Value, value))
                return;
            _syncing = true;
            target.Value = value;
            _syncing = false;
        }

        private void SyncMirrorsFromSources()
        {
            if (_shown == null)
                return;
            for (int i = 0; i < _shown.Length; i++)
            {
                SliderG source = SourceSlider(i);
                if (_speedMirrors[i] != null && source != null)
                    CopyValue(source.Value, _speedMirrors[i]);
            }
        }

        // A copy slides in (the rows below make room) while its pattern is on and slides out again when it is switched off.
        private void UpdateSpeedRows(bool instant)
        {
            if (_shown == null)
                return;
            // A folded group hides its children itself; a copy must not be switched on behind its back, and nothing needs to slide.
            bool open = _fold == null || _fold.IsOpen;
            instant |= !open;
            float used = 0f;
            for (int i = 0; i < _shown.Length; i++)
            {
                SliderG mirror = _speedMirrors[i];
                if (mirror == null)
                    continue;
                float target = SourceOn(i) ? 1f : 0f;
                if (instant)
                {
                    _shown[i] = target;
                    _shownVelocity[i] = 0f;
                }
                else
                {
                    _shown[i] = Mathf.SmoothDamp(_shown[i], target, ref _shownVelocity[i], _slideSeconds, Mathf.Infinity, Time.unscaledDeltaTime);
                    if (Mathf.Abs(_shown[i] - target) < 0.002f)
                    {
                        _shown[i] = target;
                        _shownVelocity[i] = 0f;
                    }
                }

                bool visible = _shown[i] > 0f;
                if (open && mirror.gameObject.activeSelf != visible)
                    mirror.gameObject.SetActive(visible);
                if (visible)
                {
                    ((RectTransform)mirror.transform).anchoredPosition = new Vector2(0f, _rowBaseY[0] - used);
                    if (_mirrorFade[i] != null)
                        _mirrorFade[i].alpha = _shown[i];
                }
                used += _shown[i] * _rowHeight;
            }

            for (int i = 0; i < _belowBaseY.Length; i++)
            {
                Vector2 position = _rowsBelow[i].anchoredPosition;
                position.y = _belowBaseY[i] - used;
                _rowsBelow[i].anchoredPosition = position;
            }
            if (_fold != null)
                _fold.SetExtraOpenHeight(used);
        }

        private void SetUpGradients()
        {
            if (_gradientDetail != null && _sliderDetail != null)
                _gradientDetail.Bind(_sliderDetail, position => CostColor(DetailCost(_sliderDetail.ValueAtPosition(position)), _costlyDetailColor));
            if (_gradientNoiseInterval != null && _sliderNoiseInterval != null)
                _gradientNoiseInterval.Bind(_sliderNoiseInterval, position => CostColor(IntervalCost(_sliderNoiseInterval.ValueAtPosition(position))));
            if (_gradientMaxFps != null && _sliderMaxFps != null)
                _gradientMaxFps.Bind(_sliderMaxFps, position => CostColor(FpsCost(_sliderMaxFps.ValueAtPosition(position))));
            if (_gradientSpeeds == null || _speedMirrors == null)
                return;
            for (int i = 0; i < _gradientSpeeds.Length && i < _speedMirrors.Length; i++)
            {
                SliderG mirror = _speedMirrors[i];
                if (_gradientSpeeds[i] != null && mirror != null)
                    _gradientSpeeds[i].Bind(mirror, position => CostColor(SpeedCost(mirror.ValueAtPosition(position))));
            }
        }

        // Cost 0 = free, 1 = as costly as it gets.
        private float DetailCost(float detail) => Mathf.Clamp01((detail - _detailRange.x) / Mathf.Max(1f, _detailRange.y - _detailRange.x));

        // A high frame rate limit lets the app use more of the computer: green at the lowest limit, yellow at the peak, red from the upper end.
        private float FpsCost(float fps)
        {
            if (fps <= _fpsRange.x)
                return Mathf.Clamp01((fps - FrameRateSettings.MinFps) / Mathf.Max(1f, _fpsRange.x - FrameRateSettings.MinFps)) * 0.5f;
            return 0.5f + Mathf.Clamp01((fps - _fpsRange.x) / Mathf.Max(1f, _fpsRange.y - _fpsRange.x)) * 0.5f;
        }

        // The noise is calculated every n-th update, so a high interval saves the most.
        private static float IntervalCost(float interval) => Mathf.Clamp01(1f - (interval - 1f) / (TerrainGenerator.MaxNoiseInterval - 1f));

        // Below the first limit a pattern speed costs nothing extra, from there the interval is forced down (see TerrainGenerator.MaxIntervalForSpeed).
        private static float SpeedCost(float speed)
        {
            if (speed <= 1.3f) return 0f;
            if (speed <= 2.3f) return (speed - 1.3f) * 0.5f;
            return Mathf.Clamp01(0.5f + (speed - 2.3f) * 0.5f);
        }

        private Color CostColor(float cost) => CostColor(cost, _costlyColor);

        private Color CostColor(float cost, Color costly) => cost < 0.5f ? Color.Lerp(_cheapColor, _midColor, cost * 2f) : Color.Lerp(_midColor, costly, (cost - 0.5f) * 2f);
        #endregion

        public override void ExtractPreset(TerrainSettingsPreset preset)
        {
            base.ExtractPreset(preset);
            if (_sliderNoiseInterval != null)
                preset.NoiseInterval = _sliderNoiseInterval.ValueInt;
            if (_toggleNoiseSpread != null)
                preset.NoiseSpread = _toggleNoiseSpread.isOn;
        }

        public override void ShowPreset(TerrainSettingsPreset preset)
        {
            base.ShowPreset(preset);
            if (_sliderNoiseInterval != null)
                _sliderNoiseInterval.ValueInt = preset.NoiseInterval;
            if (_toggleNoiseSpread != null)
                _toggleNoiseSpread.isOn = preset.NoiseSpread;
        }
    }
}
