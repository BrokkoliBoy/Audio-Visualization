using UnityEngine;

namespace Gavi
{
    /// <summary>
    /// Patterns that shape the height of the terrain before the audio is applied. Every pattern returns a value
    /// between 0 and 1. The coordinates are grid positions of the reference terrain, so a pattern keeps its size
    /// when the face count changes.
    /// </summary>
    public static class TerrainNoise
    {
        /// <summary>Appended only, profiles store the index.</summary>
        public enum Pattern
        {
            Perlin, Simplex, Value, Fractal, Ridged, Billow, DomainWarp,
            Voronoi, Mosaic, Cracks, Rings, Interference, Spiral, Waffle
        }

        public static readonly string[] Names =
        {
            "Perlin", "Simplex", "Value", "Fractal", "Ridged", "Billow", "Domain warp",
            "Voronoi", "Mosaic", "Cracks", "Rings", "Interference", "Spiral", "Waffle"
        };

        /// <summary>Center of the reference terrain, the middle of the radial patterns.</summary>
        private const float Center = 48f;
        private const float Tau = 6.2831853f;

        /// <param name="x">Grid position x of the reference terrain.</param>
        /// <param name="z">Grid position z of the reference terrain.</param>
        /// <param name="offsetX">Scrolling offset on x, in grid units.</param>
        /// <param name="offsetZ">Scrolling offset on z. Radial patterns move outward when it gets smaller.</param>
        /// <param name="zoom">Larger zoom = smaller structures.</param>
        public static float Evaluate(Pattern pattern, float x, float z, float offsetX, float offsetZ, float zoom)
        {
            float u = (x + offsetX) * zoom;
            float v = (z + offsetZ) * zoom;
            switch (pattern)
            {
                case Pattern.Simplex:
                    return Mathf.Clamp01(0.5f + 0.5f * Simplex(u, v));
                case Pattern.Value:
                    return ValueNoise(u, v);
                case Pattern.Fractal:
                    return Fractal(u, v, 4);
                case Pattern.Ridged:
                    return Ridged(u, v, 4);
                case Pattern.Billow:
                    return Billow(u, v, 4);
                case Pattern.DomainWarp:
                    return DomainWarp(u, v);
                case Pattern.Voronoi:
                {
                    Cellular(u * 0.5f, v * 0.5f, out float f1, out _, out _);
                    return Smooth(1f - Mathf.Clamp01(f1 * 1.2f));
                }
                case Pattern.Mosaic:
                {
                    Cellular(u * 0.5f, v * 0.5f, out _, out _, out float id);
                    return id;
                }
                case Pattern.Cracks:
                {
                    Cellular(u * 0.5f, v * 0.5f, out float f1, out float f2, out _);
                    return Smooth(Mathf.Clamp01((f2 - f1) * 3f));
                }
                case Pattern.Rings:
                    return 0.5f + 0.5f * Mathf.Sin(Tau * (Distance(x, z, Center, Center) + offsetZ) * zoom * 0.35f);
                case Pattern.Interference:
                    return Interference(x, z, offsetZ, zoom * 0.35f);
                case Pattern.Spiral:
                {
                    float angle = Mathf.Atan2(z - Center, x - Center);
                    return 0.5f + 0.5f * Mathf.Sin(3f * angle + Tau * (Distance(x, z, Center, Center) + offsetZ) * zoom * 0.35f);
                }
                case Pattern.Waffle:
                    return 0.5f + 0.5f * Mathf.Sin(Tau * u * 0.35f) * Mathf.Sin(Tau * v * 0.35f);
                default:
                    return Mathf.PerlinNoise(u, v);
            }
        }

        #region Helpers
        private static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private static float Distance(float x, float z, float cx, float cz)
        {
            return Mathf.Sqrt((x - cx) * (x - cx) + (z - cz) * (z - cz));
        }

