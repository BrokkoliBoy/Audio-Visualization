using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using UnityEngine;
using System.IO;
using Gavi.Utility;

namespace Gavi
{
    public class TerrainSettingsProfileManager : MonoBehaviour
    {
        public static TerrainSettingsProfileManager Instance;
        
        private const string SubFolder = "TerrainProfiles";

        private void Awake()
        {
            if (Instance != null)
                Debugger.LogInstanceError(GetType());
            Instance = this;
        }

        private void Start()
        {
            ImportProfilesFromJson();
            StartCoroutine(LoadFirstProfileNextFrame());
        }

        /// <summary>One frame later, so TerrainSettingsManager.Start has already read the scene values and does not overwrite the loaded profile.</summary>
        private IEnumerator LoadFirstProfileNextFrame()
        {
            yield return null;
            TerrainGroupUIProfiles.Instance.LoadFirstProfile();
        }

        public void ExportProfilesFromJson(TerrainSettingsProfile profile)
        {
            string folder = TerrainUserFiles.WriteFolder(SubFolder);
            Directory.CreateDirectory(folder);
            profile.FormatVersion = TerrainSettingsProfile.CurrentFormatVersion;
            string json = JsonUtility.ToJson(profile);
            File.WriteAllText(folder + profile.Name + ".txt", json + "\n");
        }
        
        private void ImportProfilesFromJson()
        {
            try
            {
                // Files of the player come after the shipped presets and replace presets with the same name.
                var profiles = new List<TerrainSettingsProfile>();
                foreach (string currentFile in TerrainUserFiles.ListFiles(SubFolder))
                {
                    // A broken file must not stop the other profiles from loading.
                    try
                    {
                        TerrainSettingsProfile profile = JsonUtility.FromJson<TerrainSettingsProfile>(File.ReadAllText(currentFile));
                        if (profile == null || string.IsNullOrEmpty(profile.Name))
                            continue;
                        // Version 2: the menu intensity of the colors has its default at 3 instead of 1.
                        if (profile.FormatVersion < 2)
                        {
                            for (int c = 0; profile.ColorValues != null && c < profile.ColorValues.Count; c++)
                            {
                                Vector3 color = profile.ColorValues[c];
                                color.z = TerrainColorMapping.ConvertLegacyMenu(color.z);
                                profile.ColorValues[c] = color;
                            }
                            profile.FormatVersion = 2;
                        }
                        profiles.RemoveAll(p => p.Name == profile.Name);
                        profiles.Add(profile);
                    }
                    catch (Exception fileError)
                    {
                        Debugger.LogError("Skipped profile file " + currentFile + ": " + fileError.Message);
                    }
                }

                foreach (TerrainSettingsProfile profile in profiles)
                    TerrainGroupUIProfiles.Instance.AddProfile(profile, false);
            }
            catch (Exception e)
            {
                Debugger.LogError(e.Message + " in TerrainGroupUIProfile.ImportProfilesFromJson()");
            }
        }

        public void DeleteProfileFromDisk(TerrainSettingsProfile profile)
        {
            try
            {
                string folder = TerrainUserFiles.WriteFolder(SubFolder);
                if (!Directory.Exists(folder))
                    return;

                foreach (string currentFile in Directory.EnumerateFiles(folder, "*.txt"))
                {
                    TerrainSettingsProfile fileProfile = JsonUtility.FromJson<TerrainSettingsProfile>(File.ReadAllText(currentFile));
                    if (fileProfile != null && fileProfile.Name == profile.Name)
                    {
                        TerrainUserFiles.DeleteFile(currentFile);
                        return;
                    }
                }
            }
            catch (IOException e)
            {
                Debugger.LogError(e + " in TerrainSettingsProfileManager.DeleteProfileFromDisk(" + profile.Name + ")");
            }
        }
    }
}
