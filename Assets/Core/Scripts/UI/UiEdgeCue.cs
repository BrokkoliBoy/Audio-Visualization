using UnityEngine;

namespace Gavi
{
    /// <summary>
    /// A hint that a hidden menu exists: it breathes softly while the pointer is far away
    /// and wakes up and leans toward the pointer when it comes closer.
    /// Can fade out while the menu it belongs to is open.
    /// </summary>
    public class UiEdgeCue : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [Tooltip("Moved by the peek and bob animation.")]
        [SerializeField] private RectTransform _target;
        [Tooltip("Distance to the pointer is measured to this rect. Uses the target if empty.")]
        [SerializeField] private RectTransform _proximityRegion;

        [Header("--- Proximity ---")]
        [Range(0, 1)]
        [SerializeField] private float _idleAlpha = 0.45f;
        [Range(0, 1)]
        [SerializeField] private float _nearAlpha = 1f;
        [Tooltip("Within this distance (canvas reference pixels) the cue starts to react.")]
        [SerializeField] private float _nearDistance = 300f;
        [Tooltip("Offset in canvas pixels at full proximity.")]
        [SerializeField] private Vector2 _peekOffset = new Vector2(-10f, 0f);

        [Header("--- Idle animation ---")]
        [Range(0, 1)]
        [SerializeField] private float _pulseAmount = 0.2f;
        [SerializeField] private float _pulseSpeed = 2f;
        [Tooltip("Back and forth movement in canvas pixels while the pointer is far away.")]
        [SerializeField] private Vector2 _bobAmount = new Vector2(3f, 0f);

        [Header("--- Menu ---")]
        [SerializeField] private UiHoverReveal _reveal;
        [Tooltip("Fades the cue out while the menu is open.")]
        [SerializeField] private bool _hideWhileRevealed;
        [SerializeField] private float _fadeSpeed = 6f;

        [Header("--- Idle ---")]
        [Tooltip("Fades the cue out when the mouse was not moved for a while. It returns on the next movement. Key presses do not bring it back.")]
        [SerializeField] private bool _hideWhenIdle = true;
        [Tooltip("Uses the time after which the cursor is hidden (BasicUi), so cursor and cue disappear together.")]
        [SerializeField] private bool _followCursorHideTime = true;
        [SerializeField] private float _idleSeconds = 3f;

        private Vector2 _basePosition;
        private float _visibility = 1f;

        private void Awake()
        {
            if (_target == null)
                _target = transform as RectTransform;
            if (_proximityRegion == null)
                _proximityRegion = _target;
            if (_group == null)
                _group = GetComponent<CanvasGroup>();
            _basePosition = _target.anchoredPosition;
        }

        private void OnDisable()
        {
            if (_target != null)
                _target.anchoredPosition = _basePosition;
        }

        private void Update()
        {
            float scale = GetCanvasScale();
            float distance = UiPointer.DistanceTo(_proximityRegion);
            float proximity = distance == float.MaxValue ? 0f : 1f - Mathf.Clamp01(distance / Mathf.Max(1f, _nearDistance * scale));
            proximity *= proximity;

            float wave = Mathf.Sin(Time.unscaledTime * _pulseSpeed);
            float far = 1f - proximity;

            bool hidden = _hideWhileRevealed && _reveal != null && _reveal.IsRevealed;
            float idleLimit = _followCursorHideTime && Gavi.Player.BasicUi.Instance != null ? Gavi.Player.BasicUi.Instance.CursorHideSeconds : _idleSeconds;
            if (_hideWhenIdle && UiPointer.IdleSeconds > idleLimit)
                hidden = true;
            float targetVisibility = hidden ? 0f : 1f;
            _visibility = Mathf.MoveTowards(_visibility, targetVisibility, _fadeSpeed * Time.unscaledDeltaTime);

            if (_group != null)
            {
                float alpha = Mathf.Lerp(_idleAlpha, _nearAlpha, proximity) + wave * _pulseAmount * 0.5f * far * _idleAlpha;
                _group.alpha = Mathf.Clamp01(alpha) * _visibility;
            }

            _target.anchoredPosition = _basePosition + _peekOffset * proximity + _bobAmount * (wave * far);
        }

        private float GetCanvasScale()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            return canvas != null ? canvas.rootCanvas.scaleFactor : 1f;
        }
    }
}
