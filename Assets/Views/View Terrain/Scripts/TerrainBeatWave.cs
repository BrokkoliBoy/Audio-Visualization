using UnityEngine;
using Gavi.Audio;
using Gavi.Modules.DataModules.ArrayModules.FloatArrayModules.VoidToFloatArrayModules;
using Gavi.Modules.MathModules.FloatModules;

namespace Gavi
{
    /// <summary>
    /// Ring waves that run over the terrain. Every beat sends a wave off at once, but it stays flat (invisible) until the
    /// bass is strong enough. If that happens within a short time, the wave which is already on its way grows to the height
    /// of the bass level (the highest level shortly before and after the beat). Beats that fall into a quiet part of the
    /// song never grow. The terrain asks it for the height of every vertex.
    /// This class also detects the beats for the terrain (see <see cref="BeatCount"/>), even while the waves are off.
    /// </summary>
    public class TerrainBeatWave : MonoBehaviour
    {
        /// <summary>What a wave does to the height: add its height, lift everything below it, or pull everything towards it.</summary>
        public enum Mode { Add, Floor, Absorb }
        /// <summary>Where a wave starts. Alternate switches between middle and edge with every wave. BassEdge starts where the bass bands end and runs outward.</summary>
        public enum Origin { Middle, Edge, Alternate, BassEdge }

        public static readonly string[] ModeNames = { "Add", "Floor", "Absorb" };
        public static readonly string[] OriginNames = { "Middle", "Edge", "Alternate", "Bass edge" };

        public const bool DefaultEnabled = false;
        public const float DefaultStrength = 0.4f;
        public const float DefaultSpeed = 1f;
        public const float DefaultWidth = 0.12f;
        public const float DefaultFade = 0f;
        public const int DefaultMode = (int)Mode.Floor;
        public const int DefaultOrigin = (int)Origin.Middle;
        public const bool DefaultUseNoise = false;

        [Tooltip("Beat pulse between 0 and 1. A new wave starts when it jumps up.")]
        [SerializeField] private FloatModule _beatPulseModule;
        [Tooltip("Spectrum the bass level is taken from (the lowest bands).")]
        [SerializeField] private SpectrumDataContainerModule _spectrum;

        [Header("--- Settings (part of the terrain profile) ---")]
        [Tooltip("Waves only run when this is on.")]
        [SerializeField] private bool _enabled = DefaultEnabled;
        [Tooltip("Height of the wave in terrain heights. 0 = off.")]
        [SerializeField] private float _strength = DefaultStrength;
        [Tooltip("Speed of the wave. 1 = a wave crosses half the terrain width in exactly one beat of the song, 2 = in half a beat.")]
        [SerializeField] private float _speed = DefaultSpeed;
        [Tooltip("Width of the ring in half terrain widths.")]
        [SerializeField] private float _width = DefaultWidth;
        [Tooltip("How much height the wave loses on its way. 0 = full height until it leaves the terrain, 1 = fades out completely.")]
        [Range(0f, 1f)]
        [SerializeField] private float _fade = DefaultFade;
        [SerializeField] private Mode _mode = (Mode)DefaultMode;
        [SerializeField] private Origin _origin = (Origin)DefaultOrigin;
        [Tooltip("The noise pattern shapes the wave: it is higher where the noise leaves the terrain high.")]
        [SerializeField] private bool _useNoise = DefaultUseNoise;

        public bool UseNoise { get => _useNoise; set => _useNoise = value; }
        public bool Enabled { get => _enabled; set => _enabled = value; }
        public float Strength { get => _strength; set => _strength = value; }
        public float Speed { get => _speed; set => _speed = value; }
        public float Width { get => _width; set => _width = Mathf.Max(0.01f, value); }
        public float Fade { get => _fade; set => _fade = Mathf.Clamp01(value); }
        public int WaveMode { get => (int)_mode; set => _mode = (Mode)Mathf.Clamp(value, 0, ModeNames.Length - 1); }
        public int WaveOrigin { get => (int)_origin; set => _origin = (Origin)Mathf.Clamp(value, 0, OriginNames.Length - 1); }

