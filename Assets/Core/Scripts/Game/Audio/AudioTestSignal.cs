using UnityEngine;

namespace Gavi.Audio
{
    /// <summary>
    /// A calm, predictable stand-in for the audio. While it is on, the spectrum shows fixed band levels, so the effects of
    /// the terrain can be judged without a moving song. Beats and the kick follow the tempo and the beat of the song that
    /// is playing, but they can be switched on separately: beats alone drive waves and flips on a still terrain,
    /// the kick also moves the bands.
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
        [Tooltip("Sends a beat on every beat of the song (no tempo needed, it is the BPM of the playing song). Waves and flips react to it.")]
        [SerializeField] private bool _beat;
        [Tooltip("All bands pulse on the beat of the song, the bass the most, the outer bands much less.")]
        [SerializeField] private bool _kick;
        [Tooltip("The noise pattern stands still while the test signal is on.")]
        [SerializeField] private bool _freezeNoise = true;

        /// <summary>Number of lowest bands that count as bass (Bass only signal).</summary>
        private const int BassBands = 3;
        /// <summary>Tempo and beat time that are used when no song with a BPM is playing.</summary>
        private const float FallbackBpm = 120f;
        /// <summary>The kick takes the bass down to this share of its level between two beats.</summary>
        private const float KickDepth = 0.75f;
        /// <summary>The last band follows the kick by this share of the bass (bands in between fall off logarithmically).</summary>
        private const float OuterKickShare = 0.1f;
        /// <summary>Speed of the kick falling back (per second).</summary>
        private const float KickDecay = 7f;

        public bool Enabled { get => _enabled; set => _enabled = value; }
        public int SignalIndex { get => (int)_signal; set => _signal = (Signal)Mathf.Clamp(value, 0, SignalNames.Length - 1); }
        public float Level { get => _level; set => _level = Mathf.Clamp01(value); }
        public bool Beat { get => _beat; set => _beat = value; }
        public bool Kick { get => _kick; set => _kick = value; }
        public bool FreezeNoise { get => _freezeNoise; set => _freezeNoise = value; }

        /// <summary>Seconds of one beat: from the BPM of the playing song, otherwise 120 BPM.</summary>
        public float BeatInterval
        {
            get
            {
                AudioClipContainer clip = AudioPlayer.Instance != null ? AudioPlayer.Instance.CurrentlyPlayingClip : null;
                return 60f / (clip != null && clip.BPM > 0f ? clip.BPM : FallbackBpm);
            }
        }

        /// <summary>1 right at a beat, falling back to 0. Always 0 without the beat option.</summary>
        public float Pulse => _beat ? Envelope() : 0f;

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
            float fall = _kick ? KickDepth * (1f - Envelope()) : 0f;
            for (int i = 0; i < count; i++)
            {
                float share;
                switch (_signal)
                {
                    case Signal.BassOnly:
                        share = i < BassBands ? 1f : 0f;
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
                values[i] = _level * share * (1f - fall * KickShare(i, count));
            }
        }

        /// <summary>How much of the kick a band takes: 1 for the bass, falling off logarithmically to <see cref="OuterKickShare"/>.</summary>
        private static float KickShare(int band, int count)
        {
            if (count < 2)
                return 1f;
            return 1f - (1f - OuterKickShare) * Mathf.Log(1f + band) / Mathf.Log(count);
        }

        /// <summary>Falls from 1 at a beat of the song towards 0 until the next beat.</summary>
        private float Envelope()
        {
            float interval = BeatInterval;
            float time = Time.time;
            AudioPlayer player = AudioPlayer.Instance;
            if (player != null && player.IsPlaying && player.CurrentlyPlayingClip != null && player.CurrentlyPlayingClip.BPM > 0f)
                time = player.CurrentPlaybackTime - player.CurrentlyPlayingClip.BeatOffset;

            float sinceBeat = time - Mathf.Floor(time / interval) * interval;
            return Mathf.Exp(-KickDecay * sinceBeat);
        }
    }
}
