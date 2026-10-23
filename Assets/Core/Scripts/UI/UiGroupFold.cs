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
        [Tooltip("Shows v while the group is pinned open and > otherwise.")]
        [SerializeField] private TMP_Text _label;
        [Tooltip("Children that stay visible while folded: the header and the background.")]
        [SerializeField] private List<GameObject> _keepVisible = new List<GameObject>();
        [Tooltip("Height of the group while folded. Only used for groups sized by a Layout Element (groups with their own layout group fit themselves).")]
        [SerializeField] private float _foldedHeight = 34f;
        [SerializeField] private bool _startFolded;
        [Tooltip("A folded group opens while the pointer is on it.")]
        [SerializeField] private bool _openOnHover = true;
        [Tooltip("Seconds a hover-opened group stays open after the pointer left it, so the pointer can move on to the next group in one go.")]
        [SerializeField] private float _closeDelay = 0.3f;
        [Tooltip("Seconds the group takes to slide open or shut. 0 = instant.")]
        [SerializeField] private float _slideTime = 0.15f;

        private LayoutElement _layout;
        private bool _ownLayout;
        private float _openMin;
        private float _openPreferred;
        private TMP_Dropdown[] _dropdowns;
        private readonly List<GameObject> _hidden = new List<GameObject>();

        private bool _started;
        private bool _pinned;
        private bool _hover;
        private bool _hoverSuppressed;
        private bool _hoverReady;
        private float _enterTime;
        private float _enterDelay;
        private bool _leaving;
        private float _leaveTime;
        private bool _exitPending;
        private bool _applied;
        private bool _open;
        private bool _dirty;
        private RectMask2D _mask;
        private bool _sliding;
        private bool _slideOpen;
        private float _slideFrom;
        private float _slideTo;
        private float _slideT;
        private float _slideHeight;

        private void Awake()
        {
            _layout = GetComponent<LayoutElement>();
            if (_layout == null)
                _layout = gameObject.AddComponent<LayoutElement>();
            _ownLayout = GetComponent<LayoutGroup>() != null;
            _openMin = _layout.minHeight;
            _openPreferred = _layout.preferredHeight;
            _mask = gameObject.AddComponent<RectMask2D>();
            _mask.enabled = false;
            _dropdowns = GetComponentsInChildren<TMP_Dropdown>(true);
            if (_button != null)
                _button.onClick.AddListener(OnClick);
        }

        private void Start()
        {
            _started = true;
            _pinned = !_startFolded;
            Apply();
        }

        private void OnDisable()
        {
            _hover = false;
            _hoverReady = false;
            _hoverSuppressed = false;
            _leaving = false;
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

        private void OnEnable()
        {
            // The pointer state is stale after the group was switched off (closed tab).
            _hover = false;
            _hoverReady = false;
            _hoverSuppressed = false;
            _leaving = false;
            _exitPending = false;
            _dirty = true;
        }

        private void Update()
        {
            if (_leaving && Time.unscaledTime - _leaveTime >= _closeDelay)
                EndHover();
            if (_exitPending && !HoldsOpen())
            {
                _exitPending = false;
                EndHover();
            }
            // a pointer that only passes over the group must not open it (and shift the layout under the pointer)
            if (_hover && !_hoverReady && Time.unscaledTime - _enterTime >= _enterDelay)
            {
                _hoverReady = true;
                HoverOpenSibling(true);
                _dirty = true;
            }
            if (_dirty && _started)
            {
                _dirty = false;
                Apply();
            }
            if (_sliding)
                StepSlide();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _exitPending = false;
            _leaving = false;
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
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // A slider that is dragged out of the group or an open dropdown list must not disappear under the pointer.
            if (HoldsOpen())
            {
                _exitPending = true;
                return;
            }
            if (_open && !_pinned)
            {
                _leaving = true;
                _leaveTime = Time.unscaledTime;
                return;
            }
            EndHover();
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
                        other.EndHover();
                    any = true;
                }
            }
            return any;
        }

        private void EndHover()
        {
            _leaving = false;
            _hover = false;
            _hoverReady = false;
            _hoverSuppressed = false;
            _dirty = true;
        }

        private bool HoldsOpen()
        {
            if (Input.GetMouseButton(0))
                return true;
            foreach (TMP_Dropdown dropdown in _dropdowns)
                if (dropdown != null && dropdown.IsExpanded)
                    return true;
            return false;
        }

        private void OnClick()
        {
            bool pin = !_pinned;
            SetPinned(pin);
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                // Shift + click opens or folds every group of the tab
                Transform parent = transform.parent;
                if (parent == null)
                    return;
                foreach (Transform sibling in parent)
                {
                    UiGroupFold other = sibling.GetComponent<UiGroupFold>();
                    if (other != null && other.isActiveAndEnabled && other != this)
                        other.SetPinned(pin);
                }
            }
        }

        private void SetPinned(bool pin)
        {
            if (!pin && _pinned)
            {
                // fold right away although the pointer is still on the group
                _hoverSuppressed = true;
                _hoverReady = false;
            }
            _pinned = pin;
            _dirty = true;
        }

        private void Apply()
        {
            bool open = _pinned || (_openOnHover && _hover && _hoverReady && !_hoverSuppressed);
            if (_label != null)
                _label.text = _pinned ? "v" : ">";
            if (_applied && open == _open)
                return;
            bool slide = _applied && _slideTime > 0f && isActiveAndEnabled;
            _applied = true;
            _open = open;

            if (slide)
                StartSlide(open);
            else
                SetInstant(open);
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
            float from = _sliding ? _slideHeight : ((RectTransform)transform).rect.height;
            _slideTo = open ? MeasureOpen() : MeasureFolded();
            _slideFrom = from;
            _slideHeight = from;
            _slideT = 0f;
            _slideOpen = open;
            _sliding = true;
            // the content is cut at the group's edge while it slides
            _mask.enabled = true;
            SetLayoutHeight(from);
        }

        private void StepSlide()
        {
            _slideT += Time.unscaledDeltaTime / _slideTime;
            float t = Mathf.Clamp01(_slideT);
            _slideHeight = Mathf.Lerp(_slideFrom, _slideTo, Mathf.SmoothStep(0f, 1f, t));
            SetLayoutHeight(_slideHeight);
            if (t >= 1f)
                EndSlide();
        }

        private void EndSlide()
        {
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
                return _openPreferred >= 0f ? _openPreferred : (_openMin >= 0f ? _openMin : ((RectTransform)transform).rect.height);
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

        private void RestoreOpenLayout()
        {
            _layout.minHeight = _openMin;
            _layout.preferredHeight = _openPreferred;
        }

        private void SetFoldedLayout()
        {
            if (_ownLayout)
            {
                // the group fits its header by itself
                RestoreOpenLayout();
                return;
            }
            if (_openMin >= 0f)
                _layout.minHeight = _foldedHeight;
            _layout.preferredHeight = _foldedHeight;
        }

        private void MarkLayout()
        {
            if (transform.parent is RectTransform parent && isActiveAndEnabled)
                LayoutRebuilder.MarkLayoutForRebuild(parent);
        }
    }
}
