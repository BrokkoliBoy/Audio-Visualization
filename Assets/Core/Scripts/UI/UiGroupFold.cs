using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gavi
{
    /// <summary>
    /// Lets a menu group fold up to its header and open again. A folded group opens while the pointer is on it and closes when the
    /// pointer leaves. A click on the fold button pins it open (click again to fold it). The group is folded by switching off its
    /// children except the header and the background, and by shrinking its layout height.
    /// </summary>
    public class UiGroupFold : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("The button in the header. A click pins the group open or folds it.")]
        [SerializeField] private Button _button;
        [Tooltip("Small arrow in the header. Hidden while folded, appears turning half way while opened by hovering and points down while pinned open.")]
        [SerializeField] private RectTransform _arrow;
        public RectTransform Arrow => _arrow;
        [Tooltip("Children that stay visible while folded: the header and the background.")]
        [SerializeField] private List<GameObject> _keepVisible = new List<GameObject>();
        [Tooltip("Height of the group while folded. Only used for groups sized by a Layout Element (groups with their own layout group fit themselves).")]
        [SerializeField] private float _foldedHeight = 34f;
        [SerializeField] private bool _startFolded;
        [Tooltip("A folded group opens while the pointer is on it.")]
        [SerializeField] private bool _openOnHover = true;
        [Tooltip("Seconds the group takes to slide open or shut. 0 = instant.")]
        [SerializeField] private float _slideTime = 0.15f;
        [Tooltip("Opacity of the header highlight that stays while the group is pinned open and the pointer is elsewhere. Hovering it is brighter still.")]
        [SerializeField] private float _pinnedHighlight = 0.008f;

        private LayoutElement _layout;
        private bool _ownLayout;
        private float _openMin;
        private float _openPreferred;
        private float _extraHeight;
        private readonly List<GameObject> _hidden = new List<GameObject>();

        private bool _started;
        private bool _pinnedSet;
        private float _arrowFade;
        private float _arrowAngle;
        private Image _arrowImage;
        private float _arrowAlpha = 1f;
        private bool _pinned;
        private bool _hover;
        private bool _hoverSuppressed;
        private bool _stayOpen;
        private bool _takenOver;
        private bool _hoverReady;
        private float _enterTime;
        private float _enterDelay;
        private bool _exitPending;
        private bool _applied;
        private bool _open;
        private bool _dirty;
        private RectMask2D _mask;
        private bool _sliding;
        private bool _slideOpen;
        private float _slideFrom;
        private float _slideTo;
        private float _slideStart;
        private float _slideHeight;
        private float _slideEndTime = float.NegativeInfinity;
        private float _handoverHeight = -1f;

        /// <summary>The pointer left the group for good (not while a slider is dragged or a dropdown is open).</summary>
        public event System.Action PointerLeft;
        private ColorBlock _buttonColors;
        private bool _buttonColorsKept;
        private bool _pinnedTinted;

        private void Awake()
        {
            _layout = GetComponent<LayoutElement>();
            if (_layout == null)
                _layout = gameObject.AddComponent<LayoutElement>();
            _ownLayout = GetComponent<LayoutGroup>() != null;
            if (_ownLayout)
                KeepChildHeights();
            _openMin = _layout.minHeight;
            _openPreferred = _layout.preferredHeight;
            if (_arrow != null)
            {
                _arrowImage = _arrow.GetComponent<Image>();
                if (_arrowImage != null)
                    _arrowAlpha = _arrowImage.color.a;
            }
            _mask = gameObject.AddComponent<RectMask2D>();
            _mask.enabled = false;
            if (_button != null)
                _button.onClick.AddListener(OnClick);
        }

        // A layout group that gets less height than its children want squeezes every child that is allowed to be smaller than its
        // preferred height. While the group slides that would shrink the header and push its text up, so children keep their height.
        private void KeepChildHeights()
        {
            foreach (Transform child in transform)
            {
                LayoutElement element = child.GetComponent<LayoutElement>();
                if (element != null && !element.ignoreLayout && element.minHeight < element.preferredHeight)
                    element.minHeight = element.preferredHeight;
            }
        }

        private void Start()
        {
            _started = true;
            if (!_pinnedSet)
                _pinned = !_startFolded;
            Apply();
            // a group that was changed while its tab was off shows the new state in the very frame the tab opens
            if (_started)
            {
                _dirty = false;
                Apply();
            }
            _arrowFade = ArrowTarget();
            _arrowAngle = _pinned ? -90f : 0f;
            ApplyArrow();
        }

        private void OnDisable()
        {
            _hover = false;
            _hoverReady = false;
            _hoverSuppressed = false;
            _stayOpen = false;
            _exitPending = false;
            if (!_started)
                return;
            // a group opened by hovering folds when its tab is left
            if (_sliding)
                EndSlide();
            if (_open && !_pinned)
            {
                _open = false;
                SetInstant(false);
            }
        }

        /// <summary>
        /// Takes over the fold state of a group that shares its window with this one (see UiGroupSwitch), before this group is
        /// switched on. The fold state is taken over as it is; if the heights of the two windows differ, this one slides from the
        /// height of the old one to its own.
        /// </summary>
        public void TakeOver(UiGroupFold from)
        {
            _pinned = from._pinned;
            _pinnedSet = true;
            _hover = from._hover;
            _hoverReady = from._hoverReady;
            _hoverSuppressed = from._hoverSuppressed;
            _enterTime = from._enterTime;
            _enterDelay = from._enterDelay;
            _takenOver = true;
            _stayOpen = from._stayOpen;
            _handoverHeight = from._sliding ? from._slideHeight : ((RectTransform)from.transform).rect.height;
            _applied = false;
            _dirty = true;
        }

        private void OnEnable()
        {
            // The pointer state is stale after the group was switched off (closed tab), unless it was handed over on purpose.
            if (!_takenOver)
            {
                _hover = false;
                _hoverReady = false;
                _hoverSuppressed = false;
            }
            _takenOver = false;
            _exitPending = false;
            _dirty = true;
            // a group changed while its tab was off shows the new state in the very frame the tab opens
            if (_started)
            {
                _dirty = false;
                Apply();
            }
            _arrowFade = ArrowTarget();
            _arrowAngle = _pinned ? -90f : 0f;
            ApplyArrow();
        }

        private void Update()
        {
            if (_exitPending && !HoldsOpen())
            {
                _exitPending = false;
                LeaveHover();
            }
            // a pointer that only passes over the group must not open it (and shift the layout under the pointer)
            if (_hover && !_hoverReady && Time.unscaledTime - _enterTime >= _enterDelay)
            {
                _hoverReady = true;
                HoverOpenSibling(true);
                _dirty = true;
            }
            // hovering was switched off: a window that is open by hovering stays open instead of folding when the pointer leaves
            if (_open && !_pinned && !_stayOpen && !_hoverSuppressed && !MenuNavigation.WindowHover)
            {
                _stayOpen = true;
                _dirty = true;
            }
            // a right click on the header folds the window, whether it is pinned or only opened by hovering
            if (_open && _hover && Input.GetMouseButtonDown(1) && PointerOnHeader())
                Fold();
            if (_dirty && _started)
            {
                _dirty = false;
                Apply();
            }
            StepPinnedTint();
            if (_sliding)
                StepSlide();
            StepArrow();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _exitPending = false;
            if (!_hover)
            {
                // coming from a group opened by hovering has its own delay, so one can switch straight through the groups
                _enterDelay = HoverOpenSibling(false) ? MenuHoverSettings.SwitchDelay : MenuHoverSettings.OpenDelay;
                _enterTime = Time.unscaledTime;
                _hoverReady = _enterDelay <= 0f;
                if (_hoverReady)
                    HoverOpenSibling(true);
            }
            _hover = true;
            _dirty = true;
            if (_hoverReady)
                ApplyPending();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // A slider that is dragged out of the group or an open dropdown list must not disappear under the pointer.
            if (HoldsOpen())
            {
                _exitPending = true;
                return;
            }
            LeaveHover();
        }

        // Whether another group of the tab is open by hovering, closing those if asked. The group that opens takes over from them at once.
        private bool HoverOpenSibling(bool close)
        {
            bool any = false;
            Transform parent = transform.parent;
            if (parent == null)
                return false;
            foreach (Transform sibling in parent)
            {
                UiGroupFold other = sibling.GetComponent<UiGroupFold>();
                if (other != null && other.isActiveAndEnabled && other != this && other._open && !other._pinned)
                {
                    if (close)
                    {
                        other.EndHover();
                        other.ApplyPending();
                    }
                    any = true;
                }
            }
            return any;
        }

        // Starts a pending change now. A window that closes because another one opens must start in the same moment as the other,
        // both read the same clock, so the two slides stay in step.
        private void ApplyPending()
        {
            if (!_dirty || !_started)
                return;
            _dirty = false;
            Apply();
        }

        // The pointer left the group: a group that was opened by hovering stays open until another group takes over, so the
        // menu can be left and come back with the last window still there.
        private void LeaveHover()
        {
            _stayOpen = _open && !_pinned;
            _hover = false;
            _hoverReady = false;
            _hoverSuppressed = false;
            _dirty = true;
            PointerLeft?.Invoke();
        }

        private void EndHover()
        {
            _stayOpen = false;
            _hover = false;
            _hoverReady = false;
            _hoverSuppressed = false;
            _dirty = true;
        }

        private bool HoldsOpen()
        {
            if (Input.GetMouseButton(0))
                return true;
            // looked up now: dropdowns can be added later (e.g. the shape list)
            foreach (TMP_Dropdown dropdown in GetComponentsInChildren<TMP_Dropdown>())
                if (dropdown != null && dropdown.IsExpanded)
                    return true;
            return false;
        }

        private void OnClick()
        {
            bool pin = !_pinned;
            SetPinned(pin);
            // Ctrl + click opens or folds every group of every tab, Shift + click every group of the open tab
            bool all = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool tab = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (!all && !tab)
                return;
            Transform parent = transform.parent;
            if (parent == null)
                return;
            foreach (Transform sibling in parent)
            {
                UiGroupFold other = sibling.GetComponent<UiGroupFold>();
                if (other != null && other != this && (all || other.isActiveAndEnabled))
                    other.SetPinned(pin);
            }
        }

        private bool PointerOnHeader()
        {
            RectTransform header = _button != null ? _button.transform.parent as RectTransform : null;
            if (header == null || header == transform)
                header = _button != null ? (RectTransform)_button.transform : null;
            if (header == null)
                return false;
            Canvas canvas = GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            return RectTransformUtility.RectangleContainsScreenPoint(header, Input.mousePosition, camera);
        }

        private void Fold()
        {
            _pinned = false;
            _pinnedSet = true;
            _stayOpen = false;
            _hoverSuppressed = true;
            _hoverReady = false;
            _dirty = true;
        }

        private void SetPinned(bool pin)
        {
            if (!pin && _pinned)
            {
                // fold right away although the pointer is still on the group
                _hoverSuppressed = true;
                _stayOpen = false;
                _hoverReady = false;
            }
            _pinned = pin;
            _pinnedSet = true;
            // a group of another tab changes without sliding, it is not on screen
            if (!isActiveAndEnabled)
                _applied = false;
            _dirty = true;
        }

        // A pinned group keeps a faint highlight on its header when the pointer is elsewhere, so the open windows can be found at a glance.
        private void StepPinnedTint()
        {
            if (_button == null || _pinned == _pinnedTinted)
                return;
            if (!_buttonColorsKept)
            {
                _buttonColors = _button.colors;
                _buttonColorsKept = true;
            }
            _pinnedTinted = _pinned;
            ColorBlock colors = _buttonColors;
            if (_pinned)
            {
                Color tint = colors.normalColor;
                tint.a = Mathf.Max(tint.a, _pinnedHighlight);
                colors.normalColor = tint;
                colors.selectedColor = tint;
                Color hover = colors.highlightedColor;
                hover.a = Mathf.Max(hover.a, tint.a + 0.03f);
                colors.highlightedColor = hover;
            }
            _button.colors = colors;
        }

        private float ArrowTarget()
        {
            return _pinned || _open ? 1f : 0f;
        }

        private void StepArrow()
        {
            float k = 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime);
            _arrowFade = Mathf.Lerp(_arrowFade, ArrowTarget(), k);
            _arrowAngle = Mathf.Lerp(_arrowAngle, _pinned ? -90f : 0f, k);
            ApplyArrow();
        }

        private void ApplyArrow()
        {
            if (_arrow == null)
                return;
            _arrow.localRotation = Quaternion.Euler(0f, 0f, _arrowAngle);
            // the arrow points right while hovering, turns down when pinned and is hidden while folded, so a folded menu is not a wall of arrows
            if (_arrowImage != null)
            {
                Color color = _arrowImage.color;
                color.a = _arrowAlpha * _arrowFade;
                _arrowImage.color = color;
            }
        }

        private void Apply()
        {
            bool open = _pinned || _stayOpen || (_openOnHover && MenuNavigation.WindowHover && _hover && _hoverReady && !_hoverSuppressed);
            if (_applied && open == _open)
                return;
            bool slide = (_applied || _handoverHeight >= 0f) && _slideTime > 0f && isActiveAndEnabled;
            _applied = true;
            _open = open;

            if (slide)
                StartSlide(open);
            else
                SetInstant(open);
            _handoverHeight = -1f;
        }

        private void SetInstant(bool open)
        {
            _sliding = false;
            _mask.enabled = false;
            if (open)
            {
                ShowChildren();
                RestoreOpenLayout();
            }
            else
            {
                HideChildren();
                SetFoldedLayout();
            }
            MarkLayout();
        }

        private void StartSlide(bool open)
        {
            float from = _handoverHeight >= 0f ? _handoverHeight : (_sliding ? _slideHeight : ((RectTransform)transform).rect.height);
            _handoverHeight = -1f;
            _slideTo = open ? MeasureOpen() : MeasureFolded();
            // windows of the same height need no slide (and must not hold back the next hover switch)
            if (Mathf.Abs(from - _slideTo) < 0.5f)
            {
                SetInstant(open);
                return;
            }
            _slideFrom = from;
            _slideHeight = from;
            _slideStart = Time.unscaledTime;
            _slideOpen = open;
            _sliding = true;
            // the content is cut at the group's edge while it slides
            _mask.enabled = true;
            SetLayoutHeight(from);
        }

        private void StepSlide()
        {
            float t = Mathf.Clamp01((Time.unscaledTime - _slideStart) / _slideTime);
            _slideHeight = Mathf.Lerp(_slideFrom, _slideTo, Mathf.SmoothStep(0f, 1f, t));
            SetLayoutHeight(_slideHeight);
            if (t >= 1f)
                EndSlide();
        }

        /// <summary>Opens the group as if the pointer had just been on it (it folds again when another group opens), unless it is pinned.</summary>
        public void OpenAsHover()
        {
            if (_pinned)
                return;
            _stayOpen = true;
            // a window that is there from the first frame of its tab does not slide open
            if (_started && isActiveAndEnabled)
            {
                _applied = false;
                _dirty = false;
                Apply();
            }
            else
            {
                _dirty = true;
            }
        }

        /// <summary>Whether the group is open (pinned or by hovering), also while it is still sliding open.</summary>
        public bool IsOpen => _open;

        /// <summary>Whether the window slides or finished sliding less than the given time ago.</summary>
        public bool IsSlidingWithin(float settleTime)
        {
            return _sliding || Time.unscaledTime - _slideEndTime < settleTime;
        }

        private void EndSlide()
        {
            _slideEndTime = Time.unscaledTime;
            SetInstant(_slideOpen);
        }

        private void SetLayoutHeight(float height)
        {
            _layout.minHeight = height;
            _layout.preferredHeight = height;
            MarkLayout();
        }

        private float MeasureOpen()
        {
            ShowChildren();
            RestoreOpenLayout();
            if (!_ownLayout)
                return OpenPreferred >= 0f ? OpenPreferred : (OpenMin >= 0f ? OpenMin : ((RectTransform)transform).rect.height);
            return MeasureOwn();
        }

        private float MeasureFolded()
        {
            if (!_ownLayout)
                return _foldedHeight;
            HideChildren();
            SetFoldedLayout();
            float height = MeasureOwn();
            ShowChildren();
            RestoreOpenLayout();
            return height;
        }

        private float MeasureOwn()
        {
            RectTransform rect = (RectTransform)transform;
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            return LayoutUtility.GetPreferredHeight(rect);
        }

        private void ShowChildren()
        {
            foreach (GameObject child in _hidden)
                if (child != null)
                    child.SetActive(true);
            _hidden.Clear();
        }

        private void HideChildren()
        {
            foreach (Transform child in transform)
            {
                // switched off children (e.g. a confirmation) stay off when the group opens again
                if (!child.gameObject.activeSelf || _keepVisible.Contains(child.gameObject))
                    continue;
                _hidden.Add(child.gameObject);
                child.gameObject.SetActive(false);
            }
        }

        // The open height plus the room of a row that is shown at the moment (see SetExtraOpenHeight).
        private float OpenMin => _openMin >= 0f ? _openMin + _extraHeight : _openMin;
        private float OpenPreferred => _openPreferred >= 0f ? _openPreferred + _extraHeight : _openPreferred;

        /// <summary>
        /// Makes the open group taller by this much, for a row that comes and goes. Call it every frame while the row slides in or
        /// out; the folded height stays.
        /// </summary>
        public void SetExtraOpenHeight(float extra)
        {
            if (Mathf.Approximately(extra, _extraHeight))
                return;
            _extraHeight = extra;
            if (_applied && _open && !_sliding)
            {
                RestoreOpenLayout();
                MarkLayout();
            }
        }

        private void RestoreOpenLayout()
        {
            _layout.minHeight = OpenMin;
            _layout.preferredHeight = OpenPreferred;
        }

        private void SetFoldedLayout()
        {
            if (_ownLayout)
            {
                // the group fits its header by itself
                RestoreOpenLayout();
                return;
            }
            // the minimum is set on every slide step, a slide cut short must not leave its height behind
            _layout.minHeight = _openMin >= 0f ? _foldedHeight : _openMin;
            _layout.preferredHeight = _foldedHeight;
        }

        private void MarkLayout()
        {
            if (transform.parent is RectTransform parent && isActiveAndEnabled)
                LayoutRebuilder.MarkLayoutForRebuild(parent);
        }
    }
}
