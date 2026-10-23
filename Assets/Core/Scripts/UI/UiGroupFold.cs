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
        [Tooltip("Shows - while the group is pinned open and + otherwise.")]
        [SerializeField] private TMP_Text _label;
        [Tooltip("Children that stay visible while folded: the header and the background.")]
        [SerializeField] private List<GameObject> _keepVisible = new List<GameObject>();
        [Tooltip("Height of the group while folded. Only used for groups sized by a Layout Element (groups with their own layout group fit themselves).")]
        [SerializeField] private float _foldedHeight = 34f;
        [SerializeField] private bool _startFolded;
        [Tooltip("A folded group opens while the pointer is on it.")]
        [SerializeField] private bool _openOnHover = true;

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
        private bool _exitPending;
        private bool _applied;
        private bool _open;
        private bool _dirty;

        private void Awake()
        {
            _layout = GetComponent<LayoutElement>();
            _ownLayout = GetComponent<LayoutGroup>() != null;
            if (_layout != null)
            {
                _openMin = _layout.minHeight;
                _openPreferred = _layout.preferredHeight;
            }
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

        private void OnEnable()
        {
            // The pointer state is stale after the group was switched off (closed tab).
            _hover = false;
            _hoverSuppressed = false;
            _exitPending = false;
            _dirty = true;
        }

        private void Update()
        {
            if (_exitPending && !HoldsOpen())
            {
                _exitPending = false;
                _hover = false;
                _hoverSuppressed = false;
                _dirty = true;
            }
            if (_dirty && _started)
            {
                _dirty = false;
                Apply();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _exitPending = false;
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
            _hover = false;
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
            if (_pinned)
            {
                // fold right away although the pointer is still on the group
                _pinned = false;
                _hoverSuppressed = true;
            }
            else
            {
                _pinned = true;
            }
            _dirty = true;
        }

        private void Apply()
        {
            bool open = _pinned || (_openOnHover && _hover && !_hoverSuppressed);
            if (_label != null)
                _label.text = _pinned ? "-" : "+";
            if (_applied && open == _open)
                return;
            _applied = true;
            _open = open;

            if (open)
            {
                foreach (GameObject child in _hidden)
                    if (child != null)
                        child.SetActive(true);
                _hidden.Clear();
                if (_layout != null && !_ownLayout)
                {
                    _layout.minHeight = _openMin;
                    _layout.preferredHeight = _openPreferred;
                }
            }
            else
            {
                foreach (Transform child in transform)
                {
                    // switched off children (e.g. a confirmation) stay off when the group opens again
                    if (!child.gameObject.activeSelf || _keepVisible.Contains(child.gameObject))
                        continue;
                    _hidden.Add(child.gameObject);
                    child.gameObject.SetActive(false);
                }
                if (_layout != null && !_ownLayout)
                {
                    if (_openMin >= 0f)
                        _layout.minHeight = _foldedHeight;
                    _layout.preferredHeight = _foldedHeight;
                }
            }

            if (transform.parent is RectTransform parent && isActiveAndEnabled)
                LayoutRebuilder.MarkLayoutForRebuild(parent);
        }
    }
}
