using UnityEngine;
using TMPro;

namespace Gavi.Utility
{
    /// <summary>
    /// Shows the frame rate in a text and points to the detail slider when it stays low. Only runs while its object is
    /// active, put it into the menu next to the detail slider.
    /// </summary>
    public class FpsHint : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text;
        [Tooltip("The frame rate counts as low below this value ...")]
        [SerializeField] private float _lowBelow = 60f;
        [Tooltip("... and as very low below this value.")]
        [SerializeField] private float _veryLowBelow = 30f;
        [Tooltip("The hint appears when the frame rate stayed low for this long, so short stutters do not trigger it.")]
        [SerializeField] private float _lowSeconds = 1.5f;
        [Tooltip("Seconds over which the frame rate is averaged.")]
        [SerializeField] private float _measureSeconds = 0.5f;
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _lowColor = new Color(1f, 0.82f, 0.35f);
        [SerializeField] private Color _veryLowColor = new Color(1f, 0.45f, 0.4f);
        [SerializeField] private string _lowHint = " - less detail helps";
        [SerializeField] private string _veryLowHint = " - lower the detail!";

        /// <summary>Fastest pattern speed right now, set by the terrain. A fast pattern is the likely cause of a slightly low frame rate, so the
        /// speed hint speaks first and the hint for less detail waits for a very low frame rate.</summary>
        public static float HighestSpeed;
        public const float SpeedBlamedFrom = 2f;

        private int _frames;
        private float _elapsed;
        private float _lowFor;
        private float _veryLowFor;

        private void OnEnable()
        {
            _frames = 0;
            _elapsed = 0f;
            _lowFor = 0f;
            _veryLowFor = 0f;
        }

        private void Update()
        {
            _frames++;
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed < _measureSeconds)
                return;

            float fps = _frames / _elapsed;
            _lowFor = fps < _lowBelow ? _lowFor + _elapsed : 0f;
            _veryLowFor = fps < _veryLowBelow ? _veryLowFor + _elapsed : 0f;
            _frames = 0;
            _elapsed = 0f;

            string hint = "";
            Color color = _normalColor;
            if (_veryLowFor >= _lowSeconds)
            {
                hint = _veryLowHint;
                color = _veryLowColor;
            }
            else if (_lowFor >= _lowSeconds && HighestSpeed <= SpeedBlamedFrom)
            {
                hint = _lowHint;
                color = _lowColor;
            }

            if (_text != null)
            {
                _text.text = "FPS: " + Mathf.RoundToInt(fps) + hint;
                _text.color = color;
            }
        }
    }
}
