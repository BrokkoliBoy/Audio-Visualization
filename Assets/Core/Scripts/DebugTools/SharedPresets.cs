using System;
using System.IO;
using UnityEngine;

namespace Gavi
{
    /// <summary>
    /// Debug mode and the shared folder for the live link: one folder on the disk that the Editor and every build of this project read and write.
    /// The folder is in the persistent data path (same for the Editor and a build, outside the repository).
    /// Everything that uses it is only active in debug mode (see <see cref="DebugOn"/>).
    /// </summary>
    public static class SharedPresets
    {
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
