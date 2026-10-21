using UnityEngine;

namespace Gavi.Audio
{
    /// <summary>
    /// A calm, predictable stand-in for the audio. While it is on, the spectrum shows fixed band levels, so the effects of
    /// the terrain can be judged without a moving song. Beats and the beat strength follow the tempo and the beat of the
    /// song that is playing, but they can be used separately: beats alone drive waves and flips on a still terrain,
    /// the beat strength also lifts the bands on every beat. With the band editor on, single band levels can be
    /// raised and lowered with the mouse (see <see cref="Gavi.TerrainBandPicker"/>).
    /// </summary>
    public class AudioTestSignal : MonoBehaviour
    {
        public enum Signal { Flat, BassOnly, Falling, Rising, EveryOtherBand }

        public static readonly string[] SignalNames = { "Flat", "Bass only", "Falling", "Rising", "Every other band" };

        public static AudioTestSignal Instance { get; private set; }
        /// <summary>True while the test signal replaces the audio.</summary>
        public static bool IsActive => Instance != null && Instance._enabled;
        /// <summary>True while the mouse edits the band levels (then the left and right mouse button belong to the editor, not to the camera).</summary>
        public static bool IsEditingBands => IsActive && Instance._editBands;

        [SerializeField] private bool _enabled;
        [SerializeField] private Signal _signal = Signal.Flat;
        [Tooltip("Level of the bands (0-1).")]
        [Range(0f, 1f)]
        [SerializeField] private float _level = 0.6f;
        [Tooltip("Sends a beat on every beat of the song (no tempo needed, it is the BPM of the playing song). Waves and flips react to it.")]
        [SerializeField] private bool _beat;
        [Tooltip("How far the bands jump up on every beat (added on top of the level). The bass jumps the most, the outer bands much less. 0 = the bands stay calm.")]
        [Range(0f, 1f)]
        [SerializeField] private float _beatStrength;
        [Tooltip("Takes the strength of every beat from the bass of the song that is playing instead of the slider. Without a playing song the slider counts.")]
        [SerializeField] private bool _beatStrengthFromSong;
        [Tooltip("The noise pattern stands still while the test signal is on.")]
        [SerializeField] private bool _freezeNoise = true;
        [Tooltip("The left mouse button raises the band under the cursor, the right one lowers it. Starts from the signal as it is.")]
        [SerializeField] private bool _editBands;

        /// <summary>Number of lowest bands that count as bass (Bass only signal).</summary>
        private const int BassBands = 3;
        /// <summary>Tempo and beat time that are used when no song with a BPM is playing.</summary>
        private const float FallbackBpm = 120f;
        /// <summary>The last band follows the beat strength by this share of the bass (bands in between fall off logarithmically).</summary>
        private const float OuterKickShare = 0.1f;
        /// <summary>Speed of the kick falling back (per second).</summary>
        private const float KickDecay = 7f;
        /// <summary>How fast the mouse raises or lowers a band (level per second).</summary>
        private const float EditRate = 1.2f;

        /// <summary>The bass of the song is measured this long after a beat (seconds).</summary>
        private const float SongPeakWindow = 0.12f;

        // levels of the band editor, null while it is off
        private float[] _edited;
        // beat strength taken from the song, see SongBeatStrength
        private float _songPeak, _lastSinceBeat;

        public bool Enabled { get => _enabled; set => _enabled = value; }
        public int SignalIndex { get => (int)_signal; set => _signal = (Signal)Mathf.Clamp(value, 0, SignalNames.Length - 1); }
        public float Level { get => _level; set => _level = Mathf.Clamp01(value); }
        public bool Beat { get => _beat; set => _beat = value; }
        public float BeatStrength { get => _beatStrength; set => _beatStrength = Mathf.Clamp01(value); }
        public bool BeatStrengthFromSong { get => _beatStrengthFromSong; set => _beatStrengthFromSong = value; }
        public bool FreezeNoise { get => _freezeNoise; set => _freezeNoise = value; }
        public bool EditBands
        {
            get => _editBands;
            set
            {
                _editBands = value;
                // a new session of editing starts from the signal as it is
                _edited = null;
            }
        }

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

