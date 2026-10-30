using UnityEngine;

namespace Gavi.VideoDirector
{
    public enum Ease { Linear, Smooth, In, Out }

    /// <summary>What events can reach. The runner owns one of these.</summary>
    public class DirectorContext
    {
        public DirectorFade Fade;
        public DirectorCamera Camera;

        public TerrainSettingsPreset Current => TerrainSettingsManager.Instance.CurrentPreset;

        /// <summary>
        /// Shows the current preset in the menu and tells the terrain to take it over. Sliders pass their change on by themselves, but a dropdown that is
        /// set without a notification (the noise pattern) does not, so a single Set of the pattern stayed unseen by the terrain until a slider moved.
        /// </summary>
        public void ShowCurrent()
        {
            Current.ApplyPresetToUI();
            TerrainSettingsManager.Instance.MarkUIChanged();
        }

        /// <summary>The current state of the menu as a copy, to glide away from.</summary>
        public TerrainSettingsPreset Snapshot()
        {
            Current.ExtractPresetFromUI();
            TerrainSettingsPreset copy = new TerrainSettingsPreset();
            copy.CopyFromPreset(Current);
            copy.Name = Current.Name;
            return copy;
        }
    }

    /// <summary>One thing that happens at a beat. <see cref="Duration"/> in beats, 0 = at once.</summary>
    public abstract class DirectorEvent
    {
        public float Beat;
        public float Duration;
        public Ease Ease = Ease.Smooth;

        public abstract string Describe();
        /// <summary>Events that only support another one (single numbers set with a pattern) are left out of the F11 panel and the "last" text.</summary>
        public virtual bool Hidden => false;
        public virtual void Begin(DirectorContext context) { }
        /// <param name="t">Progress between 0 and 1 with the ease applied. The last call is always 1.</param>
        public virtual void Tick(DirectorContext context, float t) { }
        public virtual void End(DirectorContext context) { }

        /// <summary>Turns the progress of the event in time (0 to 1) into the value handed to Tick. Normally just the ease.</summary>
        public virtual float Evaluate(float progress) { return Apply(Ease, progress); }

        public static float Apply(Ease ease, float t)
        {
            t = Mathf.Clamp01(t);
            switch (ease)
            {
                case Ease.Smooth: return t * t * (3f - 2f * t);
                case Ease.In: return t * t;
                case Ease.Out: return 1f - (1f - t) * (1f - t);
                default: return t;
            }
        }
    }

    /// <summary>Loads a preset at once, like picking it in the menu.</summary>
    public class CutEvent : DirectorEvent
    {
        private readonly string _preset;
        public CutEvent(string preset) { _preset = preset; }
        public override string Describe() => "Cut to " + _preset;

        public override void Begin(DirectorContext context)
        {
            context.Current.CopyFromPreset(DirectorPresets.Load(_preset));
            context.ShowCurrent();
        }
    }

    /// <summary>A number that leaves its glide around the switch of a morph: it moves to <see cref="Peak"/> at the switch and back to its glide.</summary>
    public class MorphBump
    {
        public string Path;
        public float Peak;
        /// <summary>Half width of the bump as a share of the morph (0.3 = from 0.2 to 0.8 when the switch is at 0.5).</summary>
        public float Width;
        /// <summary>Only the way up counts: at the switch the number snaps back to the glide of the morph (the new preset's own value), it does not glide back.</summary>
        public bool RampOnly;
        /// <summary>Half width after the switch as a share of the morph; 0 or less = same as <see cref="Width"/>. A shorter one makes the way back faster than the way down.</summary>
        public float WidthAfter;
        /// <summary>0 = smooth bump. Above 0 the bump is a pointed dip: slow start, steep end at the switch, steep start after it, slow tail (the share to the power of this number).</summary>
        public float Sharpness;
    }

    /// <summary>A number that keeps its start value until a later point of the morph and only then glides to the value of the preset.</summary>
    public class MorphLateGlide
    {
        public string Path;
        /// <summary>Share of the morph at which the glide starts (0.5 = at the switch). It reaches the value of the preset at the end of the morph.</summary>
        public float StartAt;
        /// <summary>True = the glide eases in and out (smoothstep over the way), so it starts and ends without a kink instead of running straight.</summary>
        public bool Smooth;
    }

    /// <summary>
    /// A number that moves to the value of the preset in three parts: slowly up to <see cref="Before"/> of the way until shortly before the switch, steeply to
    /// <see cref="After"/> of the way around the switch, then slowly (decelerating) the rest until the end of the morph.
    /// </summary>
    public class MorphPull
    {
        public string Path;
        /// <summary>Share of the way to the value of the preset that is reached when the steep part starts.</summary>
        public float Before;
        /// <summary>Share of the way that is reached when the steep part ends.</summary>
        public float After;
        /// <summary>Half length of the steep part as a share of the morph (it is centred on the switch).</summary>
        public float Width;
        /// <summary>Share of the morph at which the number starts to move at all (0 = with the morph). Before it the number keeps its start value.</summary>
        public float StartAt;

