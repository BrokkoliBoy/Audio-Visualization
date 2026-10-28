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
        /// <summary>Quietest bands (or the weakest parts of the pattern) keep this share of the brightness when the brightness follows them.</summary>
        private const float MinBandBrightness = 0.2f;
        /// <summary>Extra brightness of the strongest bands (or pattern parts) at full glow (in addition to the normal color).</summary>
        private const float MaxBandGlow = 3f;

        // --- grid ---
        public int FaceCountX;
        public int FaceCountZ;
        public float ScaleX;
        public float ScaleY;
        public float ScaleZ;

        // --- noise: the raw pattern per vertex, blended between the last two calculations ---
        public bool ApplyNoise;
        /// <summary>The deepest carve a single pattern layer reaches (at least a small value). A pattern turned upside down on the beat flows within it, and the colors scale with it.</summary>
        public float CarveReference;
        public float InvertEased;
        /// <summary>The noise is calculated again every NoiseInterval updates, per vertex in the update of its slot. NoiseTick is the current update.</summary>
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
        public float ColorScale;
        public float ColorHeadroom;
        public float ColorBandHue;
        public float ColorBandBrightness;
        public float ColorBandGlow;
        public float ColorPatternHue;
        public float ColorPatternBrightness;
        public float ColorPatternGlow;
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

            for (int x = 0; x <= FaceCountX; x++, index++)
            {
                float noise = 0f;
                if (ApplyNoise)
                {
                    noise = ReadNoise(index);
                    noise = Mathf.Lerp(noise, Mathf.Max(0f, CarveReference - noise), InvertEased);
                }

                float y = GetY(x, z, index, noise, out float band);
                Vertices[index] = new Vector3(x * ScaleX, y * ScaleY, z * ScaleZ);
                Colors[index] = GetColor(y, band, ApplyNoise ? 1f - Mathf.Clamp01(noise / CarveReference) : 1f, ColorOutOfBounds && OutOfAllShapes[index]);
                LastYs[index] = y;
                LastBands[index] = band;
            }
        }

        #region Noise
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
                noiseFactor = 1f - Mathf.Clamp01(noise);
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
                y = Mathf.Lerp(last, y, Smooth(y < last ? DampenDown : DampenUp));
                float lastBand = LastBands[index];
                band = Mathf.Lerp(lastBand, bandFactor, Smooth(bandFactor < lastBand ? DampenDown : DampenUp));
            }
            if (TerracePoint == 2)
                y = Terrace(y);
            return y;
        }

        // FpsEqualizer is the number of reference steps since the last update, so the dampening takes the same real time at any frame rate
        private float Smooth(float factor)
        {
            return 1f - Mathf.Pow(1f - Mathf.Clamp01(factor), FpsEqualizer);
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
                else if (WaveMode == (int)TerrainBeatWave.Mode.Subtract)
                    y -= level * weight;
                else if (WaveMode == (int)TerrainBeatWave.Mode.Minimum)
                    y = Mathf.Min(y, level * weight);
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
        /// <summary>pattern: how strong the noise pattern is at the vertex (1 = the part of the pattern that stands highest, like the height it gives).</summary>
        private Color GetColor(float y, float band, float pattern, bool outsideAllShapes)
        {
            if (!ApplyColor)
                return Color.magenta;

            float position = Mathf.Lerp(SoftEnd(y * ColorScale), band, ColorBandHue);
            Color color = ReadGradient(Mathf.Lerp(position, pattern, ColorPatternHue));
            if (ColorBandBrightness > 0f)
                color = Scale(color, Mathf.Lerp(1f, MinBandBrightness + (1f - MinBandBrightness) * Mathf.Clamp01(band), ColorBandBrightness));
            if (ColorBandGlow > 0f)
                color = Scale(color, 1f + ColorBandGlow * MaxBandGlow * Mathf.Clamp01(band));
            if (ColorPatternBrightness > 0f)
                color = Scale(color, Mathf.Lerp(1f, MinBandBrightness + (1f - MinBandBrightness) * Mathf.Clamp01(pattern), ColorPatternBrightness));
            if (ColorPatternGlow > 0f)
                color = Scale(color, 1f + ColorPatternGlow * MaxBandGlow * Mathf.Clamp01(pattern));

            if (outsideAllShapes)
            {
                color.r *= OutOfBoundsBrightness;
                color.g *= OutOfBoundsBrightness;
                color.b *= OutOfBoundsBrightness;
            }
            return color;
        }

        /// <summary>
        /// Position in the gradient for a height. Above the knee the position bends towards 1 so heights over 1 still differ a little
        /// (1 at the height of 2) instead of all landing on the last color. ColorHeadroom blends from the hard end to this curve.
        /// </summary>
        private float SoftEnd(float time)
        {
            float hard = Mathf.Clamp01(time);
            if (ColorHeadroom <= 0f || time <= SoftKnee)
                return hard;

            float x = (time - SoftKnee) / (SoftReach - SoftKnee) * SoftSharpness;
            float e = Mathf.Exp(-2f * x);
            float soft = SoftKnee + (1f - SoftKnee) * ((1f - e) / (1f + e));
            return Mathf.Lerp(hard, soft, ColorHeadroom);
        }

        private const float SoftKnee = 0.6f;
        private const float SoftReach = 1.5f;
        private const float SoftSharpness = 1.66f;

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

    /// <summary>
    /// Fake light on the colors: the terrain has no light of its own, so every contour comes from the color. This job shades every
    /// vertex by the slope of the ground against a fixed light direction (surfaces facing the light brighter, the others darker),
    /// after the colors are set. The alpha of the color is the weight (1 = flat glow curve there, less where the curve is steep), it is set back to 1. The shading is in exposure steps, and stronger for bright colors because the tone mapping
    /// flattens steps there.
    /// </summary>
    [BurstCompile]
    public struct TerrainReliefJob : IJobParallelFor
    {
        /// <summary>Largest factor the shading of a bright color is scaled up by.</summary>
        private const float MaxBoost = 4f;
        /// <summary>Slope at which the shading reaches half of its strength.</summary>
        private const float HalfSlope = 0.5f;

        public int FaceCountX;
        public int FaceCountZ;
        public float ScaleX;
        public float ScaleZ;
        /// <summary>Largest shading in exposure steps, up and down.</summary>
        public float Strength;
        /// <summary>Direction towards the light on the ground (length 1).</summary>
        public float LightX;
        public float LightZ;
        [ReadOnly] public NativeArray<Vector3> Vertices;
        [NativeDisableParallelForRestriction] public NativeArray<Color> Colors;

        public void Execute(int z)
        {
            int stride = FaceCountX + 1;
            int zA = Mathf.Max(z - 1, 0);
            int zB = Mathf.Min(z + 1, FaceCountZ);
            for (int x = 0; x <= FaceCountX; x++)
            {
                int xA = Mathf.Max(x - 1, 0);
                int xB = Mathf.Min(x + 1, FaceCountX);
                float gradientX = (Vertices[z * stride + xB].y - Vertices[z * stride + xA].y) / Mathf.Max((xB - xA) * ScaleX, 0.0001f);
                float gradientZ = (Vertices[zB * stride + x].y - Vertices[zA * stride + x].y) / Mathf.Max((zB - zA) * ScaleZ, 0.0001f);
                float lit = -(gradientX * LightX + gradientZ * LightZ);
                float relief = lit / (HalfSlope + Mathf.Abs(lit));

                int index = z * stride + x;
                Color color = Colors[index];
                float brightness = 0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b;
                float x2 = Mathf.Min(brightness, 20f);
                float sensitivity = x2 < 0.001f ? 1f : x2 / (Mathf.Exp(x2) - 1f);
                float boost = Mathf.Clamp(1f / Mathf.Max(sensitivity, 0.01f), 1f, MaxBoost);
                float factor = Mathf.Pow(2f, Strength * relief * boost * color.a);
                Colors[index] = new Color(color.r * factor, color.g * factor, color.b * factor, 1f);
            }
        }
    }

    /// <summary>Bends the flat vertices of the terrain by the morph (dome, planet, tube ...), one vertex per job step.</summary>
    [BurstCompile]
    public struct TerrainMorphJob : IJobParallelFor
    {
        /// <summary>Angle (in radians) of the planet from its top to the edge of the terrain. More than a half sphere, so the rim hangs below the equator.</summary>
        public const float PlanetAngle = 0.75f * Mathf.PI;
        /// <summary>Angle (in radians) of the tube from its top to the left and right edge of the terrain at full amount: both edges meet at the bottom, a closed tube.</summary>
        public const float TubeAngle = Mathf.PI;
        /// <summary>Size of the pillar at full amount compared to the flat terrain (the column is as tall as the terrain is wide, so it is scaled down to fit the picture).</summary>
        public const float PillarFit = 0.5f;
        /// <summary>Largest height of dome, pringle and flag, as a share of half the terrain width.</summary>
        public const float BendHeight = 0.5f;
        public const float PringleHeight = 0.35f;
        public const float FlagHeight = 0.1f;
        /// <summary>Largest turn of the twist (in radians) at the edge of the terrain.</summary>
        public const float TwistAngle = Mathf.PI;

        public int Shape;
        public float Amount;
        /// <summary>Height added to planet and tube, see TerrainGenerator.MorphLiftTarget.</summary>
        public float Lift;
        public float Half;
        public float CenterX;
        public float CenterZ;
        public float Seconds;
        /// <summary>While the shape changes: the old shape (0 = none), its amount and lift. <see cref="Blend"/> 0 shows only the old one, 1 only the new one.</summary>
        public int FromShape;
        public float FromAmount;
        public float FromLift;
        public float Blend;
        [ReadOnly] public NativeArray<Vector3> Flat;
        [WriteOnly] public NativeArray<Vector3> Bent;

        public void Execute(int i)
        {
            Vector3 flat = Flat[i];
            Vector3 bent = Bend(flat, Shape, Amount, Lift);
            // the positions of both shapes are mixed, so a change of the shape glides instead of moving every vertex in one frame
            if (FromShape != 0 && Blend < 1f)
                bent = Vector3.Lerp(Bend(flat, FromShape, FromAmount, FromLift), bent, Blend);
            Bent[i] = bent;
        }

        private Vector3 Bend(Vector3 flat, int shape, float amount, float liftHeight)
        {
            float planetRadius = Half / PlanetAngle;
            // the tube rolls up further the more the amount grows, its radius shrinks with it so the width stays the same
            float tubeRadius = Half / (Mathf.Max(amount, 0.0005f) * TubeAngle);

            float dx = flat.x - CenterX;
            float dz = flat.z - CenterZ;
            float u = dx / Half;
            float v = dz / Half;
            float h = flat.y;
            Vector3 bent = flat;

            switch ((TerrainGenerator.MorphShape)shape)
            {
                case TerrainGenerator.MorphShape.Dome:
                    bent.y = h + amount * BendHeight * Half * (1f - (u * u + v * v));
                    break;
                case TerrainGenerator.MorphShape.Pringle:
                    bent.y = h + amount * PringleHeight * Half * (u * u - v * v);
                    break;
                case TerrainGenerator.MorphShape.Flag:
                    bent.y = h + amount * FlagHeight * Half * Mathf.Sin(u * 5f + v * 2.5f - Seconds * 2.5f);
                    break;
                case TerrainGenerator.MorphShape.Twist:
                {
                    float angle = amount * TwistAngle * Mathf.Sqrt(u * u + v * v);
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
                    bent.x = CenterX + Mathf.Lerp(dx, dx * scale, amount);
                    bent.z = CenterZ + Mathf.Lerp(dz, dz * scale, amount);
                    bent.y = Mathf.Lerp(h, lift, amount) + liftHeight;
                    break;
                }
                case TerrainGenerator.MorphShape.Tube:
                {
                    // the terrain rolls up along its width, at full amount to a closed tube
                    float angle = u * amount * TubeAngle;
                    bent.x = CenterX + (tubeRadius + h) * Mathf.Sin(angle);
                    bent.y = (tubeRadius + h) * Mathf.Cos(angle) - tubeRadius + liftHeight;
                    break;
                }
                case TerrainGenerator.MorphShape.Pillar:
                {
                    // the tube of above, which then stands up: the depth of the terrain becomes the height of the pillar (the near edge,
                    // where the bass is, ends up at the bottom), the roll becomes the round of the pillar, the height of the terrain
                    // becomes the distance from its axis. The stand-up turn is around the top of the tube, so nothing jumps at amount 0.
                    float angle = u * amount * TubeAngle;
                    float tubeX = (tubeRadius + h) * Mathf.Sin(angle);
                    float tubeY = (tubeRadius + h) * Mathf.Cos(angle) - tubeRadius;
                    float stand = amount * 0.5f * Mathf.PI;
                    float sin = Mathf.Sin(stand), cos = Mathf.Cos(stand);
                    // at full amount the tube has the radius Half / PI, the axis is moved into the middle of the terrain by that
                    float axis = Half / Mathf.PI * sin;
                    // the whole thing shrinks with the amount, so the standing column fits on the screen at the start zoom
                    float fit = Mathf.Lerp(1f, PillarFit, amount);
                    bent.x = CenterX + tubeX * fit;
                    bent.y = (tubeY * cos + dz * sin) * fit;
                    bent.z = CenterZ + (dz * cos - tubeY * sin - axis) * fit;
                    break;
                }
            }
            return bent;
        }
    }
}
