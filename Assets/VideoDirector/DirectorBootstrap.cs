using UnityEngine;

namespace Gavi.VideoDirector
{
    /// <summary>Creates the Director when the scene has loaded. Nothing in a scene file points at it, so it stays out of the scenes of <c>next</c>.</summary>
    public static class DirectorBootstrap
    {
        /// <summary>The preset the scene starts with on this branch, the first setup of the take.</summary>
        public const string StartPreset = "Video Spacewarp";

        /// <summary>Same key as the preset group of the menu uses to remember the last preset (it is private there).</summary>
        private const string LastPresetKey = "TerrainPresets.Last";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            DirectorSdr.SwitchOffHdrOnStart();
            // Runs before the Start of the menu, which loads the preset of the last session: this makes it the start preset.
            PlayerPrefs.SetString(LastPresetKey, StartPreset);
            if (DirectorRunner.Instance != null)
                return;
            GameObject director = new GameObject("Video Director");
            director.AddComponent<DirectorRunner>();
        }
    }
}
