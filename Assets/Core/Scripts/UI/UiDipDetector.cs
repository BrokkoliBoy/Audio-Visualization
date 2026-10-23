using UnityEngine;

namespace Gavi
{
    /// <summary>
    /// Recognizes a dip of the pointer into a rect: it comes in from outside, reaches a good way in and leaves on the same side
    /// within a short time. Resting on the rect, passing through it, only clipping a corner or a click are no dip.
    /// </summary>
    public class UiDipDetector
    {
        public enum Side { None, Left, Right, Bottom, Top }

        private readonly float _maxDipTime;
        private readonly float _minDepth;
        private readonly float _cooldown;

        private bool _inside;
        private bool _hasLast;
        private Vector2 _last;
        private Vector2 _lastMin;
        private Side _entrySide;
        private float _entryTime;
        private float _depth;
        private bool _spoiled;
        private float _blockedUntil;
        private int _swipeDirection;
        private float _swipeTime;
        private const float _maxSwipeTime = 0.3f;
        private const float _swipeWindow = 0.6f;

        /// <summary>Sides the pointer may not come in from (e.g. where a neighbouring button sits). A dip from there is ignored.</summary>
        public bool[] BlockedSides = new bool[5];

        public UiDipDetector(float maxDipTime = 0.4f, float minDepth = 0.2f, float cooldown = 0.2f)
        {
            _maxDipTime = maxDipTime;
            _minDepth = minDepth;
            _cooldown = cooldown;
        }

        /// <summary>Forget a dip in progress, e.g. when the rect is switched off.</summary>
        public void Reset()
        {
            _inside = false;
            _hasLast = false;
            _swipeDirection = 0;
        }

        /// <summary>Call every frame. True in the frame the pointer leaves the rect after a dip.</summary>
        public bool Update(RectTransform box, bool usable)
        {
            Rect rect = UiPointer.GetScreenRect(box, Vector4.zero);
            Vector2 point = UiPointer.Position;

            // a rect that moves (menu or window sliding) is not a target
            bool moving = _hasLast && (rect.min - _lastMin).sqrMagnitude > 1f;
            _lastMin = rect.min;
            if (!usable || moving)
            {
                _inside = false;
                _last = point;
                _hasLast = usable;
                return false;
            }

            bool dip = false;
            bool inside = rect.Contains(point);
            if (inside && !_inside)
            {
                // entered: only counts when it came from outside
                if (_hasLast && !rect.Contains(_last) && Time.unscaledTime >= _blockedUntil && !BlockedSides[(int)SideOf(rect, _last)])
                {
                    _inside = true;
                    _entrySide = SideOf(rect, _last);
                    _entryTime = Time.unscaledTime;
                    _depth = 0f;
                    _spoiled = false;
                }
            }
            else if (inside && _inside)
            {
                _depth = Mathf.Max(_depth, Depth(rect, point, _entrySide));
                if (Input.GetMouseButton(0))
                    _spoiled = true;
            }
            else if (!inside && _inside)
            {
                _inside = false;
                float dwell = Time.unscaledTime - _entryTime;
                Side exitSide = SideOf(rect, point);
                bool clean = !_spoiled && !Input.GetMouseButton(0);
                dip = clean && dwell <= _maxDipTime && exitSide == _entrySide && _depth >= _minDepth;

                // A swipe straight through the rect; two of them in opposite directions in a row count as a dip. Both sides must be
                // free, otherwise the swipe would hit the neighbour as well.
                int direction = SwipeDirection(_entrySide, exitSide);
                if (clean && direction != 0 && dwell <= _maxSwipeTime && !BlockedSides[(int)exitSide])
                {
                    dip |= _swipeDirection == -direction && Time.unscaledTime - _swipeTime <= _swipeWindow;
                    _swipeDirection = dip ? 0 : direction;
                    _swipeTime = Time.unscaledTime;
                }
                else
                    _swipeDirection = 0;

                if (dip)
                {
                    _swipeDirection = 0;
                    _blockedUntil = Time.unscaledTime + _cooldown;
                }
            }

            _last = point;
            _hasLast = true;
            return dip;
        }

        // 1 / -1 for a swipe from left to right / right to left, 2 / -2 from bottom to top / top to bottom, else 0.
        private static int SwipeDirection(Side entry, Side exit)
        {
            if (entry == Side.Left && exit == Side.Right) return 1;
            if (entry == Side.Right && exit == Side.Left) return -1;
            if (entry == Side.Bottom && exit == Side.Top) return 2;
            if (entry == Side.Top && exit == Side.Bottom) return -2;
            return 0;
        }

        // The side of the rect the point lies on (the dominant direction when it lies at a corner).
        public static Side SideOf(Rect rect, Vector2 point)
        {
            float dx = point.x < rect.xMin ? rect.xMin - point.x : point.x > rect.xMax ? point.x - rect.xMax : 0f;
            float dy = point.y < rect.yMin ? rect.yMin - point.y : point.y > rect.yMax ? point.y - rect.yMax : 0f;
            if (dx >= dy)
                return point.x < rect.center.x ? Side.Left : Side.Right;
            return point.y < rect.center.y ? Side.Bottom : Side.Top;
        }

        // How far the point reaches into the rect from the given side, 0..1 of the rect size in that direction.
        public static float Depth(Rect rect, Vector2 point, Side side)
        {
            switch (side)
            {
                case Side.Left: return (point.x - rect.xMin) / rect.width;
                case Side.Right: return (rect.xMax - point.x) / rect.width;
                case Side.Bottom: return (point.y - rect.yMin) / rect.height;
                case Side.Top: return (rect.yMax - point.y) / rect.height;
            }
            return 0f;
        }
    }
}