        /// <summary>Share of the way (0 to 1) at the share <paramref name="t"/> of the morph.</summary>
        public float Progress(float t, float switchAt)
        {
            float start = Mathf.Max(0.01f, switchAt - Width);
            float end = Mathf.Min(0.99f, switchAt + Width);
            float first = Mathf.Min(StartAt, start - 0.01f);
            if (t < start)
                return t <= first ? 0f : Before * Mathf.Pow((t - first) / (start - first), 1.5f);
            if (t < end)
            {
                float k = (t - start) / (end - start);
                return Mathf.Lerp(Before, After, k * k * (3f - 2f * k));
            }
            float rest = Mathf.Clamp01((t - end) / (1f - end));
            return Mathf.Lerp(After, 1f, 1f - (1f - rest) * (1f - rest));
        }
    }

    /// <summary>A number that belongs to the pattern: it takes the value of the new preset at once at the switch, together with the pattern, instead of gliding.</summary>
    public class MorphSnap
    {
        public string Path;
        /// <summary>False = the number keeps its start value until the switch. True = it glides as usual before the switch (a decorative move towards the new value) and still snaps to the target at the switch.</summary>
        public bool GlideBefore;
    }

    /// <summary>
    /// Glides from the current setup into a preset. Fields that cannot glide (patterns, switches, shapes, the detail of the mesh) change at <see cref="SwitchAt"/>.
    /// <see cref="Bumps"/> hide that moment, e.g. the terrain height sinks to almost flat while the patterns change.
    /// </summary>
    public class MorphEvent : DirectorEvent
    {
        public float SwitchAt = 0.5f;
        /// <summary>Own switch point for the mesh morph shape (share of the morph, below 0 = it switches at <see cref="SwitchAt"/> like the rest). The generator then glides to the new shape over <see cref="ShapeBlendSeconds"/>.</summary>
        public float ShapeAt = -1f;
        public float ShapeBlendSeconds = TerrainGenerator.DefaultMorphBlendSeconds;
        /// <summary>
        /// Share of its normal time the part after the switch takes (1 = as long as the part before, 0.5 = half the time). The glide of the part before
        /// and the shape of the curve stay the same, only the way from the switch to the end is run through faster. <see cref="DirectorEvent.Duration"/> already
        /// holds the shortened length (set by <see cref="DirectorTimeline.AfterSwitchTime"/>).
        /// </summary>
        public float AfterTimeShare = 1f;
        /// <summary>Audio shape (<c>TerrainShape.AudioShape</c> as a number) the first shape takes at once when the morph starts, so the bands move to a shape that fits both ends before the shape of the preset comes at the switch. -1 = off.</summary>
        public int StartShape = -1;
        public readonly System.Collections.Generic.List<MorphBump> Bumps = new System.Collections.Generic.List<MorphBump>();
        public readonly System.Collections.Generic.List<MorphLateGlide> LateGlides = new System.Collections.Generic.List<MorphLateGlide>();
        public readonly System.Collections.Generic.List<MorphSnap> Snaps = new System.Collections.Generic.List<MorphSnap>();
        public readonly System.Collections.Generic.List<MorphPull> Pulls = new System.Collections.Generic.List<MorphPull>();

        private readonly string _preset;
        private TerrainSettingsPreset _from, _to, _scratch;

        public MorphEvent(string preset) { _preset = preset; }
        public override string Describe() => "Morph into " + _preset + " over " + Duration + " beats";

        /// <summary>Time to progress: the part before the switch keeps its pace, the part after it is run through in <see cref="AfterTimeShare"/> of the time.</summary>
        public override float Evaluate(float progress)
        {
            if (AfterTimeShare >= 0.999f)
                return base.Evaluate(progress);

            float switchProgress = SwitchAt / (SwitchAt + (1f - SwitchAt) * AfterTimeShare);
            progress = Mathf.Clamp01(progress);
            float raw = progress <= switchProgress
                ? progress / switchProgress * SwitchAt
                : SwitchAt + (progress - switchProgress) / (1f - switchProgress) * (1f - SwitchAt);
            return Apply(Ease, raw);
        }

        public override void Begin(DirectorContext context)
        {
            _from = context.Snapshot();
            if (StartShape >= 0 && _from.Shapes.Count > 0)
                _from.Shapes[0].Shape = StartShape;
            _to = DirectorPresets.Load(_preset);
            _scratch = new TerrainSettingsPreset();
        }

