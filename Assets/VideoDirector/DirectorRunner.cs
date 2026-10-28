using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Gavi.Audio;
using UnityEngine;

namespace Gavi.VideoDirector
{
    /// <summary>
    /// Plays a <see cref="DirectorTimeline"/> on the running app: F9 starts a take (restarts the song from the beginning), F10 aborts it, F11 shows
    /// the beat counter. Created by code at the start of the scene (<see cref="DirectorBootstrap"/>), it is not part of any scene.
    /// During a take the mouse pointer is hidden and locked, so nothing opens by accident.
    /// </summary>
    public class DirectorRunner : MonoBehaviour
    {
        public static DirectorRunner Instance { get; private set; }

        private const KeyCode StartKey = KeyCode.F9;
        private const KeyCode AbortKey = KeyCode.F10;
        private const KeyCode InfoKey = KeyCode.F11;
        private const KeyCode MarkKey = KeyCode.F8;
        private const KeyCode PreviousJumpKey = KeyCode.F6;
        private const KeyCode NextJumpKey = KeyCode.F7;
        private const string OldJumpMarkerPref = "VideoDirector.JumpMarker";
        /// <summary>F6 within this many seconds after a jump point goes to the one before it (like "previous" in a media player).</summary>
        private const float PreviousGraceSeconds = 1f;
        /// <summary>Time between loading the first preset and the start of the song, so the terrain is built when the picture opens.</summary>
        private const float PrerollSeconds = 2f;

        public enum State { Idle, Preroll, Running }

        private readonly DirectorClock _clock = new DirectorClock();
        private readonly DirectorContext _context = new DirectorContext();
        private readonly DirectorOverlay _overlay = new DirectorOverlay();
        private readonly List<DirectorEvent> _pending = new List<DirectorEvent>();
        private readonly List<DirectorEvent> _active = new List<DirectorEvent>();

        private DirectorTimeline _timeline;
        private State _state = State.Idle;
        private bool _showInfo;
        private string _lastEvent = "";
        private string _problem = "";
        private int _markerCount;

        public State Current => _state;

        #region Mono
        private void Awake()
        {
            Instance = this;
            _context.Fade = new DirectorFade(transform);
            _context.Camera = new DirectorCamera();
            // the own jump point of the removed F5 key was stored in the player prefs, it is deleted so it does not stay around
            PlayerPrefs.DeleteKey(OldJumpMarkerPref);
        }

        private void Update()
        {
            if (!UiPointer.IsTyping)
            {
                if (Input.GetKeyDown(StartKey))
                    StartTake();
                else if (Input.GetKeyDown(AbortKey))
                    AbortTake();
                else if (Input.GetKeyDown(InfoKey))
                    _showInfo = !_showInfo;
                else if (Input.GetKeyDown(MarkKey))
                    SetMarker();
                else if (Input.GetKeyDown(PreviousJumpKey))
                    JumpStep(-1);
                else if (Input.GetKeyDown(NextJumpKey))
                    JumpStep(1);
            }

            if (_state == State.Idle)
                return;

            UiPointer.SetLocked(true);
            if (_state == State.Running)
            {
                RunEvents();
                if (_clock.IsValid && _state == State.Running)
                    _overlay.Record(_clock.Beat, _context.Current);
            }
        }

        private void OnGUI()
        {
            if (!_showInfo)
                return;

            if (_timeline == null)
                _timeline = HeroVideoTimeline.Build();

            bool hasBeat = _state != State.Idle && _clock.IsValid;
            string header = "Director: " + _state;
            if (hasBeat)
                header += "     beat " + _clock.Beat.ToString("0.0") + "     bar " + (Mathf.FloorToInt(_clock.Beat / 4f) + 1) + "     " + (_clock.Beat * _clock.SecondsPerBeat).ToString("0.0") + " s";
            if (_lastEvent.Length > 0)
                header += "     last: " + _lastEvent;
            if (_problem.Length > 0)
                header += "     PROBLEM: " + _problem;
            _overlay.Draw(header, _timeline, hasBeat ? _clock.Beat : 0f, SecondsPerBeat(), hasBeat, JumpBeats());
        }

