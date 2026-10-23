using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Gavi.Utility;
using Gavi.Audio;
using Gavi.Modules.DataModules.ArrayModules.FloatArrayModules.VoidToFloatArrayModules;

namespace Gavi
{
    public class TerrainGenerator : MonoBehaviour
    {
        [Tooltip("Used for debug purposes in the editor.")]
        [SerializeField] private bool _generate;
        [Tooltip("When true, this will update the mesh with all of its calculations depending on the settings each frame.")]
        [SerializeField] private bool _generateOnUpdate;

        [Header("--- Terrain ---")]
        [Tooltip("Size of the terrains local transforms positions x component in world coordinates, regardless of the face count.")]
        [SerializeField] private float _terrainSizeX = 300;
        [Tooltip("Size of the terrains local transforms positions y component in world coordinates. Note, that this value can potentially exceeded depending on the other settings!")]
        [SerializeField] private float _terrainSizeY = 100;
        public float TerrainSizeY { get => _terrainSizeY; set => _terrainSizeY = value; }
        [Tooltip("Size of the terrains local transforms positions z component in world coordinates, regardless of the face count.")]
        [SerializeField] private float _terrainSizeZ = 300;
        //[SerializeField] private Vector3Int _faceCount = new Vector3Int(30, 1, 30);
        [Tooltip("Amount of faces of the mesh in x dimension.")]
        [SerializeField] private int _faceCountX = 30;
        public int FaceCountX { get => _faceCountX; set => _faceCountX = value; }
        [Tooltip("Amount of faces of the mesh in z dimension.")]
        [SerializeField] private int _faceCountZ = 30;
        public int FaceCountZ { get => _faceCountZ; set => _faceCountZ = value; }

        [Header("--- Perlin ---")]
        [Tooltip("When true, applies perlin noise with the specified settings.")]
        [SerializeField] private bool _applyPerlinNoise;
        public bool ApplyPerlinNoise { get => _applyPerlinNoise; set => _applyPerlinNoise = value; }
        [Tooltip("Amount of deformation in respect to the terrains height.")]
        [SerializeField] private float _perlinStrength = 0.5f;
        public float PerlinStrength { get => _perlinStrength; set => _perlinStrength = value; }
        [Tooltip("Offset x of perlin noise.")]
        [SerializeField] private float _perlinOffsetX;
        [Tooltip("Offset y of perlin noise.")]
        [SerializeField] private float _perlinOffsetY;
        [Tooltip("Each frame applies offset to the perlin noise.")]
        [SerializeField] private Vector2 _perlinOffsetSpeed;
        public Vector2 PerlinOffsetSpeed { get => _perlinOffsetSpeed; set => _perlinOffsetSpeed = value; }
        [Tooltip("Zoom factor of perlin noise.")]
        [SerializeField] private float _noiseZoom = 0.3f;
        public float PerlinNoiseZoom { get => _noiseZoom; set => _noiseZoom = value; }
        [Tooltip("How strongly the terrain height follows the noise zoom, so the slope of the shapes stays the same: zooming out lowers the terrain, zooming in raises it. 0 = off, 1 = full.")]
        [Range(0f, 1f)]
        [SerializeField] private float _heightZoomStrength;
        public float HeightZoomStrength { get => _heightZoomStrength; set => _heightZoomStrength = Mathf.Clamp01(value); }
        private const float HeightZoomReference = 0.354f; // the noise zoom around the middle of the zoom slider, where the height is the set terrain height
        private const float HeightZoomMinFactor = 0.35f;
        private const float HeightZoomMaxFactor = 2.5f;
        /// <summary>Terrain height with the zoom coupling applied, limited so extreme zoom values neither flatten nor spike the terrain.</summary>
        public float EffectiveTerrainSizeY => _heightZoomStrength <= 0f
            ? _terrainSizeY
            : _terrainSizeY * Mathf.Lerp(1f, Mathf.Clamp(HeightZoomReference / Mathf.Max(0.01f, _noiseZoom), HeightZoomMinFactor, HeightZoomMaxFactor), _heightZoomStrength);
        /// <summary>The face count the perlin zoom values were made for. At this face count the zoom is unchanged.</summary>
        private const float PerlinReferenceFaceCount = 97f;
        [Tooltip("Pattern that shapes the height with the settings above. Perlin is the original.")]
        [SerializeField] private TerrainNoise.Pattern _noisePattern = TerrainNoise.Pattern.Perlin;
        public int NoisePattern { get => (int)_noisePattern; set => _noisePattern = (TerrainNoise.Pattern)Mathf.Clamp(value, 0, TerrainNoise.Names.Length - 1); }
        [Tooltip("Turns the pattern upside down: high becomes low and low becomes high. Some patterns (Voronoi, Hexagons ...) are upside down by themselves, this turns them back.")]
        [SerializeField] private bool _noiseInvert;
        public bool NoiseInvert { get => _noiseInvert; set => _noiseInvert = value; }
        [Tooltip("Turns the pattern upside down on the beat, every this many beats. 0 = off. The pattern flows from one state to the other.")]
        [SerializeField] private int _noiseFlipBeats;
        public int NoiseFlipBeats { get => _noiseFlipBeats; set => _noiseFlipBeats = Mathf.Max(0, value); }
        /// <summary>Seconds the pattern needs to flow to the other state at most (shorter when the beats are faster).</summary>
        private const float InvertFlowSeconds = 0.4f;
        // 0 = the pattern as it is, 1 = upside down. Moves smoothly towards the wanted state.
        private float _invertAmount;
        private float _invertEased;
        private bool _invertStateKnown;
        private bool _invertMoving;
        private TerrainNoise.Pattern _invertPattern;
        private int _lastBeatCount;
        private bool _beatFlipped;

        [Header("--- Beat wave ---")]
        [Tooltip("Rings that run over the terrain on the beat and the bass. If null, there are no waves.")]
        [SerializeField] private TerrainBeatWave _beatWave;
        public TerrainBeatWave BeatWave => _beatWave;

        [Header("--- Terrace ---")]
        [Tooltip("Applies a terrace like structure to the mesh. The final terrace value will be clamped to a maximum value of 1 and a minimum value of 0.")]
        [SerializeField] private bool _applyTerrace;
        public bool ApplyTerracing { get => _applyTerrace; set => _applyTerrace = value; }
        [Tooltip("The terrace count dictates how many terraces should be applied.")]
        [SerializeField] private int _terraceCount;
        public int TerraceCount { get => _terraceCount; set => _terraceCount = value; }
        [Tooltip("Tells when to execute the terrace function.")]
        [SerializeField] private TerraceExecutionPointEnum _terraceExecutionPoint;
        public enum TerraceExecutionPointEnum { AfterPerlin, AfterAudio, AfterDampening }
        public TerraceExecutionPointEnum TerraceExecutionPoint { get => _terraceExecutionPoint; set => _terraceExecutionPoint = value; }

        [Header("--- Color ---")]
        [Tooltip("Apply color of the vertices by the height of it.")]
        [SerializeField] private bool _applyColorByHeight;
        [Tooltip("Color gradient of the vertices. Left displays height of 0, right displays height of _terrainSizeY.")]
        [GradientUsage(true)]
        [SerializeField] private Gradient _colorHeightGradient;
        public Gradient ColorHeightGradient => _colorHeightGradient;

