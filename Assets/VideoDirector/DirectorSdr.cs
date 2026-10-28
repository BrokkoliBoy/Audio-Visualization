using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace Gavi.VideoDirector
{
    /// <summary>
    /// The videos are recorded in SDR, so on this branch the app starts with the HDR output switch off (the menu still lets one turn it on).
    /// <c>MainMenuUI</c> reads its start setting in Start, after this runs, so the private field is set through reflection and neither the
    /// script nor the scene changes. When the display already runs in HDR at start (Windows HDR on), that setting alone is not enough:
    /// the output stays active and the toggle shows it as ticked. So one frame later, after the Start of the menu, the output is switched off
    /// and the toggle unticked.
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

            GameObject helper = new GameObject("Video Director SDR");
            helper.AddComponent<SdrStartRoutine>().Run(menu);
        }

        private class SdrStartRoutine : MonoBehaviour
        {
            public void Run(MainMenuUI menu) { StartCoroutine(Routine(menu)); }

            private IEnumerator Routine(MainMenuUI menu)
            {
                yield return null; // the Start of the menu has run
                HDROutputSettings hdr = HDROutputSettings.main;
                if (hdr != null && hdr.available && hdr.active)
                    hdr.RequestHDRModeChange(false);

                FieldInfo toggleField = typeof(MainMenuUI).GetField("_toggleHdr", BindingFlags.NonPublic | BindingFlags.Instance);
                Toggle toggle = menu != null && toggleField != null ? toggleField.GetValue(menu) as Toggle : null;
                if (toggle != null)
                    toggle.SetIsOnWithoutNotify(false);
                Destroy(gameObject);
            }
        }
    }
}
