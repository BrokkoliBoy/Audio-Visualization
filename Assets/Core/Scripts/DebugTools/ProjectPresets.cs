using System;
using System.IO;
using UnityEngine;

namespace Gavi
{
    /// <summary>
    /// Lets a build in debug mode use the preset files of the Unity project (Assets/StreamingAssets/Presets) instead of its own copy. With it, Save in
    /// the menu of a build writes straight into the project, so the preset is there in the Editor and in the next build.
    /// The choice is made in the debug window (the project files are the default in debug mode); after a change the menu reads its preset list
    /// again (<see cref="TerrainGroupUIPresets.ReloadPresets"/>). In the Editor the menu already works in the project files, so nothing changes there.
    /// </summary>
    public static class ProjectPresets
    {
        private const string WantedKey = "Debug.ProjectPresets";
        /// <summary>Written by the Editor when it plays, so a build outside the project folder finds the project too.</summary>
        private const string PathFile = "_project.path";
        private const int MaxLevelsUp = 8;

        private static string _found;

        /// <summary>The user wants the project files (remembered; on unless switched off in the debug window).</summary>
        public static bool Wanted => PlayerPrefs.GetInt(WantedKey, 1) == 1;

        public static void SetWanted(bool wanted)
        {
            PlayerPrefs.SetInt(WantedKey, wanted ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// The StreamingAssets folder of the Unity project this game reads and writes presets in, or null for the normal folders (always null in the
        /// Editor, and when debug mode is off or the project was not found). The folder search is done once, until it succeeds.
        /// </summary>
        public static string Root
        {
            get
            {
                if (Application.isEditor || !SharedPresets.DebugOn || !Wanted)
                    return null;
                if (_found == null)
                    _found = Find();
                return _found;
            }
        }

        /// <summary>The project folder of this build, or null. First the folders above the build (a build in the project's Builds folder), then the path the Editor left behind.</summary>
        public static string Find()
        {
            string found = FindAbove(Application.dataPath);
            if (found != null)
                return found;

            try
            {
                string file = Path.Combine(SharedPresets.Folder, PathFile);
                if (File.Exists(file))
                {
                    string path = File.ReadAllText(file).Trim();
                    if (Directory.Exists(path))
                        return path.Replace('\\', '/').TrimEnd('/');
                }
            }
            catch (Exception)
            {
                // No readable path: the caller shows "not found".
            }
            return null;
        }

        /// <summary>Climbs from the folder up until one has Assets/StreamingAssets and ProjectSettings next to each other.</summary>
        public static string FindAbove(string start)
        {
            DirectoryInfo directory = string.IsNullOrEmpty(start) ? null : new DirectoryInfo(start);
            for (int level = 0; directory != null && level < MaxLevelsUp; level++, directory = directory.Parent)
            {
                string streaming = Path.Combine(directory.FullName, "Assets", "StreamingAssets");
                if (Directory.Exists(streaming) && Directory.Exists(Path.Combine(directory.FullName, "ProjectSettings")))
                    return streaming.Replace('\\', '/');
            }
            return null;
        }

        /// <summary>Called by the Editor when it plays: leaves its StreamingAssets path in the shared folder for builds outside the project folder.</summary>
        public static void WriteEditorPath()
        {
            if (!Application.isEditor)
                return;
            try
            {
                Directory.CreateDirectory(SharedPresets.Folder);
                File.WriteAllText(Path.Combine(SharedPresets.Folder, PathFile), Application.streamingAssetsPath);
            }
            catch (Exception)
            {
                // Only a convenience for builds outside the project.
            }
        }
    }
}
