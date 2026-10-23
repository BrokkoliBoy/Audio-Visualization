using UnityEngine;
using Side = Gavi.UiDipDetector.Side;
using UnityEngine.UI;

namespace Gavi
{
    /// <summary>
    /// Switches a tick box without a click: a quick dip of the pointer into the box and back out on the same side counts as a click,
    /// and so do two quick swipes straight through it, one each way.
    /// Only the box itself reacts (no margin). Resting on the box, passing through it, only clipping its corner or a real click
    /// (which switches it by itself) switch nothing.
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public class UiToggleDip : MonoBehaviour
    {
        [Tooltip("A dip must be over after this long (s), a pointer that stays longer is just resting.")]
        [SerializeField] private float _maxDipTime = 0.4f;
        [Tooltip("A swipe through the box must be over after this long (s).")]
        [SerializeField] private float _maxSwipeTime = 0.3f;
        [Tooltip("How far a dip has to reach into the box, as a share of the box size, so clipping a corner does not count.")]
        [SerializeField] private float _minDepth = 0.3f;
        [Tooltip("Pause (s) after a switch before the next dip counts.")]
        [SerializeField] private float _cooldown = 0.2f;
        [Tooltip("Two swipes straight through the box in opposite directions within this time (s) switch it.")]
        [SerializeField] private float _swipeWindow = 0.6f;


        private Toggle _toggle;
        private RectTransform _box;
        private UiHoverReveal _reveal;
        private bool _inside;
        private bool _hasLast;
        private Vector2 _last;
        private Vector2 _lastBoxMin;
        private Side _entrySide;
        private float _entryTime;
        private float _depth;
        private bool _valueAtEntry;
        private bool _spoiled;
        private float _blockedUntil;
        private int _swipeDirection;
        private float _swipeTime;

        private void Awake()
        {
            _toggle = GetComponent<Toggle>();
            _reveal = GetComponentInParent<UiHoverReveal>();
            Graphic graphic = _toggle.targetGraphic;
            _box = graphic != null ? graphic.rectTransform : (RectTransform)transform;
        }

        private void OnDisable()
        {
            _inside = false;
            _hasLast = false;
        }

        private void Update()
        {
            bool usable = _toggle.interactable && !UiPointer.IsLocked && UiPointer.IsInsideWindow
                && (_reveal == null || _reveal.IsRevealed);
            Rect rect = UiPointer.GetScreenRect(_box, Vector4.zero);
            Vector2 point = UiPointer.Position;

            // a box that moves (menu or window sliding) is not a target
            bool moving = _hasLast && (rect.min - _lastBoxMin).sqrMagnitude > 1f;
            _lastBoxMin = rect.min;

            if (!usable || moving)
            {
                _inside = false;
                _last = point;
                _hasLast = usable;
                return;
            }

            bool inside = rect.Contains(point);
            if (inside && !_inside)
            {
                // entered: only counts when it came from outside
                if (_hasLast && !rect.Contains(_last) && Time.unscaledTime >= _blockedUntil)
                {
                    _inside = true;
                    _entrySide = UiDipDetector.SideOf(rect, _last);
                    _entryTime = Time.unscaledTime;
                    _depth = 0f;
                    _valueAtEntry = _toggle.isOn;
                    _spoiled = false;
                }
            }
            else if (inside && _inside)
            {
                _depth = Mathf.Max(_depth, UiDipDetector.Depth(rect, point, _entrySide));
                // a click, or anything that changed the box meanwhile, takes over
                if (Input.GetMouseButton(0) || _toggle.isOn != _valueAtEntry)
                    _spoiled = true;
            }
            else if (!inside && _inside)
            {
                _inside = false;
                float dwell = Time.unscaledTime - _entryTime;
                Side exitSide = UiDipDetector.SideOf(rect, point);
                bool clean = !_spoiled && !Input.GetMouseButton(0) && _toggle.isOn == _valueAtEntry;
                bool dip = clean && dwell <= _maxDipTime && exitSide == _entrySide && _depth >= _minDepth;
                // a swipe straight through the box; two of them in opposite directions in a row switch it, without hitting the turn
                int direction = _entrySide == Side.Left && exitSide == Side.Right ? 1 : _entrySide == Side.Right && exitSide == Side.Left ? -1 : 0;
                bool swipe = false;
                if (clean && dwell <= _maxSwipeTime && direction != 0)
                {
                    swipe = _swipeDirection == -direction && Time.unscaledTime - _swipeTime <= _swipeWindow;
                    _swipeDirection = swipe ? 0 : direction;
                    _swipeTime = Time.unscaledTime;
                }
                else
                    _swipeDirection = 0;
                if ((dip || swipe) && !UiPointer.IsAnyDropdownExpanded())
                {
                    _toggle.isOn = !_toggle.isOn;
                    _swipeDirection = 0;
                    _blockedUntil = Time.unscaledTime + _cooldown;
                }
            }

            _last = point;
            _hasLast = true;
        }
    }
}
