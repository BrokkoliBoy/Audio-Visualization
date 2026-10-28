using System;
using System.IO;
using UnityEngine;

namespace Gavi.VideoDirector
{
    /// <summary>
    /// Finds a terrain preset by name in the preset files. The files are read again on every call, so a preset that was changed or saved is picked up
    /// by the next take without a restart. Video presets are normal preset files with the prefix "Video ".
    /// </summary>
    public static class DirectorPresets
    {
        private const string SubFolder = "TerrainPresets";

        /// <summary>A copy of the preset, so the blend never changes the original.</summary>
        public static TerrainSettingsPreset Load(string name)
        {
            TerrainSettingsPreset found = null;
            foreach (string file in TerrainUserFiles.ListFiles(SubFolder))
            {
                TerrainSettingsPreset preset;
                try
                {
                    preset = TerrainSettingsPresetManager.ParsePreset(File.ReadAllText(file));
                }
                catch (ArgumentException)
                {
                    continue;
                }

                if (preset != null && preset.Name == name)
                    found = preset;
            }

            if (found == null)
                throw new FileNotFoundException("Video Director: no terrain preset named '" + name + "'");

            TerrainSettingsPreset copy = new TerrainSettingsPreset();
            copy.CopyFromPreset(found);
            copy.Name = found.Name;
            return copy;
        }
    }
}