        /// <summary>Own color table (TerrainJobs.LutSize entries) that replaces the gradient. Null = use the gradient.</summary>
        private Color[] _colorTableOverride;
        public void SetColorTable(Color[] table) => _colorTableOverride = table;
        [Tooltip("When true, vertices that lie outside of every active audio shape are darkened by the brightness below. Only the color changes, their height still follows the out of bounds operation.")]
        [SerializeField] private bool _colorOutOfBounds;
        [Tooltip("Brightness of the height gradient color for vertices outside of all active audio shapes. 1 = unchanged, 0 = black. Only used if _colorOutOfBounds is true.")]
        [Range(0, 1)]
        [SerializeField] private float _outOfBoundsBrightness = 0.2f;
        [Tooltip("When true, triangles whose three vertices all lie outside of every active audio shape are left out of the mesh, so the unused area is not rendered at all.")]
        [SerializeField] private bool _hideOutOfBounds;
        [Tooltip("Where the color comes from: 0 = the height of the vertex (as before), 1 = how strongly the frequency band of the vertex moves. Hue: the place in the gradient.")]
        [Range(0, 1)]
        [SerializeField] private float _colorBandHue;
        public float ColorBandHue { get => _colorBandHue; set => _colorBandHue = Mathf.Clamp01(value); }
        [Tooltip("0 = the brightness stays as the gradient gives it, 1 = quiet bands are dark and strong bands bright.")]
        [Range(0, 1)]
        [SerializeField] private float _colorBandBrightness;
        public float ColorBandBrightness { get => _colorBandBrightness; set => _colorBandBrightness = Mathf.Clamp01(value); }
        [Tooltip("0 = no extra glow, 1 = strong bands shine up to four times brighter, which the bloom turns into glow.")]
        [Range(0, 1)]
        [SerializeField] private float _colorBandGlow;
        public float ColorBandGlow { get => _colorBandGlow; set => _colorBandGlow = Mathf.Clamp01(value); }
        [Tooltip("Evens out the colors between noise patterns. A pattern that is high on average would show only the upper colors, a low one only the lower. 0 = colors follow the height as it is, 1 = every pattern uses about the same part of the gradient.")]
        [Range(0, 1)]
        [SerializeField] private float _colorNoiseBalance = 1f;
        public float ColorNoiseBalance { get => _colorNoiseBalance; set => _colorNoiseBalance = Mathf.Clamp01(value); }
        [Tooltip("Fake light: shades the colors by the slope of the ground against a fixed light, in exposure steps (surfaces facing the light brighter, the others darker). Gives contours where the color is flat. 0 = off.")]
        [Range(0, 3)]
        [SerializeField] private float _reliefStrength;
        public float ReliefStrength { get => _reliefStrength; set => _reliefStrength = Mathf.Clamp(value, 0f, 3f); }
        [Tooltip("Heights above the top of the gradient (loud bands push vertices over 1) keep a little color variation instead of all showing the last color. 0 = hard end as before, 1 = soft end: the top colors are reached later and the last one only approaches.")]
        [Range(0, 1)]
        [SerializeField] private float _colorHeadroom;
        public float ColorHeadroom { get => _colorHeadroom; set => _colorHeadroom = Mathf.Clamp01(value); }

        [Header("--- Morph ---")]
        [Tooltip("Bends the whole mesh after the heights are calculated. Off = the flat terrain.")]
        [SerializeField] private bool _applyMorph;
        public bool ApplyMorph { get => _applyMorph; set => _applyMorph = value; }
        [SerializeField] private MorphShape _morphShape = MorphShape.Dome;
        /// <summary>The numbers are what profiles store, so they stay as they were when a shape was removed (0 was Bowl, 4 Halfpipe).</summary>
        public enum MorphShape { Dome = 1, Planet = 2, Tube = 3, Pringle = 5, Flag = 6, Twist = 7 }
        /// <summary>The shapes in the order of the menu.</summary>
        public static readonly MorphShape[] MorphOrder = { MorphShape.Planet, MorphShape.Twist, MorphShape.Tube, MorphShape.Dome, MorphShape.Pringle, MorphShape.Flag };
        public static readonly string[] MorphNames = { "Planet", "Twist", "Tube", "Dome", "Pringle", "Flag" };
        /// <summary>The <see cref="MorphShape"/> as number. A number of a removed shape gives the dome.</summary>
        public int Morph { get => (int)_morphShape; set => _morphShape = System.Array.IndexOf(MorphOrder, (MorphShape)value) >= 0 ? (MorphShape)value : MorphShape.Dome; }
        [Tooltip("How far the mesh is bent. 0 = flat, 1 = fully bent.")]
        [Range(0, 1)]
        [SerializeField] private float _morphAmount = 0.5f;
        public float MorphAmount { get => _morphAmount; set => _morphAmount = Mathf.Clamp01(value); }

        [Header("--- Morph motion ---")]
        [Tooltip("How much the amount rises on every beat (added to the amount). 0 = off.")]
        [Range(0f, 1f)]
        [SerializeField] private float _motionBeat;
        [Tooltip("Beats the amount takes to rise to the peak after a beat.")]
        [SerializeField] private float _motionAttack = DefaultMotionAttack;
        [Tooltip("Beats the amount takes to fall back after the peak.")]
        [SerializeField] private float _motionRelease = DefaultMotionRelease;
        [Tooltip("0 = the amount glides, 1 = it swings over the peak like on a spring.")]
        [Range(0f, 1f)]
        [SerializeField] private float _motionBounce;
        [Tooltip("The amount swings around its value by this much, slowly and in time with the beats. 0 = off.")]
        [Range(0f, 1f)]
        [SerializeField] private float _motionWaveDepth;
        [Tooltip("Beats of one full swing of the wave.")]
        [SerializeField] private float _motionWaveBeats = DefaultMotionWaveBeats;

        public const float DefaultMotionAttack = 0.5f;
        public const float DefaultMotionRelease = 1.5f;
        public const float DefaultMotionWaveBeats = 8f;
        public float MotionBeat { get => _motionBeat; set => _motionBeat = Mathf.Clamp01(value); }
        public float MotionAttack { get => _motionAttack; set => _motionAttack = Mathf.Clamp(value, 0.1f, 4f); }
        public float MotionRelease { get => _motionRelease; set => _motionRelease = Mathf.Clamp(value, 0.1f, 8f); }
        public float MotionBounce { get => _motionBounce; set => _motionBounce = Mathf.Clamp01(value); }
        public float MotionWaveDepth { get => _motionWaveDepth; set => _motionWaveDepth = Mathf.Clamp01(value); }
        public float MotionWaveBeats { get => _motionWaveBeats; set => _motionWaveBeats = Mathf.Clamp(value, 1f, 128f); }

        [Header("--- Audio ---")]
        [Tooltip("When true, the mesh will apply height to the vertices according to audio sample data.")]
        [SerializeField] private bool _applyAudio;
        public bool ApplyAudioSetting { get => _applyAudio; set => _applyAudio = value; }
        [Tooltip("The samples to use the audio information from. If null, no audio will be applied.")]
        [SerializeField] SpectrumDataContainerModule _samplesContainer;
        //public SamplesContainer SamplesContainer { get => _samplesContainer; set => _samplesContainer = value; }
        //[SerializeField] private AudioSampleSettings _audioSampleSettings;
        //[SerializeField] private AudioSampler _audioSampler;


        [Header("- Audio Shape -")]
        [Tooltip("List of shapes that the samples should be cast onto. If none are specified, no audio will be displayed.")]
        [SerializeField] private List<TerrainShape> _audioShapeSettings = new List<TerrainShape>();

        public List<TerrainShape> Shapes { get => _audioShapeSettings; set => _audioShapeSettings = value; }

        [Tooltip("Only usable in editor mode. If true, the original audio data will be replaced with the value of 1 at the first audio band and constantly diminishing values for following audio bands.")]
        [SerializeField] private bool _applyAudioDebugMode;
        [Header("- Audio Index Out Of Bounds -")]
        [Tooltip("Handles vertices that are out of the shapes bounds. For example, a circle does contain vertices at normalized coordinates (1/1) but the mesh does.")]
        [SerializeField] private OutOfBounds _outOfBoundsOperation = OutOfBounds.InnerBand;
        public OutOfBounds OutOfBoundsOperation { get => _outOfBoundsOperation; set => _outOfBoundsOperation = value; }
        [Tooltip("Flat: out of bounds vertices stay at the bottom (height 0). Appended last so that existing profiles keep their meaning.")]
        public enum OutOfBounds { Constant, OuterBand, InnerBand, Average, Flat }
        [Tooltip("Factor to apply to the out of bounds vertices height.")]
        [SerializeField] private float _outOfBoundsFactor = 0.3f;
        public float OutOfBoundsFactor { get => _outOfBoundsFactor; set => _outOfBoundsFactor = value; }
        [Tooltip("If true, the out of bounds value will be capped by the value of the last band. This capping happens AFTER apply the _outOfBoundsFactor.")]
        [SerializeField] private bool _outOfBoundsMaximumIsOutestBand = true;
        public bool OutOfBoundsMaximumIsOutestBand { get => _outOfBoundsMaximumIsOutestBand; set => _outOfBoundsMaximumIsOutestBand = value; }