        /// <summary>Number of beats detected so far. Counts also while the waves are off.</summary>
        public int BeatCount { get; private set; }
        /// <summary>1 right at a beat, falling back to 0 within a part of the beat interval. Only moves while beats come in.</summary>
        public float BeatEnvelope => Mathf.Exp(-(_clock - _lastBeatTime) / Mathf.Max(0.05f, EnvelopeBeatShare * BeatInterval));
        /// <summary>True when the waves start from the bass edge, the terrain then has to tell where it is (<see cref="SetBassEdge"/>).</summary>
        public bool NeedsBassEdge => _enabled && _origin == Origin.BassEdge;
        /// <summary>Number of lowest bands that count as bass.</summary>
        public int BassBandCount => BassWeights.Length;

        /// <summary>Seconds of one beat: from the BPM of the playing song, otherwise from the beats that were detected.</summary>
        public float BeatInterval
        {
            get
            {
                if (AudioTestSignal.IsActive)
                    return AudioTestSignal.Instance.BeatInterval;
                AudioClipContainer clip = AudioPlayer.Instance != null ? AudioPlayer.Instance.CurrentlyPlayingClip : null;
                if (clip != null && clip.BPM > 0f)
                    return 60f / clip.BPM;
                return _detectedInterval;
            }
        }

        /// <summary>The beat envelope falls to 1/e after this share of a beat.</summary>
        private const float EnvelopeBeatShare = 0.35f;
        public const int MaxWaves = 6;
        /// <summary>Distance from the middle to the corners of the terrain (in half terrain widths), a bit more.</summary>
        private const float EdgeDistance = 1.45f;
        /// <summary>The Bass edge origin starts this many wave widths inside of the bass edge.</summary>
        private const float BassEdgeLead = 0.75f;
        /// <summary>Speed 1 means: this distance (half the terrain width) in one beat.</summary>
        private const float SpeedDistance = 1f;
        /// <summary>A beat needs a pulse of at least this much that rises at least this fast (per second). Soft beats count too, slow drifts of the pulse do not.</summary>
        private const float MinPulse = 0.06f;
        private const float MinRiseRate = 1.8f;
        private const float MinInterval = 0.2f;
        /// <summary>The highest bass level this long before the beat counts for the height of the wave ...</summary>
        private const float BassWindowBefore = 0.1f;
        /// <summary>... and so does the highest level this long after the beat. A wave whose bass is too weak at first can still
        /// become visible within this time, and it keeps growing while the bass gets higher.</summary>
        private const float BassWindowAfter = 0.1f;
        /// <summary>A wave that becomes visible after its beat (or grows later) goes from flat to full height while it runs
        /// this far (in half terrain widths), but never slower than <see cref="MaxGrowSeconds"/> and never faster than <see cref="MinGrowSeconds"/>.</summary>
        private const float GrowDistance = 0.3f;
        private const float MaxGrowSeconds = 0.15f;
        private const float MinGrowSeconds = 0.04f;
        /// <summary>Waves never get smaller than this.</summary>
        private const float MinAmplitude = 0.03f;
        /// <summary>A bass level of this share of the recent loudest bass gives the full wave height.</summary>
        private const float FullBassShare = 0.9f;
        /// <summary>A beat starts a wave only if the bass reaches this share of the recent loudest bass ...</summary>
        private const float MinBassShare = 0.35f;
        /// <summary>... and if the bass is the loudest part of the sound or the loudest band reaches this share of the recent loudest level.
        /// So nothing runs in quiet parts, but the bass always has priority.</summary>
        private const float MinOverallShare = 0.5f;
        /// <summary>Weights of the lowest bands. The bass is the highest weighted band, so a kick that sits in the second band still counts.</summary>
        private static readonly float[] BassWeights = { 1f, 0.8f, 0.5f };
        private const int HistorySize = 64;
        /// <summary>The loudest levels are forgotten slowly: the bass peak halves in this many seconds ...</summary>
        private const float BassPeakHalfLife = 4f;
        /// <summary>... and the overall peak in this many seconds.</summary>
        private const float OverallPeakHalfLife = 8f;
        private const float MinPeak = 0.1f;
        /// <summary>Absorb pulls the terrain towards the wave only by this share, so it stays gentle.</summary>
        public const float AbsorbShare = 0.6f;
        private const int IntervalCount = 8;

