using TMPro;
using UnityEngine;

namespace Gavi
{
    /// <summary>
    /// Screen space helpers to test how close the mouse pointer is to a UI element.
    /// Works with the rect transform only, so it also works while the element is invisible or not clickable.
    /// </summary>
    public static class UiPointer
    {
        private static readonly Vector3[] _corners = new Vector3[4];

        private static bool _locked;
        private static Vector2 _lockedPosition;

        /// <summary>
        /// The pointer position the menus and cues react to. While the pointer is locked it stays where it was when the
        /// lock started, so moving the mouse (e.g. to steer the camera) does not open or wake anything.
        /// </summary>
        public static Vector2 Position => _locked ? _lockedPosition : (Vector2)Input.mousePosition;

        /// <summary>True while the pointer is locked, i.e. the mouse is steering the camera.</summary>
        public static bool IsLocked => _locked;

        /// <summary>Freezes or releases the position the menus react to. Safe to call every frame.</summary>
        public static void SetLocked(bool locked)
        {
            if (locked && !_locked)
                _lockedPosition = Input.mousePosition;
            _locked = locked;
        }

        /// <summary>True while a text input field has the keyboard focus. Hotkeys must not react then.</summary>
        public static bool IsTyping
        {
            get
            {
                var eventSystem = UnityEngine.EventSystems.EventSystem.current;
                if (eventSystem == null || eventSystem.currentSelectedGameObject == null)
                    return false;
                var input = eventSystem.currentSelectedGameObject.GetComponent<TMP_InputField>();
                return input != null && input.isFocused;
            }
        }

        private static int _activityFrame = -1;
        private static Vector3 _lastMouse;
        private static float _lastActivityTime;

        /// <summary>Seconds since the mouse was last moved. Key presses do not count.</summary>
        public static float IdleSeconds
        {
            get
            {
                if (_activityFrame != Time.frameCount)
                {
                    _activityFrame = Time.frameCount;
                    // Movement while the pointer is locked does not count as activity.
                    if (!_locked && Input.mousePosition != _lastMouse)
                        _lastActivityTime = Time.unscaledTime;
                    _lastMouse = Input.mousePosition;
                }
                return Time.unscaledTime - _lastActivityTime;
            }
        }

        public static bool IsInsideWindow
        {
            get
            {
                Vector3 mouse = Input.mousePosition;
                return mouse.x >= 0 && mouse.y >= 0 && mouse.x <= Screen.width && mouse.y <= Screen.height;
            }
        }

        /// <summary>Screen rect of a rect transform, grown by padding (left, bottom, right, top) given in canvas reference pixels.</summary>
        public static Rect GetScreenRect(RectTransform rectTransform, Vector4 padding, Vector4 extension = default)
        {
            Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
            Canvas root = canvas != null ? canvas.rootCanvas : null;
            Camera camera = root != null && root.renderMode != RenderMode.ScreenSpaceOverlay ? root.worldCamera : null;
            float scale = root != null ? root.scaleFactor : 1f;

            rectTransform.GetWorldCorners(_corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, _corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(camera, _corners[2]);
            Vector2 size = max - min;
            min -= new Vector2(size.x * extension.x, size.y * extension.y);
            max += new Vector2(size.x * extension.z, size.y * extension.w);
            return Rect.MinMaxRect(min.x - padding.x * scale, min.y - padding.y * scale, max.x + padding.z * scale, max.y + padding.w * scale);
        }

        /// <summary>Distance in screen pixels from the pointer to the rect (0 when inside, or when the pointer is outside the window).</summary>
        public static float DistanceTo(RectTransform rectTransform)
        {
            if (rectTransform == null || !rectTransform.gameObject.activeInHierarchy || !IsInsideWindow)
                return float.MaxValue;

            Rect rect = GetScreenRect(rectTransform, Vector4.zero);
            Vector2 point = Position;
            float dx = Mathf.Max(rect.xMin - point.x, 0f, point.x - rect.xMax);
            float dy = Mathf.Max(rect.yMin - point.y, 0f, point.y - rect.yMax);
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        public static bool IsNear(RectTransform rectTransform, Vector4 padding, Vector4 extension = default)
        {
            if (rectTransform == null || !rectTransform.gameObject.activeInHierarchy || !IsInsideWindow)
                return false;
            return GetScreenRect(rectTransform, padding, extension).Contains(Position);
        }

        /// <summary>True while any dropdown list is open. Its list may reach outside of the menu it belongs to.</summary>
        public static bool IsAnyDropdownExpanded()
        {
            foreach (TMP_Dropdown dropdown in Object.FindObjectsByType<TMP_Dropdown>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (dropdown.IsExpanded)
                    return true;
            return false;
        }
    }
}
