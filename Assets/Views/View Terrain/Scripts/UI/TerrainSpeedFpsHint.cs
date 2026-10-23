using Gavi.Utility;
using TMPro;
using UnityEngine;

namespace Gavi
{
    /// <summary>
    /// Sits on a group with a pattern speed slider. When the speed is high and the frame rate stays low it slides in a row below the
    /// slider that says to lower the speed (the rows below make room, the group grows). The soft hint for less detail
    /// (<see cref="FpsHint"/>) keeps quiet while the speed is high, only a very low frame rate still points to the detail.
    /// </summary>
    public class TerrainSpeedFpsHint : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text;
        [Tooltip("Rows below the hint row; they move down while the hint is shown.")]
        [SerializeField] private RectTransform[] _rowsBelow;
        [SerializeField] private UiGroupFold _fold;
        [SerializeField] private float _rowHeight = 25f;
        [Tooltip("The hint starts above this speed ...")]
        [SerializeField] private float _speedFrom = FpsHint.SpeedBlamedFrom;
        [Tooltip("... and from this speed on it already shows when the frame rate dips only a little.")]
        [SerializeField] private float _speedFromStrict = 3f;
        [Tooltip("Frame rate that counts as low from the first speed ...")]
        [SerializeField] private float _lowBelow = 45f;
        [Tooltip("... and from the strict speed on.")]
        [SerializeField] private float _lowBelowStrict = 57f;
        [Tooltip("Below this frame rate the text turns red.")]
        [SerializeField] private float _veryLowBelow = 30f;
        [Tooltip("The hint appears when the frame rate stayed low for this long, so short stutters do not trigger it.")]
        [SerializeField] private float _lowSeconds = 1.5f;
        [Tooltip("Seconds over which the frame rate is averaged.")]
        [SerializeField] private float _measureSeconds = 0.5f;
        [SerializeField] private float _slideSeconds = 0.2f;
        [SerializeField] private Color _lowColor = new Color(1f, 0.82f, 0.35f);
        [SerializeField] private Color _veryLowColor = new Color(1f, 0.45f, 0.4f);

        private TerrainGroupUIPerlin _perlin;
        private TerrainGroupUINoiseLayer _layer;
        private Vector2[] _rowsStart;
        private int _frames;
        private float _elapsed;
        private float _lowFor;
        private bool _veryLow;
        private float _shown;
        private float _shownVelocity;

        private void Awake()
        {
            _perlin = GetComponent<TerrainGroupUIPerlin>();
            _layer = GetComponent<TerrainGroupUINoiseLayer>();
            _rowsStart = new Vector2[_rowsBelow.Length];
            for (int i = 0; i < _rowsBelow.Length; i++)
                _rowsStart[i] = _rowsBelow[i].anchoredPosition;
        }

        private void OnEnable()
        {
            _frames = 0;
            _elapsed = 0f;
            _lowFor = 0f;
        }

        private void Update()
        {
            Measure();

            float target = _lowFor >= _lowSeconds ? 1f : 0f;
            _shown = Mathf.SmoothDamp(_shown, target, ref _shownVelocity, _slideSeconds, Mathf.Infinity, Time.unscaledDeltaTime);
            if (Mathf.Abs(_shown - target) < 0.002f)
            {
                _shown = target;
                _shownVelocity = 0f;
            }

            float offset = _shown * _rowHeight;
            for (int i = 0; i < _rowsBelow.Length; i++)
                _rowsBelow[i].anchoredPosition = _rowsStart[i] + new Vector2(0f, -offset);
            if (_fold != null)
                _fold.SetExtraOpenHeight(offset);

            if (_text != null)
            {
                Color color = _veryLow ? _veryLowColor : _lowColor;
                color.a = _shown;
                _text.color = color;
                _text.text = _veryLow ? "Very low FPS - lower the speed!" : "Low FPS - less speed helps";
            }
        }

        private void Measure()
        {
            _frames++;
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed < _measureSeconds)
                return;

            float fps = _frames / _elapsed;
            float speed = _perlin != null ? _perlin.ActiveSpeed : _layer != null ? _layer.ActiveSpeed : 0f;
            float lowBelow = speed >= _speedFromStrict ? _lowBelowStrict : _lowBelow;
            bool low = speed > _speedFrom && fps < lowBelow;
            _lowFor = low ? _lowFor + _elapsed : 0f;
            _veryLow = fps < _veryLowBelow;
            _frames = 0;
            _elapsed = 0f;
        }
    }
}
