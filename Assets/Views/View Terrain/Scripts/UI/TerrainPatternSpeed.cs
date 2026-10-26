using System;
using UnityEngine;

namespace Gavi
{
    /// <summary>A pattern group with two speed sliders (x and y). Lets the speed copies in the Performance window follow the pair as one speed.</summary>
    public interface IPatternSpeed
    {
        /// <summary>True while the pattern is switched on.</summary>
        bool SpeedOn { get; }
        /// <summary>Length of the speed (x and y together), as the speed copy shows it.</summary>
        float SpeedLength { get; }
        /// <summary>Scales the speed to this length and keeps its direction (a standing pattern moves along y).</summary>
        void SetSpeedLength(float length);
        event Action SpeedChanged;
    }

    /// <summary>
    /// The two speed sliders of a pattern, from -10 to 10 with 0 in the middle. A positive value moves the pattern toward higher x and y
    /// (the offset that is stored for the pattern gets smaller), so the speed of the old single slider is the y slider.
    /// </summary>
    public static class TerrainPatternSpeed
    {
        /// <summary>The offset speed that is stored in a preset.</summary>
        public static Vector2 Stored(SliderG x, SliderG y)
        {
            return new Vector2(x != null ? -x.Value : 0f, y != null ? -y.Value : 0f);
        }

        public static void Show(SliderG x, SliderG y, float storedX, float storedY)
        {
            if (x != null)
                x.Value = -storedX;
            if (y != null)
                y.Value = -storedY;
        }

        public static float Length(SliderG x, SliderG y)
        {
            return Stored(x, y).magnitude;
        }

        /// <summary>The stored speed with another length and the same direction (a standing pattern moves along y).</summary>
        public static Vector2 WithLength(Vector2 stored, float length)
        {
            return stored.magnitude < 0.0001f ? new Vector2(0f, -length) : stored * (length / stored.magnitude);
        }

        public static void SetLength(SliderG x, SliderG y, float length)
        {
            Vector2 speed = WithLength(Stored(x, y), length);
            Show(x, y, speed.x, speed.y);
        }
    }
}
