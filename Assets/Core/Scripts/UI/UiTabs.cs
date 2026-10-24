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
            [Tooltip("Color the bar under the tab ends in. Transparent: the accent whose hue is furthest from the first window.")]
            public Color BarEnd = new Color(0f, 0f, 0f, 0f);
            [Tooltip("The accent hues of the windows are calculated from their order: the first window gets 'Hue From', the last 'Hue To', the others in between. Saturation and brightness stay as set on each stripe. The subheadings follow the accent in any case.")]
            public bool AutoAccents;
            [Range(0f, 1f)] public float HueFrom;
            [Range(0f, 1f)] public float HueTo;
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
                AddTrigger(trigger, EventTriggerType.PointerEnter, () => { if (index != _current && MenuNavigation.TabHover) Select(index); });
            }
            ApplyAccents();
            Select(_startIndex);
            _started = true;
        }

        // Every window hands its accent color to its controls, and the bar under a tab shows the accents of the windows on its pages.
        private void ApplyAccents()
        {
            foreach (Tab tab in _tabs)
            {
                if (tab.AutoAccents)
                    AssignHues(tab);
                List<Color> colors = new List<Color>();
                foreach (GameObject page in OnScreenOrder(tab))
                {
                    if (page == null)
                        continue;
                    UiGroupSwitch groupSwitch = page.GetComponent<UiGroupSwitch>();
                    List<GameObject> groups = GroupsOf(page);
                    foreach (GameObject group in groups)
                    {
                        UiAccent.TintGroup(group);
                        if (groupSwitch != null && group != groups[0])
                            continue;
                        if (UiAccent.TryGetAccent(group, out Color accent))
                            colors.Add(accent);
                    }
                }
                if (tab.Indicator != null && colors.Count > 0)
                    tab.Indicator.sprite = UiAccent.GradientSprite(new[] { colors[0], tab.BarEnd.a > 0f ? tab.BarEnd : MostDistinct(colors) });
            }
        }

        // the windows of a page: the page itself, or all windows that share its place (see UiGroupSwitch)
        private static List<GameObject> GroupsOf(GameObject page)
        {
            List<GameObject> groups = new List<GameObject>();
            UiGroupSwitch groupSwitch = page.GetComponent<UiGroupSwitch>();
            if (groupSwitch != null)
                groups.AddRange(groupSwitch.Groups);
            else
                groups.Add(page);
            return groups;
        }

        // The pages as they appear on screen, top to bottom (the list in the inspector is in any order).
        private static List<GameObject> OnScreenOrder(Tab tab)
        {
            List<GameObject> pages = new List<GameObject>();
            foreach (GameObject page in tab.Pages)
                if (page != null)
                    pages.Add(page);
            pages.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
            return pages;
        }

        // the hue of every window follows its place in the tab
        private static void AssignHues(Tab tab)
        {
            List<GameObject> pages = OnScreenOrder(tab);
            int count = pages.Count;
            int index = 0;
            foreach (GameObject page in pages)
            {
                float hue = Mathf.Lerp(tab.HueFrom, tab.HueTo, count > 1 ? index / (float)(count - 1) : 0f);
                foreach (GameObject group in GroupsOf(page))
                    UiAccent.SetHue(group, hue);
                index++;
            }
        }

        // the accent whose hue is furthest from the first one, so the bar of a tab shows a clear transition instead of the same color twice
        private static Color MostDistinct(List<Color> colors)
        {
            Color.RGBToHSV(colors[0], out float baseHue, out _, out _);
            Color best = colors[0];
            float bestDistance = -1f;
            foreach (Color color in colors)
            {
                Color.RGBToHSV(color, out float hue, out _, out _);
                float distance = Mathf.Abs(hue - baseHue);
                distance = Mathf.Min(distance, 1f - distance);
                if (distance > bestDistance)
                {
                    bestDistance = distance;
                    best = color;
                }
            }
            return best;
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
                {
                    bool gradient = _tabs[i].Indicator.sprite != null;
                    Color inactive = gradient ? new Color(1f, 1f, 1f, 0.3f) : _indicatorInactiveColor;
                    _tabs[i].Indicator.color = active ? (gradient ? Color.white : _indicatorActiveColor) : inactive;
                }
            }
        }
    }
}
