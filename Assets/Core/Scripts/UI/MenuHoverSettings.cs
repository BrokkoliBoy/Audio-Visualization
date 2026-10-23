using UnityEngine;

namespace Gavi
{
    /// <summary>
    /// How long the pointer must rest on a folded menu group before it opens. <see cref="OpenDelay"/> counts when no other group is
    /// open by hovering, <see cref="SwitchDelay"/> when the pointer comes from such a group (0 = switch straight through). Saved on
    /// this computer, not in a profile.
    /// </summary>
    public static class MenuHoverSettings
    {
        public const float MaxDelay = 1f;
        private const string KeyOpen = "MenuOpenDelay";
        private const string KeySwitch = "MenuSwitchDelay";

        public static float OpenDelay
        {
            get => Mathf.Clamp(PlayerPrefs.GetFloat(KeyOpen, 0.3f), 0f, MaxDelay);
            set => PlayerPrefs.SetFloat(KeyOpen, Mathf.Clamp(value, 0f, MaxDelay));
        }

        public static float SwitchDelay
        {
            get => Mathf.Clamp(PlayerPrefs.GetFloat(KeySwitch, 0f), 0f, MaxDelay);
            set => PlayerPrefs.SetFloat(KeySwitch, Mathf.Clamp(value, 0f, MaxDelay));
        }
    }
}