        private readonly float[] _startTime = new float[MaxWaves];
        private readonly float[] _amplitude = new float[MaxWaves];
        private readonly float[] _targetAmplitude = new float[MaxWaves];
        private readonly float[] _waveSpeed = new float[MaxWaves];
        private readonly float[] _startFront = new float[MaxWaves];
        private readonly float[] _direction = new float[MaxWaves];
        private readonly float[] _totalWay = new float[MaxWaves];

        // state of the waves that are running in this frame
        private readonly float[] _front = new float[MaxWaves];
        private readonly float[] _level = new float[MaxWaves];
        private int _activeCount;
        private float _activeWidth;

        // recent levels, to find the highest one around a beat
        private readonly float[] _historyTimes = new float[HistorySize];
        private readonly float[] _historyBass = new float[HistorySize];
        private readonly float[] _historyOther = new float[HistorySize];
        private int _historyCount;
        private int _historyNext;
        private float _bassPeak;
        private float _overallPeak;

        // the wave of the last beat: flat until the bass is strong enough, it can still grow while its window is open
        private bool _tracking;
        private bool _confirmed;
        private int _trackSlot;
        private float _trackStart;
        private float _trackEnd;
        private float _trackBass;
        private float _trackOther;

        private float _lastTickTime;
        private float _lastPulse;
        private float _lastBeatTime = -10f;
        private bool _nextFromEdge;
        private float _bassEdge;

        // beat intervals, for songs without a BPM
        private readonly float[] _intervals = new float[IntervalCount];
        private int _intervalCount;
        private int _intervalNext;
        private float _detectedInterval = 0.5f;

        /// <summary>Distance from the middle (in half terrain widths) where the bass bands end. Only used by the origin Bass edge.</summary>
        public void SetBassEdge(float distance)
        {
            _bassEdge = Mathf.Clamp(distance, 0f, EdgeDistance);
        }

        // The time of the waves. It stops while the terrain is frozen (paused), so the waves stand still and go on from there.
        private float _clock;

        /// <summary>
        /// Called once per mesh update before the vertices are calculated. Detects beats, starts new waves and moves the running ones.
        /// deltaTime is the time since the last call, the default is the time of the last frame. While frozen, time stands still:
        /// no beats, the running waves stay where they are.
        /// </summary>
        public void Tick(float deltaTime = -1f, bool frozen = false)
        {
            if (deltaTime < 0f)
                deltaTime = Time.deltaTime;
            if (frozen)
                deltaTime = 0f;
            _clock += deltaTime;
            float time = _clock;
            float pulse = AudioTestSignal.IsActive ? AudioTestSignal.Instance.Pulse
                : _beatPulseModule != null ? Mathf.Clamp01(_beatPulseModule.GetValue()) : 0f;
            bool beat = !frozen && DetectBeat(pulse, time);

            if (!_enabled || _strength <= 0f)
            {
                _tracking = false;
                _historyCount = 0;
                _historyNext = 0;
                _activeCount = 0;
                for (int i = 0; i < MaxWaves; i++)
                {
                    _amplitude[i] = 0f;
                    _targetAmplitude[i] = 0f;
                }
                return;
            }

            GetLevels(out float bass, out float other);
            if (!frozen)
                RecordLevels(time, bass, other);
            _bassPeak = Mathf.Max(bass, _bassPeak * Mathf.Pow(0.5f, deltaTime / BassPeakHalfLife));
            _overallPeak = Mathf.Max(Mathf.Max(bass, other), _overallPeak * Mathf.Pow(0.5f, deltaTime / OverallPeakHalfLife));

            // Every beat sends a flat wave off at once. While its window is open it collects the bass levels.
            if (beat)
            {
                _trackBass = HighestSince(_historyBass, time - BassWindowBefore);
                _trackOther = HighestSince(_historyOther, time - BassWindowBefore);
                _trackStart = time;
                _trackEnd = time + BassWindowAfter;
                _trackSlot = Spawn(0f, time);
                _tracking = true;
                _confirmed = false;
            }
            else if (_tracking)
            {
                if (time > _trackEnd || _startTime[_trackSlot] != _trackStart)
                {
                    _tracking = false;
                }
                else
                {
                    _trackBass = Mathf.Max(_trackBass, bass);
                    _trackOther = Mathf.Max(_trackOther, other);
                }
            }

            // As soon as the bass is strong enough the wave grows to its height. Strong right at the beat: it starts
            // at full height. Strong only a little later: it grows from flat, so it does not jump up.
            if (_tracking)
            {
                if (!_confirmed && IsStrongBeat(_trackBass, _trackOther))
                    _confirmed = true;
                if (_confirmed)
                {
                    float target = Mathf.Max(MinAmplitude, AmplitudeOf(_trackBass));
                    _targetAmplitude[_trackSlot] = Mathf.Max(_targetAmplitude[_trackSlot], target);
                    if (beat)
                        _amplitude[_trackSlot] = _targetAmplitude[_trackSlot];
                }
            }

            GrowWaves(deltaTime);
            PrepareFrame();
        }

