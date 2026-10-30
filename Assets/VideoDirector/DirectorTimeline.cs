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
        /// <summary>The take starts here: the song is put at this beat when it starts, so a slow beginning of the song can be left out. Events stay at the beats of the song.</summary>
        public float StartBeat;
        /// <summary>The take ends at this beat.</summary>
        public float EndBeat;

        public readonly List<DirectorEvent> Preroll = new List<DirectorEvent>();
        public readonly List<DirectorEvent> Events = new List<DirectorEvent>();
        /// <summary>Jump points set by hand, in addition to the ones <see cref="JumpPoints"/> finds before every change of the picture.</summary>
        public readonly List<float> ExtraJumpPoints = new List<float>();

        public DirectorTimeline(string name) { Name = name; }

        /// <summary>Adds a jump point for the jump keys (F6 / F7), e.g. in front of the live part that has no transition of its own.</summary>
        public DirectorTimeline JumpPoint(float beat) { ExtraJumpPoints.Add(beat); return this; }

        #region Before the song
        public DirectorTimeline StartWith(string preset) { Preroll.Add(new CutEvent(preset)); return this; }
        public DirectorTimeline StartBlack() { Preroll.Add(new FadeEvent(1f)); return this; }
        /// <summary>The output is mute when the song starts, so <see cref="Volume"/> can fade it in.</summary>
        public DirectorTimeline StartMute() { Preroll.Add(new VolumeEvent(0f)); return this; }
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
        /// A morph that starts <paramref name="gapBeats"/> after the END of the morph before it, so moving or shortening the first one moves the second with it.
        /// </summary>
        public DirectorTimeline MorphAfter(float gapBeats, float beats, string preset, float switchAt = 0.5f, Ease ease = Ease.Smooth)
        {
            float end = 0f;
            foreach (DirectorEvent e in Events)
            {
                if (e is MorphEvent)
                    end = e.Beat + e.Duration;
            }
            if (end <= 0f)
                UnityEngine.Debug.LogWarning("[Director] MorphAfter needs a Morph before it, it starts at beat " + gapBeats + ".");
            return Morph(end + gapBeats, beats, preset, switchAt, ease);
        }

        /// <summary>
        /// A morph that starts <paramref name="offsetBeats"/> after the START of the morph before it (e.g. every 4 beats, on the bar), so moving the first one moves
        /// the second with it. Warns when the first one is still running at that beat, two morphs at once would fight over the same values.
        /// </summary>
        public DirectorTimeline MorphFromStart(float offsetBeats, float beats, string preset, float switchAt = 0.5f, Ease ease = Ease.Smooth)
        {
            float start = 0f, end = 0f;
            foreach (DirectorEvent e in Events)
            {
                if (e is MorphEvent)
                {
                    start = e.Beat;
                    end = e.Beat + e.Duration;
                }
            }
            if (end <= 0f)
                UnityEngine.Debug.LogWarning("[Director] MorphFromStart needs a Morph before it, it starts at beat " + offsetBeats + ".");
            else if (start + offsetBeats < end)
                UnityEngine.Debug.LogWarning("[Director] The morph into " + preset + " starts at beat " + (start + offsetBeats) + " while the one before it runs until beat " + end + ".");
            return Morph(start + offsetBeats, beats, preset, switchAt, ease);
        }

        /// <summary>
        /// For the morph just added: the part after the switch takes only <paramref name="share"/> of its time (0.5 = half). The morph gets shorter by that;
        /// what happens before the switch stays as it was.
        /// </summary>
        public DirectorTimeline AfterSwitchTime(float share)
        {
            if (Events.Count > 0 && Events[Events.Count - 1] is MorphEvent morph)
            {
                share = UnityEngine.Mathf.Clamp(share, 0.05f, 1f);
                morph.Duration *= morph.SwitchAt + (1f - morph.SwitchAt) * share;
                morph.AfterTimeShare = share;
            }
            else
                UnityEngine.Debug.LogWarning("[Director] AfterSwitchTime needs a Morph right before it and was skipped.");
            return this;
        }

        /// <summary>
        /// For the morph just added: a number moves to <paramref name="peak"/> around the switch and back, so the jump of the patterns and the mesh detail is hidden.
        /// <c>.Bump("TerrainHeight", 0.2f)</c> sinks the terrain, <c>.Bump("PerlinSpeedY", -10f)</c> speeds the scroll up.
        /// </summary>
        public DirectorTimeline Bump(string path, float peak, float width = 0.3f, bool rampOnly = false, float widthAfter = 0f, float sharpness = 0f)
        {
            if (Events.Count > 0 && Events[Events.Count - 1] is MorphEvent morph)
                morph.Bumps.Add(new MorphBump { Path = path, Peak = peak, Width = width, RampOnly = rampOnly, WidthAfter = widthAfter, Sharpness = sharpness });
            else
                UnityEngine.Debug.LogWarning("[Director] Bump " + path + " needs a Morph right before it and was skipped.");
            return this;
        }

        /// <summary>
        /// For the morph just added: a number takes the value of the new preset at once at the switch, like the pattern does. Before the switch it keeps its start value,
        /// or with <paramref name="glideBefore"/> it still glides towards the new value (a decorative pull) and snaps to it at the switch.
        /// Needed for the zoom of the patterns: while it glided, the pattern that is already switched ran with a zoom between the two presets (fine grain).
        /// </summary>
        public DirectorTimeline SnapAtSwitch(string path, bool glideBefore = false)
        {
            if (Events.Count > 0 && Events[Events.Count - 1] is MorphEvent morph)
                morph.Snaps.Add(new MorphSnap { Path = path, GlideBefore = glideBefore });
            else
                UnityEngine.Debug.LogWarning("[Director] SnapAtSwitch " + path + " needs a Morph right before it and was skipped.");
            return this;
        }

        /// <summary>For the morph just added: the zoom of the main pattern and of layers 2 and 3 snap at the switch (see <see cref="SnapAtSwitch"/>).</summary>
        public DirectorTimeline SnapPatternZoom(bool glideBefore = false)
        {
            return SnapAtSwitch("PerlinZoom", glideBefore).SnapAtSwitch("NoiseLayer2.Zoom", glideBefore).SnapAtSwitch("NoiseLayer3.Zoom", glideBefore);
        }

        /// <summary>
        /// For the morph just added: a number moves to the value of the new preset in three parts: slowly up to <paramref name="before"/> of the way until shortly before
        /// the switch, steeply to <paramref name="after"/> of the way around the switch (<paramref name="width"/> = half length as a share of the morph), then slowly the rest. With <paramref name="startAfterBeats"/> the number keeps its start value for that many beats after the start of the morph.
        /// </summary>
        public DirectorTimeline Pull(string path, float before = 0.3f, float after = 0.7f, float width = 0.05f, float startAfterBeats = 0f)
        {
            if (Events.Count > 0 && Events[Events.Count - 1] is MorphEvent morph && morph.Duration > 0.01f)
                morph.Pulls.Add(new MorphPull { Path = path, Before = before, After = after, Width = width, StartAt = UnityEngine.Mathf.Clamp01(startAfterBeats / morph.Duration) });
            else
                UnityEngine.Debug.LogWarning("[Director] Pull " + path + " needs a Morph right before it and was skipped.");
            return this;
        }

        /// <summary>For the morph just added: the zoom of the main pattern and of layers 2 and 3 move with <see cref="Pull"/> (small move first, strong one at the switch, slow rest).</summary>
        public DirectorTimeline PullPatternZoom(float before = 0.3f, float after = 0.7f, float width = 0.05f, float startAfterBeats = 0f)
        {
            return Pull("PerlinZoom", before, after, width, startAfterBeats).Pull("NoiseLayer2.Zoom", before, after, width, startAfterBeats).Pull("NoiseLayer3.Zoom", before, after, width, startAfterBeats);
        }

        /// <summary>
        /// For the morph just added: a number keeps the value it had at the start until <paramref name="startShare"/> of the morph (default: the switch) and only then
        /// glides to the value of the preset, reaching it at the end. <c>.LateGlide("Shapes[0].Zoom")</c> keeps the audio shape zoom, so shapes do not shrink while the first half runs.
        /// </summary>
        public DirectorTimeline LateGlide(string path, float startShare = -1f, bool smooth = false)
        {
            if (Events.Count > 0 && Events[Events.Count - 1] is MorphEvent morph)
                morph.LateGlides.Add(new MorphLateGlide { Path = path, StartAt = startShare < 0f ? morph.SwitchAt : startShare, Smooth = smooth });
            else
                UnityEngine.Debug.LogWarning("[Director] LateGlide " + path + " needs a Morph right before it and was skipped.");
            return this;
        }

        /// <summary>
        /// For the morph just added: the zoom of the main pattern and of layers 2 and 3 keep their start value for <paramref name="afterBeats"/> beats and then glide
        /// smoothly (eased in and out) to the zoom of the new preset over the rest of the morph, arriving at its end.
        /// </summary>
        public DirectorTimeline LateGlidePatternZoom(float afterBeats)
        {
            if (Events.Count > 0 && Events[Events.Count - 1] is MorphEvent morph && morph.Duration > 0.01f)
            {
                float share = DirectorEvent.Apply(morph.Ease, afterBeats / morph.Duration);   // Tick gets the eased progress, so the start in beats is eased too
                LateGlide("PerlinZoom", share, true).LateGlide("NoiseLayer2.Zoom", share, true).LateGlide("NoiseLayer3.Zoom", share, true);
            }
            else
                UnityEngine.Debug.LogWarning("[Director] LateGlidePatternZoom needs a Morph right before it and was skipped.");
            return this;
        }

        /// <summary>
        /// For the morph just added: the mesh morph shape (twist, flag, pillar...) starts to change <paramref name="afterBeats"/> after the start of the morph
        /// instead of at the switch, and glides over <paramref name="blendSeconds"/> seconds (the generator's own glide, default 1 s). Everything else still switches at the middle.
        /// </summary>
        public DirectorTimeline ShapeStart(float afterBeats, float blendSeconds)
        {
            if (Events.Count > 0 && Events[Events.Count - 1] is MorphEvent morph && morph.Duration > 0.01f)
            {
                morph.ShapeAt = UnityEngine.Mathf.Clamp01(afterBeats / morph.Duration);
                morph.ShapeBlendSeconds = blendSeconds;
            }
            else
                UnityEngine.Debug.LogWarning("[Director] ShapeStart needs a Morph right before it and was skipped.");
            return this;
        }

        /// <summary>
        /// For the morph just added: the first audio shape changes to <paramref name="shape"/> at once when the morph starts (not at the switch), e.g. Circle to Square
        /// so the undrawn corners appear while the mesh morph hides it, but the bass stays in the middle. At the switch the shape of the target preset follows.
        /// </summary>
        public DirectorTimeline StartShape(TerrainShape.AudioShape shape)
        {
            if (Events.Count > 0 && Events[Events.Count - 1] is MorphEvent morph)
                morph.StartShape = (int)shape;
            else
                UnityEngine.Debug.LogWarning("[Director] StartShape needs a Morph right before it and was skipped.");
            return this;
        }

        /// <summary>Sets one value at once: <c>Set(40, "NoisePattern", 12)</c>, <c>Set(40, "NoiseLayer2.Enabled", true)</c>.</summary>
        public DirectorTimeline Set(float beat, string path, object value) { return Add(new SetEvent(path, value), beat, 0f); }

        /// <summary>Switches the main pattern of the terrain at once: <c>Pattern(42.5f, TerrainNoise.Pattern.Checker)</c>.</summary>
        public DirectorTimeline Pattern(float beat, TerrainNoise.Pattern pattern) { return Set(beat, "NoisePattern", (int)pattern); }

        /// <summary>Switches the color palette at once (name as in the palette files, e.g. "Hero-Video Spacewarp Live"), with the look that belongs to it.</summary>
        public DirectorTimeline Palette(float beat, string palette) { return Add(new PaletteEvent(palette), beat, 0f); }

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

        /// <summary>Fades the music to a volume (0 = mute, 1 = full), e.g. <c>Volume(8, 8, 1, Ease.In)</c> after <see cref="StartMute"/>.</summary>
        public DirectorTimeline Volume(float beat, float beats, float to, Ease ease = Ease.Smooth)
        {
            return Add(new VolumeEvent(to), beat, beats, ease);
        }

        /// <summary>Camera to a pose: yaw and pitch in degrees, scale = distance as a multiple of the start distance (below 1 = closer).</summary>
        public DirectorTimeline Camera(float beat, float beats, float yaw, float pitch, float scale, Ease ease = Ease.Smooth)
        {
            return Add(new CameraEvent(yaw, pitch, scale), beat, beats, ease);
        }
        #endregion


        /// <summary>
        /// The beats the jump keys (F6 / F7) go to: the start of the take (<see cref="StartBeat"/>) and <paramref name="leadBeats"/> before every change of the picture
        /// (Cut, Morph; the fade to black has none), so a transition can be watched again without playing the take from the start.
        /// </summary>
        public List<float> JumpPoints(float leadBeats = 4f)
        {
            List<float> points = new List<float> { StartBeat };
            float previousEnd = 0f;
            foreach (DirectorEvent e in Events)
            {
                bool change = e is CutEvent || e is MorphEvent;
                // not before the end of the change before it (plus half a beat), so a jump never lands inside the animation of the previous transition
                // and if that leaves less than a beat before the change (they follow each other directly), the change has no point of its own
                float beat = UnityEngine.Mathf.Max(e.Beat - leadBeats, change && previousEnd > 0f ? previousEnd + 0.5f : 0f);
                if (change && beat < e.Beat - 1f && beat > StartBeat + 0.5f && !points.Exists(p => UnityEngine.Mathf.Abs(p - beat) < 1f))
                    points.Add(beat);
                if (change)
                    previousEnd = e.Beat + e.Duration;
            }
            foreach (float extra in ExtraJumpPoints)
                if (!points.Exists(p => UnityEngine.Mathf.Abs(p - extra) < 0.5f))
                    points.Add(extra);
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
