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
        [SerializeField] private Color _indicatorActiveColor = new Color(0.1f, 0.83f, 0.88f, 1f);
        [SerializeField] private Color _indicatorInactiveColor = new Color(1f, 1f, 1f, 0.12f);

        [Tooltip("Hovering a tab does not pick its layer while the window slides and for this long (s) afterwards.")]
        [SerializeField] private float _hoverSettleTime = 0.3f;

        private int _current;
        private int _hoverIndex = -1;
        private bool _wired;
        private float _markFade = 1f;

        private void Awake()
        {
            _current = Mathf.Clamp(_startIndex, 0, Mathf.Max(0, _entries.Count - 1));
        }

        private void Start()
        {
            foreach (Entry entry in _entries)
            {
                if (entry.Buttons == null)
                    continue;
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
            _wired = true;
        }

        private void OnEnable()
        {
            _hoverIndex = -1;
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

            if (_hoverIndex < 0 || _hoverIndex == _current || target < 0.5f)
                return;
            if (current != null && current.IsSlidingWithin(_hoverSettleTime))
                return;
            Select(_hoverIndex);
        }

        private void OnDisable()
        {
            foreach (Entry entry in _entries)
                if (entry.Group != null)
                    entry.Group.SetActive(false);
        }

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
                    color.a *= _markFade;
                    entry.Indicators[k].color = color;
                }
            }
        }
    }
}
