using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gavi
{
    /// <summary>
    /// Simple tab switcher: each tab has a button and a set of pages (GameObjects). Only the pages of the selected tab are active.
    /// </summary>
    public class UiTabs : MonoBehaviour
    {
        [Serializable]
        public class Tab
        {
            public Button Button;
            public Image Indicator;
            public List<GameObject> Pages = new List<GameObject>();
        }

        [SerializeField] private List<Tab> _tabs = new List<Tab>();
        [SerializeField] private int _startIndex;
        [SerializeField] private Color _indicatorActiveColor = new Color(0.1f, 0.83f, 0.88f, 1f);
        [SerializeField] private Color _indicatorInactiveColor = new Color(1f, 1f, 1f, 0.12f);

        [Tooltip("Hovering a tab selects it after this delay, so sweeping the mouse across other tabs does not switch them. Clicking still works instantly.")]
        [SerializeField] private float _hoverDelay = 0.1f;

        private int _hoverIndex = -1;
        private float _hoverTime;

        private void Start()
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                int index = i;
                if (_tabs[i].Button == null)
                    continue;

                _tabs[i].Button.onClick.AddListener(() => Select(index));

                EventTrigger trigger = _tabs[i].Button.gameObject.GetComponent<EventTrigger>();
                if (trigger == null)
                    trigger = _tabs[i].Button.gameObject.AddComponent<EventTrigger>();
                AddTrigger(trigger, EventTriggerType.PointerEnter, () => { _hoverIndex = index; _hoverTime = Time.unscaledTime; });
                AddTrigger(trigger, EventTriggerType.PointerExit, () => { if (_hoverIndex == index) _hoverIndex = -1; });
            }
            Select(_startIndex);
        }

        private void Update()
        {
            if (_hoverIndex < 0 || Time.unscaledTime - _hoverTime < _hoverDelay)
                return;

            int index = _hoverIndex;
            _hoverIndex = -1;
            if (index != _current)
                Select(index);
        }

        private static void AddTrigger(EventTrigger trigger, EventTriggerType type, Action action)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }

        private int _current = -1;

        public void Select(int index)
        {
            _current = index;
            for (int i = 0; i < _tabs.Count; i++)
            {
                bool active = i == index;
                foreach (GameObject page in _tabs[i].Pages)
                    if (page != null)
                        page.SetActive(active);
                if (_tabs[i].Indicator != null)
                    _tabs[i].Indicator.color = active ? _indicatorActiveColor : _indicatorInactiveColor;
            }
        }
    }
}