        private float SecondsPerBeat()
        {
            if (_clock.SecondsPerBeat > 0f)
                return _clock.SecondsPerBeat;
            Gavi.Audio.AudioClipContainer clip = AudioPlayer.Instance != null ? AudioPlayer.Instance.CurrentlyPlayingClip : null;
            return clip != null && clip.BPM > 0f ? 60f / clip.BPM : 60f / 134f;
        }

        /// <summary>The text of the info overlay (F11) and of a marker (F8): where the take is, what runs, and the numbers that matter at a transition.</summary>
        private string BuildInfo()
        {
            string text = "Director: " + _state;
            if (_state == State.Running && _clock.IsValid)
            {
                float beat = _clock.Beat;
                text += "   beat " + beat.ToString("0.0") + "   bar " + (Mathf.FloorToInt(beat / 4f) + 1) + "   " + (beat * _clock.SecondsPerBeat).ToString("0.0") + " s";
                foreach (DirectorEvent running in _active)
                {
                    float percent = Mathf.Clamp01((beat - running.Beat) / running.Duration) * 100f;
                    text += "\nRUNS   " + running.Describe() + "   " + percent.ToString("0") + " %   (beat " + running.Beat + " to " + (running.Beat + running.Duration) + ")";
                }
                if (_pending.Count > 0)
                    text += "\nNEXT   " + _pending[0].Describe() + "   at beat " + _pending[0].Beat + "   (in " + ((_pending[0].Beat - beat) * _clock.SecondsPerBeat).ToString("0.0") + " s)";
                text += "\nLAST   " + _lastEvent;
                text += "\n" + Values() + "\nF8 = marker (screenshot + values to the log folder), F6 / F7 = jump, F10 = abort";
            }
            else if (_state == State.Idle)
                text += "   F9 start, F6 / F7 jump, F10 abort, F11 hide";
            if (_problem.Length > 0)
                text += "\n" + _problem;
            return text;
        }

        /// <summary>The values of the current setup that change the picture the most. A switch that has already changed shows here.</summary>
        private string Values()
        {
            TerrainSettingsPreset preset = TerrainSettingsManager.Instance != null ? TerrainSettingsManager.Instance.CurrentPreset : null;
            if (preset == null)
                return "";
            string[] paths =
            {
                "NoisePattern", "NoiseLayer2.Pattern", "NoiseLayer3.Pattern", "MorphShape", "MorphAmount", "FaceCountX", "TerrainHeight", "WaveFade", "ColorPaletteName"
            };
            string text = "NOW  ";
            foreach (string path in paths)
            {
                object value = FieldPath.Get(preset, path);
                text += "  " + path + " " + (value is float f ? f.ToString("0.00") : value);
            }
            return text;
        }

