using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
        [Tooltip("When true, vertices that lie outside of every active audio shape are darkened by the brightness below. Only the color changes, their height still follows the out of bounds operation.")]
        [SerializeField] private bool _colorOutOfBounds;
        [Tooltip("Brightness of the height gradient color for vertices outside of all active audio shapes. 1 = unchanged, 0 = black. Only used if _colorOutOfBounds is true.")]
        [Range(0, 1)]
        [SerializeField] private float _outOfBoundsBrightness = 0.2f;
        [Tooltip("When true, triangles whose three vertices all lie outside of every active audio shape are left out of the mesh, so the unused area is not rendered at all.")]
        [SerializeField] private bool _hideOutOfBounds;

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

        [Header("--- References ---")]
        [SerializeField] private List<MeshFilter> _meshFilters;
        // [SerializeField] private MeshRenderer _meshRenderer;
        //[SerializeField] MeshCollider _meshCollider;

        private Mesh _mesh;
        private Vector3[] _vertices;
        private int[] _triangles;
        private int _triangleCount;
        private bool[] _outOfBoundsVertices;


        private float[,] _lastYs;
        private Vector2[] _uvs;
        private Color[] _colors;
        private float _outOfBoundsFactorValue; // only set when _outOfBoundsSettings != OutOfBounds.OutestBand

        //private int _bandSize;

        #region Mono
        private void Update()
        {
            ApplyPerlinSpeed();

            if (_generateOnUpdate)
                GenerateMesh();
        }
        #endregion


        #region Mesh Update
        private void ApplyPerlinSpeed()
        {
            if (AudioTestSignal.IsActive && AudioTestSignal.Instance.FreezeNoise)
                return;
            _perlinOffsetX += _perlinOffsetSpeed.x * Time.deltaTime;
            _perlinOffsetY += _perlinOffsetSpeed.y * Time.deltaTime;
        }
        #endregion


        #region Mesh Generation
        private void GenerateMesh()
        {
            if (_mesh == null)
            {
                _mesh = new Mesh();
                foreach(MeshFilter meshFilter in _meshFilters)
                    meshFilter.mesh = _mesh;
            }

            CreateShape();
            ApplyMesh();
        }

        private void CreateShape()
        {
            // audio preparation
            PrepareAudio();
            if (_beatWave != null)
            {
                if (_beatWave.NeedsBassEdge)
                    _beatWave.SetBassEdge(GetBassEdgeDistance());
                _beatWave.Tick();
            }
            UpdateInvert();

            bool redoMesh = _vertices == null || (_faceCountX + 1) * (_faceCountZ + 1) != _vertices.Length;
            if (redoMesh)
                _vertices = new Vector3[(_faceCountX + 1) * (_faceCountZ + 1)];
            if (_applyDampening && (_lastYs == null || redoMesh))
                _lastYs = new float[_faceCountX + 1, _faceCountZ + 1];
            if (_colors == null || redoMesh)
                _colors = new Color[_vertices.Length];
            if (_outOfBoundsVertices == null || redoMesh)
                _outOfBoundsVertices = new bool[_vertices.Length];

            float scaleModifierX = _terrainSizeX / _faceCountX;
            float scaleModifierY = _terrainSizeY;
            float scaleModifierZ = _terrainSizeZ / _faceCountZ;

            //Vector3 position = Vector3.zero;
            for (int i = 0, z = 0; z < _faceCountZ + 1; z++)
            {
                for (int x = 0; x < _faceCountX + 1; x++)
                {
                    float y = GetY(x, z);
                    _vertices[i] = new Vector3(x * scaleModifierX, y * scaleModifierY, z * scaleModifierZ);
                    _colors[i] = GetColor(x, y, z);
                    _outOfBoundsVertices[i] = _hideOutOfBounds && IsOutOfAllShapes(x, z);
                    i++;
                    if (_applyDampening)
                        _lastYs[x, z] = y;
                }
            }

            if (_triangles == null || redoMesh)
                _triangles = new int[_faceCountX * _faceCountZ * 6];

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
                    if (!_hideOutOfBounds || !(_outOfBoundsVertices[v0] && _outOfBoundsVertices[v2] && _outOfBoundsVertices[v1]))
                    {
                        _triangles[triangleIndex + 0] = v0;
                        _triangles[triangleIndex + 1] = v2;
                        _triangles[triangleIndex + 2] = v1;
                        triangleIndex += 3;
                    }
                    if (!_hideOutOfBounds || !(_outOfBoundsVertices[v1] && _outOfBoundsVertices[v2] && _outOfBoundsVertices[v3]))
                    {
                        _triangles[triangleIndex + 0] = v1;
                        _triangles[triangleIndex + 1] = v2;
                        _triangles[triangleIndex + 2] = v3;
                        triangleIndex += 3;
                    }

                    vertexIndex++;
                    //yield return new WaitForSeconds(0.5f);
                }
                vertexIndex++;
            }
            _triangleCount = triangleIndex;

            if (_uvs == null || redoMesh)
                _uvs = new Vector2[_vertices.Length];
            for (int i = 0, x = 0; x < _faceCountX + 1; x++)
            {
                for (int z = 0; z < _faceCountZ + 1; z++)
                {
                    _uvs[i] = new Vector2((float)x / _faceCountX, (float)z / _faceCountZ);
                    i++;
                }
            }

            //ApplyMesh();
            // _mesh.vertices = _vertices;
            // _mesh.colors = _colors;
        }

        private void ApplyMesh()
        {
            _mesh.Clear();
            _mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            _mesh.vertices = _vertices;
            _mesh.SetTriangles(_triangles, 0, _triangleCount, 0);
            _mesh.RecalculateNormals();
            _mesh.uv = _uvs;
            _mesh.colors = _colors;
            //if (_meshCollider != null)
            //    _meshCollider.sharedMesh = _mesh;
        }
        #endregion


        #region Calculating Y
        private float GetY(int x, int z)
        {
            float y = 1;
            y = ApplyPerlin(x, y, z);
            if (_terraceExecutionPoint == TerraceExecutionPointEnum.AfterPerlin)
                y = ApplyTerrace(y);
            y = ApplyAudio(x, y, z);
            if (_terraceExecutionPoint == TerraceExecutionPointEnum.AfterAudio)
                y = ApplyTerrace(y);
            if (_beatWave != null)
                y = _beatWave.Apply(x, z, _faceCountX, _faceCountZ, y, _noiseFactor);
            y = ApplyDampening(x, y, z);
            if (_terraceExecutionPoint == TerraceExecutionPointEnum.AfterDampening)
                y = ApplyTerrace(y);
            return y;
        }
        /// <summary>How high the noise pattern leaves the current vertex (1 = full height). Set by ApplyPerlin, used by the beat wave.</summary>
        private float _noiseFactor = 1f;

        private float ApplyPerlin(int x, float originalY, int z)
        {
            _noiseFactor = 1f;
            if (!_applyPerlinNoise)
                return originalY;

            float y = originalY;
            // Scaled by the face count so the noise keeps its size on the terrain when the face count changes.
            float xScaled = x * (PerlinReferenceFaceCount / Mathf.Max(1, _faceCountX));
            float zScaled = z * (PerlinReferenceFaceCount / Mathf.Max(1, _faceCountZ));
            float perlinValue = TerrainNoise.Evaluate(_noisePattern, xScaled, zScaled, _perlinOffsetX, _perlinOffsetY, _noiseZoom);
            perlinValue = Mathf.Lerp(perlinValue, 1f - perlinValue, _invertEased);
            _noiseFactor = Utility.Utility.Remap(0, 1, 1, 1 - _perlinStrength, perlinValue);
            y *= _noiseFactor;
            return y;
        }

        /// <summary>
        /// Moves the pattern towards its wanted state once per frame: upside down or not, from the pattern itself, the invert
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
                return;
            }

            float beatInterval = _beatWave != null ? _beatWave.BeatInterval : InvertFlowSeconds;
            float seconds = Mathf.Clamp(0.8f * beatInterval, 0.1f, InvertFlowSeconds);
            // an eased flow: the amount moves linearly, the pattern follows it smoothed
            _invertAmount = Mathf.MoveTowards(_invertAmount, target, Time.deltaTime / seconds);
            _invertEased = _invertAmount * _invertAmount * (3f - 2f * _invertAmount);
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

        private float ApplyAudio(int x, float originalY, int z)
        {
            if (!_applyAudio || _samplesContainer == null || _audioShapeSettings.Count == 0)
                return originalY;

            // apply y height
            float yFactor = -1;
            foreach (TerrainShape shapeSettings in _audioShapeSettings)
            {
                if (!shapeSettings.ApplyShape)
                    continue;
                    
                // get band index
                float bandIndexNormalized = shapeSettings.GetNormalizedBandIndexByShape((float)x / _faceCountX, (float)z / _faceCountZ);
                bandIndexNormalized = Mathf.Min(1, bandIndexNormalized);
                float yFactorThisBand = GetHeightFactorByAudioBand(/*originalY, */bandIndexNormalized);
                
                float newYFactor = yFactor;
                // the first shape always overrides
                if (yFactor == -1)
                {
                    yFactor = 0;
                    newYFactor = yFactorThisBand;
                }
                else if (shapeSettings.Operation == TerrainShape.AudioShapeOperation.Override)
                    newYFactor = yFactorThisBand;
                //else if (shapeSettings.Operation == AudioShapeSettings.AudioShapeOperation.Multiply)
                //    y *= (yThisBand + 1);
                else if (shapeSettings.Operation == TerrainShape.AudioShapeOperation.Add)
                    newYFactor += yFactorThisBand;
                else if (shapeSettings.Operation == TerrainShape.AudioShapeOperation.Subtract)
                    newYFactor = Mathf.Abs(yFactorThisBand - yFactor);
                else if (shapeSettings.Operation == TerrainShape.AudioShapeOperation.Maximum)
                    newYFactor = Mathf.Max(yFactorThisBand, yFactor);
                else if (shapeSettings.Operation == TerrainShape.AudioShapeOperation.Minimum)
                    newYFactor = Mathf.Min(yFactorThisBand, yFactor);
                else if (shapeSettings.Operation == TerrainShape.AudioShapeOperation.Average)
                    newYFactor = (yFactorThisBand + yFactor) / 2;

                yFactor += shapeSettings.Strength * (newYFactor - yFactor);
            }

            return originalY * yFactor;
        }
        private float ApplyDampening(int x, float originalY, int z)
        {
            if (!_applyDampening)
                return originalY;

            float y = originalY;

            if (y < _lastYs[x, z])
                y = Mathf.Lerp(_lastYs[x, z], y, _dampenLerpFactorDown * Utility.Utility.GetFpsEqualizer());
            else
                y = Mathf.Lerp(_lastYs[x, z], y, _dampenLerpFactorUp * Utility.Utility.GetFpsEqualizer());

            return y;
        }
        private float ApplyTerrace(float originalY)
        {
            if (!_applyTerrace)
                return originalY;

            float y = originalY;
            if (_terraceCount > 0)
                y = y - (y % (1 / _terraceCount));
            return y;
        }
        #endregion


        #region Color
        private Color GetColor(float x, float y, float z)
        {
            if (!_applyColorByHeight)
                return Color.magenta;

            Color color = GetColorByHeight(y);

            if (_colorOutOfBounds && IsOutOfAllShapes((int)x, (int)z))
            {
                color.r *= _outOfBoundsBrightness;
                color.g *= _outOfBoundsBrightness;
                color.b *= _outOfBoundsBrightness;
            }

            return color;
        }

        private Color GetColorByHeight(float y)
        {
            return _colorHeightGradient.Evaluate(y);
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


        #region Audio
        private void PrepareAudio()
        {
            if (!_applyAudio)
                return;

            _samplesContainer.TryRecalculate();
            if (_outOfBoundsOperation == OutOfBounds.Constant)
                _outOfBoundsFactorValue = 1;
            else if (_outOfBoundsOperation == OutOfBounds.Average)
                _outOfBoundsFactorValue = _samplesContainer.Average;
            else if (_outOfBoundsOperation == OutOfBounds.OuterBand)
                _outOfBoundsFactorValue = _samplesContainer.GetValue(_samplesContainer.BandSize - 1);
            else if (_outOfBoundsOperation == OutOfBounds.InnerBand)
                _outOfBoundsFactorValue = _samplesContainer.GetValue(0);
            else if (_outOfBoundsOperation == OutOfBounds.Flat)
                _outOfBoundsFactorValue = 0;

            _outOfBoundsFactorValue *= _outOfBoundsFactor;

            if (_outOfBoundsMaximumIsOutestBand)
                _outOfBoundsFactorValue = Mathf.Min(_outOfBoundsFactorValue, _samplesContainer.GetValue(_samplesContainer.BandSize - 1));
        }
        private float GetHeightFactorByAudioBand(/*float originalY, */float bandIndexNormalized)
        {
            int bandIndex = (int)(bandIndexNormalized * _samplesContainer.BandSize);
            bandIndex = Mathf.Min(bandIndex, _samplesContainer.BandSize - 1);

            if (bandIndexNormalized < 0) // handle out of bounds band
            {
                if (_outOfBoundsOperation == OutOfBounds.OuterBand)
                {
                    bandIndexNormalized = 0.999999f;
                    bandIndex = _samplesContainer.BandSize - 1;
                    if (_applyAudioSmoothing)
                        return ApplyAudioBandSmooth(/*originalY, */bandIndex, bandIndexNormalized);
                    else if (_applyAudioDebugMode)
                        return (1 - ((float)bandIndex / _samplesContainer.BandSize));
                    else
                        return _outOfBoundsFactorValue;
                }
                else
                    return _outOfBoundsFactorValue;
            }


            if (_applyAudioSmoothing)
                return ApplyAudioBandSmooth(/*originalY, */bandIndex, bandIndexNormalized);
            else if (_applyAudioDebugMode)
                return 1 - ((float)bandIndex / _samplesContainer.BandSize);
            else
                return _samplesContainer.GetValue()[bandIndex];
        }

        private float ApplyAudioBandSmooth(/*float originalY,*/ int bandIndex, float indexNormalized)
        {
            if (!_applyAudioSmoothing)
                return 1;

            if (_samplesContainer.BandSize < 2)
                return 1;

            float ownSampleValue = _applyAudioDebugMode ? /*originalY **/ (1 - ((float)bandIndex / _samplesContainer.BandSize)) : _samplesContainer.GetValue(bandIndex);
            float indexFraction = (indexNormalized * _samplesContainer.BandSize) % 1; // index of 3.7f is now 0.7f

            if (indexNormalized >= 1)
                indexFraction = 0.999999f;

            // first band: only apply the second band
            if (bandIndex == 0)
            {
                if (indexFraction > 0.5f)
                {
                    float nextValue = _samplesContainer.GetValue(bandIndex + 1);
#if UNITY_EDITOR
                    if (_applyAudioDebugMode)
                        nextValue = /*originalY **/ (1 - (((float)bandIndex + 1) / _samplesContainer.BandSize));
#endif
                    float curveValue = _audioSmoothingCurve.Evaluate((indexFraction - 0.5f) * 2);
                    return ownSampleValue * curveValue + nextValue * (1 - curveValue);
                }
                else
                    return ownSampleValue;
            }
            // last band: apply the second last band and the out of bounds "band"
            else if (bandIndex == _samplesContainer.BandSize - 1)
            {
                if (indexFraction < 0.5f)
                {
                    float prevValue = _samplesContainer.GetValue(bandIndex - 1);
#if UNITY_EDITOR
                    if (_applyAudioDebugMode)
                        prevValue = /*originalY **/ (1 - (((float)bandIndex - 1) / _samplesContainer.BandSize));
#endif
                    float curveValue = _audioSmoothingCurve.Evaluate(Utility.Utility.Remap(0.5f, 0, 0, 1, indexFraction)/*(indexFraction - 0.5f) * 2*/);
                    return ownSampleValue * curveValue + prevValue * (1 - curveValue);
                }
                else
                {
                    // handle smoothing to out of bounds. If _outOfBoundsOperation == OutOfBounds.OutestBand, no smoothing is neccessary
                    if (!_smoothLastBandWithOutOfBounds)
                        return ownSampleValue;

                    float curveValue = _audioSmoothingCurve.Evaluate((indexFraction - 0.5f) * 2);
                    if (_smoothLastBandWithOutOfBoundsCompletely) // blend in completely instead of just half (if the last curves value at x==1 equals y=0.5f
                        curveValue = Utility.Utility.Remap(1, 0.5f, 1, 0, curveValue);
                    return ownSampleValue * curveValue + _outOfBoundsFactorValue * (1 - curveValue);
                }
            }
            // middle bands: apply left previous and next band
            else
            {
                if (indexFraction < 0.5f)
                {
                    float prevValue = _samplesContainer.GetValue(bandIndex - 1);
#if UNITY_EDITOR
                    if (_applyAudioDebugMode)
                        prevValue = /*originalY **/ (1 - (((float)bandIndex - 1) / _samplesContainer.BandSize));
#endif
                    float curveValue = _audioSmoothingCurve.Evaluate(Utility.Utility.Remap(0.5f, 0, 0, 1, indexFraction)/*(indexFraction - 0.5f) * 2*/);
                    return ownSampleValue * curveValue + prevValue * (1 - curveValue);
                }
                else /*if (indexFraction > 0.5f)*/
                {
                    float nextValue = _samplesContainer.GetValue(bandIndex + 1);
#if UNITY_EDITOR
                    if (_applyAudioDebugMode)
                        nextValue = /*originalY **/ (1 - (((float)bandIndex + 1) / _samplesContainer.BandSize));
#endif
                    float curveValue = _audioSmoothingCurve.Evaluate((indexFraction - 0.5f) * 2);
                    return ownSampleValue * curveValue + nextValue * (1 - curveValue);
                }
            }
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