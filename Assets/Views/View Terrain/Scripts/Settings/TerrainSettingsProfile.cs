using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi
{
    [System.Serializable]
    public class TerrainSettingsProfile
    {
        /// <summary>Version of the file format. 0 = files from before versions existed. Raise CurrentFormatVersion when a change needs old files to be converted.</summary>
        public const int CurrentFormatVersion = 1;
        public int FormatVersion;
        public string Name = "Terrain Profile";
        
        #region Settings Variables
        [Header("- Terrain -")]
        public int FaceCountX = 97;
        public int FaceCountY = 97;
        public float TerrainHeight = 3;
        
        [Header("- Perlin -")]
        public bool ApplyPerlin = true;
        public float PerlinDepth = 0.6f;
        public float PerlinZoom = 0.3f;
        public float PerlinSpeedX = 0;
        public float PerlinSpeedY = 1;
        [Tooltip("Index of the pattern (TerrainNoise.Pattern). 0 = Perlin, also for old profiles.")]
        public int NoisePattern = 0;

        [Header("- Beat wave -")]
        public float WaveStrength = 0.25f;
        public float WaveSpeed = 1f;

        [Header("- Smoothing -")]
        public bool ApplySmoothing = true;
        [HideInInspector] public bool SmoothWithOutOfBounds = true; // not in use
        public bool SmoothWithOutOfBoundsCompletely = true;

        [Header("- Dampening -")]
        public bool ApplyDampening = true;
        public float DampenUp = 0.1f;
        public float DampenDown = 0.03f;

        [Header("- Out of Bounds -")]
        public int OutOfBoundsOperation = 2;
        public float OutOfBoundsMultiplier = 0.3f;
        public bool NeverTopHighestBand = true;

        [Header("- Terrace -")]
        public bool ApplyTerrace = false;
        [Min(1)] public int TerraceCount = 1;
        public int TerraceExecutionPoint = 0;

        [Header("- Shape -")]
        public List<TerrainShapeData> Shapes = new List<TerrainShapeData>() { new TerrainShapeData() };

        [Header("- Color -")]
        [Tooltip("Name of the palette the colors come from, or Custom. Profiles without color data use the default palette.")]
        public string ColorPaletteName = TerrainColorPalette.DefaultName;
        [Tooltip("Per color: x = hue (0-360), y = saturation (0-1), z = menu intensity. Empty means the palette of ColorPaletteName.")]
        public List<Vector3> ColorValues = new List<Vector3>();
        public float ColorOverallIntensity = 1f;
        public float BloomThreshold = TerrainColorPalette.DefaultBloomThreshold;
        public float BloomIntensity = TerrainColorPalette.DefaultBloomIntensity;
        public float BloomScatter = TerrainColorPalette.DefaultBloomScatter;
        [Tooltip("Lens dirt on the glow: fixed strength and extra strength on the beat.")]
        public float DirtStrength = TerrainLensDirt.DefaultStrength;
        public float DirtBeat = TerrainLensDirt.DefaultBeat;
        #endregion
        
        
        #region Constructor
        public static TerrainSettingsProfile ImportProfile(string path)
        {
            TerrainSettingsProfile profile = new TerrainSettingsProfile();
            
            return profile;
        }
        #endregion


        #region Reading
        public void CopyFromProfile(TerrainSettingsProfile profile)
        {
            if (profile == null)
                return;
            
            // Terrain
            FaceCountX = profile.FaceCountX;
            FaceCountY = profile.FaceCountY;
            TerrainHeight = profile.TerrainHeight;
            
            // Perlin
            ApplyPerlin = profile.ApplyPerlin;
            PerlinDepth = profile.PerlinDepth;
            PerlinZoom = profile.PerlinZoom;
            PerlinSpeedX = profile.PerlinSpeedX;
            PerlinSpeedY = profile.PerlinSpeedY;
            NoisePattern = profile.NoisePattern;
            WaveStrength = profile.WaveStrength;
            WaveSpeed = profile.WaveSpeed;

            // Smoothing
            ApplySmoothing = profile.ApplySmoothing;
            SmoothWithOutOfBounds = profile.SmoothWithOutOfBounds;
            SmoothWithOutOfBoundsCompletely = profile.SmoothWithOutOfBoundsCompletely;

            // Dampening
            ApplyDampening = profile.ApplyDampening;
            DampenUp = profile.DampenUp;
            DampenDown = profile.DampenDown;

            // Out of Bounds
            OutOfBoundsOperation = profile.OutOfBoundsOperation;
            OutOfBoundsMultiplier = profile.OutOfBoundsMultiplier;
            NeverTopHighestBand = profile.NeverTopHighestBand;

            // Terrace
            ApplyTerrace = profile.ApplyTerrace;
            TerraceCount = profile.TerraceCount;
            TerraceExecutionPoint = profile.TerraceExecutionPoint;

            // Color
            ColorPaletteName = profile.ColorPaletteName;
            ColorValues = new List<Vector3>(profile.ColorValues ?? new List<Vector3>());
            ColorOverallIntensity = profile.ColorOverallIntensity;
            BloomThreshold = profile.BloomThreshold;
            BloomIntensity = profile.BloomIntensity;
            BloomScatter = profile.BloomScatter;
            DirtStrength = profile.DirtStrength;
            DirtBeat = profile.DirtBeat;

            // Shape
            Shapes.Clear();
            foreach (TerrainShapeData shapeData in profile.Shapes)
                Shapes.Add(new TerrainShapeData(shapeData));
        }
        
        public void CopyFromTerrainSettings()
        {
            TerrainGenerator terrain = TerrainSettingsManager.Instance.Terrain;
            
            // Terrain
            FaceCountX = terrain.FaceCountX;
            FaceCountY = terrain.FaceCountZ;
            TerrainHeight = terrain.TerrainSizeY;
            
            // Perlin
            ApplyPerlin = terrain.ApplyPerlinNoise;
            PerlinDepth = terrain.PerlinStrength;
            PerlinZoom = terrain.PerlinNoiseZoom;
            PerlinSpeedX = terrain.PerlinOffsetSpeed.x;
            PerlinSpeedY = terrain.PerlinOffsetSpeed.y;
            NoisePattern = terrain.NoisePattern;
            WaveStrength = terrain.WaveStrength;
            WaveSpeed = terrain.WaveSpeed;

            // Smoothing
            ApplySmoothing = terrain.ApplyAudioSmoothing;
            SmoothWithOutOfBounds = terrain.SmoothLastBandWithOutOfBounds;
            SmoothWithOutOfBoundsCompletely = terrain.SmoothLastBandWithOutOfBoundsCompletely;

            // Dampening
            ApplyDampening = terrain.ApplyDampeningSetting;
            DampenUp = terrain.DampenLerpFactorUp;
            DampenDown = terrain.DampenLerpFactorDown;

            // Out of Bounds
            OutOfBoundsOperation = (int)terrain.OutOfBoundsOperation;
            OutOfBoundsMultiplier = terrain.OutOfBoundsFactor;
            NeverTopHighestBand = terrain.OutOfBoundsMaximumIsOutestBand;

            // Terrace
            ApplyTerrace = terrain.ApplyTerracing;
            TerraceCount = terrain.TerraceCount;
            TerraceExecutionPoint = (int)terrain.TerraceExecutionPoint;

            // Shape
            Shapes = new List<TerrainShapeData>();
            foreach (TerrainShape shapeSettings in terrain.Shapes)
                Shapes.Add(new TerrainShapeData(shapeSettings));
        }
        
        public void ExtractProfileFromUI()
        {
            foreach (TerrainGroupUI group in TerrainSettingsManager.Instance.Groups)
                group.ExtractProfile(this); 
        }
        #endregion
        
        
        #region Writing
        public void ApplyProfileToTerrain()
        {
            TerrainGenerator terrain = TerrainSettingsManager.Instance.Terrain;
            
            // Terrain
            terrain.FaceCountX = FaceCountX;
            terrain.FaceCountZ = FaceCountY;
            terrain.TerrainSizeY = TerrainHeight;
            
            // Perlin
            terrain.ApplyPerlinNoise = ApplyPerlin;
            terrain.PerlinStrength = PerlinDepth;
            terrain.PerlinNoiseZoom = PerlinZoom;
            terrain.PerlinOffsetSpeed = new Vector2(PerlinSpeedX, PerlinSpeedY);
            terrain.NoisePattern = NoisePattern;
            terrain.WaveStrength = WaveStrength;
            terrain.WaveSpeed = WaveSpeed;

            // Smoothing
            terrain.ApplyAudioSmoothing = ApplySmoothing;
            terrain.SmoothLastBandWithOutOfBounds = SmoothWithOutOfBounds;
            // terrain.SmoothLastBandWithOutOfBoundsCompletely = SmoothWithOutOfBoundsCompletely;

            // Dampening
            terrain.ApplyDampeningSetting = ApplyDampening;
            terrain.DampenLerpFactorUp = DampenUp;
            terrain.DampenLerpFactorDown = DampenDown;

            // Out of Bounds
            terrain.OutOfBoundsOperation = (TerrainGenerator.OutOfBounds)OutOfBoundsOperation;
            terrain.OutOfBoundsFactor = OutOfBoundsMultiplier;
            terrain.OutOfBoundsMaximumIsOutestBand = NeverTopHighestBand;

            // Terrace
            terrain.ApplyTerracing = ApplyTerrace;
            terrain.TerraceCount = TerraceCount;
            terrain.TerraceExecutionPoint = (TerrainGenerator.TerraceExecutionPointEnum)TerraceExecutionPoint;

            // Shape
            // terrain.Shapes ??= new List<TerrainShape>();
            while (terrain.Shapes.Count != Shapes.Count)
            {
                if (terrain.Shapes.Count < Shapes.Count)
                    terrain.Shapes.Add(new TerrainShape());
                else if (terrain.Shapes.Count > Shapes.Count)
                    terrain.Shapes.RemoveAt(terrain.Shapes.Count - 1);
            }

            for (int i = 0; i < Shapes.Count; i++)
            {
                TerrainShape terrainShape = terrain.Shapes[i];
                TerrainShapeData profileShape = Shapes[i];

                terrainShape.ApplyShape = profileShape.ApplyShape;
                terrainShape.Shape = (TerrainShape.AudioShape)profileShape.Shape;
                terrainShape.Strength = profileShape.Strength;
                terrainShape.Zoom = profileShape.Zoom;
                terrainShape.Invert = profileShape.Invert;
                terrainShape.Operation = (TerrainShape.AudioShapeOperation)profileShape.Operation;
                terrainShape.ApplyCustomAudioBandWidth = profileShape.BroadenInnerBands;
            }
        }
        
        public void ApplyProfileToUI()
        {
            foreach (TerrainGroupUI group in TerrainSettingsManager.Instance.Groups)
                group.ShowProfile(this); 
        }
        #endregion
    }

    
    [System.Serializable]
    public class TerrainShapeData
    {
        public bool ApplyShape;
        public int Shape;
        public float Strength;
        public float Zoom;
        public bool Invert;
        public int Operation;
        public bool BroadenInnerBands;

        public TerrainShapeData()
        {
            ApplyShape = true;
            Strength = 1;
            Zoom = 1.41f;
            Operation = 3;
        }
        
        public TerrainShapeData(TerrainShape profile)
        {
            ApplyShape = profile.ApplyShape;
            Shape = (int)profile.Shape;
            Strength = profile.Strength;
            Zoom = profile.Zoom;
            Invert = profile.Invert;
            Operation = (int) profile.Operation;
            BroadenInnerBands = profile.ApplyCustomAudioBandWidth;
        }
        
        public TerrainShapeData(TerrainShapeData profile)
        {
            ApplyShape = profile.ApplyShape;
            Shape = profile.Shape;
            Strength = profile.Strength;
            Zoom = profile.Zoom;
            Invert = profile.Invert;
            Operation = profile.Operation;
            BroadenInnerBands = profile.BroadenInnerBands;
        }
    }
}