        [Header("- Audio Smooth between Bands -")]
        [Tooltip("If true, the vertices height of an audio band will blend into the adjacent bands height according to the specified blending curve.")]
        [SerializeField] private bool _applyAudioSmoothing = true;
        public bool ApplyAudioSmoothing { get => _applyAudioSmoothing; set => _applyAudioSmoothing = value; }
        [Tooltip("Blending curve to be used. The final height of a vertices height will be determined with the formula: ownHeight * f(x) + adjacentHeight * (1 - f(x)). Here, x is determined by the " +
            "distance fraction to its band center [0-1]. A value of x near 1 means that the vertex is near the border to the adjacent audio band. To make things smooth, make sure to have f(0)=1 and f(1)=0.5f. " +
            "When this is the case, the vertices that are placed exactly on the border of two bands will inherit the height exactly between the two bands. Otherwise the border will have a height gap.")]
        [SerializeField] private AnimationCurve _audioSmoothingCurve;
        [Tooltip("By default, the vertices of the first and last audio band facing their respective adjacents will only into its only adjacent band. If set to true, this will enable the last band to " +
            "try to blend into the out of bounds value. (The first band stays untouched)")]
        [SerializeField] private bool _smoothLastBandWithOutOfBounds = true;
        public bool SmoothLastBandWithOutOfBounds { get => _smoothLastBandWithOutOfBounds; set => _smoothLastBandWithOutOfBounds = value; }
        [Tooltip("When true, the evaluation of the _audioSmoothingCurve will be remapped from value f(1) = y [typically with y=0.5f] to f(1) = 0 in order for the last band to completely blend into " +
            "the out of bounds band. It actually would not blend into the out of bounds band perfectly since the out of bounds band does not blend into the last band. Enabling it also has the effect " +
            "that this smoothing of the last band into the out of bounds band will have a twice as steep curviture than the other blendings.")]
        [SerializeField] private bool _smoothLastBandWithOutOfBoundsCompletely = true;
        public bool SmoothLastBandWithOutOfBoundsCompletely { get => _smoothLastBandWithOutOfBoundsCompletely; set => _smoothLastBandWithOutOfBoundsCompletely = value; }

        [Header("--- Damping Y ---")]
        [Tooltip("When set to true, the height of the vertices will be lerped to its actual target height instead of doing so immediately.")]
        [SerializeField] private bool _applyDampening = true;
        public bool ApplyDampeningSetting { get => _applyDampening; set => _applyDampening = value; }
        [Tooltip("Lerp factor when the vertices target height is lower that the current height.")]
        [SerializeField] private float _dampenLerpFactorDown = 0.5f;
        public float DampenLerpFactorDown { get => _dampenLerpFactorDown; set => _dampenLerpFactorDown = value; }
        [Tooltip("Lerp factor when the vertices target height is higher that the current height.")]
        [SerializeField] private float _dampenLerpFactorUp = 0.5f;
        public float DampenLerpFactorUp { get => _dampenLerpFactorUp; set => _dampenLerpFactorUp = value; }

        [Header("--- Performance (part of the terrain profile) ---")]
        [Tooltip("The noise pattern is calculated again only every this many mesh updates. In between it flows smoothly from the last result to the next one. 1 = every update.")]
        [SerializeField] private int _noiseInterval = DefaultNoiseInterval;
        [Tooltip("Only meaningful with a noise interval above 1. Off: the whole pattern is calculated in one update every n-th update. On: every update calculates a different n-th of the points, so the work is the same in every frame and no part of the pattern flows in step with the others.")]
        [SerializeField] private bool _noiseSpread = DefaultNoiseSpread;

        public const int DefaultNoiseInterval = 1;
        public const bool DefaultNoiseSpread = false;
        public const int MaxNoiseInterval = 8;

        public int NoiseInterval { get => _noiseInterval; set => _noiseInterval = Mathf.Clamp(value, 1, MaxNoiseInterval); }
        public bool NoiseSpread { get => _noiseSpread; set => _noiseSpread = value; }

        /// <summary>Time the last mesh updates took on average (milliseconds on the main thread, the worker threads included).</summary>
        public float UpdateMilliseconds => _updateMilliseconds;
        private float _updateMilliseconds;

        [Header("--- References ---")]
        [SerializeField] private List<MeshFilter> _meshFilters;
        // [SerializeField] private MeshRenderer _meshRenderer;
        //[SerializeField] MeshCollider _meshCollider;

        private Mesh _mesh;
        private int[] _triangles;
        private int _triangleCount;

        // Per vertex data (index = z * (faceCountX + 1) + x). The vertex job fills it on all processor cores.
        private NativeArray<Vector3> _vertices;
        private NativeArray<Vector3> _drawnVertices;
        private NativeArray<Color> _colors;
        // last height and band activity per vertex, for the dampening
        private NativeArray<float> _lastYs;
        private NativeArray<float> _lastBands;
        // true when the mesh shows _drawnVertices (bent by the morph) instead of _vertices
        private bool _morphed;
        // GetFpsEqualizer() for the time since the last mesh update, the same for every vertex
        private float _fpsEqualizer;
        private float _noiseMean = ColorReferenceNoise;
        private float _colorScale = 1f;
        private bool _colorScaleKnown;
        /// <summary>Average noise value of a typical pattern (over all patterns it is 0.52), the colors of other patterns are moved towards it.</summary>
        private const float ColorReferenceNoise = 0.5f;
        /// <summary>Share of the way per reference step (240 per second) the color scale moves to its goal, so a pattern change glides.</summary>
        private const float ColorScaleSmoothing = 0.01f;
        // Time since the last mesh update. It is longer than a frame when the mesh is not updated every frame.
        private float _timeSinceMesh;
        private float _stepDelta;

        // Band index per vertex and shape, and whether a vertex lies outside of all shapes. They only depend on the shapes and
        // the face count, so they are calculated again only when one of those changes (see EnsureShapeCache).
        private NativeArray<float> _shapeIndices;
        private NativeArray<bool> _outOfAllShapes;
        private float[] _shapeScratch;
        private bool[] _outOfAllShapesCache;
        private int _shapeCacheSignature;
        private int _shapeCacheVersion;
        private bool _shapeCacheBuilt;

        // The mesh structure (triangles and uvs) only changes with the face count and the out of bounds triangles.
        private bool _structureDirty = true;
        private int _structureFaceCountX = -1;
        private int _structureFaceCountZ = -1;
        private bool _structureHide;
        private int _structureShapeVersion = -1;
        private Vector2[] _uvs;

        // The band values of this frame, read once instead of per vertex
        private NativeArray<float> _bandValues;
        private int _bandCount;
        private NativeArray<TerrainShapeJobData> _shapeJobData;
        private NativeArray<float> _smoothingCurveTable;
        private NativeArray<Color> _gradientTable;
        private NativeArray<float> _waveFronts;
        private NativeArray<float> _waveLevels;
        private float _outOfBoundsFactorValue; // only set when _outOfBoundsSettings != OutOfBounds.OutestBand

        // The noise pattern per vertex: the last two results, blended over NoiseInterval updates
        private NativeArray<float> _noisePrevious;
        private NativeArray<float> _noiseNext;
        private float[] _noiseScratch;
        private bool _noiseValid;
        private int _noiseAge;
        /// <summary>Per grid point the update (0 to interval - 1) in which it is calculated again.</summary>
        private NativeArray<byte> _noiseSlots;
        private float[] _noisePreviousScratch;
        private bool _noiseDueOnly;
        private int _noiseIntervalUsed = 1;
        private int _noiseSlotSignature;
        private int _noiseGridWidth;
        private int _noiseGridHeight;
        /// <summary>Below this many grid points the noise is calculated on the main thread, the threads would cost more than they save.</summary>
        private const int ParallelNoiseMinPoints = 2500;
        private System.Action<int> _noiseRowAction;

        //private int _bandSize;

        #region Mono
        private void Update()
        {
            ApplyPerlinSpeed();

            if (!_generateOnUpdate)
                return;

            _timeSinceMesh += Time.deltaTime;
            GenerateMesh();
        }

        private void OnDestroy()
        {
            DisposeBuffers();
        }
        #endregion


