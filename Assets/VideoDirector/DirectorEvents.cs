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

        /// <summary>Shows the current preset in the menu; the menu then passes it on to the terrain (as it does for every slider).</summary>
        public void ShowCurrent()
        {
            Current.ApplyPresetToUI();
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
        public virtual void Begin(DirectorContext context) { }
        /// <param name="t">Progress between 0 and 1 with the ease applied. The last call is always 1.</param>
        public virtual void Tick(DirectorContext context, float t) { }
        public virtual void End(DirectorContext context) { }

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
    }

    /// <summary>
    /// Glides from the current setup into a preset. Fields that cannot glide (patterns, switches, shapes, the detail of the mesh) change at <see cref="SwitchAt"/>.
    /// <see cref="Bumps"/> hide that moment, e.g. the terrain height sinks to almost flat while the patterns change.
    /// </summary>
    public class MorphEvent : DirectorEvent
    {
        public float SwitchAt = 0.5f;
        public readonly System.Collections.Generic.List<MorphBump> Bumps = new System.Collections.Generic.List<MorphBump>();

        private readonly string _preset;
        private TerrainSettingsPreset _from, _to, _scratch;

        public MorphEvent(string preset) { _preset = preset; }
        public override string Describe() => "Morph into " + _preset + " over " + Duration + " beats";

        public override void Begin(DirectorContext context)
        {
            _from = context.Snapshot();
            _to = DirectorPresets.Load(_preset);
            _scratch = new TerrainSettingsPreset();
        }

        public override void Tick(DirectorContext context, float t)
        {
            PresetBlender.Blend(_from, _to, _scratch, t, t >= SwitchAt);
            foreach (MorphBump bump in Bumps)
            {
                float k = Mathf.Clamp01(1f - Mathf.Abs(t - SwitchAt) / Mathf.Max(0.01f, bump.Width));
                k = k * k * (3f - 2f * k);
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
        public override string Describe() => "Set " + _path + " = " + _value;

        public override void Begin(DirectorContext context)
        {
            context.Snapshot();
            FieldPath.Set(context.Current, _path, _value);
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
}
