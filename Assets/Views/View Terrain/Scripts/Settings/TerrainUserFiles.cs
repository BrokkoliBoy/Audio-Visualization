using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Gavi
{
    /// <summary>
    /// Where terrain presets and color palettes live on disk.
    /// Shipped presets are in Assets/StreamingAssets/Presets/..., so they are copied into every build.
    /// In the Editor new files are written there too, so they become part of the project and of the next build.
    /// In a build the game folder may be read only (e.g. Program Files), so new files go to the persistent data path instead.
    /// </summary>
    public static class TerrainUserFiles
    {
        public static string PresetFolder(string subFolder)
        {
            return Application.streamingAssetsPath + "/Presets/" + subFolder + "/";
        }

        public static string WriteFolder(string subFolder)
        {
#if UNITY_EDITOR
            return PresetFolder(subFolder);
#else
            return Application.persistentDataPath + "/Presets/" + subFolder + "/";
#endif
        }

        /// <summary>All files of the folder: shipped presets first, then the files of the player. Later files override earlier ones with the same name.</summary>
        public static List<string> ListFiles(string subFolder)
        {
            var files = new List<string>();
            AddFiles(files, PresetFolder(subFolder));
#if !UNITY_EDITOR
            AddFiles(files, WriteFolder(subFolder));
#endif
            return files;
        }

        private static void AddFiles(List<string> files, string folder)
        {
            if (Directory.Exists(folder))
                files.AddRange(Directory.GetFiles(folder, "*.txt"));
        }

        /// <summary>Deletes the file, and in the Editor its meta file.</summary>
        public static void DeleteFile(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
            if (File.Exists(path + ".meta"))
                File.Delete(path + ".meta");
        }
    }
}