        /// <summary>A beat counts when the pulse jumps up fast enough. Also keeps the beat count and the beat interval.</summary>
        private bool DetectBeat(float pulse, float time)
        {
            float riseRate = (pulse - _lastPulse) / Mathf.Max(time - _lastTickTime, 0.001f);
            _lastPulse = pulse;
            _lastTickTime = time;
            if (pulse < MinPulse || riseRate < MinRiseRate || time - _lastBeatTime < MinInterval)
                return false;

            if (_lastBeatTime > time - 2f)
                RecordInterval(time - _lastBeatTime);
            _lastBeatTime = time;
            BeatCount++;
            return true;
        }

        private void RecordInterval(float interval)
        {
            // Twice as fast as any usual tempo means a double time part, count it as one beat.
            if (interval < 0.3f)
                interval *= 2f;
            _intervals[_intervalNext] = interval;
            _intervalNext = (_intervalNext + 1) % IntervalCount;
            _intervalCount = Mathf.Min(_intervalCount + 1, IntervalCount);

            // median of the recent intervals
            var sorted = new float[_intervalCount];
            System.Array.Copy(_intervals, sorted, _intervalCount);
            System.Array.Sort(sorted);
            _detectedInterval = Mathf.Clamp(sorted[_intervalCount / 2], 0.25f, 1.5f);
        }

        /// <summary>The bass has to be strong compared to the recent loudest bass, and either the loudest part of the sound or in a loud part of the song.</summary>
        private bool IsStrongBeat(float bass, float other)
        {
            if (bass < MinBassShare * Mathf.Max(_bassPeak, MinPeak))
                return false;
            if (bass >= other)
                return true;
            return other >= MinOverallShare * Mathf.Max(_overallPeak, MinPeak);
        }

        /// <summary>Height of a wave (0-1) from the highest bass level around the beat.</summary>
        private float AmplitudeOf(float bass)
        {
            // Relative to the loudest bass of the last seconds, so the height does not depend on the volume of the song.
            return Mathf.Clamp01(bass / (Mathf.Max(_bassPeak, MinPeak) * FullBassShare));
        }

        /// <summary>Waves that have not reached their height yet grow towards it.</summary>
        private void GrowWaves(float deltaTime)
        {
            for (int i = 0; i < MaxWaves; i++)
            {
                if (_amplitude[i] >= _targetAmplitude[i])
                    continue;
                // a fast wave grows over a short time, so it reaches its height after about the same distance
                float seconds = Mathf.Clamp(GrowDistance / Mathf.Max(0.01f, _waveSpeed[i]), MinGrowSeconds, MaxGrowSeconds);
                _amplitude[i] = Mathf.MoveTowards(_amplitude[i], _targetAmplitude[i], deltaTime / seconds);
            }
        }

        /// <summary>Speed of a wave in half terrain widths per second: Speed 1 crosses half the terrain in one beat.</summary>
        private float WaveSpeedNow()
        {
            return _speed * SpeedDistance / Mathf.Max(0.05f, BeatInterval);
        }

