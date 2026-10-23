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
            [Tooltip("Coming into this tab does not open its first window.")]
            public bool KeepFolded;
        }

        [SerializeField] private List<Tab> _tabs = new List<Tab>();
        [SerializeField] private int _startIndex;
        [SerializeField] private Color _indicatorActiveColor = new Color(0.1f, 0.83f, 0.88f, 1f);
        [SerializeField] private Color _indicatorInactiveColor = new Color(1f, 1f, 1f, 0.12f);

        // hovering a tab selects it at once

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
                AddTrigger(trigger, EventTriggerType.PointerEnter, () => { if (index != _current) Select(index); });
            }
            Select(_startIndex);
            _started = true;
        }

        private static void AddTrigger(EventTrigger trigger, EventTriggerType type, Action action)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }

        private int _current = -1;
        private bool _started;

        // Coming into a tab by hand opens its first window (not pinned), so the top of the tab is at hand.
        private static void OpenFirstGroup(Tab tab)
        {
            if (tab.Pages.Count == 0 || tab.Pages[0] == null)
                return;
            GameObject page = tab.Pages[0];
            UiGroupFold fold = page.GetComponent<UiGroupSwitch>() != null ? page.GetComponent<UiGroupSwitch>().CurrentFold : page.GetComponent<UiGroupFold>();
            if (fold != null)
                fold.OpenAsHover();
        }

        public void Select(int index)
        {
            _current = index;
            for (int i = 0; i < _tabs.Count; i++)
            {
                bool active = i == index;
                foreach (GameObject page in _tabs[i].Pages)
                    if (page != null)
                        page.SetActive(active);
                if (active && _started && !_tabs[i].KeepFolded)
                    OpenFirstGroup(_tabs[i]);
                if (_tabs[i].Indicator != null)
                    _tabs[i].Indicator.color = active ? _indicatorActiveColor : _indicatorInactiveColor;
            }
        }
    }
}
