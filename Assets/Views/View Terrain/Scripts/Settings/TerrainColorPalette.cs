using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Gavi
{
    /// <summary>
    /// A saved color look of the terrain: four colors (hue, saturation, menu intensity), the overall intensity and the
    /// bloom (glow) settings. Stored as a small json file next to the terrain profiles.
    /// </summary>
    [Serializable]
    public class TerrainColorPalette
    {
        /// <summary>The palette that is selected when nothing else is chosen. It is a normal file like every other palette.</summary>
        public const string DefaultName = "Lava";
        /// <summary>Name that profiles saved before the rename use for the default palette.</summary>
        public const string LegacyDefaultName = "Default";
        /// <summary>Shown when the colors do not come from a saved palette.</summary>
        public const string CustomName = "Custom";

        public const float DefaultBloomThreshold = 0.5f;
        public const float DefaultBloomIntensity = 0.6f;
        public const float DefaultBloomScatter = 0.7f;

        /// <summary>2 = menu intensity with the default at 3 (version 1 had it at 1, see TerrainColorMapping.ConvertLegacyMenu).</summary>
        public const int CurrentFormatVersion = 2;
        public int FormatVersion;
        public string Name = "My Palette";
        [Tooltip("Per color: x = hue (0-360), y = saturation (0-1), z = menu intensity (0-10, 1 = Unity intensity 0).")]
        public List<Vector3> Colors = new List<Vector3>();
        public float OverallIntensity = 1f;
        public float BloomThreshold = DefaultBloomThreshold;
        public float BloomIntensity = DefaultBloomIntensity;
        public float BloomScatter = DefaultBloomScatter;

        public const int DefaultTiltDirection = 0;
        public const float DefaultCurveFloor = 0.3f;
        public const float DefaultColorShift = 0f;
        public const float DefaultColorHold = 1f;
        public const float DefaultPlateauTilt = 2f;
        public const float DefaultRelief = 0.15f;
        public const float DefaultHueBalance = 0f;

        [Tooltip("Color curve: direction of the plateau tilt (TerrainColorGroupUI.TiltMode), the glow of the lowest vertices on the menu glow scale (0.3 = exposure -4.5).")]
        public int TiltDirection = DefaultTiltDirection;
        public float CurveFloor = DefaultCurveFloor;
        [Tooltip("Color curve: -1..1 moves the colors down (+) or up (-) the height, hold = width of the two long color areas (1 = as in the scene gradient).")]
        public float ColorShift = DefaultColorShift;
        public float ColorHold = DefaultColorHold;
        public float PlateauTilt = DefaultPlateauTilt;
        public float ReliefStrength = DefaultRelief;
        public float HueBalance = DefaultHueBalance;

        public TerrainColorPalette Clone()
        {
            return new TerrainColorPalette
            {
                FormatVersion = FormatVersion,
                Name = Name,
                Colors = new List<Vector3>(Colors),
                OverallIntensity = OverallIntensity,
                BloomThreshold = BloomThreshold,
                BloomIntensity = BloomIntensity,
                BloomScatter = BloomScatter,
                TiltDirection = TiltDirection,
                CurveFloor = CurveFloor,
                ColorShift = ColorShift,
                ColorHold = ColorHold,
                PlateauTilt = PlateauTilt,
                ReliefStrength = ReliefStrength,
                HueBalance = HueBalance
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
                        if (palette.FormatVersion < 2)
                        {
                            for (int c = 0; c < palette.Colors.Count; c++)
                            {
                                Vector3 color = palette.Colors[c];
                                color.z = TerrainColorMapping.ConvertLegacyMenu(color.z);
                                palette.Colors[c] = color;
                            }
                            palette.FormatVersion = 2;
                        }
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