        #region Mesh Update
        /// <summary>
        /// True while the song is paused: the terrain time stands still (noise, waves, beats, morph animation) and the picture
        /// stays as it is. The test signal stands in for the song, so it never counts as paused.
        /// </summary>
        public bool Frozen => !AudioTestSignal.IsActive && AudioSampler.Instance != null && AudioSampler.Instance.AudioPlayer.IsPaused;

        // Seconds of terrain time, stops while frozen.
        private float _clock;

        private void ApplyPerlinSpeed()
        {
            if (Frozen || (AudioTestSignal.IsActive && AudioTestSignal.Instance.FreezeNoise))
                return;
            _perlinOffsetX += _perlinOffsetSpeed.x * Time.deltaTime;
            _perlinOffsetY += _perlinOffsetSpeed.y * Time.deltaTime;
        }
        #endregion


        #region Mesh Generation
        private void GenerateMesh()
        {
            long start = System.Diagnostics.Stopwatch.GetTimestamp();

            if (_mesh == null)
            {
                _mesh = new Mesh();
                foreach(MeshFilter meshFilter in _meshFilters)
                    meshFilter.mesh = _mesh;
            }

            _stepDelta = _timeSinceMesh > 0f ? _timeSinceMesh : Time.deltaTime;
            _timeSinceMesh = 0f;
            if (!Frozen)
                _clock += _stepDelta;

            CreateShape();
            ApplyMesh();

            float milliseconds = (System.Diagnostics.Stopwatch.GetTimestamp() - start) * 1000f / System.Diagnostics.Stopwatch.Frequency;
            _updateMilliseconds = _updateMilliseconds <= 0f ? milliseconds : Mathf.Lerp(_updateMilliseconds, milliseconds, 0.05f);
        }

        private void CreateShape()
        {
            // audio preparation
            PrepareAudio();
            if (_beatWave != null)
            {
                if (_beatWave.NeedsBassEdge)
                    _beatWave.SetBassEdge(GetBassEdgeDistance());
                _beatWave.Tick(_stepDelta, Frozen);
            }
            UpdateInvert();

            int vertexCount = (_faceCountX + 1) * (_faceCountZ + 1);
            bool redoMesh = !_vertices.IsCreated || _vertices.Length != vertexCount;
            if (redoMesh)
                AllocateVertexBuffers(vertexCount);
            _fpsEqualizer = Utility.Utility.GetFpsEqualizer(_stepDelta);
            EnsureShapeCache();
            UpdateNoise();

            JobHandle handle = ScheduleVertexJob(vertexCount);
            handle = ScheduleRelief(handle);
            _morphed = ScheduleMorph(ref handle);

            // the triangles are built while the vertices are calculated
            bool structureChanged = redoMesh || _triangles == null || _structureFaceCountX != _faceCountX || _structureFaceCountZ != _faceCountZ
                || _structureHide != _hideOutOfBounds || (_hideOutOfBounds && _structureShapeVersion != _shapeCacheVersion);
            if (structureChanged)
                BuildStructure(redoMesh);

            handle.Complete();
        }

