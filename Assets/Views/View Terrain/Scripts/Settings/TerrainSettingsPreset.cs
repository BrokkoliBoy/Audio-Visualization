using Gavi.Audio;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi
{
    [System.Serializable]
    public class TerrainSettingsPreset
    {
        /// <summary>Version of the file format. 0 = files from before versions existed. Raise CurrentFormatVersion when a change needs old files to be converted.</summary>
        public const int CurrentFormatVersion = 5;
        public int FormatVersion;
        public string Name = "Terrain Preset";
        
        #region Settings Variables
        [Header("- Terrain -")]
        public int FaceCountX = 90;
        public int FaceCountY = 90;
        public float TerrainHeight = 2;
        
        [Header("- Perlin -")]
        public bool ApplyPerlin = true;
        public float PerlinDepth = 0.6f;
        public float PerlinZoom = 0.3f;
        /// <summary>Zoom at the middle of the zoom slider (belongs to the pattern, not saved): the height follows the zoom relative to it. Travels with the zoom so both reach the terrain in the same frame.</summary>
        [System.NonSerialized] public float HeightZoomReference = 0.354f;
        public float HeightZoomStrength;
        public float PerlinSpeedX = 0;
        public float PerlinSpeedY = 1;
        [Tooltip("Index of the pattern (TerrainNoise.Pattern). 0 = Perlin, also for old presets.")]
        public int NoisePattern = (int)TerrainNoise.Pattern.DomainWarp;
        public bool NoiseInvert = false;
        [Tooltip("Turns the whole noise (all layers mixed) upside down.")]
        public bool NoiseInvertAll = false;
        [Tooltip("Extra pattern layers on top of the main pattern: layer 2 and layer 3.")]
        public TerrainNoiseLayerData NoiseLayer2 = new TerrainNoiseLayerData();
        public TerrainNoiseLayerData NoiseLayer3 = new TerrainNoiseLayerData();
        [Tooltip("Turns the pattern upside down on the beat every this many beats, 0 = off.")]
        public int NoiseFlipBeats = 0;

        [Header("- Beat wave -")]
        public bool ApplyWave = TerrainBeatWave.DefaultEnabled;
        public float WaveStrength = TerrainBeatWave.DefaultStrength;
        public float WaveSpeed = TerrainBeatWave.DefaultSpeed;
        public float WaveWidth = TerrainBeatWave.DefaultWidth;
        public float WaveFade = TerrainBeatWave.DefaultFade;
        [Tooltip("A wave starts on every n-th beat (1-8).")]
        public int WaveEveryNthBeat = TerrainBeatWave.DefaultEveryNthBeat;
        [Tooltip("TerrainBeatWave.Mode: 0 = Add, 1 = Floor, 2 = Absorb, 3 = Subtract, 4 = Minimum.")]
        public int WaveMode = TerrainBeatWave.DefaultMode;
        [Tooltip("TerrainBeatWave.Origin: 0 = Middle, 1 = Edge, 2 = Alternate, 3 = Bass edge.")]
        public int WaveOrigin = TerrainBeatWave.DefaultOrigin;
        public bool WaveUseNoise = TerrainBeatWave.DefaultUseNoise;

        [Header("- Smoothing -")]
        public bool ApplySmoothing = true;
        [HideInInspector] public bool SmoothWithOutOfBounds = true; // not in use
        public bool SmoothWithOutOfBoundsCompletely = true;

        [Header("- Dampening -")]
        public bool ApplyDampening = true;
        public float DampenUp = 0.02f;
        public float DampenDown = 0.02f;

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
        [Tooltip("Name of the palette the colors come from, or Custom. Presets without color data use the default palette.")]
        public string ColorPaletteName = TerrainColorPalette.DefaultName;
        [Tooltip("Per color: x = hue (0-360), y = saturation (0-1), z = menu intensity. Empty means the palette of ColorPaletteName.")]
        public List<Vector3> ColorValues = new List<Vector3>();
        public float ColorOverallIntensity = 1f;
        [Tooltip("Color curve, see TerrainColorPalette.")]
        public int TiltDirection = TerrainColorPalette.DefaultTiltDirection;
        public float FloorShare = TerrainColorPalette.DefaultFloorShare;
        public float ColorShift = TerrainColorPalette.DefaultColorShift;
        public float ColorHold = TerrainColorPalette.DefaultColorHold;
        public float PlateauTilt = TerrainColorPalette.DefaultPlateauTilt;
        public float ReliefStrength = TerrainColorPalette.DefaultRelief;
        public float HueBalance = TerrainColorPalette.DefaultHueBalance;
        public float BloomThreshold = TerrainColorPalette.DefaultBloomThreshold;
        public float BloomIntensity = TerrainColorPalette.DefaultBloomIntensity;
        public float BloomScatter = TerrainColorPalette.DefaultBloomScatter;
        [Tooltip("The whole glow module on or off. The tinted background does not depend on it.")]
        public bool BloomEnabled = true;
        public bool DirtEnabled = true;
        [Tooltip("Lens dirt on the glow: fixed strength and extra strength on the beat.")]
        public float DirtStrength = TerrainLensDirt.DefaultStrength;
        public float DirtBeat = TerrainLensDirt.DefaultBeat;
        [Tooltip("How far the background takes the hue of the base color: 0 = untinted, 1 = fully tinted.")]
        public float TintBackground = 1f;
        [Tooltip("How much the color comes from the band activity instead of the height: gradient position, brightness, glow. 0 = from the height.")]
        public float ColorBandHue = 0f;
        public float ColorBandBrightness = 0f;
        public float ColorBandGlow = 0f;
        [Tooltip("Same from the strength of the noise pattern at the vertex: gradient position, brightness, glow. 0 = off.")]
        public float ColorPatternHue = 0f;
        public float ColorPatternBrightness = 0f;
        public float ColorPatternGlow = 0f;
        [Tooltip("Evens out the colors between noise patterns. 0 = off.")]
        public float ColorNoiseBalance = 1f;
        [Tooltip("Soft end of the gradient for heights over 1. 0 = hard end.")]
        public float ColorHeadroom = 0f;

        [Header("- Morph -")]
        public bool ApplyMorph = false;
        [Tooltip("TerrainGenerator.MorphShape: 1 = Dome, 2 = Planet, 3 = Tube, 5 = Pringle, 6 = Flag, 7 = Twist. Other numbers (removed shapes) give the dome.")]
        public int MorphShape = (int)TerrainGenerator.MorphShape.Planet;
        public float MorphAmount = 0.5f;
        public float MorphBlendSeconds = TerrainGenerator.DefaultMorphBlendSeconds;
        [Header("- Morph motion -")]
        public float MotionBeat = 0f;
        public float MotionAttack = TerrainGenerator.DefaultMotionAttack;
        public float MotionRelease = TerrainGenerator.DefaultMotionRelease;
        public float MotionBounce = 0f;
        public float MotionWaveDepth = 0f;
        public float MotionWaveBeats = TerrainGenerator.DefaultMotionWaveBeats;

        [Header("- Performance -")]
        [Tooltip("The noise is calculated again every this many mesh updates, 1 = every update.")]
        public int NoiseInterval = TerrainGenerator.DefaultNoiseInterval;
        [Tooltip("The noise work is spread over the updates instead of done all at once every n-th update.")]
        public bool NoiseSpread = TerrainGenerator.DefaultNoiseSpread;

        [Header("- Test signal -")]
        [Tooltip("AudioTestSignal.Signal: 0 = Flat, 1 = Bass only, 2 = Falling, 3 = Rising, 4 = Every other band. Whether the test signal is on is not saved, it always starts off.")]
        public int TestSignal = 0;
        public float TestSignalLevel = AudioTestSignal.DefaultLevel;
        public float TestSignalBeatStrength = 0f;
        public bool TestSignalBeatFromSong = false;
        public bool TestSignalFreezeNoise = false;
        #endregion
        
        
        #region Constructor
        public static TerrainSettingsPreset ImportPreset(string path)
        {
            TerrainSettingsPreset preset = new TerrainSettingsPreset();
            
            return preset;
        }
        #endregion


        #region Reading
        public void CopyFromPreset(TerrainSettingsPreset preset)
        {
            if (preset == null)
                return;
            
            // Terrain
            FaceCountX = preset.FaceCountX;
            FaceCountY = preset.FaceCountY;
            TerrainHeight = preset.TerrainHeight;
            
            // Perlin
            ApplyPerlin = preset.ApplyPerlin;
            PerlinDepth = preset.PerlinDepth;
            PerlinZoom = preset.PerlinZoom;
            HeightZoomReference = preset.HeightZoomReference;
            HeightZoomStrength = preset.HeightZoomStrength;
            PerlinSpeedX = preset.PerlinSpeedX;
            PerlinSpeedY = preset.PerlinSpeedY;
            NoisePattern = preset.NoisePattern;
            NoiseInvert = preset.NoiseInvert;
            NoiseInvertAll = preset.NoiseInvertAll;
            NoiseLayer2.CopyFrom(preset.NoiseLayer2);
            NoiseLayer3.CopyFrom(preset.NoiseLayer3);
            NoiseFlipBeats = preset.NoiseFlipBeats;
            ApplyWave = preset.ApplyWave;
            WaveStrength = preset.WaveStrength;
            WaveSpeed = preset.WaveSpeed;
            WaveWidth = preset.WaveWidth;
            WaveFade = preset.WaveFade;
            WaveEveryNthBeat = preset.WaveEveryNthBeat;
            WaveMode = preset.WaveMode;
            WaveOrigin = preset.WaveOrigin;
            WaveUseNoise = preset.WaveUseNoise;

            // Smoothing
            ApplySmoothing = preset.ApplySmoothing;
            SmoothWithOutOfBounds = preset.SmoothWithOutOfBounds;
            SmoothWithOutOfBoundsCompletely = preset.SmoothWithOutOfBoundsCompletely;

            // Dampening
            ApplyDampening = preset.ApplyDampening;
            DampenUp = preset.DampenUp;
            DampenDown = preset.DampenDown;

            // Out of Bounds
            OutOfBoundsOperation = preset.OutOfBoundsOperation;
            OutOfBoundsMultiplier = preset.OutOfBoundsMultiplier;
            NeverTopHighestBand = preset.NeverTopHighestBand;

            // Terrace
            ApplyTerrace = preset.ApplyTerrace;
            TerraceCount = preset.TerraceCount;
            TerraceExecutionPoint = preset.TerraceExecutionPoint;

            // Color
            ColorPaletteName = preset.ColorPaletteName;
            ColorValues = new List<Vector3>(preset.ColorValues ?? new List<Vector3>());
            ColorOverallIntensity = preset.ColorOverallIntensity;
            TiltDirection = preset.TiltDirection;
            FloorShare = preset.FloorShare;
            ColorShift = preset.ColorShift;
            ColorHold = preset.ColorHold;
            PlateauTilt = preset.PlateauTilt;
            ReliefStrength = preset.ReliefStrength;
            HueBalance = preset.HueBalance;
            BloomThreshold = preset.BloomThreshold;
            BloomIntensity = preset.BloomIntensity;
            BloomScatter = preset.BloomScatter;
            BloomEnabled = preset.BloomEnabled;
            DirtEnabled = preset.DirtEnabled;
            DirtStrength = preset.DirtStrength;
            DirtBeat = preset.DirtBeat;
            TintBackground = preset.TintBackground;
            ColorBandHue = preset.ColorBandHue;
            ColorBandBrightness = preset.ColorBandBrightness;
            ColorBandGlow = preset.ColorBandGlow;
            ColorPatternHue = preset.ColorPatternHue;
            ColorPatternBrightness = preset.ColorPatternBrightness;
            ColorPatternGlow = preset.ColorPatternGlow;
            ColorNoiseBalance = preset.ColorNoiseBalance;
            ColorHeadroom = preset.ColorHeadroom;

            // Morph
            ApplyMorph = preset.ApplyMorph;
            MorphShape = preset.MorphShape;
            MorphAmount = preset.MorphAmount;
            MorphBlendSeconds = preset.MorphBlendSeconds;
            MotionBeat = preset.MotionBeat;
            MotionAttack = preset.MotionAttack;
            MotionRelease = preset.MotionRelease;
            MotionBounce = preset.MotionBounce;
            MotionWaveDepth = preset.MotionWaveDepth;
            MotionWaveBeats = preset.MotionWaveBeats;

            // Performance
            NoiseInterval = preset.NoiseInterval;
            NoiseSpread = preset.NoiseSpread;

            // Test signal
            TestSignal = preset.TestSignal;
            TestSignalLevel = preset.TestSignalLevel;
            TestSignalBeatStrength = preset.TestSignalBeatStrength;
            TestSignalBeatFromSong = preset.TestSignalBeatFromSong;
            TestSignalFreezeNoise = preset.TestSignalFreezeNoise;

            // Shape
            Shapes.Clear();
            foreach (TerrainShapeData shapeData in preset.Shapes)
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
            HeightZoomReference = terrain.HeightZoomReference;
            HeightZoomStrength = terrain.HeightZoomStrength;
            PerlinSpeedX = terrain.PerlinOffsetSpeed.x;
            PerlinSpeedY = terrain.PerlinOffsetSpeed.y;
            NoisePattern = terrain.NoisePattern;
            NoiseInvert = terrain.NoiseInvert;
            NoiseInvertAll = terrain.NoiseInvertAll;
            NoiseLayer2.CopyFrom(terrain.GetExtraLayer(0));
            NoiseLayer3.CopyFrom(terrain.GetExtraLayer(1));
            NoiseFlipBeats = terrain.NoiseFlipBeats;
            if (terrain.BeatWave != null)
            {
                ApplyWave = terrain.BeatWave.Enabled;
                WaveStrength = terrain.BeatWave.Strength;
                WaveSpeed = terrain.BeatWave.Speed;
                WaveWidth = terrain.BeatWave.Width;
                WaveFade = terrain.BeatWave.Fade;
                WaveEveryNthBeat = terrain.BeatWave.EveryNthBeat;
                WaveMode = terrain.BeatWave.WaveMode;
                WaveOrigin = terrain.BeatWave.WaveOrigin;
                WaveUseNoise = terrain.BeatWave.UseNoise;
            }

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

            // Color by band
            ColorBandHue = terrain.ColorBandHue;
            ColorBandBrightness = terrain.ColorBandBrightness;
            ColorBandGlow = terrain.ColorBandGlow;
            ColorPatternHue = terrain.ColorPatternHue;
            ColorPatternBrightness = terrain.ColorPatternBrightness;
            ColorPatternGlow = terrain.ColorPatternGlow;
            ColorNoiseBalance = terrain.ColorNoiseBalance;
            ColorHeadroom = terrain.ColorHeadroom;

            // Morph
            ApplyMorph = terrain.ApplyMorph;
            MorphShape = terrain.Morph;
            MorphAmount = terrain.MorphAmount;
            MorphBlendSeconds = terrain.MorphBlendSeconds;
            MotionBeat = terrain.MotionBeat;
            MotionAttack = terrain.MotionAttack;
            MotionRelease = terrain.MotionRelease;
            MotionBounce = terrain.MotionBounce;
            MotionWaveDepth = terrain.MotionWaveDepth;
            MotionWaveBeats = terrain.MotionWaveBeats;

            // Performance
            NoiseInterval = terrain.NoiseInterval;
            NoiseSpread = terrain.NoiseSpread;

            // Test signal
            if (AudioTestSignal.Instance != null)
            {
                TestSignal = AudioTestSignal.Instance.SignalIndex;
                TestSignalLevel = AudioTestSignal.Instance.Level;
                TestSignalBeatStrength = AudioTestSignal.Instance.BeatStrength;
                TestSignalBeatFromSong = AudioTestSignal.Instance.BeatStrengthFromSong;
                TestSignalFreezeNoise = AudioTestSignal.Instance.FreezeNoise;
            }

            // Shape
            Shapes = new List<TerrainShapeData>();
            foreach (TerrainShape shapeSettings in terrain.Shapes)
                Shapes.Add(new TerrainShapeData(shapeSettings));
        }
        
        public void ExtractPresetFromUI()
        {
            foreach (TerrainGroupUI group in TerrainSettingsManager.Instance.Groups)
                group.ExtractPreset(this); 
        }
        #endregion
        
        
        #region Writing
        public void ApplyPresetToTerrain()
        {
            TerrainGenerator terrain = TerrainSettingsManager.Instance.Terrain;
            terrain.RequestRefresh();

            // Terrain
            terrain.FaceCountX = FaceCountX;
            terrain.FaceCountZ = FaceCountY;
            terrain.TerrainSizeY = TerrainHeight;
            
            // Perlin
            terrain.ApplyPerlinNoise = ApplyPerlin;
            terrain.PerlinStrength = PerlinDepth;
            terrain.PerlinNoiseZoom = PerlinZoom;
            terrain.HeightZoomReference = HeightZoomReference;
            terrain.HeightZoomStrength = HeightZoomStrength;
            terrain.PerlinOffsetSpeed = new Vector2(PerlinSpeedX, PerlinSpeedY);
            terrain.NoisePattern = NoisePattern;
            terrain.NoiseInvert = NoiseInvert;
            terrain.NoiseInvertAll = NoiseInvertAll;
            terrain.GetExtraLayer(0).CopyFrom(NoiseLayer2);
            terrain.GetExtraLayer(1).CopyFrom(NoiseLayer3);
            terrain.NoiseFlipBeats = NoiseFlipBeats;
            if (terrain.BeatWave != null)
            {
                terrain.BeatWave.Enabled = ApplyWave;
                terrain.BeatWave.Strength = WaveStrength;
                terrain.BeatWave.Speed = WaveSpeed;
                terrain.BeatWave.Width = WaveWidth;
                terrain.BeatWave.Fade = WaveFade;
                terrain.BeatWave.EveryNthBeat = WaveEveryNthBeat;
                terrain.BeatWave.WaveMode = WaveMode;
                terrain.BeatWave.WaveOrigin = WaveOrigin;
                terrain.BeatWave.UseNoise = WaveUseNoise;
            }

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

            // Color by band
            terrain.ColorBandHue = ColorBandHue;
            terrain.ColorBandBrightness = ColorBandBrightness;
            terrain.ColorBandGlow = ColorBandGlow;
            terrain.ColorPatternHue = ColorPatternHue;
            terrain.ColorPatternBrightness = ColorPatternBrightness;
            terrain.ColorPatternGlow = ColorPatternGlow;
            terrain.ColorNoiseBalance = ColorNoiseBalance;
            terrain.ColorHeadroom = ColorHeadroom;

            // Morph
            terrain.ApplyMorph = ApplyMorph;
            terrain.Morph = MorphShape;
            terrain.MorphAmount = MorphAmount;
            terrain.MorphBlendSeconds = MorphBlendSeconds;
            terrain.MotionBeat = MotionBeat;
            terrain.MotionAttack = MotionAttack;
            terrain.MotionRelease = MotionRelease;
            terrain.MotionBounce = MotionBounce;
            terrain.MotionWaveDepth = MotionWaveDepth;
            terrain.MotionWaveBeats = MotionWaveBeats;

            // Performance
            terrain.NoiseInterval = NoiseInterval;
            terrain.NoiseSpread = NoiseSpread;

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
                TerrainShapeData presetShape = Shapes[i];

                terrainShape.ApplyShape = presetShape.ApplyShape;
                // old presets can still hold one of the removed corner shapes
                terrainShape.Shape = System.Enum.IsDefined(typeof(TerrainShape.AudioShape), (int)presetShape.Shape)
                    ? (TerrainShape.AudioShape)presetShape.Shape
                    : TerrainShape.AudioShape.Circle;
                terrainShape.Strength = presetShape.Strength;
                terrainShape.Zoom = presetShape.Zoom;
                terrainShape.Invert = presetShape.Invert;
                terrainShape.Operation = (TerrainShape.AudioShapeOperation)presetShape.Operation;
                terrainShape.ApplyCustomAudioBandWidth = presetShape.BroadenInnerBands;
            }
        }
        
        public void ApplyPresetToUI()
        {
            foreach (TerrainGroupUI group in TerrainSettingsManager.Instance.Groups)
                group.ShowPreset(this); 
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
            Zoom = 1f;
            Operation = 3;
        }
        
        public TerrainShapeData(TerrainShape preset)
        {
            ApplyShape = preset.ApplyShape;
            Shape = (int)preset.Shape;
            Strength = preset.Strength;
            Zoom = preset.Zoom;
            Invert = preset.Invert;
            Operation = (int) preset.Operation;
            BroadenInnerBands = preset.ApplyCustomAudioBandWidth;
        }
        
        public TerrainShapeData(TerrainShapeData preset)
        {
            ApplyShape = preset.ApplyShape;
            Shape = preset.Shape;
            Strength = preset.Strength;
            Zoom = preset.Zoom;
            Invert = preset.Invert;
            Operation = preset.Operation;
            BroadenInnerBands = preset.BroadenInnerBands;
        }
    }
}
