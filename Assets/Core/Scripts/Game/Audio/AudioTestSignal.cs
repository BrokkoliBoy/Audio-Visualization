using UnityEngine;

namespace Gavi.Audio
{
    /// <summary>
    /// A calm, predictable stand-in for the audio. While it is on, the spectrum shows fixed band levels (and a kick on the
    /// lowest bands at a steady tempo if wanted), so the effects of the terrain can be judged without a moving song.
    /// </summary>
    public class AudioTestSignal : MonoBehaviour
    {
        public enum Signal { Flat, BassOnly, Falling, Rising, EveryOtherBand }

        public static readonly string[] SignalNames = { "Flat", "Bass only", "Falling", "Rising", "Every other band" };

        public static AudioTestSignal Instance { get; private set; }
        /// <summary>True while the test signal replaces the audio.</summary>
        public static bool IsActive => Instance != null && Instance._enabled;

        [SerializeField] private bool _enabled;
        [SerializeField] private Signal _signal = Signal.Flat;
        [Tooltip("Level of the bands (0-1).")]
        [Range(0f, 1f)]
        [SerializeField] private float _level = 0.6f;
        [Tooltip("A kick on the lowest bands and a beat at the tempo below.")]
        [SerializeField] private bool _beat;
        [SerializeField] private float _bpm = 120f;
        [Tooltip("The noise pattern stands still while the test signal is on.")]
        [SerializeField] private bool _freezeNoise = true;

        /// <summary>Number of lowest bands the kick is played on.</summary>
        private const int KickBands = 3;
        /// <summary>Share of the level the kick falls back to between two beats.</summary>
        private const float KickFloor = 0.25f;
        /// <summary>Speed of the kick falling back (per second).</summary>
        private const float KickDecay = 7f;

        public bool Enabled { get => _enabled; set => _enabled = value; }
        public int SignalIndex { get => (int)_signal; set => _signal = (Signal)Mathf.Clamp(value, 0, SignalNames.Length - 1); }
        public float Level { get => _level; set => _level = Mathf.Clamp01(value); }
        public bool Beat { get => _beat; set => _beat = value; }
        public float Bpm { get => _bpm; set => _bpm = Mathf.Clamp(value, 30f, 300f); }
        public bool FreezeNoise { get => _freezeNoise; set => _freezeNoise = value; }

        /// <summary>Seconds of one beat of the test tempo.</summary>
        public float BeatInterval => 60f / Mathf.Max(1f, _bpm);

        /// <summary>1 right at a beat, falling back to 0. Always 0 without the beat option.</summary>
        public float Pulse => _beat ? Kick() : 0f;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>Replaces the band levels with the test signal. Does nothing while the test signal is off.</summary>
        public static void Override(float[] values)
        {
            if (IsActive && values != null)
                Instance.Fill(values);
        }

        private void Fill(float[] values)
        {
            int count = values.Length;
            float kick = _beat ? Mathf.Lerp(KickFloor, 1f, Kick()) : 1f;
            for (int i = 0; i < count; i++)
            {
                float share;
                switch (_signal)
                {
                    case Signal.BassOnly:
                        share = i < KickBands ? 1f : 0f;
                        break;
                    case Signal.Falling:
                        share = 1f - (float)i / count;
                        break;
                    case Signal.Rising:
                        share = (float)(i + 1) / count;
                        break;
                    case Signal.EveryOtherBand:
                        share = i % 2 == 0 ? 1f : 0f;
                        break;
                    default:
                        share = 1f;
                        break;
                }
                values[i] = _level * share * (i < KickBands ? kick : 1f);
            }
        }

        /// <summary>Falls from 1 at the beat towards 0 until the next beat.</summary>
        private float Kick()
        {
            float interval = BeatInterval;
            float sinceBeat = Time.time - Mathf.Floor(Time.time / interval) * interval;
            return Mathf.Exp(-KickDecay * sinceBeat);
        }
    }
}
