namespace Gavi
{
    /// <summary>
    /// How long the pointer must rest on a folded menu group before it opens. <see cref="OpenDelay"/> counts when no other group is
    /// open by hovering, <see cref="SwitchDelay"/> when the pointer comes from such a group (0 = switch straight through).
    /// </summary>
    public static class MenuHoverSettings
    {
        public const float OpenDelay = 0.3f;
        public const float SwitchDelay = 0f;
    }
}
