using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Gavi
{
    /// <summary>
    /// Settings of one extra pattern layer on top of the main pattern (layers 2 and 3). The main pattern keeps its own fields
    /// in the terrain profile. Also what the noise job needs to know about a layer (see <see cref="TerrainNoiseJob"/>).
    /// </summary>
    [System.Serializable]
    public class TerrainNoiseLayerData
    {
        /// <summary>The number of extra layers.</summary>
        public const int Count = 2;

        public bool Enabled;
        [Tooltip("Index of the pattern (TerrainNoise.Pattern).")]
        public int Pattern = (int)TerrainNoise.Pattern.Simplex;
        [Tooltip("TerrainNoise.LayerOperation: how the layer is mixed with the layers below.")]
        public int Operation = (int)TerrainNoise.LayerOperation.Add;
        [Tooltip("How much of the mixed result is used, 0 = the layers below stay as they are.")]
        public float Depth = 0.5f;
        public float Zoom = 0.6f;
        public float SpeedX;
        public float SpeedY;
        public bool Invert;

        public void CopyFrom(TerrainNoiseLayerData other)
        {
            if (other == null)
                return;
            Enabled = other.Enabled;
            Pattern = other.Pattern;
            Operation = other.Operation;
            Depth = other.Depth;
            Zoom = other.Zoom;
            SpeedX = other.SpeedX;
            SpeedY = other.SpeedY;
            Invert = other.Invert;
        }
    }

    /// <summary>One layer as the noise job reads it. Inverting is already done by the job with Invert (0 = as it is, 1 = upside down).</summary>
    public struct TerrainNoiseLayerJobData
    {
        public int Pattern;
        public int Operation;
        public float Depth;
        public float Zoom;
        public float OffsetX;
        public float OffsetZ;
        public float Invert;
    }

    /// <summary>
    /// Calculates the pattern (all layers mixed) of every grid point of the terrain, one row per job step on all processor cores.
    /// Writes into NoiseNext. With DueOnly only the grid points of the current slot are calculated again, their old result moves to
    /// NoisePrevious (the update mode that spreads the work over several updates).
    /// </summary>
    [BurstCompile]
    public struct TerrainNoiseJob : IJobParallelFor
    {
        public int Width;
        /// <summary>Grid steps of the reference terrain per vertex, so a pattern keeps its size when the face count changes.</summary>
        public float StepX;
        public float StepZ;
        public bool DueOnly;
        public int Age;
        public int LayerCount;
        [ReadOnly] public NativeArray<TerrainNoiseLayerJobData> Layers;
        [ReadOnly] public NativeArray<byte> Slots;
        [NativeDisableParallelForRestriction] public NativeArray<float> Previous;
        [NativeDisableParallelForRestriction] public NativeArray<float> Next;

        public void Execute(int row)
        {
            float z = row * StepZ;
            int index = row * Width;
            for (int column = 0; column < Width; column++, index++)
            {
                if (DueOnly && Slots[index] != Age)
                    continue;

                float x = column * StepX;
                float value = 0f;
                for (int l = 0; l < LayerCount; l++)
                {
                    TerrainNoiseLayerJobData layer = Layers[l];
                    float layerValue = TerrainNoise.Evaluate((TerrainNoise.Pattern)layer.Pattern, x, z, layer.OffsetX, layer.OffsetZ, layer.Zoom);
                    layerValue = Mathf.Lerp(layerValue, 1f - layerValue, layer.Invert);
                    value = l == 0 ? layerValue : TerrainNoise.Combine(value, layerValue, layer.Operation, layer.Depth);
                }

                if (DueOnly)
                    Previous[index] = Next[index];
                Next[index] = value;
            }
        }
    }
}
