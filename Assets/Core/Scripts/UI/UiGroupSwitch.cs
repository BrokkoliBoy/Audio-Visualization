using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gavi
{
    /// <summary>
    /// A row of small tabs that shows one of several menu groups at a time, so they share one window. Hovering a tab shows its group
    /// at once, a click does the same. The groups are not part of any tab page list: this switch turns them on when it is shown and
    /// off when it is hidden (e.g. when its menu tab is closed).
    /// </summary>
    public class UiGroupSwitch : MonoBehaviour
    {
        [Serializable]
        public class Entry
        {
            public Button Button;
            public Image Indicator;
            public GameObject Group;
        }

        [SerializeField] private List<Entry> _entries = new List<Entry>();
        [SerializeField] private int _startIndex;
        [SerializeField] private Color _indicatorActiveColor = new Color(0.1f, 0.83f, 0.88f, 1f);
        [SerializeField] private Color _indicatorInactiveColor = new Color(1f, 1f, 1f, 0.12f);

        private int _current;
        private bool _wired;

        private void Awake()
        {
            _current = Mathf.Clamp(_startIndex, 0, Mathf.Max(0, _entries.Count - 1));
        }

        private void Start()
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                int index = i;
                if (_entries[i].Button == null)
                    continue;
                _entries[i].Button.onClick.AddListener(() => Select(index));
                EventTrigger trigger = _entries[i].Button.gameObject.GetComponent<EventTrigger>();
                if (trigger == null)
                    trigger = _entries[i].Button.gameObject.AddComponent<EventTrigger>();
                EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                enter.callback.AddListener(_ => Select(index));
                trigger.triggers.Add(enter);
            }
            _wired = true;
        }

        private void OnEnable()
        {
            Apply();
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
            _current = index;
            if (isActiveAndEnabled)
                Apply();
        }

        private void Apply()
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                bool active = i == _current;
                if (_entries[i].Group != null && _entries[i].Group.activeSelf != active)
                    _entries[i].Group.SetActive(active);
                if (_entries[i].Indicator != null)
                    _entries[i].Indicator.color = active ? _indicatorActiveColor : _indicatorInactiveColor;
            }
        }
    }
}
