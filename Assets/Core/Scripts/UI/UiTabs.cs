using System;
using System.Collections.Generic;
using UnityEngine;
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

        private void Start()
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                int index = i;
                if (_tabs[i].Button != null)
                    _tabs[i].Button.onClick.AddListener(() => Select(index));
            }
            Select(_startIndex);
        }

        public void Select(int index)
        {
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