        private void AllocateVertexBuffers(int vertexCount)
        {
            DisposeArray(ref _vertices);
            DisposeArray(ref _drawnVertices);
            DisposeArray(ref _colors);
            DisposeArray(ref _lastYs);
            DisposeArray(ref _lastBands);
            _vertices = new NativeArray<Vector3>(vertexCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _colors = new NativeArray<Color>(vertexCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _lastYs = new NativeArray<float>(vertexCount, Allocator.Persistent);
            _lastBands = new NativeArray<float>(vertexCount, Allocator.Persistent);
            _noiseValid = false;
        }

        /// <summary>Triangles and uvs, only needed again when the face count changes or the triangles outside of the shapes change.</summary>
        private void BuildStructure(bool redoMesh)
        {
            if (_triangles == null || redoMesh || _triangles.Length != _faceCountX * _faceCountZ * 6)
                _triangles = new int[_faceCountX * _faceCountZ * 6];

            bool hide = _hideOutOfBounds && _outOfAllShapesCache != null;
            int vertexIndex = 0;
            int triangleIndex = 0;
            for (int z = 0; z < _faceCountZ; z++)
            {
                for (int x = 0; x < _faceCountX; x++)
                {
                    int v0 = vertexIndex;
                    int v1 = vertexIndex + 1;
                    int v2 = vertexIndex + 1 + _faceCountX;
                    int v3 = vertexIndex + 2 + _faceCountX;

                    // skip triangles whose three vertices are all outside of every shape
                    if (!hide || !(_outOfAllShapesCache[v0] && _outOfAllShapesCache[v2] && _outOfAllShapesCache[v1]))
                    {
                        _triangles[triangleIndex + 0] = v0;
                        _triangles[triangleIndex + 1] = v2;
                        _triangles[triangleIndex + 2] = v1;
                        triangleIndex += 3;
                    }
                    if (!hide || !(_outOfAllShapesCache[v1] && _outOfAllShapesCache[v2] && _outOfAllShapesCache[v3]))
                    {
                        _triangles[triangleIndex + 0] = v1;
                        _triangles[triangleIndex + 1] = v2;
                        _triangles[triangleIndex + 2] = v3;
                        triangleIndex += 3;
                    }

                    vertexIndex++;
                }
                vertexIndex++;
            }
            _triangleCount = triangleIndex;

            // the uvs only depend on the face count
            int uvCount = (_faceCountX + 1) * (_faceCountZ + 1);
            if (_uvs == null || _uvs.Length != uvCount || _structureFaceCountX != _faceCountX || _structureFaceCountZ != _faceCountZ)
            {
                _uvs = new Vector2[uvCount];
                for (int i = 0, x = 0; x < _faceCountX + 1; x++)
                {
                    for (int z = 0; z < _faceCountZ + 1; z++)
                    {
                        _uvs[i] = new Vector2((float)x / _faceCountX, (float)z / _faceCountZ);
                        i++;
                    }
                }
            }

            _structureFaceCountX = _faceCountX;
            _structureFaceCountZ = _faceCountZ;
            _structureHide = _hideOutOfBounds;
            _structureShapeVersion = _shapeCacheVersion;
            _structureDirty = true;
        }

        private void ApplyMesh()
        {
            if (_structureDirty)
            {
                _mesh.Clear();
                _mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            _mesh.SetVertices(_morphed ? _drawnVertices : _vertices);
            if (_structureDirty)
            {
                _mesh.SetTriangles(_triangles, 0, _triangleCount, 0);
                _mesh.uv = _uvs;
                _structureDirty = false;
            }
            _mesh.SetColors(_colors);
            _mesh.RecalculateNormals();
            //if (_meshCollider != null)
            //    _meshCollider.sharedMesh = _mesh;
        }

        private bool _refreshRequested;

        /// <summary>Settings changed. While paused the next update shows them on the frozen moment, otherwise nothing would move.</summary>
        public void RequestRefresh() => _refreshRequested = true;

        /// <summary>Sets up the job that calculates height and color of every vertex and starts it.</summary>
        private JobHandle ScheduleVertexJob(int vertexCount)
        {
            int shapeCount = _audioShapeSettings.Count;
            bool audio = _applyAudio && _samplesContainer != null && shapeCount > 0 && _bandCount > 0;
            bool paused = Frozen;
            // A flowing invert is followed every frame, otherwise it would show up as one jump at the next menu change.
            bool refresh = _refreshRequested || _invertMoving;
            _refreshRequested = false;
            bool smoothing = audio && _applyAudioSmoothing && _audioSmoothingCurve != null;
            bool color = _applyColorByHeight && _colorHeightGradient != null;

            EnsureArray(ref _bandValues, Mathf.Max(1, _bandCount));
            EnsureArray(ref _shapeJobData, Mathf.Max(1, shapeCount));
            for (int i = 0; i < shapeCount; i++)
            {
                TerrainShape shape = _audioShapeSettings[i];
                _shapeJobData[i] = new TerrainShapeJobData { Active = shape.ApplyShape, Operation = (int)shape.Operation, Strength = shape.Strength };
            }

            EnsureArray(ref _smoothingCurveTable, TerrainVertexJob.LutSize);
            if (smoothing)
            {
                for (int i = 0; i < TerrainVertexJob.LutSize; i++)
                    _smoothingCurveTable[i] = _audioSmoothingCurve.Evaluate((float)i / (TerrainVertexJob.LutSize - 1));
            }

            EnsureArray(ref _gradientTable, TerrainVertexJob.LutSize);
            if (color)
            {
                bool own = _colorTableOverride != null && _colorTableOverride.Length == TerrainVertexJob.LutSize;
                for (int i = 0; i < TerrainVertexJob.LutSize; i++)
                    _gradientTable[i] = own ? _colorTableOverride[i] : _colorHeightGradient.Evaluate((float)i / (TerrainVertexJob.LutSize - 1));
            }

            EnsureArray(ref _waveFronts, TerrainBeatWave.MaxWaves);
            EnsureArray(ref _waveLevels, TerrainBeatWave.MaxWaves);
            int waveCount = 0;
            if (_beatWave != null && _beatWave.ActiveCount > 0)
            {
                waveCount = _beatWave.ActiveCount;
                _beatWave.CopyWaves(_waveFronts, _waveLevels);
            }

            UpdateColorScale();

            var job = new TerrainVertexJob
            {
                FaceCountX = _faceCountX,
                FaceCountZ = _faceCountZ,
                ScaleX = _terrainSizeX / _faceCountX,
                ScaleY = EffectiveTerrainSizeY,
                ScaleZ = _terrainSizeZ / _faceCountZ,

                ApplyNoise = _applyPerlinNoise,
                PerlinStrength = _perlinStrength,
                InvertEased = _invertEased,
                NoiseInterval = _noiseIntervalUsed,
                NoiseTick = _noiseAge,
                NoiseSlots = _noiseSlots,
                NoisePrevious = _noisePrevious,
                NoiseNext = _noiseNext,

                ApplyTerrace = _applyTerrace,
                TerraceCount = _terraceCount,
                TerracePoint = (int)_terraceExecutionPoint,

                ApplyAudio = audio,
#if UNITY_EDITOR
                DebugMode = _applyAudioDebugMode,
#endif
                BandCount = _bandCount,
                VertexCount = vertexCount,
                ShapeCount = shapeCount,
                OutOfBoundsOperation = (int)_outOfBoundsOperation,
                OutOfBoundsValue = _outOfBoundsFactorValue,
                Smoothing = smoothing,
                SmoothLastWithOutOfBounds = _smoothLastBandWithOutOfBounds,
                SmoothLastWithOutOfBoundsCompletely = _smoothLastBandWithOutOfBoundsCompletely,
                BandValues = _bandValues,
                ShapeIndices = _shapeIndices,
                Shapes = _shapeJobData,
                SmoothingCurve = _smoothingCurveTable,

                WaveCount = waveCount,
                WaveWidth = waveCount > 0 ? _beatWave.ActiveWidth : 1f,
                WaveMode = _beatWave != null ? _beatWave.WaveMode : 0,
                WaveUseNoise = _beatWave != null && _beatWave.UseNoise,
                WaveFronts = _waveFronts,
                WaveLevels = _waveLevels,

                Dampen = _applyDampening,
                // Paused: no more dampening steps, so the picture stays exactly where it is.
                // A menu change snaps the picture once to the frozen moment with the new settings.
                DampenDown = paused ? (refresh ? 1f : 0f) : _dampenLerpFactorDown,
                DampenUp = paused ? (refresh ? 1f : 0f) : _dampenLerpFactorUp,
                FpsEqualizer = _fpsEqualizer,

                ApplyColor = color,
                ColorScale = _colorScale,
                ColorHeadroom = _colorHeadroom,
                ColorBandHue = _colorBandHue,
                ColorBandBrightness = _colorBandBrightness,
                ColorBandGlow = _colorBandGlow,
                ColorOutOfBounds = _colorOutOfBounds,
                OutOfBoundsBrightness = _outOfBoundsBrightness,
                OutOfAllShapes = _outOfAllShapes,
                GradientTable = _gradientTable,

                Vertices = _vertices,
                Colors = _colors,
                LastYs = _lastYs,
                LastBands = _lastBands,
            };
            return job.Schedule(_faceCountZ + 1, 1);
        }

        /// <summary>Shades the colors by the slope once the vertex job is done, see <see cref="TerrainReliefJob"/>. Does nothing at strength 0.</summary>
        private JobHandle ScheduleRelief(JobHandle handle)
        {
            if (_reliefStrength <= 0.0005f || !_applyColorByHeight)
                return handle;

            var job = new TerrainReliefJob
            {
                FaceCountX = _faceCountX,
                FaceCountZ = _faceCountZ,
                ScaleX = _terrainSizeX / _faceCountX,
                ScaleZ = _terrainSizeZ / _faceCountZ,
                Strength = _reliefStrength,
                LightX = -0.7071f,
                LightZ = -0.7071f,
                Vertices = _vertices,
                Colors = _colors,
            };
            return job.Schedule(_faceCountZ + 1, 1, handle);
        }

        private static void EnsureArray<T>(ref NativeArray<T> array, int length) where T : struct
        {
            if (array.IsCreated && array.Length == length)
                return;
            DisposeArray(ref array);
            array = new NativeArray<T>(length, Allocator.Persistent);
        }

        private static void DisposeArray<T>(ref NativeArray<T> array) where T : struct
        {
            if (array.IsCreated)
                array.Dispose();
        }

        private void DisposeBuffers()
        {
            DisposeArray(ref _vertices);
            DisposeArray(ref _drawnVertices);
            DisposeArray(ref _colors);
            DisposeArray(ref _lastYs);
            DisposeArray(ref _lastBands);
            DisposeArray(ref _shapeIndices);
            DisposeArray(ref _outOfAllShapes);
            DisposeArray(ref _bandValues);
            DisposeArray(ref _shapeJobData);
            DisposeArray(ref _smoothingCurveTable);
            DisposeArray(ref _gradientTable);
            DisposeArray(ref _waveFronts);
            DisposeArray(ref _waveLevels);
            DisposeArray(ref _noisePrevious);
            DisposeArray(ref _noiseNext);
            DisposeArray(ref _noiseSlots);
        }
        #endregion


        #region Noise
        /// <summary>
        /// Keeps the pattern up to date once per mesh update. The pattern is calculated row by row on all processor cores, and
        /// only every NoiseInterval-th update. In between the vertex job blends from the last result
        /// to the new one, so the pattern keeps flowing.
        /// </summary>
        private void UpdateNoise()
        {
            if (!_applyPerlinNoise)
            {
                _noiseValid = false;
                EnsureArray(ref _noisePrevious, 1);
                EnsureArray(ref _noiseNext, 1);
                EnsureArray(ref _noiseSlots, 1);
                _noiseIntervalUsed = 1;
                _noiseGridWidth = 1;
                _noiseGridHeight = 1;
                return;
            }

            int width = _faceCountX + 1;
            int height = _faceCountZ + 1;
            if (!_noiseNext.IsCreated || _noiseNext.Length != width * height)
            {
                DisposeArray(ref _noisePrevious);
                DisposeArray(ref _noiseNext);
                _noisePrevious = new NativeArray<float>(width * height, Allocator.Persistent);
                _noiseNext = new NativeArray<float>(width * height, Allocator.Persistent);
                _noiseValid = false;
            }
            _noiseGridWidth = width;
            _noiseGridHeight = height;

            int interval = Mathf.Clamp(_noiseInterval, 1, MaxNoiseInterval);
            bool spread = _noiseSpread && interval > 1;
            _noiseIntervalUsed = interval;

            // The slot of every grid point depends on the grid, the interval and the spread, a change starts over.
            int signature = (width * 31 + height) * 31 + interval * 2 + (spread ? 1 : 0);
            if (!_noiseSlots.IsCreated || _noiseSlots.Length != width * height || signature != _noiseSlotSignature)
            {
                BuildNoiseSlots(width, height, interval, spread);
                _noiseSlotSignature = signature;
                _noiseValid = false;
            }

            if (!_noiseValid)
            {
                CalculateNoise(false);
                _noisePrevious.CopyFrom(_noiseNext);
                _noiseAge = 0;
                _noiseValid = true;
            }
            else
            {
                _noiseAge = (_noiseAge + 1) % interval;
                if (spread)
                {
                    CalculateNoise(true);
                }
                else if (_noiseAge == 0)
                {
                    NativeArray<float> older = _noisePrevious;
                    _noisePrevious = _noiseNext;
                    _noiseNext = older;
                    CalculateNoise(false);
                }
            }
        }

        /// <summary>Spread over the updates in a scattered way, so neighbours are not recalculated in the same update.</summary>
        private void BuildNoiseSlots(int width, int height, int interval, bool spread)
        {
            EnsureArray(ref _noiseSlots, width * height);
            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    uint hash = (uint)(x * 73856093) ^ (uint)(z * 19349663);
                    hash ^= hash >> 13;
                    hash *= 0x5bd1e995u;
                    hash ^= hash >> 15;
                    _noiseSlots[z * width + x] = spread ? (byte)(hash % (uint)interval) : (byte)0;
                }
            }
        }

        /// <param name="onlyDue">Only the grid points whose slot is this update are calculated, the others keep their results.</param>
        private void CalculateNoise(bool onlyDue)
        {
            int count = _noiseGridWidth * _noiseGridHeight;
            if (_noiseScratch == null || _noiseScratch.Length != count)
                _noiseScratch = new float[count];
            _noiseDueOnly = onlyDue;
            if (onlyDue)
            {
                if (_noisePreviousScratch == null || _noisePreviousScratch.Length != count)
                    _noisePreviousScratch = new float[count];
                _noiseNext.CopyTo(_noiseScratch);
                _noisePrevious.CopyTo(_noisePreviousScratch);
            }

            if (count < ParallelNoiseMinPoints)
            {
                for (int row = 0; row < _noiseGridHeight; row++)
                    CalculateNoiseRow(row);
            }
            else
            {
                // The pattern functions only read their arguments, so the rows do not disturb each other.
                if (_noiseRowAction == null)
                    _noiseRowAction = CalculateNoiseRow;
                System.Threading.Tasks.Parallel.For(0, _noiseGridHeight, _noiseRowAction);
            }
            _noiseNext.CopyFrom(_noiseScratch);
            _noiseMean = SampleMean(_noiseScratch);
            if (onlyDue)
                _noisePrevious.CopyFrom(_noisePreviousScratch);
        }


        /// <summary>Average of at most 2048 evenly spread points, enough to tell a high pattern from a low one.</summary>
        private static float SampleMean(float[] values)
        {
            int step = Mathf.Max(1, values.Length / 2048);
            float sum = 0f;
            int count = 0;
            for (int i = 0; i < values.Length; i += step, count++)
                sum += values[i];
            return count > 0 ? sum / count : ColorReferenceNoise;
        }

        /// <summary>
        /// Factor for the place in the gradient. A pattern that is higher or lower than a typical one (noise lowers the terrain) is
        /// scaled so its average height lands where a typical pattern's does. The balance slider blends it in, the result glides.
        /// </summary>
        private void UpdateColorScale()
        {
            float target = 1f;
            if (_applyPerlinNoise && _colorNoiseBalance > 0f)
            {
                float mean = Mathf.Lerp(_noiseMean, 1f - _noiseMean, _invertEased);
                float factor = Mathf.Max(0.05f, 1f - _perlinStrength * mean);
                float reference = 1f - _perlinStrength * ColorReferenceNoise;
                target = Mathf.Lerp(1f, Mathf.Clamp(reference / factor, 0.5f, 3f), _colorNoiseBalance);
            }
            _colorScale = _colorScaleKnown ? Mathf.Lerp(_colorScale, target, Utility.Utility.Smooth(ColorScaleSmoothing, _fpsEqualizer)) : target;
            _colorScaleKnown = true;
        }
        private void CalculateNoiseRow(int row)
        {
            // Scaled by the face count so the noise keeps its size on the terrain when the face count changes.
            float scaleX = PerlinReferenceFaceCount / Mathf.Max(1, _faceCountX);
            float zScaled = row * (PerlinReferenceFaceCount / Mathf.Max(1, _faceCountZ));
            int index = row * _noiseGridWidth;
            for (int column = 0; column < _noiseGridWidth; column++, index++)
            {
                if (_noiseDueOnly)
                {
                    if (_noiseSlots[index] != _noiseAge)
                        continue;
                    _noisePreviousScratch[index] = _noiseScratch[index];
                }
                float x = column * scaleX;
                _noiseScratch[index] = TerrainNoise.Evaluate(_noisePattern, x, zScaled, _perlinOffsetX, _perlinOffsetY, _noiseZoom);
            }
        }

        /// <summary>
        /// Moves the pattern towards its wanted state once per mesh update: upside down or not, from the pattern itself, the invert
        /// option and the flips on the beat. A change of the pattern jumps, a change of the options or a flip flows.
        /// </summary>
        private void UpdateInvert()
        {
            if (_noiseFlipBeats <= 0)
            {
                _beatFlipped = false;
                _lastBeatCount = _beatWave != null ? _beatWave.BeatCount : 0;
            }
            else if (_beatWave != null)
            {
                int beats = _beatWave.BeatCount;
                if (beats / _noiseFlipBeats != _lastBeatCount / _noiseFlipBeats)
                    _beatFlipped = !_beatFlipped;
                _lastBeatCount = beats;
            }

            bool upsideDown = TerrainNoise.InvertedByDefault(_noisePattern) ^ _noiseInvert ^ _beatFlipped;
            float target = upsideDown ? 1f : 0f;
            if (!_invertStateKnown || _invertPattern != _noisePattern)
            {
                _invertAmount = target;
                _invertEased = target;
                _invertPattern = _noisePattern;
                _invertStateKnown = true;
                _invertMoving = false;
                return;
            }

            float beatInterval = _beatWave != null ? _beatWave.BeatInterval : InvertFlowSeconds;
            float seconds = Mathf.Clamp(0.8f * beatInterval, 0.1f, InvertFlowSeconds);
            // an eased flow: the amount moves linearly, the pattern follows it smoothed
            _invertAmount = Mathf.MoveTowards(_invertAmount, target, _stepDelta / seconds);
            _invertEased = _invertAmount * _invertAmount * (3f - 2f * _invertAmount);
            _invertMoving = !Mathf.Approximately(_invertAmount, target);
        }

        /// <summary>Distance from the middle (in half terrain widths) where the bass bands end, found on the first active audio shape.</summary>
        private float GetBassEdgeDistance()
        {
            if (_samplesContainer == null || _samplesContainer.BandSize < 1 || _beatWave == null)
                return 0f;

            TerrainShape shape = null;
            foreach (TerrainShape candidate in _audioShapeSettings)
            {
                if (candidate.ApplyShape)
                {
                    shape = candidate;
                    break;
                }
            }
            if (shape == null)
                return 0f;

            // The band index grows with the distance, search where it reaches the end of the bass bands.
            float target = Mathf.Min(1f, (float)_beatWave.BassBandCount / _samplesContainer.BandSize);
            float low = 0f, high = 1.2f;
            for (int i = 0; i < 12; i++)
            {
                float middle = 0.5f * (low + high);
                float index = shape.GetNormalizedBandIndexByShape(0.5f + 0.5f * middle, 0.5f);
                if (index < 0f || index >= target)
                    high = middle;
                else
                    low = middle;
            }
            return 0.5f * (low + high);
        }
        #endregion


        #region Shape cache
        /// <summary>
        /// Calculates the band index of every vertex for every shape again when the shapes, the face count or the audio switch
        /// changed since the last frame. The index only depends on those, not on the audio itself.
        /// </summary>
        private void EnsureShapeCache()
        {
            int vertexCount = (_faceCountX + 1) * (_faceCountZ + 1);
            int signature = _faceCountX;
            unchecked
            {
                signature = signature * 31 + _faceCountZ;
                signature = signature * 31 + (_applyAudio ? 1 : 0);
                signature = signature * 31 + _audioShapeSettings.Count;
                foreach (TerrainShape shape in _audioShapeSettings)
                    signature = signature * 31 + shape.GetIndexSettingsHash();
            }

            if (_shapeCacheBuilt && signature == _shapeCacheSignature && _outOfAllShapesCache != null && _outOfAllShapesCache.Length == vertexCount)
                return;

            _shapeCacheBuilt = true;
            _shapeCacheSignature = signature;
            _shapeCacheVersion++;
            int shapeCount = _audioShapeSettings.Count;
            int indexCount = Mathf.Max(1, shapeCount * vertexCount);
            if (_shapeScratch == null || _shapeScratch.Length != indexCount)
                _shapeScratch = new float[indexCount];
            else
                System.Array.Clear(_shapeScratch, 0, indexCount);
            if (_outOfAllShapesCache == null || _outOfAllShapesCache.Length != vertexCount)
                _outOfAllShapesCache = new bool[vertexCount];

            bool audioShapes = _applyAudio && shapeCount > 0;
            for (int s = 0; s < shapeCount; s++)
            {
                TerrainShape shape = _audioShapeSettings[s];
                if (!audioShapes || !shape.ApplyShape)
                    continue;
                int offset = s * vertexCount;
                for (int i = 0, z = 0; z <= _faceCountZ; z++)
                    for (int x = 0; x <= _faceCountX; x++, i++)
                        _shapeScratch[offset + i] = shape.GetNormalizedBandIndexByShape((float)x / _faceCountX, (float)z / _faceCountZ);
            }

            for (int i = 0; i < vertexCount; i++)
            {
                // same as IsOutOfAllShapes
                bool anyActive = false, inside = false;
                if (audioShapes)
                {
                    for (int s = 0; s < shapeCount; s++)
                    {
                        if (!_audioShapeSettings[s].ApplyShape)
                            continue;
                        anyActive = true;
                        if (_shapeScratch[s * vertexCount + i] >= 0)
                        {
                            inside = true;
                            break;
                        }
                    }
                }
                _outOfAllShapesCache[i] = anyActive && !inside;
            }

            EnsureArray(ref _shapeIndices, indexCount);
            _shapeIndices.CopyFrom(_shapeScratch);
            EnsureArray(ref _outOfAllShapes, vertexCount);
            _outOfAllShapes.CopyFrom(_outOfAllShapesCache);
        }

        /// <summary>Like <see cref="IsOutOfAllShapes"/>, but takes the result of the last frame when there is one.</summary>
        private bool IsOutOfAllShapesCached(int x, int z)
        {
            int index = z * (_faceCountX + 1) + x;
            if (_shapeCacheBuilt && _outOfAllShapesCache != null && index >= 0 && index < _outOfAllShapesCache.Length)
                return _outOfAllShapesCache[index];
            return IsOutOfAllShapes(x, z);
        }

        /// <summary>True if audio is applied and the vertex lies in none of the active shapes.</summary>
        private bool IsOutOfAllShapes(int x, int z)
        {
            if (!_applyAudio || _audioShapeSettings.Count == 0)
                return false;

            bool anyActive = false;
            foreach (TerrainShape shape in _audioShapeSettings)
            {
                if (!shape.ApplyShape)
                    continue;

                anyActive = true;
                if (shape.GetNormalizedBandIndexByShape((float)x / _faceCountX, (float)z / _faceCountZ) >= 0)
                    return false;
            }

            return anyActive;
        }
        #endregion


        #region Picking
        /// <summary>Steps of the search along a camera ray in <see cref="TryPickBand"/>.</summary>
        private const int PickSteps = 320;

        /// <summary>The transform of the drawn terrain. The mesh starts in its corner, <see cref="LocalCenter"/> is its middle.</summary>
        public Transform TerrainTransform => _meshFilters != null && _meshFilters.Count > 0 ? _meshFilters[0].transform : null;

        /// <summary>Middle of the terrain box in the local space of <see cref="TerrainTransform"/>.</summary>
        public Vector3 LocalCenter => new Vector3(0.5f * _terrainSizeX, 0.5f * EffectiveTerrainSizeY, 0.5f * _terrainSizeZ);

        /// <summary>
        /// Finds the audio band under a ray (for example from the camera through the mouse) on the terrain as it is drawn right now.
        /// False when the ray misses the terrain or hits a spot that lies outside of all shapes.
        /// </summary>
        public bool TryPickBand(Ray ray, out int band)
        {
            band = -1;
            if (_samplesContainer == null || _samplesContainer.BandSize < 1)
                return false;
            if (!TryRaycast(ray, true, out Vector3 hit))
                return false;
            return BandAt(hit.x / _terrainSizeX, hit.z / _terrainSizeZ, out band);
        }

        /// <summary>True when the ray hits the terrain. With a mesh morph on, the box around the terrain counts, because the drawn surface is bent.</summary>
        public bool TryHitTerrain(Ray ray)
        {
            bool bent = _applyMorph && _shownAmount > 0.0005f;
            if (!TryRaycast(ray, !bent, out Vector3 hit))
                return false;
            if (bent || !_hideOutOfBounds)
                return true;
            // the parts outside of all shapes are not drawn, so they are not the terrain
            int x = Mathf.Clamp(Mathf.RoundToInt(hit.x / _terrainSizeX * _faceCountX), 0, _faceCountX);
            int z = Mathf.Clamp(Mathf.RoundToInt(hit.z / _terrainSizeZ * _faceCountZ), 0, _faceCountZ);
            return !IsOutOfAllShapesCached(x, z);
        }

        /// <summary>Ray against the terrain in its local space: the surface as it is drawn, or only the box around it.</summary>
        private bool TryRaycast(Ray ray, bool surface, out Vector3 hit)
        {
            hit = default;
            if (!_vertices.IsCreated || _meshFilters == null || _meshFilters.Count == 0)
                return false;

            Transform terrain = _meshFilters[0].transform;
            Vector3 origin = terrain.InverseTransformPoint(ray.origin);
            Vector3 direction = terrain.InverseTransformPoint(ray.origin + ray.direction) - origin;

            // the part of the ray inside the box around the terrain (heights can exceed the terrain height a little)
            float height = EffectiveTerrainSizeY;
            Vector3 min = new Vector3(0f, -0.2f * height, 0f);
            Vector3 max = new Vector3(_terrainSizeX, 2f * height, _terrainSizeZ);
            float enter = 0f, exit = 100000f;
            for (int axis = 0; axis < 3; axis++)
            {
                if (Mathf.Abs(direction[axis]) < 1e-6f)
                {
                    if (origin[axis] < min[axis] || origin[axis] > max[axis])
                        return false;
                    continue;
                }
                float t0 = (min[axis] - origin[axis]) / direction[axis];
                float t1 = (max[axis] - origin[axis]) / direction[axis];
                enter = Mathf.Max(enter, Mathf.Min(t0, t1));
                exit = Mathf.Min(exit, Mathf.Max(t0, t1));
            }
            if (enter >= exit)
                return false;

            if (!surface)
            {
                hit = origin + direction * enter;
                return true;
            }

            for (int i = 0; i <= PickSteps; i++)
            {
                Vector3 point = origin + direction * Mathf.Lerp(enter, exit, (float)i / PickSteps);
                if (point.x < 0f || point.x > _terrainSizeX || point.z < 0f || point.z > _terrainSizeZ)
                    continue;
                if (point.y > HeightAt(point.x, point.z))
                    continue;
                hit = point;
                return true;
            }
            return false;
        }

        /// <summary>Height of the drawn surface at a local position, bilinear between the vertices.</summary>
        private float HeightAt(float localX, float localZ)
        {
            float fx = Mathf.Clamp(localX / _terrainSizeX * _faceCountX, 0f, _faceCountX - 0.001f);
            float fz = Mathf.Clamp(localZ / _terrainSizeZ * _faceCountZ, 0f, _faceCountZ - 0.001f);
            int ix = (int)fx, iz = (int)fz;
            float tx = fx - ix, tz = fz - iz;
            int stride = _faceCountX + 1;
            float a = Mathf.Lerp(_vertices[iz * stride + ix].y, _vertices[iz * stride + ix + 1].y, tx);
            float b = Mathf.Lerp(_vertices[(iz + 1) * stride + ix].y, _vertices[(iz + 1) * stride + ix + 1].y, tx);
            return Mathf.Lerp(a, b, tz);
        }

        private bool BandAt(float normalizedX, float normalizedZ, out int band)
        {
            band = -1;
            foreach (TerrainShape shape in _audioShapeSettings)
            {
                if (!shape.ApplyShape)
                    continue;
                float index = shape.GetNormalizedBandIndexByShape(normalizedX, normalizedZ);
                if (index < 0f)
                    continue;
                band = Mathf.Min((int)(Mathf.Min(1f, index) * _samplesContainer.BandSize), _samplesContainer.BandSize - 1);
                return true;
            }
            return false;
        }
        #endregion

        #region Morph
        // Morph motion: the amount that is drawn is the amount of the slider plus the beat breath plus the slow wave.
        private float _shownAmount = 0.5f;
        private float _breath, _breathVelocity, _breathHold, _wavePhase;
        private int _breathBeatCount;
        /// <summary>A critically damped spring is settled (to about 1 %) after this many of its time constants.</summary>
        private const float SpringSettle = 5f;
        /// <summary>Damping ratio of the spring at full bounce (1 = no overshoot).</summary>
        private const float SpringMinDamping = 0.25f;
        private const float SpringMaxStep = 1f / 240f;

        /// <summary>
        /// Moves the beat breath and the wave and sets the amount that is drawn. A beat holds the target of the breath at 1 for the
        /// attack time, then it falls back for the release time. The breath follows its target like on a spring, so it never jumps.
        /// </summary>
        private void UpdateMorphMotion()
        {
            float delta = Frozen ? 0f : _stepDelta;
            float interval = _beatWave != null ? Mathf.Max(0.1f, _beatWave.BeatInterval) : 0.5f;

            int beats = _beatWave != null ? _beatWave.BeatCount : 0;
            if (beats > _breathBeatCount)
                _breathHold = _motionAttack * interval;
            _breathBeatCount = beats;

            if (_motionBeat <= 0.0005f)
            {
                _breath = 0f;
                _breathVelocity = 0f;
                _breathHold = 0f;
            }
            else if (delta > 0f)
            {
                float target = _breathHold > 0f ? 1f : 0f;
                _breathHold = Mathf.Max(0f, _breathHold - delta);
                float seconds = Mathf.Max(0.05f, (target > _breath ? _motionAttack : _motionRelease) * interval);
                float omega = SpringSettle / seconds;
                float damping = Mathf.Lerp(1f, SpringMinDamping, _motionBounce);
                int steps = Mathf.CeilToInt(delta / SpringMaxStep);
                float step = delta / steps;
                for (int i = 0; i < steps; i++)
                {
                    _breathVelocity += (omega * omega * (target - _breath) - 2f * damping * omega * _breathVelocity) * step;
                    _breath += _breathVelocity * step;
                }
            }

            float wave = 0f;
            if (_motionWaveDepth > 0.0005f)
            {
                _wavePhase = (_wavePhase + delta / (_motionWaveBeats * interval)) % 1f;
                wave = _motionWaveDepth * Mathf.Sin(_wavePhase * 2f * Mathf.PI);
            }

            _shownAmount = Mathf.Clamp01(_morphAmount + _motionBeat * _breath + wave);
        }

        /// <summary>Starts the job that bends the vertices by the morph. False (and no job) when the terrain stays flat.</summary>
        private bool ScheduleMorph(ref JobHandle handle)
        {
            UpdateMorphMotion();
            float amount = _shownAmount;
            // at amount 0 the job keeps running until the lift has glided back down, so it does not vanish at once
            if (!_applyMorph || (amount <= 0.0005f && _morphLift < MorphLiftRest))
            {
                _morphLift = 0f;
                // the lift starts in place the next time the morph is on (no shape is nothing the morph can have)
                _liftShape = (MorphShape)0;
                return false;
            }

            EnsureArray(ref _drawnVertices, _vertices.Length);
            float half = _terrainSizeX * 0.5f;
            float liftTarget = MorphLiftTarget();
            // a new shape starts with its lift in place, only changes of the amount glide
            if (_morphShape != _liftShape)
            {
                _liftShape = _morphShape;
                _morphLift = liftTarget;
            }
            else
                _morphLift = Mathf.Lerp(_morphLift, liftTarget, 1f - Mathf.Exp(-MorphLiftSmoothing * Time.unscaledDeltaTime));
            var job = new TerrainMorphJob
            {
                Shape = (int)_morphShape,
                Amount = amount,
                Lift = _morphLift,
                Half = half,
                CenterX = _terrainSizeX * 0.5f,
                CenterZ = _terrainSizeZ * 0.5f,
                Seconds = _clock,
                Flat = _vertices,
                Bent = _drawnVertices,
            };
            handle = job.Schedule(_vertices.Length, 4096, handle);
            return true;
        }

        // Planet and tube hang down from the top, so the camera would look at their tip. They are lifted by a share of the screen
        // height instead (the camera stays where it is), once the amount passed the start and fully at the end of the ramp.
        private const float MorphLiftScreenShare = 0.2f;
        private const float MorphLiftStartAmount = 0.15f;
        private const float MorphLiftEndAmount = 0.6f;
        private const float MorphLiftSmoothing = 6f;
        /// <summary>Below this lift (local units) the morph is switched off at amount 0.</summary>
        private const float MorphLiftRest = 0.002f;
        private float _morphLift;
        private MorphShape _liftShape;

        /// <summary>How far the bent mesh is lifted, in local units of the terrain.</summary>
        private float MorphLiftTarget()
        {
            Transform terrain = TerrainTransform;
            Camera view = Camera.main;
            if ((_morphShape != MorphShape.Planet && _morphShape != MorphShape.Tube) || terrain == null || view == null)
                return 0f;

            float ramp = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(MorphLiftStartAmount, MorphLiftEndAmount, _shownAmount));
            float visibleHeight = view.orthographic
                ? 2f * view.orthographicSize
                : 2f * Mathf.Max(0.01f, Vector3.Dot(terrain.TransformPoint(LocalCenter) - view.transform.position, view.transform.forward)) * Mathf.Tan(0.5f * Mathf.Deg2Rad * view.fieldOfView);
            // the local up of the terrain is only partly up on the screen when the camera looks down at it
            float upOnScreen = Mathf.Max(0.3f, Vector3.Dot(terrain.up, view.transform.up));
            return ramp * MorphLiftScreenShare * visibleHeight / (upOnScreen * Mathf.Max(0.0001f, terrain.lossyScale.y));
        }
        #endregion

