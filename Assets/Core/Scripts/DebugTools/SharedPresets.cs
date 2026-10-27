using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Gavi
{
    /// <summary>
    /// The shared space: one folder on the disk that the Editor and every build of this project read and write, so a preset saved in one of them
    /// is there in the other. The folder is in the persistent data path (same for the Editor and a build, outside the repository).
    /// Everything that uses it is only active in debug mode (see <see cref="DebugOn"/>).
    /// </summary>
    public static class SharedPresets
    {
        public struct Entry
        {
            public string Name;
            public DateTime Time;
        }

        private const string DebugKey = "Debug.SharedPresets";
        private const string LiveFile = "_live.json";

        /// <summary>The Editor, a development build, or a build in which debug mode was switched on with Ctrl+F12 (remembered).</summary>
        public static bool DebugOn => Debug.isDebugBuild || PlayerPrefs.GetInt(DebugKey, 0) == 1;


        public static string Folder => Path.Combine(Application.persistentDataPath, "SharedPresets");

        public static void ToggleDebug()
        {
            PlayerPrefs.SetInt(DebugKey, PlayerPrefs.GetInt(DebugKey, 0) == 1 ? 0 : 1);
            PlayerPrefs.Save();
        }


        #region Presets
        private static string FileOf(string name)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
                name = name.Replace(invalid.ToString(), "");
            return Path.Combine(Folder, name.Trim() + ".txt");
        }

        public static bool Exists(string name) => File.Exists(FileOf(name));

        public static DateTime StampOf(string name)
        {
            string file = FileOf(name);
            return File.Exists(file) ? File.GetLastWriteTimeUtc(file) : DateTime.MinValue;
        }

        public static List<Entry> List()
        {
            List<Entry> entries = new List<Entry>();
            if (!Directory.Exists(Folder))
                return entries;
            foreach (string file in Directory.GetFiles(Folder, "*.txt"))
                entries.Add(new Entry { Name = Path.GetFileNameWithoutExtension(file), Time = File.GetLastWriteTime(file) });
            entries.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            return entries;
        }

        /// <summary>The preset of that name from the shared folder, or null when there is none or the file is half written.</summary>
        public static TerrainSettingsPreset TryLoad(string name)
        {
            try
            {
                string file = FileOf(name);
                return File.Exists(file) ? TerrainSettingsPresetManager.ParsePreset(File.ReadAllText(file)) : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static void Save(TerrainSettingsPreset preset)
        {
            Directory.CreateDirectory(Folder);
            preset.FormatVersion = TerrainSettingsPreset.CurrentFormatVersion;
            File.WriteAllText(FileOf(preset.Name), JsonUtility.ToJson(preset) + "\n");
        }

        public static void Delete(string name)
        {
            string file = FileOf(name);
            if (File.Exists(file))
                File.Delete(file);
        }
        #endregion


        #region Live link
        // One file with the whole setup. The sender writes it when its settings change, the receiver applies it. The first line names the sender,
        // so a program never applies what it wrote itself.
        public static void WriteLive(string senderId, string json)
        {
            Directory.CreateDirectory(Folder);
            string temp = Path.Combine(Folder, LiveFile + ".tmp");
            File.WriteAllText(temp, senderId + "\n" + json);
            File.Copy(temp, Path.Combine(Folder, LiveFile), true);
        }

        public static bool ReadLive(out DateTime stamp, out string senderId, out string json)
        {
            stamp = DateTime.MinValue;
            senderId = "";
            json = "";
            string file = Path.Combine(Folder, LiveFile);
            try
            {
                if (!File.Exists(file))
                    return false;
                stamp = File.GetLastWriteTimeUtc(file);
                string text = File.ReadAllText(file);
                int split = text.IndexOf('\n');
                if (split < 0)
                    return false;
                senderId = text.Substring(0, split);
                json = text.Substring(split + 1);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        #endregion
    }
}
