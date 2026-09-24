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
        
        private string Path => Application.dataPath + "/Profiles/TerrainProfiles/";

        private void Awake()
        {
            if (Instance != null)
                Debugger.LogInstanceError(GetType());
            Instance = this;
        }

        private void Start()
        {
            ImportProfilesFromJson();
        }

        public void ExportProfilesFromJson(TerrainSettingsProfile profile)
        {
            Directory.CreateDirectory(Path);
            string json = JsonUtility.ToJson(profile);
            StreamWriter writer = new StreamWriter(Path + profile.Name + ".txt");
            writer.WriteLine(json);
            writer.Close();
        }
        
        private void ImportProfilesFromJson()
        {
            try
            {
                IEnumerable<string> textFiles = Directory.EnumerateFiles(Path, "*.txt");
                foreach (string currentFile in textFiles)
                {
                    StreamReader reader = new StreamReader(currentFile);
                    string json = reader.ReadToEnd();
                    reader.Close();
                    TerrainSettingsProfile profile = JsonUtility.FromJson<TerrainSettingsProfile>(json);
                    TerrainGroupUIProfiles.Instance.AddProfile(profile);
                }
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
                IEnumerable<string> textFiles = Directory.EnumerateFiles(Path, "*.txt");
                foreach (string currentFile in textFiles)
                {
                    StreamReader reader = new StreamReader(currentFile);
                    string json = reader.ReadToEnd();
                    reader.Close();
                    TerrainSettingsProfile fileProfile = JsonUtility.FromJson<TerrainSettingsProfile>(json);
                    if (fileProfile.Name == profile.Name)
                    {
                        File.Delete(currentFile);
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