        #region Audio
        private void PrepareAudio()
        {
            if (!_applyAudio || _samplesContainer == null)
                return;

            _samplesContainer.TryRecalculate();

            // read the band values once, they stay the same for the whole frame
            _bandCount = _samplesContainer.BandSize;
            EnsureArray(ref _bandValues, Mathf.Max(1, _bandCount));
            for (int i = 0; i < _bandCount; i++)
                _bandValues[i] = _samplesContainer.GetValue(i);
#if UNITY_EDITOR
            // the debug mode replaces the spectrum by a line that falls from the first to the last band
            if (_applyAudioDebugMode)
                for (int i = 0; i < _bandCount; i++)
                    _bandValues[i] = 1f - (float)i / _bandCount;
#endif

            if (_outOfBoundsOperation == OutOfBounds.Constant)
                _outOfBoundsFactorValue = 1;
            else if (_outOfBoundsOperation == OutOfBounds.Average)
                _outOfBoundsFactorValue = _samplesContainer.Average;
            else if (_outOfBoundsOperation == OutOfBounds.OuterBand)
                _outOfBoundsFactorValue = _samplesContainer.GetValue(_bandCount - 1);
            else if (_outOfBoundsOperation == OutOfBounds.InnerBand)
                _outOfBoundsFactorValue = _samplesContainer.GetValue(0);
            else if (_outOfBoundsOperation == OutOfBounds.Flat)
                _outOfBoundsFactorValue = 0;

            _outOfBoundsFactorValue *= _outOfBoundsFactor;

            if (_outOfBoundsMaximumIsOutestBand)
                _outOfBoundsFactorValue = Mathf.Min(_outOfBoundsFactorValue, _samplesContainer.GetValue(_bandCount - 1));
        }
        #endregion

        // Temporarily disabled
        // #region Gizmos
        // private void OnDrawGizmosSelected()
        // {
        //     if (!_generate) return;
        //     _samplesContainer?.ForceRecalculate();
        //     _generate = false;
        //     GenerateMesh();
        // }
        // #endregion
    }
}
