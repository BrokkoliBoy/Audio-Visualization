using UnityEngine;

namespace Gavi.Game
{
    /// <summary>
    /// The frame rate limit of the app. It reaches from <see cref="MinFps"/> to the refresh rate of the monitor and is saved on this
    /// computer, not in a profile. Without a saved value the limit is the refresh rate.
    /// </summary>
    public static class FrameRateSettings
    {
        public const int MinFps = 30;
        private const string PrefsKey = "MaxFps";
        private const int UnknownMonitorHz = 144;

        /// <summary>Limit without a saved value: the refresh rate in a build, 60 in the Editor to spare the computer while working.</summary>
        private static int DefaultFps => Application.isEditor ? 60 : MonitorHz;

        /// <summary>Refresh rate of the monitor the window is on (240 for 239.996 Hz), a fallback if the system does not tell.</summary>
        public static int MonitorHz
        {
            get
            {
                int hz = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);
                return hz >= MinFps ? hz : UnknownMonitorHz;
            }
        }

        public static int MaxFps
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(PrefsKey, DefaultFps), MinFps, MonitorHz);
            set
            {
                value = Mathf.Clamp(value, MinFps, MonitorHz);
                PlayerPrefs.SetInt(PrefsKey, value);
                Apply();
            }
        }

        /// <summary>Sets the limit in Unity. V-Sync stays off, the limit does the work.</summary>
        public static void Apply()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = MaxFps;
        }
    }
}
