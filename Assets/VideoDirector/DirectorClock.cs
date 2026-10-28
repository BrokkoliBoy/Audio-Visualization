using Gavi.Audio;
using UnityEngine;

namespace Gavi.VideoDirector
{
    /// <summary>
    /// Beat position of the playing song. Beat 0 is the first beat of the BPM grid (song time = BeatOffset), so an event on a whole beat
    /// lands on the music. Beats before it are negative. The audio time of a source only moves in buffer steps, so the clock runs on the
    /// frame time and is pulled towards the audio time.
    /// </summary>
    public class DirectorClock
    {
        /// <summary>Seconds of difference after which the clock jumps to the audio time (restart of the song, seek).</summary>
        private const float SnapSeconds = 0.1f;
        /// <summary>Share of the difference to the audio time that is corrected per frame.</summary>
        private const float PullPerFrame = 0.1f;

        private float _songTime;
        private bool _hasTime;

        public bool IsValid => _hasTime && Bpm > 0f;
        public float SongTime => _songTime;
        public float Bpm { get; private set; }
        public float BeatOffset { get; private set; }
        public float SecondsPerBeat => Bpm > 0f ? 60f / Bpm : 0f;
        public float Beat => IsValid ? (_songTime - BeatOffset) * Bpm / 60f : float.NegativeInfinity;
        /// <summary>Beat position of the very start of the song, the first beat an event can run on.</summary>
        public float StartBeat => Bpm > 0f ? -BeatOffset * Bpm / 60f : 0f;

        /// <summary>Forgets the time, the next Tick takes the audio time as it is.</summary>
        public void Reset()
        {
            _hasTime = false;
            _songTime = 0f;
        }

        public void Tick(float unscaledDeltaTime)
        {
            AudioPlayer player = AudioPlayer.Instance;
            AudioClipContainer clip = player != null ? player.CurrentlyPlayingClip : null;
            if (clip == null || clip.BPM <= 0f || player.CurrentPlaybackTime < 0f)
            {
                _hasTime = false;
                return;
            }

            Bpm = clip.BPM;
            BeatOffset = clip.BeatOffset;
            float audioTime = player.CurrentPlaybackTime;
            if (!_hasTime || Mathf.Abs(_songTime - audioTime) > SnapSeconds)
            {
                _songTime = audioTime;
                _hasTime = true;
                return;
            }

            if (player.IsPlaying)
                _songTime += unscaledDeltaTime;
            _songTime += (audioTime - _songTime) * PullPerFrame;
        }
    }
}
