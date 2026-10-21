using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Gavi
{
    /// <summary>What the vertex job needs to know about one audio shape (the band index of every vertex is in a separate array).</summary>
    public struct TerrainShapeJobData
    {
        public bool Active;
        public int Operation;
        public float Strength;
    }

    /// <summary>
    /// Calculates height, color and dampening of every vertex, one row of the terrain per job step on all processor cores.
    /// Does the same as the former per vertex code of <see cref="TerrainGenerator"/>. The animation curve and the color gradient
    /// cannot be used in a job, so they come in as tables (<see cref="LutSize"/> entries, linear in between).
    /// </summary>
    [BurstCompile]
    public struct TerrainVertexJob : IJobParallelFor
    {
        public const int LutSize = 257;
        private const int OperationOverride = (int)TerrainShape.AudioShapeOperation.Override;
        private const int OperationAdd = (int)TerrainShape.AudioShapeOperation.Add;
        private const int OperationSubtract = (int)TerrainShape.AudioShapeOperation.Subtract;
        private const int OperationMaximum = (int)TerrainShape.AudioShapeOperation.Maximum;
        private const int OperationMinimum = (int)TerrainShape.AudioShapeOperation.Minimum;
        private const int OperationAverage = (int)TerrainShape.AudioShapeOperation.Average;
        private const int OutOfBoundsOuterBand = (int)TerrainGenerator.OutOfBounds.OuterBand;
        /// <summary>Quietest bands keep this share of the brightness when the brightness follows the band.</summary>
        private const float MinBandBrightness = 0.2f;
        /// <summary>Extra brightness of the strongest bands at full band glow (in addition to the normal color).</summary>
        private const float MaxBandGlow = 3f;

        // --- grid ---
        public int FaceCountX;
        public int FaceCountZ;
        public float ScaleX;
        public float ScaleY;
        public float ScaleZ;

        // --- noise: the raw pattern on a grid with every NoiseStep-th vertex, blended between the last two calculations ---
        public bool ApplyNoise;
        public float PerlinStrength;
        public float InvertEased;
        public int NoiseStep;
        public int NoiseWidth;
        public int NoiseHeight;
        /// <summary>Between the grid points a smooth curve (Catmull-Rom) instead of straight lines. Only used with a NoiseStep above 1.</summary>
        public bool NoiseSmooth;
        /// <summary>The noise is calculated again every NoiseInterval updates, per grid point in the update of its slot. NoiseTick is the current update.</summary>
        public int NoiseInterval;
        public int NoiseTick;
        [ReadOnly] public NativeArray<byte> NoiseSlots;
        [ReadOnly] public NativeArray<float> NoisePrevious;
        [ReadOnly] public NativeArray<float> NoiseNext;

        // --- terrace ---
        public bool ApplyTerrace;
        public int TerraceCount;
        /// <summary>0 = after the noise, 1 = after the audio, 2 = after the dampening.</summary>
        public int TerracePoint;

        // --- audio ---
        public bool ApplyAudio;
        public bool DebugMode;
        public int BandCount;
        public int VertexCount;
        public int ShapeCount;
        public int OutOfBoundsOperation;
        public float OutOfBoundsValue;
        public bool Smoothing;
        public bool SmoothLastWithOutOfBounds;
        public bool SmoothLastWithOutOfBoundsCompletely;
        [ReadOnly] public NativeArray<float> BandValues;
        [ReadOnly] public NativeArray<float> ShapeIndices;
        [ReadOnly] public NativeArray<TerrainShapeJobData> Shapes;
        [ReadOnly] public NativeArray<float> SmoothingCurve;

        // --- beat wave ---
        public int WaveCount;
        public float WaveWidth;
        public int WaveMode;
        public bool WaveUseNoise;
        [ReadOnly] public NativeArray<float> WaveFronts;
        [ReadOnly] public NativeArray<float> WaveLevels;

        // --- dampening ---
        public bool Dampen;
        public float DampenDown;
        public float DampenUp;
        public float FpsEqualizer;

        // --- color ---
        public bool ApplyColor;
        public float ColorBandHue;
        public float ColorBandBrightness;
        public float ColorBandGlow;
        public bool ColorOutOfBounds;
        public float OutOfBoundsBrightness;
        [ReadOnly] public NativeArray<bool> OutOfAllShapes;
        [ReadOnly] public NativeArray<Color> GradientTable;

        // --- results, every vertex is only written by the row it belongs to ---
        [NativeDisableParallelForRestriction] public NativeArray<Vector3> Vertices;
        [NativeDisableParallelForRestriction] public NativeArray<Color> Colors;
        [NativeDisableParallelForRestriction] public NativeArray<float> LastYs;
        [NativeDisableParallelForRestriction] public NativeArray<float> LastBands;

        public void Execute(int z)
        {
            int index = z * (FaceCountX + 1);
            GetNoiseAxis(z, FaceCountZ, NoiseHeight, out int z0, out int z1, out float zWeight);

            for (int x = 0; x <= FaceCountX; x++, index++)
            {
                float noise = 0f;
                if (ApplyNoise)
                {
                    GetNoiseAxis(x, FaceCountX, NoiseWidth, out int x0, out int x1, out float xWeight);
                    if (NoiseSmooth && NoiseStep > 1)
                    {
                        noise = SampleNoiseSmooth(z0, z1, zWeight, x0, x1, xWeight);
                    }
                    else
                    {
                        float a = Mathf.Lerp(ReadNoise(z0 * NoiseWidth + x0), ReadNoise(z0 * NoiseWidth + x1), xWeight);
                        float b = zWeight > 0f ? Mathf.Lerp(ReadNoise(z1 * NoiseWidth + x0), ReadNoise(z1 * NoiseWidth + x1), xWeight) : a;
                        noise = Mathf.Lerp(a, b, zWeight);
                    }
                    noise = Mathf.Lerp(noise, 1f - noise, InvertEased);
                }

                float y = GetY(x, z, index, noise, out float band);
                Vertices[index] = new Vector3(x * ScaleX, y * ScaleY, z * ScaleZ);
                Colors[index] = GetColor(y, band, ColorOutOfBounds && OutOfAllShapes[index]);
                LastYs[index] = y;
                LastBands[index] = band;
            }
        }

        #region Noise
        /// <summary>The two neighbours on the coarse noise grid and the weight of the second one.</summary>
        private void GetNoiseAxis(int position, int faceCount, int coarseCount, out int first, out int second, out float weight)
        {
            if (NoiseStep <= 1)
            {
                first = position;
                second = position;
                weight = 0f;
                return;
            }

            first = position / NoiseStep;
            int left = first * NoiseStep;
            if (left == position)
            {
                second = first;
                weight = 0f;
                return;
            }

            second = Mathf.Min(first + 1, coarseCount - 1);
            int right = Mathf.Min(left + NoiseStep, faceCount);
            weight = (float)(position - left) / (right - left);
        }

        /// <summary>Catmull-Rom through the four grid points around the vertex in both directions. The curve can overshoot a little, so the result is clamped to the noise range.</summary>
        private float SampleNoiseSmooth(int z0, int z1, float zWeight, int x0, int x1, float xWeight)
        {
            int xBefore = Mathf.Max(x0 - 1, 0);
            int xAfter = Mathf.Min(x1 + 1, NoiseWidth - 1);
            if (zWeight <= 0f)
                return SmoothRow(z0 * NoiseWidth, xBefore, x0, x1, xAfter, xWeight);

            int zBefore = Mathf.Max(z0 - 1, 0);
            int zAfter = Mathf.Min(z1 + 1, NoiseHeight - 1);
            float r0 = SmoothRow(zBefore * NoiseWidth, xBefore, x0, x1, xAfter, xWeight);
            float r1 = SmoothRow(z0 * NoiseWidth, xBefore, x0, x1, xAfter, xWeight);
            float r2 = SmoothRow(z1 * NoiseWidth, xBefore, x0, x1, xAfter, xWeight);
            float r3 = SmoothRow(zAfter * NoiseWidth, xBefore, x0, x1, xAfter, xWeight);
            return Mathf.Clamp01(CatmullRom(r0, r1, r2, r3, zWeight));
        }

        private float SmoothRow(int row, int xBefore, int x0, int x1, int xAfter, float xWeight)
        {
            if (xWeight <= 0f)
                return ReadNoise(row + x0);
            return CatmullRom(ReadNoise(row + xBefore), ReadNoise(row + x0), ReadNoise(row + x1), ReadNoise(row + xAfter), xWeight);
        }

        private static float CatmullRom(float p0, float p1, float p2, float p3, float t)
        {
            float t2 = t * t;
            return 0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t2 * t);
        }

        private float ReadNoise(int index)
        {
            if (NoiseInterval <= 1)
                return NoiseNext[index];

            int age = NoiseTick - NoiseSlots[index];
            if (age < 0)
                age += NoiseInterval;
            float blend = Mathf.Min(1f, (age + 1f) / NoiseInterval);
            return Mathf.Lerp(NoisePrevious[index], NoiseNext[index], blend);
        }
        #endregion

        #region Height
        private float GetY(int x, int z, int index, float noise, out float band)
        {
            float y = 1f;
            float noiseFactor = 1f;
            if (ApplyNoise)
            {
                noiseFactor = 1f - PerlinStrength * noise;
                y *= noiseFactor;
            }
            if (TerracePoint == 0)
                y = Terrace(y);

            y = ApplyAudioToHeight(index, y, out float bandFactor);
            if (TerracePoint == 1)
                y = Terrace(y);

            if (WaveCount > 0)
                y = ApplyWave(x, z, y, noiseFactor);

            band = bandFactor;
            if (Dampen)
            {
                float last = LastYs[index];
                y = Mathf.Lerp(last, y, (y < last ? DampenDown : DampenUp) * FpsEqualizer);
                float lastBand = LastBands[index];
                band = Mathf.Lerp(lastBand, bandFactor, (bandFactor < lastBand ? DampenDown : DampenUp) * FpsEqualizer);
            }
            if (TerracePoint == 2)
                y = Terrace(y);
            return y;
        }

        private float Terrace(float y)
        {
            if (ApplyTerrace && TerraceCount > 0)
                y -= y % (1f / TerraceCount);
            return y;
        }

        private float ApplyWave(int x, int z, float y, float noiseFactor)
        {
            float u = (float)x / FaceCountX * 2f - 1f;
            float v = (float)z / FaceCountZ * 2f - 1f;
            float distance = Mathf.Sqrt(u * u + v * v);

            for (int i = 0; i < WaveCount; i++)
            {
                float offset = (distance - WaveFronts[i]) / WaveWidth;
                if (offset > 3f || offset < -3f)
                    continue;

                float weight = Mathf.Exp(-offset * offset);
                float level = WaveUseNoise ? WaveLevels[i] * noiseFactor : WaveLevels[i];
                if (WaveMode == (int)TerrainBeatWave.Mode.Add)
                    y += level * weight;
                else if (WaveMode == (int)TerrainBeatWave.Mode.Floor)
                    y = Mathf.Max(y, level * weight);
                else
                    y = Mathf.Lerp(y, level, weight * TerrainBeatWave.AbsorbShare);
            }
            return y;
        }
        #endregion

        #region Audio
        private float ApplyAudioToHeight(int index, float originalY, out float bandFactor)
        {
            bandFactor = 1f;
            if (!ApplyAudio)
                return originalY;

            float yFactor = -1f;
            for (int s = 0; s < ShapeCount; s++)
            {
                TerrainShapeJobData shape = Shapes[s];
                if (!shape.Active)
                    continue;

                float bandIndexNormalized = Mathf.Min(1f, ShapeIndices[s * VertexCount + index]);
                float yFactorThisBand = GetHeightFactorByAudioBand(bandIndexNormalized);

                float newYFactor = yFactor;
                // the first shape always overrides
                if (yFactor == -1f)
                {
                    yFactor = 0f;
                    newYFactor = yFactorThisBand;
                }
                else if (shape.Operation == OperationOverride)
                    newYFactor = yFactorThisBand;
                else if (shape.Operation == OperationAdd)
                    newYFactor += yFactorThisBand;
                else if (shape.Operation == OperationSubtract)
                    newYFactor = Mathf.Abs(yFactorThisBand - yFactor);
                else if (shape.Operation == OperationMaximum)
                    newYFactor = Mathf.Max(yFactorThisBand, yFactor);
                else if (shape.Operation == OperationMinimum)
                    newYFactor = Mathf.Min(yFactorThisBand, yFactor);
                else if (shape.Operation == OperationAverage)
                    newYFactor = (yFactorThisBand + yFactor) / 2f;

                yFactor += shape.Strength * (newYFactor - yFactor);
            }

            bandFactor = Mathf.Clamp01(yFactor < 0f ? 1f : yFactor);
            return originalY * yFactor;
        }

        private float GetHeightFactorByAudioBand(float bandIndexNormalized)
        {
            int bandIndex = Mathf.Min((int)(bandIndexNormalized * BandCount), BandCount - 1);

            if (bandIndexNormalized < 0f) // out of bounds band
            {
                if (OutOfBoundsOperation == OutOfBoundsOuterBand)
                {
                    bandIndex = BandCount - 1;
                    if (Smoothing)
                        return SmoothBand(bandIndex, 0.999999f);
                    if (DebugMode)
                        return BandValues[bandIndex];
                }
                return OutOfBoundsValue;
            }

            if (Smoothing)
                return SmoothBand(bandIndex, bandIndexNormalized);
            return BandValues[bandIndex];
        }

        private float SmoothBand(int bandIndex, float indexNormalized)
        {
            if (BandCount < 2)
                return 1f;

            float ownValue = BandValues[bandIndex];
            float indexFraction = (indexNormalized * BandCount) % 1f; // index of 3.7 is now 0.7
            if (indexNormalized >= 1f)
                indexFraction = 0.999999f;

            // first band: only blend with the second band
            if (bandIndex == 0)
            {
                if (indexFraction <= 0.5f)
                    return ownValue;
                float curve = Curve((indexFraction - 0.5f) * 2f);
                return ownValue * curve + BandValues[bandIndex + 1] * (1f - curve);
            }

            // last band: blend with the second last band and with the out of bounds value
            if (bandIndex == BandCount - 1)
            {
                if (indexFraction < 0.5f)
                {
                    float curve = Curve(Remap(0.5f, 0f, 0f, 1f, indexFraction));
                    return ownValue * curve + BandValues[bandIndex - 1] * (1f - curve);
                }
                if (!SmoothLastWithOutOfBounds)
                    return ownValue;

                float outCurve = Curve((indexFraction - 0.5f) * 2f);
                if (SmoothLastWithOutOfBoundsCompletely) // blend in completely instead of just half
                    outCurve = Remap(1f, 0.5f, 1f, 0f, outCurve);
                return ownValue * outCurve + OutOfBoundsValue * (1f - outCurve);
            }

            // middle bands: blend with the previous or the next band
            if (indexFraction < 0.5f)
            {
                float curve = Curve(Remap(0.5f, 0f, 0f, 1f, indexFraction));
                return ownValue * curve + BandValues[bandIndex - 1] * (1f - curve);
            }
            else
            {
                float curve = Curve((indexFraction - 0.5f) * 2f);
                return ownValue * curve + BandValues[bandIndex + 1] * (1f - curve);
            }
        }

        private float Curve(float time)
        {
            float position = Mathf.Clamp01(time) * (LutSize - 1);
            int lower = (int)position;
            int upper = Mathf.Min(lower + 1, LutSize - 1);
            return Mathf.Lerp(SmoothingCurve[lower], SmoothingCurve[upper], position - lower);
        }

        private static float Remap(float oldMin, float oldMax, float newMin, float newMax, float value)
        {
            return newMin + (newMax - newMin) * ((value - oldMin) / (oldMax - oldMin));
        }
        #endregion

        #region Color
        private Color GetColor(float y, float band, bool outsideAllShapes)
        {
            if (!ApplyColor)
                return Color.magenta;

            Color color = ReadGradient(Mathf.Lerp(y, band, ColorBandHue));
            if (ColorBandBrightness > 0f)
                color = Scale(color, Mathf.Lerp(1f, MinBandBrightness + (1f - MinBandBrightness) * Mathf.Clamp01(band), ColorBandBrightness));
            if (ColorBandGlow > 0f)
                color = Scale(color, 1f + ColorBandGlow * MaxBandGlow * Mathf.Clamp01(band));

            if (outsideAllShapes)
            {
                color.r *= OutOfBoundsBrightness;
                color.g *= OutOfBoundsBrightness;
                color.b *= OutOfBoundsBrightness;
            }
            return color;
        }

        private Color ReadGradient(float time)
        {
            float position = Mathf.Clamp01(time) * (LutSize - 1);
            int lower = (int)position;
            int upper = Mathf.Min(lower + 1, LutSize - 1);
            float t = position - lower;
            Color a = GradientTable[lower];
            Color b = GradientTable[upper];
            return new Color(a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t, a.a + (b.a - a.a) * t);
        }

        private static Color Scale(Color color, float factor)
        {
            return new Color(color.r * factor, color.g * factor, color.b * factor, color.a);
        }
        #endregion
    }

    /// <summary>Bends the flat vertices of the terrain by the morph (bowl, planet, tube ...), one vertex per job step.</summary>
    [BurstCompile]
    public struct TerrainMorphJob : IJobParallelFor
    {
        /// <summary>Angle (in radians) of the planet from its top to the edge of the terrain. More than a half sphere, so the rim hangs below the equator.</summary>
        public const float PlanetAngle = 0.75f * Mathf.PI;
        /// <summary>Angle (in radians) of the tube from its top to the left and right edge of the terrain.</summary>
        public const float TubeAngle = 0.75f * Mathf.PI;
        /// <summary>Largest height of bowl, dome, halfpipe, saddle and flag, as a share of half the terrain width.</summary>
        public const float BendHeight = 0.5f;
        public const float SaddleHeight = 0.35f;
        public const float FlagHeight = 0.1f;
        /// <summary>Largest turn of the twist (in radians) at the edge of the terrain.</summary>
        public const float TwistAngle = Mathf.PI;

        public int Shape;
        public float Amount;
        public float Half;
        public float CenterX;
        public float CenterZ;
        public float Seconds;
        [ReadOnly] public NativeArray<Vector3> Flat;
        [WriteOnly] public NativeArray<Vector3> Bent;

        public void Execute(int i)
        {
            float planetRadius = Half / PlanetAngle;
            float tubeRadius = Half / TubeAngle;

            Vector3 flat = Flat[i];
            float dx = flat.x - CenterX;
            float dz = flat.z - CenterZ;
            float u = dx / Half;
            float v = dz / Half;
            float h = flat.y;
            Vector3 bent = flat;

            switch ((TerrainGenerator.MorphShape)Shape)
            {
                case TerrainGenerator.MorphShape.Bowl:
                    bent.y = h + Amount * BendHeight * Half * (u * u + v * v);
                    break;
                case TerrainGenerator.MorphShape.Dome:
                    bent.y = h + Amount * BendHeight * Half * (1f - (u * u + v * v));
                    break;
                case TerrainGenerator.MorphShape.Halfpipe:
                    bent.y = h + Amount * BendHeight * Half * u * u;
                    break;
                case TerrainGenerator.MorphShape.Saddle:
                    bent.y = h + Amount * SaddleHeight * Half * (u * u - v * v);
                    break;
                case TerrainGenerator.MorphShape.Flag:
                    bent.y = h + Amount * FlagHeight * Half * Mathf.Sin(u * 5f + v * 2.5f - Seconds * 2.5f);
                    break;
                case TerrainGenerator.MorphShape.Twist:
                {
                    float angle = Amount * TwistAngle * Mathf.Sqrt(u * u + v * v);
                    float sin = Mathf.Sin(angle), cos = Mathf.Cos(angle);
                    bent.x = CenterX + dx * cos - dz * sin;
                    bent.z = CenterZ + dx * sin + dz * cos;
                    break;
                }
                case TerrainGenerator.MorphShape.Planet:
                {
                    // the terrain wraps around a ball, its height becomes the distance from the middle of the ball
                    float r = Mathf.Sqrt(u * u + v * v);
                    float angle = r * PlanetAngle;
                    float ring = (planetRadius + h) * Mathf.Sin(angle);
                    float lift = (planetRadius + h) * Mathf.Cos(angle) - planetRadius;
                    float scale = r > 0.0001f ? ring / (r * Half) : 1f;
                    bent.x = CenterX + Mathf.Lerp(dx, dx * scale, Amount);
                    bent.z = CenterZ + Mathf.Lerp(dz, dz * scale, Amount);
                    bent.y = Mathf.Lerp(h, lift, Amount);
                    break;
                }
                case TerrainGenerator.MorphShape.Tube:
                {
                    // the terrain rolls up along its width
                    float angle = u * TubeAngle;
                    float ring = (tubeRadius + h) * Mathf.Sin(angle);
                    float lift = (tubeRadius + h) * Mathf.Cos(angle) - tubeRadius;
                    bent.x = CenterX + Mathf.Lerp(dx, ring, Amount);
                    bent.y = Mathf.Lerp(h, lift, Amount);
                    break;
                }
            }
            Bent[i] = bent;
        }
    }
}
