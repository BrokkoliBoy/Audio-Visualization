using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using UnityEngine;
using System.IO;
using Gavi.Utility;

namespace Gavi
{
    public class TerrainSettingsPresetManager : MonoBehaviour
    {
        public static TerrainSettingsPresetManager Instance;
        
        private const string SubFolder = "TerrainPresets";

        private void Awake()
        {
            if (Instance != null)
                Debugger.LogInstanceError(GetType());
            Instance = this;
        }

        private void Start()
        {
            ImportPresetsFromJson();
            StartCoroutine(LoadFirstPresetNextFrame());
        }

        /// <summary>One frame later, so TerrainSettingsManager.Start has already read the scene values and does not overwrite the loaded preset.</summary>
        private IEnumerator LoadFirstPresetNextFrame()
        {
            yield return null;
            TerrainGroupUIPresets.Instance.LoadStartPreset();
        }

        public void ExportPresetsFromJson(TerrainSettingsPreset preset)
        {
            string folder = TerrainUserFiles.WriteFolder(SubFolder);
            Directory.CreateDirectory(folder);
            preset.FormatVersion = TerrainSettingsPreset.CurrentFormatVersion;
            string json = JsonUtility.ToJson(preset);
            File.WriteAllText(folder + preset.Name + ".txt", json + "\n");
        }
        
        /// <summary>Reads a preset from JSON (file or clipboard) and brings old formats up to date. Null when the text is no preset.</summary>
        public static TerrainSettingsPreset ParsePreset(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith("{"))
                return null;
            TerrainSettingsPreset preset = JsonUtility.FromJson<TerrainSettingsPreset>(json);
            if (preset == null || string.IsNullOrEmpty(preset.Name))
                return null;
            // Version 2: the menu intensity of the colors has its default at 3 instead of 1.
            if (preset.FormatVersion < 2)
            {
                for (int c = 0; preset.ColorValues != null && c < preset.ColorValues.Count; c++)
                {
                    Vector3 color = preset.ColorValues[c];
                    color.z = TerrainColorMapping.ConvertLegacyMenu(color.z);
                    preset.ColorValues[c] = color;
                }
                preset.FormatVersion = 2;
            }
            // Version 3: the round shapes (circle, spiral, rings) fit the disc at zoom 1, so their old zoom is divided by the factor.
            if (preset.FormatVersion < 3)
            {
                foreach (TerrainShapeData shape in preset.Shapes)
                    if (TerrainShape.FitsDisc((TerrainShape.AudioShape)shape.Shape))
                        shape.Zoom /= TerrainShape.DiscZoom;
                preset.FormatVersion = 3;
            }
            // Version 4: the patterns are heights (they were carves, high = lowered terrain) and every layer has its own depth.
            if (preset.FormatVersion < 4)
            {
                MigrateToHeightLayers(preset);
                preset.FormatVersion = 4;
            }
            // Version 5: Honeycomb is pits (low cells, high rims) by itself, it used to be the other way round, so its invert is turned to keep the look.
            if (preset.FormatVersion < 5)
            {
                MigrateHoneycomb(preset);
                preset.FormatVersion = 5;
            }
            return preset;
        }

        private static void MigrateHoneycomb(TerrainSettingsPreset preset)
        {
            if (preset.NoisePattern == (int)TerrainNoise.Pattern.Honeycomb)
                preset.NoiseInvert = !preset.NoiseInvert;
            foreach (TerrainNoiseLayerData layer in new[] { preset.NoiseLayer2, preset.NoiseLayer3 })
                if (layer != null && layer.Pattern == (int)TerrainNoise.Pattern.Honeycomb)
                    layer.Invert = !layer.Invert;
        }

        /// <summary>The patterns that were turned upside down by themselves before version 4.</summary>
        private static bool WasInvertedByDefault(int pattern)
        {
            switch ((TerrainNoise.Pattern)pattern)
            {
                case TerrainNoise.Pattern.Voronoi:
                case TerrainNoise.Pattern.Hexagons:
                case TerrainNoise.Pattern.Bricks:
                case TerrainNoise.Pattern.Dots:
                case TerrainNoise.Pattern.Pyramids:
                case TerrainNoise.Pattern.Tiles:
                case TerrainNoise.Pattern.Cracks:
                case TerrainNoise.Pattern.Triangles:
                case TerrainNoise.Pattern.Cubes:
                    return true;
                default:
                    return false;
            }
        }

        // Old mix operations (Override, Add, Multiply, Maximum, Minimum, Difference) as the new ones (Replace, Add, Subtract, Highest, Lowest, Multiply, Difference).
        // The old Multiply (carve times carve) has no twin, Highest is the closest.
        private static readonly int[] OldOperationToNew = { 0, 2, 3, 4, 3, 6 };

        /// <summary>
        /// The old pattern value was the carve, the new one is the height, so every layer is turned upside down (together with its old invert
        /// state, which also included the patterns that were upside down by themselves). The old depth of the main pattern becomes the depth
        /// of every layer, the old depth of a layer (its share of the mix result) becomes its mix.
        /// </summary>
        private static void MigrateToHeightLayers(TerrainSettingsPreset preset)
        {
            float depth = preset.PerlinDepth;
            preset.NoiseInvert = !(WasInvertedByDefault(preset.NoisePattern) ^ preset.NoiseInvert);
            foreach (TerrainNoiseLayerData layer in new[] { preset.NoiseLayer2, preset.NoiseLayer3 })
            {
                layer.Invert = !(WasInvertedByDefault(layer.Pattern) ^ layer.Invert);
                layer.Mix = layer.Depth;
                layer.Depth = depth;
                layer.Operation = OldOperationToNew[Mathf.Clamp(layer.Operation, 0, OldOperationToNew.Length - 1)];
            }
        }

        public void ImportPresetsFromJson()
        {
            try
            {
                // Files of the player come after the shipped presets and replace presets with the same name.
                var presets = new List<TerrainSettingsPreset>();
                foreach (string currentFile in TerrainUserFiles.ListFiles(SubFolder))
                {
                    // A broken file must not stop the other presets from loading.
                    try
                    {
                        TerrainSettingsPreset preset = ParsePreset(File.ReadAllText(currentFile));
                        if (preset == null)
                            continue;
                        presets.RemoveAll(p => p.Name == preset.Name);
                        presets.Add(preset);
                    }
                    catch (Exception fileError)
                    {
                        Debugger.LogError("Skipped preset file " + currentFile + ": " + fileError.Message);
                    }
                }

                foreach (TerrainSettingsPreset preset in presets)
                    TerrainGroupUIPresets.Instance.AddPreset(preset, false);
            }
            catch (Exception e)
            {
                Debugger.LogError(e.Message + " in TerrainGroupUIPreset.ImportPresetsFromJson()");
            }
        }

        public void DeletePresetFromDisk(TerrainSettingsPreset preset)
        {
            try
            {
                string folder = TerrainUserFiles.WriteFolder(SubFolder);
                if (!Directory.Exists(folder))
                    return;

                foreach (string currentFile in Directory.EnumerateFiles(folder, "*.txt"))
                {
                    TerrainSettingsPreset filePreset = JsonUtility.FromJson<TerrainSettingsPreset>(File.ReadAllText(currentFile));
                    if (filePreset != null && filePreset.Name == preset.Name)
                    {
                        TerrainUserFiles.DeleteFile(currentFile);
                        return;
                    }
                }
            }
            catch (IOException e)
            {
                Debugger.LogError(e + " in TerrainSettingsPresetManager.DeletePresetFromDisk(" + preset.Name + ")");
            }
        }
    }
}
