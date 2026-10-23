using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gavi
{
    /// <summary>
    /// Lets several menu groups share one window: only one group is shown at a time and a row of small tabs inside each group picks
    /// it. Hovering a tab shows its group at once, a click does the same. The new group takes over the fold state of the old one,
    /// so the window stays as it is (open, pinned or opened by hovering). The groups are not part of any tab page list: this switch
    /// turns them on when it is shown and off when it is hidden (e.g. when its menu tab is closed).
    /// </summary>
    public class UiGroupSwitch : MonoBehaviour
    {
        [Serializable]
        public class Entry
        {
            [Tooltip("The group of this layer.")]
            public GameObject Group;
            [Tooltip("The tab buttons inside this group, one per layer in the same order as the entries.")]
            public Button[] Buttons;
            [Tooltip("The small bars of those buttons that show the selected layer.")]
            public Image[] Indicators;
        }

        [SerializeField] private List<Entry> _entries = new List<Entry>();
        [SerializeField] private int _startIndex;
        [Tooltip("One single button per group (the first of its Buttons) that switches to the next group while the pointer rests on it. The marks of the group show the page: one mark for the first group, two for the second.")]
        [SerializeField] private bool _toggleMode;
        [Tooltip("Optional: while this toggle is off, leaving the window shows the first group again (e.g. a window whose second page does nothing while its feature is off).")]
        [SerializeField] private Toggle _firstPageWhenOff;
        [SerializeField] private Color _indicatorActiveColor = new Color(0.1f, 0.83f, 0.88f, 1f);
        [SerializeField] private Color _indicatorInactiveColor = new Color(1f, 1f, 1f, 0.12f);

        [Tooltip("Hovering a tab does not pick its layer while the window slides and for this long (s) afterwards.")]
        [SerializeField] private float _hoverSettleTime = 0.3f;

        private int _current;
        private int _hoverIndex = -1;
        private bool _wired;
        private float _markFade = 1f;
        private bool _toggleArmed;
        private bool _toggleInside;

        private void Awake()
        {
            _current = Mathf.Clamp(_startIndex, 0, Mathf.Max(0, _entries.Count - 1));
        }

        private void Start()
        {
            for (int e = 0; e < _entries.Count; e++)
            {
                Entry entry = _entries[e];
                if (entry.Buttons == null)
                    continue;
                if (_toggleMode)
                {
                    // the pointer is followed in Update, so a button that appears under the pointer does not switch back at once
                    int next = (e + 1) % _entries.Count;
                    if (entry.Buttons.Length > 0 && entry.Buttons[0] != null)
                        entry.Buttons[0].onClick.AddListener(() => { _toggleArmed = false; Select(next); });
                    continue;
                }
                for (int i = 0; i < entry.Buttons.Length; i++)
                {
                    int index = i;
                    Button button = entry.Buttons[i];
                    if (button == null)
                        continue;
                    button.onClick.AddListener(() => Select(index));
                    EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
                    if (trigger == null)
                        trigger = button.gameObject.AddComponent<EventTrigger>();
                    EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                    enter.callback.AddListener(_ => _hoverIndex = index);
                    trigger.triggers.Add(enter);
                    EventTrigger.Entry exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                    exit.callback.AddListener(_ => { if (_hoverIndex == index) _hoverIndex = -1; });
                    trigger.triggers.Add(exit);
                }
            }
            for (int e = 0; e < _entries.Count; e++)
            {
                int index = e;
                UiGroupFold fold = _entries[e].Group != null ? _entries[e].Group.GetComponent<UiGroupFold>() : null;
                if (fold != null)
                    fold.PointerLeft += () => OnGroupLeft(index);
            }
            _wired = true;
        }

        private void OnEnable()
        {
            _hoverIndex = -1;
            _toggleArmed = false;
            Apply(-1);
        }

        // A tab under the pointer is picked by hovering, but not while the window slides: the new layer would take over the fold
        // state at once and cut the slide short. The tab is picked as soon as the window has settled. A folded window shows no tab
        // marks, so there is nothing to pick either.
        private void Update()
        {
            UiGroupFold current = _current >= 0 && _current < _entries.Count && _entries[_current].Group != null
                ? _entries[_current].Group.GetComponent<UiGroupFold>() : null;
            float target = current == null || current.IsOpen ? 1f : 0f;
            _markFade = Mathf.Lerp(_markFade, target, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
            ApplyIndicatorColors();

            if (_toggleMode)
            {
                UpdateToggle(current, target);
                return;
            }

            if (_hoverIndex < 0 || _hoverIndex == _current || target < 0.5f)
                return;
            if (current != null && current.IsSlidingWithin(_hoverSettleTime))
                return;
            Select(_hoverIndex);
        }

        private void OnGroupLeft(int index)
        {
            if (_firstPageWhenOff != null && !_firstPageWhenOff.isOn && index == _current && _current != 0)
                Select(0);
        }

        // The button of the shown group switches to the next page while the pointer rests on it. It has to be left once before it
        // switches again, otherwise the button that appears under the pointer would switch back at once.
        private void UpdateToggle(UiGroupFold current, float open)
        {
            Entry entry = _current < _entries.Count ? _entries[_current] : null;
            Button button = entry != null && entry.Buttons != null && entry.Buttons.Length > 0 ? entry.Buttons[0] : null;
            _toggleInside = button != null && UiPointer.IsInsideWindow
                && UiPointer.GetScreenRect((RectTransform)button.transform, Vector4.zero).Contains(UiPointer.Position);
            if (!_toggleInside)
            {
                _toggleArmed = true;
                return;
            }
            // a pointer that only opens the window by resting on the header must not flip its page
            if (open < 0.5f)
            {
                _toggleArmed = false;
                return;
            }
            if (!_toggleArmed || (current != null && current.IsSlidingWithin(_hoverSettleTime)))
                return;
            _toggleArmed = false;
            Select((_current + 1) % _entries.Count);
        }

        private void OnDisable()
        {
            foreach (Entry entry in _entries)
                if (entry.Group != null)
                    entry.Group.SetActive(false);
        }

        /// <summary>The fold of the group that is shown right now.</summary>
        public UiGroupFold CurrentFold => _current >= 0 && _current < _entries.Count && _entries[_current].Group != null ? _entries[_current].Group.GetComponent<UiGroupFold>() : null;

        public void Select(int index)
        {
            if (index == _current && _wired)
                return;
            int previous = _current;
            _current = index;
            if (isActiveAndEnabled)
                Apply(previous);
        }

        private void Apply(int previous)
        {
            // the window keeps its fold state when another layer takes its place
            if (previous >= 0 && previous < _entries.Count && _current < _entries.Count)
            {
                UiGroupFold from = _entries[previous].Group != null ? _entries[previous].Group.GetComponent<UiGroupFold>() : null;
                UiGroupFold to = _entries[_current].Group != null ? _entries[_current].Group.GetComponent<UiGroupFold>() : null;
                if (from != null && to != null && from != to)
                    to.TakeOver(from);
            }
            for (int i = 0; i < _entries.Count; i++)
            {
                bool active = i == _current;
                if (_entries[i].Group != null && _entries[i].Group.activeSelf != active)
                    _entries[i].Group.SetActive(active);
            }
            ApplyIndicatorColors();
        }

        // The small marks of the tabs show the selected one. They fade out while the window is folded.
        private void ApplyIndicatorColors()
        {
            foreach (Entry entry in _entries)
            {
                if (entry.Indicators == null)
                    continue;
                for (int k = 0; k < entry.Indicators.Length; k++)
                {
                    if (entry.Indicators[k] == null)
                        continue;
                    Color color = k == _current ? _indicatorActiveColor : _indicatorInactiveColor;
                    if (_toggleMode)
                    {
                        // one mark for the first page, two for the second; brighter while the pointer is on the button
                        color = _indicatorActiveColor;
                        color.a = k <= _current ? (_toggleInside ? 1f : 0.55f) : 0f;
                    }
                    color.a *= _markFade;
                    entry.Indicators[k].color = color;
                }
            }
        }
    }
}
