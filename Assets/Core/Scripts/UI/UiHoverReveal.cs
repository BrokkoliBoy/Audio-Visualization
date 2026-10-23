using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi
{
    /// <summary>
    /// Decides whether a menu is open: it opens as soon as the mouse pointer comes near it
    /// and closes as soon as the pointer leaves, without any delay.
    /// Optionally fades a CanvasGroup; other scripts can listen to Changed to slide something instead.
    /// </summary>
    public class UiHoverReveal : MonoBehaviour
    {
        [Header("--- Regions ---")]
        [Tooltip("The pointer near one of these opens the menu, even while it is hidden.")]
        [SerializeField] private List<RectTransform> _triggerRegions = new List<RectTransform>();
        [Tooltip("The pointer near one of these keeps the menu open, but only while it is open (e.g. the menu panel itself).")]
        [SerializeField] private List<RectTransform> _stayRegions = new List<RectTransform>();
        [Tooltip("Extra distance around the trigger regions in canvas reference pixels: left, bottom, right, top.")]
        [SerializeField] private Vector4 _triggerPadding = new Vector4(24, 24, 24, 24);
        [Tooltip("Same for the stay regions.")]
        [SerializeField] private Vector4 _stayPadding = new Vector4(24, 24, 24, 24);
        [Tooltip("While the menu is open, every region grows by this fraction of its own size on each side (left, bottom, right, top), so the pointer can leave a bit without closing it.")]
        [SerializeField] private Vector4 _openExtension = new Vector4(0.3f, 0.3f, 0.3f, 0.3f);
        [Tooltip("Optional trigger area for a menu docked at the right screen edge: from the edge to a bit left of the menu, and as high as the menu (as it is right now, so folded windows count) plus a margin above and below. Leave empty to use only the regions above.")]
        [SerializeField] private RectTransform _dockedMenuArea;
        [Tooltip("How far the docked trigger area reaches to the left, as a fraction of the menu width beyond the menu (0.2 = 20 % wider than the menu).")]
        [SerializeField] private float _dockedExtraWidth = 0.2f;
        [Tooltip("Width of a strip at the right screen edge, over the whole height, where the menu always comes (canvas reference pixels).")]
        [SerializeField] private float _dockedEdgeStrip = 80f;
        [Tooltip("Margin of the docked trigger area above and below the menu, as a fraction of the screen height.")]
        [SerializeField] private float _dockedExtraHeight = 0.08f;
        [Header("--- Intent (optional) ---")]
        [Tooltip("Opens the menu already when the pointer moves towards it with some speed, wherever it is on the screen.")]
        [SerializeField] private bool _openByIntent;
        [Tooltip("Minimum pointer speed for that, in screen heights per second.")]
        [SerializeField] private float _intentSpeed = 0.6f;
        [Tooltip("The pointer has to be in this part of the screen for the intent (left, bottom, right, top as fractions of the screen), e.g. the lower half for a menu at the bottom.")]
        [SerializeField] private Vector4 _intentScreenArea = new Vector4(0f, 0f, 1f, 1f);
        [Tooltip("A fast movement that does not lead to the menu takes the intent back only after this many seconds of it.")]
        [SerializeField] private float _intentCancelDelay = 0.15f;
        [Tooltip("The pointer is followed this many seconds ahead; the menu opens when that point is in the trigger area.")]
        [SerializeField] private float _intentLookAhead = 0.4f;
        [Tooltip("The menu stays open this long (s) after the last movement towards it, so a pointer that slows down on the way still finds it.")]
        [SerializeField] private float _intentHold = 0.9f;

        [Tooltip("The menu stays open while any of these is visible (e.g. a popup that belongs to it).")]
        [SerializeField] private List<CanvasGroup> _keepOpenWhileVisible = new List<CanvasGroup>();

        [Header("--- Behaviour ---")]
        [Tooltip("Shown for this many seconds after start so that new users see it.")]
        [SerializeField] private float _showOnStartSeconds = 0f;
        [Tooltip("Does not close while a mouse button is held, e.g. when dragging a slider out of the menu.")]
        [SerializeField] private bool _keepOpenWhileMouseHeld = true;

        [Header("--- Fade (optional) ---")]
        [SerializeField] private CanvasGroup _fadeGroup;
        [SerializeField] private float _fadeSpeed = 10f;
        [Range(0, 1)]
        [SerializeField] private float _hiddenAlpha = 0f;

        public bool IsRevealed { get; private set; }
        /// <summary>Stays open without the pointer being near (set by hotkeys or by clicking the handle).</summary>
        public bool Pinned { get; set; }
        public event Action<bool> Changed;

        private bool _dismissed;
        private float _startUntil;
        private float _intentUntil;
        private float _intentMissTime;
        private Vector2 _lastPosition;
        private Vector2 _velocity;


        #region Mono
        private void Awake()
        {
            _startUntil = Time.unscaledTime + _showOnStartSeconds;
            IsRevealed = _showOnStartSeconds > 0f;
            ApplyFade(true);
        }

        private void Update()
        {
            bool near = IsPointerNear(IsRevealed);
            bool intent = UpdateIntent();

            // After a dismiss the menu stays closed until the pointer has left once.
            if (_dismissed)
            {
                if (!near)
                    _dismissed = false;
                near = false;
            }

            bool want = !_dismissed && (Pinned || near || intent || Time.unscaledTime < _startUntil || IsKeptOpenByOthers());

            if (IsRevealed && !want && !_dismissed && IsStickyActive())
                want = true;

            if (want != IsRevealed)
            {
                IsRevealed = want;
                Changed?.Invoke(want);
            }

            ApplyFade(false);
        }
        #endregion


        #region Control
        /// <summary>Hotkey behaviour: closes the menu when it is open, otherwise opens it and keeps it open.</summary>
        public void ToggleByKey()
        {
            if (IsRevealed)
                Dismiss();
            else
                Open();
        }

        /// <summary>Click behaviour: keeps the menu open or lets go of it again.</summary>
        public void TogglePinned()
        {
            Pinned = !Pinned;
            if (Pinned)
                _dismissed = false;
        }

        public void Open()
        {
            _dismissed = false;
            Pinned = true;
        }

        /// <summary>Closes the menu now and keeps it closed until the pointer has left it once.</summary>
        public void Dismiss()
        {
            Pinned = false;
            _startUntil = 0f;
            _dismissed = true;
        }
        #endregion


        #region Helpers
        private bool IsPointerNear(bool includeStayRegions)
        {
            return UiPointer.IsInsideWindow && IsPointNear(UiPointer.Position, includeStayRegions);
        }

        private bool IsPointNear(Vector2 point, bool includeStayRegions)
        {
            for (int i = 0; i < _triggerRegions.Count; i++)
                if (IsInside(_triggerRegions[i], _triggerPadding, includeStayRegions ? _openExtension : Vector4.zero, point))
                    return true;

            if (IsPointInDockedArea(point))
                return true;

            if (includeStayRegions)
                for (int i = 0; i < _stayRegions.Count; i++)
                    if (IsInside(_stayRegions[i], _stayPadding, _openExtension, point))
                        return true;

            return false;
        }

        private static bool IsInside(RectTransform rectTransform, Vector4 padding, Vector4 extension, Vector2 point)
        {
            if (rectTransform == null || !rectTransform.gameObject.activeInHierarchy)
                return false;
            return UiPointer.GetScreenRect(rectTransform, padding, extension).Contains(point);
        }

        // The menu slides in from the right edge. Its own rect is used for the height only; the width is measured from the screen edge,
        // so the area is the same while the menu is slid out.
        private static float ScaleOf(RectTransform rectTransform)
        {
            Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
            return canvas != null ? canvas.rootCanvas.scaleFactor : 1f;
        }

        private bool IsPointInDockedArea(Vector2 point)
        {
            if (_dockedMenuArea == null || !_dockedMenuArea.gameObject.activeInHierarchy)
                return false;
            Rect menu = UiPointer.GetScreenRect(_dockedMenuArea, Vector4.zero);
            float margin = Screen.height * _dockedExtraHeight;
            Rect area = Rect.MinMaxRect(Screen.width - menu.width * (1f + _dockedExtraWidth), menu.yMin - margin, Screen.width, menu.yMax + margin);
            if (point.x >= Screen.width - _dockedEdgeStrip * ScaleOf(_dockedMenuArea))
                return true;
            return area.Contains(point);
        }

        // The pointer path of the next moments: a few points along its current direction.
        private bool PathReachesMenu(Vector2 position)
        {
            for (int step = 1; step <= 3; step++)
                if (IsPointNear(position + _velocity * (_intentLookAhead * step / 3f), false))
                    return true;
            return false;
        }

        // Opens the menu when the pointer moves fast enough towards it: the pointer is followed a short time ahead and the menu
        // comes as soon as that point lies in the trigger area. It then stays for a moment; any fast movement somewhere else
        // takes the intent back, and a pointer that really arrives is handled by the normal rules.
        private bool UpdateIntent()
        {
            Vector2 position = UiPointer.Position;
            float dt = Time.unscaledDeltaTime;
            if (!_openByIntent || dt <= 0f || !UiPointer.IsInsideWindow || UiPointer.IsLocked)
            {
                _lastPosition = position;
                _velocity = Vector2.zero;
                return false;
            }

            Vector2 instant = (position - _lastPosition) / dt;
            _lastPosition = position;
            _velocity = Vector2.Lerp(_velocity, instant, 1f - Mathf.Exp(-20f * dt));

            // dragging a slider or the like is no intent
            if (Input.GetMouseButton(0))
                return Time.unscaledTime < _intentUntil;

            float minSpeed = _intentSpeed * Screen.height;
            if (_velocity.magnitude >= minSpeed)
            {
                bool inArea = position.x >= Screen.width * _intentScreenArea.x && position.y >= Screen.height * _intentScreenArea.y
                    && position.x <= Screen.width * _intentScreenArea.z && position.y <= Screen.height * _intentScreenArea.w;
                if (inArea && PathReachesMenu(position))
                {
                    _intentUntil = Time.unscaledTime + _intentHold;
                    _intentMissTime = 0f;
                }
                else
                {
                    // a single frame of a jittery path must not take the menu away again
                    _intentMissTime += dt;
                    if (_intentMissTime >= _intentCancelDelay)
                        _intentUntil = 0f;
                }
            }
            else
            {
                _intentMissTime = 0f;
            }
            return Time.unscaledTime < _intentUntil;
        }

        private bool IsKeptOpenByOthers()
        {
            for (int i = 0; i < _keepOpenWhileVisible.Count; i++)
                if (_keepOpenWhileVisible[i] != null && _keepOpenWhileVisible[i].alpha > 0.05f)
                    return true;
            return false;
        }

        private bool IsStickyActive()
        {
            if (_keepOpenWhileMouseHeld && UiPointer.IsInsideWindow && Input.GetMouseButton(0))
                return true;
            return UiPointer.IsAnyDropdownExpanded();
        }

        private void ApplyFade(bool instant)
        {
            if (_fadeGroup == null)
                return;

            float target = IsRevealed ? 1f : _hiddenAlpha;
            _fadeGroup.alpha = instant ? target : Mathf.MoveTowards(_fadeGroup.alpha, target, _fadeSpeed * Time.unscaledDeltaTime);
            _fadeGroup.interactable = IsRevealed;
            _fadeGroup.blocksRaycasts = IsRevealed;
        }
        #endregion
    }
}
