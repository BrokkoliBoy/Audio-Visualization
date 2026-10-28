using System.Collections.Generic;

namespace Gavi.VideoDirector
{
    /// <summary>
    /// The script of one take: events at beats counted from the first beat of the song (see <see cref="DirectorClock"/>).
    /// The Preroll steps run once before the song starts (preset, black screen, closed menu), the rest follows the song.
    /// </summary>
    public class DirectorTimeline
    {
        public string Name;
        /// <summary>Part of the name of the song object to play ("In Motion"). Empty = the song that plays now, from the start.</summary>
        public string Song = "";
        /// <summary>The take ends at this beat.</summary>
        public float EndBeat;

        public readonly List<DirectorEvent> Preroll = new List<DirectorEvent>();
        public readonly List<DirectorEvent> Events = new List<DirectorEvent>();

        public DirectorTimeline(string name) { Name = name; }

        #region Before the song
        public DirectorTimeline StartWith(string preset) { Preroll.Add(new CutEvent(preset)); return this; }
        public DirectorTimeline StartBlack() { Preroll.Add(new FadeEvent(1f)); return this; }
        public DirectorTimeline StartMenu(bool open) { Preroll.Add(new MenuEvent(open)); return this; }
        public DirectorTimeline StartSet(string path, object value) { Preroll.Add(new SetEvent(path, value)); return this; }
        #endregion


        #region During the song
        /// <summary>Loads a preset at once.</summary>
        public DirectorTimeline Cut(float beat, string preset) { return Add(new CutEvent(preset), beat, 0f); }

        /// <summary>Glides into a preset over the given beats. Switches, patterns and shapes change at the middle (switchAt 0 to 1).</summary>
        public DirectorTimeline Morph(float beat, float beats, string preset, float switchAt = 0.5f, Ease ease = Ease.Smooth)
        {
            return Add(new MorphEvent(preset) { SwitchAt = switchAt }, beat, beats, ease);
        }

        /// <summary>
        /// For the morph just added: a number moves to <paramref name="peak"/> around the switch and back, so the jump of the patterns and the mesh detail is hidden.
        /// <c>.Bump("TerrainHeight", 0.2f)</c> sinks the terrain, <c>.Bump("PerlinSpeedY", -10f)</c> speeds the scroll up.
        /// </summary>
        public DirectorTimeline Bump(string path, float peak, float width = 0.3f)
        {
            if (Events.Count > 0 && Events[Events.Count - 1] is MorphEvent morph)
                morph.Bumps.Add(new MorphBump { Path = path, Peak = peak, Width = width });
            else
                UnityEngine.Debug.LogWarning("[Director] Bump " + path + " needs a Morph right before it and was skipped.");
            return this;
        }

        /// <summary>Sets one value at once: <c>Set(40, "NoisePattern", 12)</c>, <c>Set(40, "NoiseLayer2.Enabled", true)</c>.</summary>
        public DirectorTimeline Set(float beat, string path, object value) { return Add(new SetEvent(path, value), beat, 0f); }

        /// <summary>Glides one slider to a value: <c>Glide(40, 8, "MorphAmount", 0.9f)</c>.</summary>
        public DirectorTimeline Glide(float beat, float beats, string path, float to, Ease ease = Ease.Smooth)
        {
            return Add(new GlideEvent(path, to), beat, beats, ease);
        }

        public DirectorTimeline Menu(float beat, bool open) { return Add(new MenuEvent(open), beat, 0f); }

        /// <summary>Fades to black (1) or clear (0).</summary>
        public DirectorTimeline Fade(float beat, float beats, float to, Ease ease = Ease.Smooth)
        {
            return Add(new FadeEvent(to), beat, beats, ease);
        }

        /// <summary>Camera to a pose: yaw and pitch in degrees, scale = distance as a multiple of the start distance (below 1 = closer).</summary>
        public DirectorTimeline Camera(float beat, float beats, float yaw, float pitch, float scale, Ease ease = Ease.Smooth)
        {
            return Add(new CameraEvent(yaw, pitch, scale), beat, beats, ease);
        }
        #endregion


        /// <summary>
        /// The beats the jump keys (F6 / F7) go to: the start of the take and <paramref name="leadBeats"/> before every change of the picture
        /// (Cut, Morph, fade to black), so a transition can be watched again without playing the take from the start.
        /// </summary>
        public List<float> JumpPoints(float leadBeats = 4f)
        {
            List<float> points = new List<float> { 0f };
            foreach (DirectorEvent e in Events)
            {
                bool change = e is CutEvent || e is MorphEvent || (e is FadeEvent fade && fade.To >= 0.5f);
                float beat = e.Beat - leadBeats;
                if (change && beat > 0.5f && !points.Exists(p => UnityEngine.Mathf.Abs(p - beat) < 1f))
                    points.Add(beat);
            }
            points.Sort();
            return points;
        }

        private DirectorTimeline Add(DirectorEvent directorEvent, float beat, float beats, Ease ease = Ease.Smooth)
        {
            directorEvent.Beat = beat;
            directorEvent.Duration = beats;
            directorEvent.Ease = ease;
            Events.Add(directorEvent);
            return this;
        }
    }
}