        public override void Tick(DirectorContext context, float t)
        {
            PresetBlender.Blend(_from, _to, _scratch, t, t >= SwitchAt);
            if (ShapeAt >= 0f)
            {
                _scratch.MorphShape = t >= ShapeAt ? _to.MorphShape : _from.MorphShape;
                _scratch.MorphBlendSeconds = ShapeBlendSeconds;
            }
            foreach (MorphSnap snap in Snaps)
            {
                if (t >= SwitchAt)
                    FieldPath.Set(_scratch, snap.Path, FieldPath.GetFloat(_to, snap.Path));
                else if (!snap.GlideBefore)
                    FieldPath.Set(_scratch, snap.Path, FieldPath.GetFloat(_from, snap.Path));
            }
            foreach (MorphPull pull in Pulls)
                FieldPath.Set(_scratch, pull.Path, Mathf.Lerp(FieldPath.GetFloat(_from, pull.Path), FieldPath.GetFloat(_to, pull.Path), pull.Progress(t, SwitchAt)));
            foreach (MorphLateGlide late in LateGlides)
            {
                float k = Mathf.Clamp01((t - late.StartAt) / Mathf.Max(0.01f, 1f - late.StartAt));
                if (late.Smooth)
                    k = k * k * (3f - 2f * k);
                FieldPath.Set(_scratch, late.Path, Mathf.Lerp(FieldPath.GetFloat(_from, late.Path), FieldPath.GetFloat(_to, late.Path), k));
            }
            foreach (MorphBump bump in Bumps)
            {
                bool after = t >= SwitchAt;
                float width = after && bump.WidthAfter > 0f ? bump.WidthAfter : bump.Width;
                float distance = bump.RampOnly && after ? float.PositiveInfinity : Mathf.Abs(t - SwitchAt);
                float k = Mathf.Clamp01(1f - distance / Mathf.Max(0.01f, width));
                k = bump.Sharpness > 0f ? Mathf.Pow(k, bump.Sharpness) : k * k * (3f - 2f * k);
                FieldPath.Set(_scratch, bump.Path, Mathf.Lerp(FieldPath.GetFloat(_scratch, bump.Path), bump.Peak, k));
            }
            context.Current.CopyFromPreset(_scratch);
            context.ShowCurrent();
        }
    }

    /// <summary>Sets one value of the preset at once: a switch, a pattern number, a number. Path as in <see cref="FieldPath"/>.</summary>
    public class SetEvent : DirectorEvent
    {
        private readonly string _path;
        private readonly object _value;
        public SetEvent(string path, object value) { _path = path; _value = value; }
        public override string Describe() => IsMainPattern ? "Noise: " + ValueText() : "Set " + _path + " = " + ValueText();
        public override bool Hidden => !IsMainPattern;

        /// <summary>The switch of the main pattern (shown as "Noise: name" in the F11 panel).</summary>
        public bool IsMainPattern => _path == "NoisePattern";

        /// <summary>A pattern number is shown with the name of the pattern.</summary>
        private string ValueText()
        {
            if (_path.EndsWith("Pattern") && _value is int index && index >= 0 && index < TerrainNoise.Names.Length)
                return TerrainNoise.Names[index];
            return _value.ToString();
        }

        public override void Begin(DirectorContext context)
        {
            context.Snapshot();
            FieldPath.Set(context.Current, _path, _value);
            context.ShowCurrent();
        }
    }

    /// <summary>
    /// Switches to another color palette at once, like picking it in the dropdown of the color menu: the colors and the look that belong to the palette
    /// (intensity, bloom, tilt, floor share, shift, hold, plateau, relief, hue balance) are taken over. Looked up by name in the palette files.
    /// </summary>
    public class PaletteEvent : DirectorEvent
    {
        private readonly string _palette;
        public PaletteEvent(string palette) { _palette = palette; }
        public override string Describe() => "Palette: " + _palette;

        public override void Begin(DirectorContext context)
        {
            TerrainColorPalette palette = TerrainColorPaletteStore.LoadAll().Find(p => p.Name == _palette);
            if (palette == null)
                throw new System.IO.FileNotFoundException("Video Director: no color palette named '" + _palette + "'");

            context.Snapshot();
            TerrainSettingsPreset preset = context.Current;
            preset.ColorPaletteName = palette.Name;
            preset.ColorValues = new System.Collections.Generic.List<Vector3>(palette.Colors);
            preset.ColorOverallIntensity = palette.OverallIntensity;
            preset.BloomThreshold = palette.BloomThreshold;
            preset.BloomIntensity = palette.BloomIntensity;
            preset.BloomScatter = palette.BloomScatter;
            preset.TiltDirection = palette.TiltDirection;
            preset.FloorShare = palette.FloorShare;
            preset.ColorShift = palette.ColorShift;
            preset.ColorHold = palette.ColorHold;
            preset.PlateauTilt = palette.PlateauTilt;
            preset.ReliefStrength = palette.ReliefStrength;
            preset.HueBalance = palette.HueBalance;
            context.ShowCurrent();
        }
    }

