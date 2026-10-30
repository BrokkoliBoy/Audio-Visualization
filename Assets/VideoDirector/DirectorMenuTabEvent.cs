using System.Collections;
using System.Reflection;
using UnityEngine;

namespace Gavi.VideoDirector
{
    /// <summary>
    /// Selects a tab of the settings menu ("Color", "Light", "Terrain", "General": part of the name of the tab button) and, with <c>foldWindows</c>, folds all its windows
    /// so only the ones that are not foldable (pattern tabs) stay visible; without it the first window of the tab opens as when a tab is clicked.
    /// The tab list and the fold function are private in the menu code, so they are reached by reflection (no hook in the menu, see CLAUDE.local.md).
    /// </summary>
    public class MenuTabEvent : DirectorEvent
    {
        private readonly string _tab;
        private readonly bool _foldWindows;
        public MenuTabEvent(string tab, bool foldWindows) { _tab = tab; _foldWindows = foldWindows; }
        public override string Describe() => "Menu tab " + _tab + (_foldWindows ? " (windows folded)" : "");

        public override void Begin(DirectorContext context)
        {
            FieldInfo tabsField = typeof(UiTabs).GetField("_tabs", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo foldMethod = typeof(UiGroupFold).GetMethod("Fold", BindingFlags.Instance | BindingFlags.NonPublic);
            if (tabsField == null || foldMethod == null)
                throw new System.MissingMemberException("UiTabs._tabs or UiGroupFold.Fold was renamed");

            foreach (UiTabs tabs in Object.FindObjectsByType<UiTabs>(FindObjectsInactive.Include))
            {
                IList list = (IList)tabsField.GetValue(tabs);
                for (int i = 0; i < list.Count; i++)
                {
                    UiTabs.Tab tab = (UiTabs.Tab)list[i];
                    if (tab.Button == null || !tab.Button.name.Contains(_tab))
                        continue;
                    tabs.Select(i);
                    if (_foldWindows)
                    {
                        foreach (GameObject page in tab.Pages)
                        {
                            if (page == null)
                                continue;
                            foreach (UiGroupFold fold in page.GetComponentsInChildren<UiGroupFold>(true))
                                foldMethod.Invoke(fold, null);
                        }
                    }
                    return;
                }
            }
            throw new System.ArgumentException("no menu tab named '" + _tab + "'");
        }
    }
}
