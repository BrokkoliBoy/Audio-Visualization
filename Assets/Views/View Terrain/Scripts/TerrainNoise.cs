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
            Voronoi, Mosaic, Cracks, Rings, Interference, Spiral, Waffle, Hexagons,
            Bricks, Dots, Pyramids, Flower, Zigzag, Plasma, Tiles,
            Triangles, Checker, Steps, Cubes, Honeycomb
        }

        public static readonly string[] Names =
        {
            "Perlin", "Simplex", "Value", "Fractal", "Ridged", "Billow", "Domain warp",
            "Voronoi", "Mosaic", "Cracks", "Rings", "Interference", "Spiral", "Waffle", "Hexagons",
            "Bricks", "Dots", "Pyramids", "Flower", "Zigzag", "Plasma", "Tiles",
            "Triangles", "Checker", "Steps", "Cubes", "Honeycomb"
        };

        /// <summary>
        /// Patterns whose shapes (cells, tiles, bricks ...) are meant to stand up from the ground. The terrain is
        /// lowered where the pattern is high, so these are turned upside down before the invert option is applied.
        /// </summary>
        public static bool InvertedByDefault(Pattern pattern)
        {
            switch (pattern)
            {
                case Pattern.Voronoi:
                case Pattern.Hexagons:
                case Pattern.Bricks:
                case Pattern.Dots:
                case Pattern.Pyramids:
                case Pattern.Tiles:
                case Pattern.Cracks:
                case Pattern.Triangles:
                case Pattern.Cubes:
                    return true;
                default:
                    return false;
            }
        }

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
                case Pattern.Hexagons:
                    return Hexagons(u * 0.8f, v * 0.8f);
                case Pattern.Bricks:
                    return Bricks(u * 0.4f, v * 0.8f);
                case Pattern.Dots:
                    return Dots(u * 0.6f, v * 0.6f);
                case Pattern.Pyramids:
                    return Pyramids(u * 0.5f, v * 0.5f);
                case Pattern.Flower:
                {
                    float angle = Mathf.Atan2(z - Center, x - Center);
                    return 0.5f + 0.5f * Mathf.Sin(6f * angle) * Mathf.Cos(Tau * (Distance(x, z, Center, Center) + offsetZ) * zoom * 0.12f);
                }
                case Pattern.Zigzag:
                {
                    float fold = Mathf.Abs(Mathf.Repeat(u * 0.2f, 1f) - 0.5f) * 2f;
                    return 0.5f + 0.5f * Mathf.Sin(Tau * (v * 0.35f + fold * 0.6f));
                }
                case Pattern.Plasma:
                    return 0.5f + 0.125f * (Mathf.Sin(u * 1f) + Mathf.Sin(v * 1.2f) + Mathf.Sin((u + v) * 0.8f) + Mathf.Sin(Mathf.Sqrt(u * u + v * v) * 1f));
                case Pattern.Tiles:
                    return Tiles(u * 0.5f, v * 0.5f);
                case Pattern.Triangles:
                    return Triangles(u * 0.3f, v * 0.3f);
                case Pattern.Checker:
                    return Mathf.Clamp01(0.5f + 2f * Mathf.Sin(Mathf.PI * u * 0.35f) * Mathf.Sin(Mathf.PI * v * 0.35f));
                case Pattern.Steps:
                {
                    // concentric square steps, up and down again, they run outward when the offset gets smaller
                    float ring = Mathf.Max(Mathf.Abs(x - Center), Mathf.Abs(z - Center)) + offsetZ;
                    float wave = Mathf.PingPong(ring * zoom * 0.18f, 1f);
                    return Mathf.Floor(wave * 5f) / 4f;
                }
                case Pattern.Cubes:
                    return Cubes(u * 0.5f, v * 0.5f);
                case Pattern.Honeycomb:
                    return Honeycomb(u * 0.6f, v * 0.6f);
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


        #region Hexagons
        /// <summary>
        /// Hexagon tiles (pointy top, radius 1) with a random height each. The rim of a tile falls off softly, because
        /// the vertices of the terrain do not line up with the edges of the hexagons.
        /// </summary>
        private static float Hexagons(float x, float y)
        {
            HexCell(x, y, out int rq, out int rr, out _, out _, out float edge);
            return (0.25f + 0.75f * Hash(rq, rr, 7)) * Smooth((1f - edge) / 0.2f);
        }

        /// <summary>The hexagon (pointy top, radius 1) a point is in: its axial coordinates, the position relative to its middle and how close the point is to the rim (0 in the middle, 1 at the rim).</summary>
        private static void HexCell(float x, float y, out int rq, out int rr, out float ox, out float oy, out float edge)
        {
            // cube coordinates of the hexagon the point is in
            float q = 0.57735027f * x - 0.33333334f * y;
            float r = 0.6666667f * y;
            float s = -q - r;
            rq = Mathf.RoundToInt(q);
            rr = Mathf.RoundToInt(r);
            int rs = Mathf.RoundToInt(s);
            float dq = Mathf.Abs(rq - q), dr = Mathf.Abs(rr - r), ds = Mathf.Abs(rs - s);
            if (dq > dr && dq > ds)
                rq = -rr - rs;
            else if (dr > ds)
                rr = -rq - rs;

            ox = x - 1.7320508f * (rq + rr * 0.5f);
            oy = y - 1.5f * rr;
            edge = Mathf.Max(Mathf.Abs(ox), Mathf.Max(Mathf.Abs(0.5f * ox + 0.8660254f * oy), Mathf.Abs(0.5f * ox - 0.8660254f * oy))) / 0.8660254f;
        }

        /// <summary>Isometric cubes: every hexagon is split into three rhombi (top, left, right) of different heights, each cube with its own height.</summary>
        private static float Cubes(float x, float y)
        {
            HexCell(x, y, out int rq, out int rr, out float ox, out float oy, out _);
            float angle = Mathf.Atan2(oy, ox) * Mathf.Rad2Deg;
            int face = Mathf.FloorToInt(Mathf.Repeat(angle + 30f, 360f) / 120f);
            float faceHeight = face == 0 ? 1f : face == 1 ? 0.62f : 0.3f;
            return (0.55f + 0.45f * Hash(rq, rr, 29)) * faceHeight;
        }

        /// <summary>Hexagonal pits: the cells are low, the rims between them stay high.</summary>
        private static float Honeycomb(float x, float y)
        {
            HexCell(x, y, out _, out _, out _, out _, out float edge);
            return Smooth((1f - edge) / 0.45f);
        }
        #endregion


        #region Grids
        /// <summary>Rows of bricks, every second row shifted by half a brick. A brick is twice as wide as high, each has its own height.</summary>
        private static float Bricks(float x, float y)
        {
            int row = Mathf.FloorToInt(y);
            float shifted = x + (row & 1) * 0.5f;
            int column = Mathf.FloorToInt(shifted);
            float fx = shifted - column, fy = y - row;
            float edge = Mathf.Min(Mathf.Min(fx, 1f - fx) * 2f, Mathf.Min(fy, 1f - fy));
            return (0.35f + 0.65f * Hash(column, row, 11)) * Smooth(edge / 0.3f);
        }

        /// <summary>Round bumps on a grid, every second row shifted, each with its own height.</summary>
        private static float Dots(float x, float y)
        {
            int row = Mathf.FloorToInt(y);
            float shifted = x + (row & 1) * 0.5f;
            int column = Mathf.FloorToInt(shifted);
            float dx = shifted - column - 0.5f, dy = y - row - 0.5f;
            float distance = Mathf.Sqrt(dx * dx + dy * dy);
            return (0.4f + 0.6f * Hash(column, row, 13)) * Smooth((0.5f - distance) / 0.25f);
        }

        /// <summary>Square pyramids on a grid turned by 45 degrees, each with its own height.</summary>
        private static float Pyramids(float x, float y)
        {
            float a = (x + y) * 0.70710678f, b = (x - y) * 0.70710678f;
            int ia = Mathf.FloorToInt(a), ib = Mathf.FloorToInt(b);
            float tent = 1f - 2f * Mathf.Max(Mathf.Abs(a - ia - 0.5f), Mathf.Abs(b - ib - 0.5f));
            return (0.3f + 0.7f * Hash(ia, ib, 17)) * tent;
        }

        /// <summary>Triangular pyramids on a triangle grid (edge length 1), each with its own height.</summary>
        private static float Triangles(float x, float y)
        {
            float j = y * 1.1547005f;
            float i = x - 0.5f * j;
            int ci = Mathf.FloorToInt(i), cj = Mathf.FloorToInt(j);
            float fi = i - ci, fj = j - cj;
            bool up = fi + fj < 1f;
            // distance to the nearest of the three edges, at most 1/3 in the middle of the triangle
            float nearest = up ? Mathf.Min(fi, Mathf.Min(fj, 1f - fi - fj)) : Mathf.Min(1f - fi, Mathf.Min(1f - fj, fi + fj - 1f));
            return (0.5f + 0.5f * Hash(ci, cj * 2 + (up ? 0 : 1), 23)) * Mathf.Clamp01(nearest * 3f);
        }

        /// <summary>Square tiles with a random height each. The rim falls off softly, like with the hexagons.</summary>
        private static float Tiles(float x, float y)
        {
            int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
            float fx = x - ix, fy = y - iy;
            float edge = 2f * Mathf.Min(Mathf.Min(fx, 1f - fx), Mathf.Min(fy, 1f - fy));
            return (0.3f + 0.7f * Hash(ix, iy, 19)) * Smooth(edge / 0.3f);
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