    /// <summary>Glides one number of the preset (a slider) to a value.</summary>
    public class GlideEvent : DirectorEvent
    {
        private readonly string _path;
        private readonly float _to;
        private float _from;

        public GlideEvent(string path, float to) { _path = path; _to = to; }
        public override string Describe() => "Glide " + _path + " to " + _to + " over " + Duration + " beats";

        public override void Begin(DirectorContext context)
        {
            context.Snapshot();
            _from = FieldPath.GetFloat(context.Current, _path);
        }

        public override void Tick(DirectorContext context, float t)
        {
            FieldPath.Set(context.Current, _path, Mathf.Lerp(_from, _to, t));
            context.ShowCurrent();
        }
    }

    /// <summary>Opens or closes the settings menu on the right.</summary>
    public class MenuEvent : DirectorEvent
    {
        private readonly bool _open;
        public MenuEvent(bool open) { _open = open; }
        public override string Describe() => _open ? "Open menu" : "Close menu";

        public override void Begin(DirectorContext context)
        {
            TerrainViewMenuUI menu = TerrainViewMenuUI.Instance;
            if (menu == null || menu.MenuIsActive == _open)
                return;
            if (_open)
                menu.ToggleMenu();
            else
                menu.CloseMenu();
        }
    }

    /// <summary>Fades the black layer to a value (1 = black, 0 = clear).</summary>
    public class FadeEvent : DirectorEvent
    {
        private readonly float _to;
        private float _from;
        public FadeEvent(float to) { _to = to; }
        public float To => _to;
        public override string Describe() => "Fade to " + (_to >= 0.5f ? "black" : "clear") + " over " + Duration + " beats";

        public override void Begin(DirectorContext context) { _from = context.Fade.Alpha; }
        public override void Tick(DirectorContext context, float t) { context.Fade.Alpha = Mathf.Lerp(_from, _to, t); }
    }

    /// <summary>
    /// Fades the volume of the whole output (<c>AudioListener.volume</c>, 0 = mute, 1 = full). The volume slider of the player and the spectrum
    /// that drives the picture are not touched; the Runner puts it back to 1 when the take ends.
    /// </summary>
    public class VolumeEvent : DirectorEvent
    {
        private readonly float _to;
        private float _from;
        public VolumeEvent(float to) { _to = to; }
        public override string Describe() => "Volume to " + _to.ToString("0.00") + " over " + Duration + " beats";

        public override void Begin(DirectorContext context) { _from = AudioListener.volume; }
        public override void Tick(DirectorContext context, float t) { AudioListener.volume = Mathf.Lerp(_from, _to, t); }
    }

    /// <summary>Moves the camera to a pose, see <see cref="DirectorCamera.Pose"/>: yaw and pitch in degrees, scale = distance as a multiple of the start distance.</summary>
    public class CameraEvent : DirectorEvent
    {
        private readonly DirectorCamera.Pose _to;
        private DirectorCamera.Pose _from;

        public CameraEvent(float yaw, float pitch, float scale) { _to = new DirectorCamera.Pose { Yaw = yaw, Pitch = pitch, Scale = scale }; }
        public override string Describe() => "Camera yaw " + _to.Yaw + ", pitch " + _to.Pitch + ", scale " + _to.Scale + " over " + Duration + " beats";

        public override void Begin(DirectorContext context) { _from = context.Camera.Read(); }

        public override void Tick(DirectorContext context, float t)
        {
            context.Camera.Write(new DirectorCamera.Pose
            {
                Yaw = Mathf.Lerp(_from.Yaw, _to.Yaw, t),
                Pitch = Mathf.Lerp(_from.Pitch, _to.Pitch, t),
                Scale = Mathf.Lerp(_from.Scale, _to.Scale, t)
            });
        }
    }

    /// <summary>Pauses the song like the space key of the player does, so the last picture stands still. The take does not end by itself afterwards (the beat no longer moves), F10 ends it.</summary>
    public class PauseEvent : DirectorEvent
    {
        public override string Describe() => "Pause";
        public override void Begin(DirectorContext context) { Gavi.Audio.AudioPlayer.Instance?.Pause(); }
    }
}