        /// <summary>Writes the moment into a text file and a screenshot, so the picture a viewer is talking about can be found again.</summary>
        private void SetMarker()
        {
            string folder = Application.persistentDataPath;
            _markerCount++;
            string pictureName = "DirectorMarker_" + _markerCount + ".png";
            string line = System.DateTime.Now.ToString("HH:mm:ss") + "  marker " + _markerCount + "  " + pictureName + "\n" + BuildInfo() + "\n\n";
            System.IO.File.AppendAllText(System.IO.Path.Combine(folder, "DirectorMarkers.txt"), line);
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder, pictureName));
            _lastEvent = "Marker " + _markerCount + " set";
        }
        #endregion


        #region Take
        public void StartTake()
        {
            BeginTake(0f);
        }

        /// <summary>
        /// Starts the take at a later beat: the song jumps there and the events before it are brought to the state they have at that beat
        /// (finished ones to their end, a running one to its progress). Only the changes of the setup count, the exact look of the terrain
        /// (noise phase, beat breath) is not restored.
        /// </summary>
        public void JumpTo(float beat)
        {
            BeginTake(beat);
        }

        private void BeginTake(float startBeat)
        {
            // jumping inside a take that runs: the terrain is built already, so the preroll does not wait
            bool wasRunning = _state == State.Running;
            AbortTake();
            _problem = "";
            _timeline = HeroVideoTimeline.Build();
            StartCoroutine(TakeRoutine(_timeline, startBeat, wasRunning ? 0f : PrerollSeconds));
        }

        public void AbortTake()
        {
            StopAllCoroutines();
            if (_state != State.Idle)
                EndTake();
            _context.Fade.Alpha = 0f;
        }

        private IEnumerator TakeRoutine(DirectorTimeline timeline, float startBeat, float prerollSeconds)
        {
            _state = State.Preroll;
            Cursor.visible = false;
            UiPointer.SetLocked(true);
            _context.Camera.Find();
            _context.Camera.ResetNow();

            // The preroll steps and the checks of the preset names run first, so a typing error shows before the song starts.
            foreach (DirectorEvent step in timeline.Preroll)
            {
                if (!TryRun(step, () => { step.Begin(_context); step.Tick(_context, 1f); step.End(_context); }))
                {
                    EndTake();
                    yield break;
                }
            }

            if (prerollSeconds > 0f)
                yield return new WaitForSecondsRealtime(prerollSeconds);
            if (!StartSong(timeline.Song))
            {
                EndTake();
                yield break;
            }

            _pending.Clear();
            _active.Clear();
            _pending.AddRange(timeline.Events);
            _pending.Sort((a, b) => a.Beat.CompareTo(b.Beat));
            _lastEvent = "";
            // the take starts at the StartBeat of the timeline, a jump can only go later than that
            float seekBeat = Mathf.Max(startBeat, timeline.StartBeat);
            if (seekBeat > 0.01f)
            {
                if (SeekSong(seekBeat))
                    CatchUp(seekBeat);
                if (startBeat > timeline.StartBeat + 0.01f)
                    _lastEvent = "Jump to beat " + startBeat.ToString("0.0");
            }
            _clock.Reset();
            _overlay.StartTake(timeline.EndBeat);
            _state = State.Running;
        }

        /// <summary>Puts the song at a beat of the take. The clock is reset by the caller, it takes the audio time at the next frame.</summary>
        private bool SeekSong(float beat)
        {
            AudioPlayer player = AudioPlayer.Instance;
            AudioClipContainer clip = player != null ? player.CurrentlyPlayingClip : null;
            FieldInfo field = typeof(AudioPlayer).GetField("_audioSource", BindingFlags.Instance | BindingFlags.NonPublic);
            AudioSource source = field != null && player != null ? field.GetValue(player) as AudioSource : null;
            if (clip == null || clip.BPM <= 0f || source == null || source.clip == null)
            {
                _problem = "Jump: no song to seek in (or AudioPlayer._audioSource was renamed).";
                return false;
            }
            source.time = Mathf.Clamp(clip.BeatOffset + beat * 60f / clip.BPM, 0f, Mathf.Max(0f, source.clip.length - 0.05f));
            return true;
        }

        /// <summary>
        /// Runs all events that start before the beat: a finished one is played to its end, one that is still running is set to its
        /// progress and goes on as active event.
        /// </summary>
        private void CatchUp(float beat)
        {
            while (_pending.Count > 0 && _pending[0].Beat < beat)
            {
                DirectorEvent skipped = _pending[0];
                _pending.RemoveAt(0);
                if (!TryRun(skipped, () => skipped.Begin(_context)))
                    continue;
                bool unfinished = skipped.Duration > 0f && skipped.Beat + skipped.Duration > beat;
                if (unfinished)
                {
                    float progress = (beat - skipped.Beat) / skipped.Duration;
                    TryRun(skipped, () => skipped.Tick(_context, DirectorEvent.Apply(skipped.Ease, progress)));
                    _active.Add(skipped);
                }
                else
                    TryRun(skipped, () => { skipped.Tick(_context, 1f); skipped.End(_context); });
            }
        }

        #region Jump points
        /// <summary>The beats F6 / F7 go to: the start and one bar before every change of the picture, see <see cref="DirectorTimeline.JumpPoints"/>.</summary>
        private List<float> JumpBeats()
        {
            if (_timeline == null)
                _timeline = HeroVideoTimeline.Build();
            return _timeline.JumpPoints();
        }

        /// <summary>F6 / F7: jumps to the previous / next jump point. Without a running take F7 starts at the first one after the start.</summary>
        private void JumpStep(int direction)
        {
            if (_state == State.Preroll)
                return;
            List<float> points = JumpBeats();
            float current = _state == State.Running && _clock.IsValid ? _clock.Beat : 0f;
            float grace = PreviousGraceSeconds / Mathf.Max(0.0001f, SecondsPerBeat());

            float target = points[0];
            if (direction > 0)
            {
                bool found = false;
                foreach (float point in points)
                {
                    if (point > current + 0.25f)
                    {
                        target = point;
                        found = true;
                        break;
                    }
                }
                if (!found)
                    return;
            }
            else
            {
                for (int i = points.Count - 1; i >= 0; i--)
                {
                    if (points[i] < current - grace)
                    {
                        target = points[i];
                        break;
                    }
                }
            }
            JumpTo(target);
        }
        #endregion

        private bool StartSong(string songName)
        {
            AudioPlayer player = AudioPlayer.Instance;
            if (player == null)
            {
                _problem = "No AudioPlayer in the scene.";
                return false;
            }

            if (!string.IsNullOrEmpty(songName))
            {
                AudioClipContainer wanted = null;
                foreach (AudioClipContainer clip in FindObjectsByType<AudioClipContainer>(FindObjectsInactive.Include))
                {
                    if (clip.name.ToLowerInvariant().Contains(songName.ToLowerInvariant()))
                        wanted = clip;
                }

                if (wanted == null)
                {
                    _problem = "No song named '" + songName + "'.";
                    return false;
                }
                player.Play(wanted);
            }
            else if (player.CurrentlyPlayingClip != null)
                player.Replay();
            else
            {
                _problem = "No song is playing and the timeline names none.";
                return false;
            }
            return true;
        }

        private void EndTake()
        {
            _state = State.Idle;
            AudioListener.volume = 1f;
            _pending.Clear();
            _active.Clear();
            Cursor.visible = true;
            UiPointer.SetLocked(false);
        }

        private void RunEvents()
        {
            _clock.Tick(Time.unscaledDeltaTime);
            if (!_clock.IsValid)
                return;

            float beat = _clock.Beat;
            while (_pending.Count > 0 && _pending[0].Beat <= beat)
            {
                DirectorEvent next = _pending[0];
                _pending.RemoveAt(0);
                _lastEvent = next.Describe();
                if (!TryRun(next, () => next.Begin(_context)))
                    continue;
                if (next.Duration > 0f)
                    _active.Add(next);
                else
                {
                    TryRun(next, () => { next.Tick(_context, 1f); next.End(_context); });
                }
            }

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                DirectorEvent running = _active[i];
                float progress = (beat - running.Beat) / running.Duration;
                bool done = progress >= 1f;
                TryRun(running, () => running.Tick(_context, DirectorEvent.Apply(running.Ease, progress)));
                if (done)
                {
                    TryRun(running, () => { running.Tick(_context, 1f); running.End(_context); });
                    _active.RemoveAt(i);
                }
            }

            if (beat >= _timeline.EndBeat && _pending.Count == 0 && _active.Count == 0)
                EndTake();
        }

        /// <summary>A broken event (unknown preset or field) must not stop the take: it is logged and the take goes on.</summary>
        private bool TryRun(DirectorEvent directorEvent, System.Action action)
        {
            try
            {
                action();
                return true;
            }
            catch (System.Exception e)
            {
                _problem = directorEvent.Describe() + ": " + e.Message;
                Debug.LogError("Video Director: " + _problem + "\n" + e);
                return false;
            }
        }
        #endregion
    }
}
