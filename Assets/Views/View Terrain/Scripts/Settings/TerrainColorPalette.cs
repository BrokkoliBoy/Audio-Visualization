using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Gavi
{
    /// <summary>
    /// A saved color look of the terrain: four colors (hue, saturation, menu intensity) plus overall intensity and the
    /// intensity that menu value 10 stands for. Stored as a small json file next to the terrain profiles.
    /// </summary>
    [Serializable]
    public class TerrainColorPalette
    {
        /// <summary>The built-in palette that reproduces the gradient of the scene.</summary>
        public const string DefaultName = "Pink Neon";
        /// <summary>Name that profiles saved before the rename use for the built-in palette.</summary>
        public const string LegacyDefaultName = "Default";

        public static bool IsDefault(string name) { return name == DefaultName || name == LegacyDefaultName; }
        public const string CustomName = "Custom";

        public const int CurrentFormatVersion = 1;
        public int FormatVersion;
        public string Name = "My Palette";
        [Tooltip("Per color: x = hue (0-360), y = saturation (0-1), z = menu intensity (0-10, 1 = Unity intensity 0).")]
        public List<Vector3> Colors = new List<Vector3>();
        public float OverallIntensity = 1f;
        public float MaxIntensity = 5f;

        public TerrainColorPalette Clone()
        {
            return new TerrainColorPalette
            {
                FormatVersion = FormatVersion,
                Name = Name,
                Colors = new List<Vector3>(Colors),
                OverallIntensity = OverallIntensity,
                MaxIntensity = MaxIntensity
            };
        }
    }

    public static class TerrainColorPaletteStore
    {
        private const string SubFolder = "ColorPalettes";

        /// <summary>Shipped presets first, then the palettes of the player. A palette with the same name replaces the earlier one.</summary>
        public static List<TerrainColorPalette> LoadAll()
        {
            var palettes = new List<TerrainColorPalette>();
            try
            {
                foreach (string file in TerrainUserFiles.ListFiles(SubFolder))
                {
                    // A broken file must not stop the other palettes from loading.
                    try
                    {
                        TerrainColorPalette palette = JsonUtility.FromJson<TerrainColorPalette>(File.ReadAllText(file));
                        if (palette == null || string.IsNullOrEmpty(palette.Name) || palette.Colors == null || palette.Colors.Count == 0)
                            continue;
                        // Files from older versions: FormatVersion < CurrentFormatVersion. Convert here when a change needs it.
                        palettes.RemoveAll(p => p.Name == palette.Name);
                        palettes.Add(palette);
                    }
                    catch (Exception fileError)
                    {
                        Debug.LogError("Skipped palette file " + file + ": " + fileError.Message);
                    }
                }
                palettes.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message + " in TerrainColorPaletteStore.LoadAll()");
            }
            return palettes;
        }

        public static void Save(TerrainColorPalette palette)
        {
            try
            {
                string folder = TerrainUserFiles.WriteFolder(SubFolder);
                Directory.CreateDirectory(folder);
                palette.FormatVersion = TerrainColorPalette.CurrentFormatVersion;
                File.WriteAllText(folder + FileName(palette.Name), JsonUtility.ToJson(palette));
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message + " in TerrainColorPaletteStore.Save(" + palette.Name + ")");
            }
        }

        /// <summary>Deletes the palette from the writable folder. Shipped presets of a build cannot be deleted and reappear at the next start.</summary>
        public static void Delete(string name)
        {
            try
            {
                string path = TerrainUserFiles.WriteFolder(SubFolder) + FileName(name);
                TerrainUserFiles.DeleteFile(path);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message + " in TerrainColorPaletteStore.Delete(" + name + ")");
            }
        }

        private static string FileName(string name)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
                name = name.Replace(invalid, '_');
            return name + ".txt";
        }
    }
}