        /// <summary>
        /// Starts a wave that began at the given time (the default is now) and returns its slot. An amplitude of 0 starts a
        /// flat wave that can grow later. Public for tests.
        /// </summary>
        public int Spawn(float amplitude, float startTime = -1f)
        {
            // a free slot first, otherwise the oldest wave makes room
            int oldest = 0;
            for (int i = 1; i < MaxWaves; i++)
            {
                bool betterFree = _amplitude[i] <= 0f && _amplitude[oldest] > 0f;
                bool bothSame = (_amplitude[i] <= 0f) == (_amplitude[oldest] <= 0f);
                if (betterFree || (bothSame && _startTime[i] < _startTime[oldest]))
                    oldest = i;
            }

            float width = Mathf.Max(0.01f, _width);
            if (_origin == Origin.BassEdge)
            {
                // the wave is born a little inside of the bass, so it already shows on the bass bands
                float start = Mathf.Max(-width, _bassEdge - BassEdgeLead * width);
                _startFront[oldest] = start;
                _direction[oldest] = 1f;
                _totalWay[oldest] = EdgeDistance - start + 2f * width;
            }
            else
            {
                bool fromEdge = _origin == Origin.Edge || (_origin == Origin.Alternate && _nextFromEdge);
                _nextFromEdge = !_nextFromEdge;
                _startFront[oldest] = fromEdge ? EdgeDistance : -width;
                _direction[oldest] = fromEdge ? -1f : 1f;
                _totalWay[oldest] = EdgeDistance + 2f * width;
            }

            _startTime[oldest] = startTime >= 0f ? startTime : _clock;
            _amplitude[oldest] = amplitude <= 0f ? 0f : Mathf.Max(MinAmplitude, Mathf.Clamp01(amplitude));
            _targetAmplitude[oldest] = _amplitude[oldest];
            _waveSpeed[oldest] = WaveSpeedNow();
            return oldest;
        }

        /// <summary>Number of waves that run in this frame, see <see cref="CopyWaves"/>.</summary>
        public int ActiveCount => _activeCount;
        /// <summary>Width of the ring in half terrain widths.</summary>
        public float ActiveWidth => _activeWidth;

        /// <summary>Position (distance from the middle in half terrain widths) and height of every running wave, for the vertex job.</summary>
        public void CopyWaves(Unity.Collections.NativeArray<float> fronts, Unity.Collections.NativeArray<float> levels)
        {
            for (int i = 0; i < _activeCount; i++)
            {
                fronts[i] = _front[i];
                levels[i] = _level[i];
            }
        }

        /// <summary>Calculates position and height of every running wave once per frame, not once per vertex.</summary>
        private void PrepareFrame()
        {
            _activeCount = 0;
            _activeWidth = Mathf.Max(0.01f, _width);

            for (int i = 0; i < MaxWaves; i++)
            {
                if (_amplitude[i] <= 0f)
                    continue;

                float travelled = (_clock - _startTime[i]) * _waveSpeed[i];
                if (travelled > _totalWay[i])
                {
                    _amplitude[i] = 0f;
                    _targetAmplitude[i] = 0f;
                    continue;
                }

                _front[_activeCount] = _startFront[i] + _direction[i] * travelled;
                _level[_activeCount] = _strength * _amplitude[i] * (1f - _fade * travelled / _totalWay[i]);
                _activeCount++;
            }
        }

        private void RecordLevels(float time, float bass, float other)
        {
            _historyTimes[_historyNext] = time;
            _historyBass[_historyNext] = bass;
            _historyOther[_historyNext] = other;
            _historyNext = (_historyNext + 1) % HistorySize;
            _historyCount = Mathf.Min(_historyCount + 1, HistorySize);
        }

        /// <summary>Highest recorded level since the given time.</summary>
        private float HighestSince(float[] values, float time)
        {
            float highest = 0f;
            for (int i = 0; i < _historyCount; i++)
                if (_historyTimes[i] >= time && values[i] > highest)
                    highest = values[i];
            return highest;
        }

        /// <summary>The bass level (highest weighted level of the lowest bands) and the highest level of all other bands.</summary>
        private void GetLevels(out float bass, out float other)
        {
            bass = 0f;
            other = 0f;
            if (_spectrum == null || _spectrum.BandSize < 1)
                return;

            for (int i = 0; i < _spectrum.BandSize; i++)
            {
                float value = _spectrum.GetValue(i);
                if (i < BassWeights.Length)
                    bass = Mathf.Max(bass, value * BassWeights[i]);
                else
                    other = Mathf.Max(other, value);
            }
            bass = Mathf.Clamp01(bass);
            other = Mathf.Clamp01(other);
        }
    }
}