        /// <summary>Raises (positive) or lowers (negative) a band of the editor by this much of a second of editing.</summary>
        public void EditBand(int band, float direction, float deltaTime)
        {
            if (_edited == null || band < 0 || band >= _edited.Length)
                return;
            _edited[band] = Mathf.Clamp01(_edited[band] + direction * EditRate * deltaTime);
        }

        private void Fill(float[] values)
        {
            int count = values.Length;
            if (_editBands && (_edited == null || _edited.Length != count))
            {
                // copy the signal as it is, without the kick, as the start of editing
                _edited = new float[count];
                for (int i = 0; i < count; i++)
                    _edited[i] = _level * Share(i, count);
            }

            // the values still hold the real spectrum here, the beat strength of the song is read from it
            float strength = _beatStrength;
            if (_beatStrengthFromSong && SongIsPlaying())
                strength = SongBeatStrength(values);

            float kick = strength * Envelope();
            for (int i = 0; i < count; i++)
            {
                float own = _editBands ? _edited[i] : _level * Share(i, count);
                // the kick lifts the bands that carry the signal, it never takes anything away
                float lift = own > 0f ? kick * KickShare(i, count) : 0f;
                values[i] = Mathf.Clamp01(own + lift);
            }
        }

        private float Share(int band, int count)
        {
            switch (_signal)
            {
                case Signal.BassOnly:
                    return band < BassBands ? 1f : 0f;
                case Signal.Falling:
                    return 1f - (float)band / count;
                case Signal.Rising:
                    return (float)(band + 1) / count;
                case Signal.EveryOtherBand:
                    return band % 2 == 0 ? 1f : 0f;
                default:
                    return 1f;
            }
        }

        /// <summary>How much of the kick a band takes: 1 for the bass, falling off logarithmically to <see cref="OuterKickShare"/>.</summary>
        private static float KickShare(int band, int count)
        {
            if (count < 2)
                return 1f;
            return 1f - (1f - OuterKickShare) * Mathf.Log(1f + band) / Mathf.Log(count);
        }

        private static bool SongIsPlaying()
        {
            AudioPlayer player = AudioPlayer.Instance;
            return player != null && player.IsPlaying && player.CurrentlyPlayingClip != null;
        }

        /// <summary>
        /// The strength of the current beat: the highest bass level of the real spectrum right after the beat. It is
        /// held until the next beat, so the lift follows the loudness of the song beat by beat.
        /// </summary>
        private float SongBeatStrength(float[] realSpectrum)
        {
            float sinceBeat = SinceBeat();
            if (sinceBeat < _lastSinceBeat)
                _songPeak = 0f;
            _lastSinceBeat = sinceBeat;

            if (sinceBeat < SongPeakWindow)
            {
                int bass = Mathf.Min(BassBands, realSpectrum.Length);
                for (int i = 0; i < bass; i++)
                    _songPeak = Mathf.Max(_songPeak, realSpectrum[i]);
            }
            return Mathf.Clamp01(_songPeak);
        }

        /// <summary>Seconds since the last beat of the song.</summary>
        private float SinceBeat()
        {
            float interval = BeatInterval;
            float time = Time.time;
            AudioPlayer player = AudioPlayer.Instance;
            if (player != null && player.IsPlaying && player.CurrentlyPlayingClip != null && player.CurrentlyPlayingClip.BPM > 0f)
                time = player.CurrentPlaybackTime - player.CurrentlyPlayingClip.BeatOffset;

            return time - Mathf.Floor(time / interval) * interval;
        }

        /// <summary>Falls from 1 at a beat of the song towards 0 until the next beat.</summary>
        private float Envelope()
        {
            return Mathf.Exp(-KickDecay * SinceBeat());
        }
    }
}
