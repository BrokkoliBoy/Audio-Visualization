using UnityEngine;

namespace Gavi
{
    /// <summary>
    /// Which ways of moving through the menu by pointer alone are on (hovering, dips, right click). Saved on this computer.
    /// The main switch turns all of them off at once; the single switches keep their state meanwhile.
    /// </summary>
    public static class MenuNavigation
    {
        public enum Option { TabHover, WindowHover, LayerHover, LayerDip, CheckboxDip }

        private const string KeyPrefix = "MenuNavigation3.";

        // hovering a tab or a window is the basic feel of the menu; switching layers by hover and the dips are easy to trigger by accident and start off
        private static readonly bool[] Defaults = { true, true, false, false, true };

        private static bool _loaded;
        private static bool _master = true;
        private static readonly bool[] Values = new bool[Defaults.Length];

        /// <summary>Called when a switch changes, so windows can react at once.</summary>
        public static event System.Action Changed;

        public static bool Master
        {
            get { Load(); return _master; }
            set { Load(); _master = value; Save(); }
        }

        /// <summary>The state of a single switch, whatever the main switch says.</summary>
        public static bool Get(Option option)
        {
            Load();
            return Values[(int)option];
        }

        public static void Set(Option option, bool on)
        {
            Load();
            Values[(int)option] = on;
            Save();
        }

        public static bool TabHover => Active(Option.TabHover);
        public static bool WindowHover => Active(Option.WindowHover);
        public static bool LayerHover => Active(Option.LayerHover);
        public static bool LayerDip => Active(Option.LayerDip);
        public static bool CheckboxDip => Active(Option.CheckboxDip);

        private static bool Active(Option option)
        {
            Load();
            return _master && Values[(int)option];
        }

        private static void Load()
        {
            if (_loaded)
                return;
            _loaded = true;
            _master = PlayerPrefs.GetInt(KeyPrefix + "Master", 1) != 0;
            for (int i = 0; i < Values.Length; i++)
                Values[i] = PlayerPrefs.GetInt(KeyPrefix + (Option)i, Defaults[i] ? 1 : 0) != 0;
        }

        private static void Save()
        {
            PlayerPrefs.SetInt(KeyPrefix + "Master", _master ? 1 : 0);
            for (int i = 0; i < Values.Length; i++)
                PlayerPrefs.SetInt(KeyPrefix + (Option)i, Values[i] ? 1 : 0);
            Changed?.Invoke();
        }

        public static void ResetToDefaults()
        {
            Load();
            _master = true;
            for (int i = 0; i < Values.Length; i++)
                Values[i] = Defaults[i];
            Save();
        }
    }
}