        /// <summary>Hash of a lattice point to 0..1.</summary>
        private static float Hash(int x, int y, int seed = 0)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1274126177);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }
        #endregion


        #region Noise
        private static float ValueNoise(float x, float y)
        {
            int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
            float fx = Smooth(x - ix), fy = Smooth(y - iy);
            float a = Mathf.Lerp(Hash(ix, iy), Hash(ix + 1, iy), fx);
            float b = Mathf.Lerp(Hash(ix, iy + 1), Hash(ix + 1, iy + 1), fx);
            return Mathf.Lerp(a, b, fy);
        }

        /// <summary>2D simplex noise, roughly -1..1.</summary>
        private static float Simplex(float x, float y)
        {
            const float F2 = 0.3660254f, G2 = 0.2113249f;
            float s = (x + y) * F2;
            int i = Mathf.FloorToInt(x + s), j = Mathf.FloorToInt(y + s);
            float t = (i + j) * G2;
            float x0 = x - (i - t), y0 = y - (j - t);
            int i1 = x0 > y0 ? 1 : 0, j1 = 1 - i1;
            float x1 = x0 - i1 + G2, y1 = y0 - j1 + G2;
            float x2 = x0 - 1f + 2f * G2, y2 = y0 - 1f + 2f * G2;
            return 70f * (SimplexCorner(i, j, x0, y0) + SimplexCorner(i + i1, j + j1, x1, y1) + SimplexCorner(i + 1, j + 1, x2, y2));
        }

        private static float SimplexCorner(int i, int j, float x, float y)
        {
            float t = 0.5f - x * x - y * y;
            if (t < 0f)
                return 0f;
            t *= t;
            float angle = Hash(i, j, 3) * Tau;
            return t * t * (Mathf.Cos(angle) * x + Mathf.Sin(angle) * y);
        }

        /// <summary>Layers of perlin noise, each twice as fine and half as strong.</summary>
        private static float Fractal(float x, float y, int octaves)
        {
            float sum = 0f, amplitude = 1f, total = 0f;
            for (int o = 0; o < octaves; o++)
            {
                sum += amplitude * Mathf.PerlinNoise(x + o * 17.3f, y + o * 9.1f);
                total += amplitude;
                amplitude *= 0.5f;
                x *= 2f;
                y *= 2f;
            }
            return sum / total;
        }

        /// <summary>Sharp ridges: the folded noise, sharpened and layered.</summary>
        private static float Ridged(float x, float y, int octaves)
        {
            float sum = 0f, amplitude = 1f, total = 0f;
            for (int o = 0; o < octaves; o++)
            {
                float n = 1f - Mathf.Abs(2f * Mathf.PerlinNoise(x + o * 17.3f, y + o * 9.1f) - 1f);
                sum += amplitude * n * n;
                total += amplitude;
                amplitude *= 0.5f;
                x *= 2f;
                y *= 2f;
            }
            return sum / total;
        }

        /// <summary>Round, cloudy bumps: the folded noise turned upside down compared to ridged.</summary>
        private static float Billow(float x, float y, int octaves)
        {
            float sum = 0f, amplitude = 1f, total = 0f;
            for (int o = 0; o < octaves; o++)
            {
                sum += amplitude * Mathf.Abs(2f * Mathf.PerlinNoise(x + o * 17.3f, y + o * 9.1f) - 1f);
                total += amplitude;
                amplitude *= 0.5f;
                x *= 2f;
                y *= 2f;
            }
            return sum / total;
        }

        /// <summary>Fractal noise whose coordinates are shifted by two other fractal noises: swirls like marble.</summary>
        private static float DomainWarp(float x, float y)
        {
            float qx = Fractal(x + 5.2f, y + 1.3f, 3);
            float qy = Fractal(x + 8.3f, y + 2.8f, 3);
            return Fractal(x + 3f * (qx - 0.5f), y + 3f * (qy - 0.5f), 3);
        }
        #endregion


        #region Cells
        /// <summary>
        /// Voronoi: distance to the nearest (f1) and second nearest (f2) random point, and a random value
        /// of the nearest cell. The cell size is 1.
        /// </summary>
        private static void Cellular(float x, float y, out float f1, out float f2, out float id)
        {
            int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
            f1 = 8f;
            f2 = 8f;
            id = 0f;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int cx = ix + dx, cy = iy + dy;
                    float px = cx + Hash(cx, cy, 1) - x;
                    float py = cy + Hash(cx, cy, 2) - y;
                    float d = Mathf.Sqrt(px * px + py * py);
                    if (d < f1)
                    {
                        f2 = f1;
                        f1 = d;
                        id = Hash(cx, cy, 5);
                    }
                    else if (d < f2)
                        f2 = d;
                }
            }
        }
        #endregion


        #region Waves
        /// <summary>Three wave sources placed around the center, the waves add up and cancel out.</summary>
        private static float Interference(float x, float z, float offsetZ, float frequency)
        {
            float sum = 0f;
            for (int s = 0; s < 3; s++)
            {
                float angle = s * Tau / 3f + 0.5f;
                float d = Distance(x, z, Center + 34f * Mathf.Cos(angle), Center + 34f * Mathf.Sin(angle));
                sum += Mathf.Sin(Tau * (d + offsetZ) * frequency);
            }
            return Mathf.Clamp01(0.5f + sum / 5f);
        }
        #endregion
    }
}
