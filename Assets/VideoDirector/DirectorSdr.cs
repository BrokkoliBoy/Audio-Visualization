using System.Reflection;
using UnityEngine;

namespace Gavi.VideoDirector
{
    /// <summary>
    /// The videos are recorded in SDR, so on this branch the app starts with the HDR output switch off (the menu still lets one turn it on).
    /// <c>MainMenuUI</c> reads its start setting in Start, after this runs, so the private field is set through reflection and neither the
    /// script nor the scene changes.
    /// </summary>
    public static class DirectorSdr
    {
        public static void SwitchOffHdrOnStart()
        {
            MainMenuUI menu = Object.FindFirstObjectByType<MainMenuUI>();
            FieldInfo field = typeof(MainMenuUI).GetField("_enableHdrOnStart", BindingFlags.NonPublic | BindingFlags.Instance);
            if (menu == null || field == null)
            {
                Debug.LogWarning("Video Director: could not switch the HDR start setting off (MainMenuUI or its field not found).");
                return;
            }
            field.SetValue(menu, false);
        }
    }
}
