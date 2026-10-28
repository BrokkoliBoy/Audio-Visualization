using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi.VideoDirector
{
    /// <summary>
    /// The info panel of the Director (F11): the events of the take as bars on a time axis with a countdown, and one small graph per setting
    /// that matters at a transition. All of them share the time axis, so a jump in a graph lines up with the event that causes it.
    /// The history of the last take stays on screen after the take ended.
    /// </summary>
    public class DirectorOverlay
    {
        private struct Metric
        {
            public string Label;
            public string Path;
            public Color Color;
            /// <summary>Whole numbers (patterns, mesh size): shown as an integer.</summary>
            public bool Whole;

            public Metric(string label, string path, Color color, bool whole)
            {
                Label = label;
                Path = path;
                Color = color;
                Whole = whole;
            }
        }

        private static readonly Metric[] Metrics =
        {
            new Metric("Pattern", "NoisePattern", new Color(1f, 0.55f, 0.3f), true),
            new Metric("Layer 2 pattern", "NoiseLayer2.Pattern", new Color(1f, 0.55f, 0.3f), true),
            new Metric("Layer 3 pattern", "NoiseLayer3.Pattern", new Color(1f, 0.55f, 0.3f), true),
            new Metric("Morph shape", "MorphShape", new Color(1f, 0.55f, 0.3f), true),
            new Metric("Morph amount", "MorphAmount", new Color(0.45f, 0.8f, 1f), false),
            new Metric("Mesh size", "FaceCountX", new Color(1f, 0.55f, 0.3f), true),
            new Metric("Terrain height", "TerrainHeight", new Color(0.45f, 0.8f, 1f), false),
            new Metric("Pattern speed Y", "PerlinSpeedY", new Color(0.45f, 0.8f, 1f), false),
            new Metric("Plateau tilt", "PlateauTilt", new Color(0.45f, 0.8f, 1f), false),
            new Metric("Wave fade", "WaveFade", new Color(0.45f, 0.8f, 1f), false),
            new Metric("Headroom (brightness)", "ColorHeadroom", new Color(0.7f, 0.9f, 0.5f), false),
            new Metric("Hue of color 1", "ColorValues[0].x", new Color(0.7f, 0.9f, 0.5f), false)
        };

        private const float PanelWidth = 960f;
        private const float LabelWidth = 250f;
        private const float GraphWidth = 500f;
        private const float ValueWidth = 170f;
        private const float Pad = 12f;
        private const float EventRowHeight = 15f;
        private const float GraphRowHeight = 21f;
        private const float RecordInterval = 0.05f;

        private readonly List<float> _beats = new List<float>();
        private readonly List<float>[] _values = new List<float>[Metrics.Length];
        // The range of each graph is remembered from take to take, so the scale does not jump while a take runs.
        private readonly float[] _min = new float[Metrics.Length];
        private readonly float[] _max = new float[Metrics.Length];
        private float _lastRecordTime = float.NegativeInfinity;
        private float _endBeat = 68f;

        private GUIStyle _text;

        public DirectorOverlay()
        {
            for (int i = 0; i < Metrics.Length; i++)
            {
                _values[i] = new List<float>();
                _min[i] = float.PositiveInfinity;
                _max[i] = float.NegativeInfinity;
            }
        }

        public void StartTake(float endBeat)
        {
            _beats.Clear();
            foreach (List<float> list in _values)
                list.Clear();
            _endBeat = Mathf.Max(1f, endBeat);
            _lastRecordTime = float.NegativeInfinity;
        }

        /// <summary>Remembers the values at this beat, about twenty times a second.</summary>
        public void Record(float beat, TerrainSettingsPreset preset, bool force = false)
        {
            if (preset == null || (!force && Time.unscaledTime - _lastRecordTime < RecordInterval))
                return;
            _lastRecordTime = Time.unscaledTime;
            _beats.Add(beat);
            for (int i = 0; i < Metrics.Length; i++)
            {
                float value = Read(preset, Metrics[i].Path);
                _values[i].Add(value);
                if (!float.IsNaN(value))
                {
                    _min[i] = Mathf.Min(_min[i], value);
                    _max[i] = Mathf.Max(_max[i], value);
                }
            }
        }

        private static float Read(TerrainSettingsPreset preset, string path)
        {
            try
            {
                return FieldPath.GetFloat(preset, path);
            }
            catch (Exception)
            {
                return float.NaN;
            }
        }


        #region Draw
        public void Draw(string header, DirectorTimeline timeline, float beat, float secondsPerBeat, bool hasBeat, List<float> jumpBeats = null)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            EnsureStyles();

            float scale = Mathf.Max(0.75f, Screen.height / 1080f);
            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            int eventCount = timeline != null ? timeline.Events.Count : 0;
            float height = Pad + 22f + (eventCount > 0 ? 18f + eventCount * EventRowHeight : 0f) + 18f + Metrics.Length * GraphRowHeight + 22f + Pad;
            // Next to the debug window when it is open and there is room, so the two do not cover each other.
            float left = SharedPresetsWindow.RightEdge > 0f && SharedPresetsWindow.RightEdge + PanelWidth < Screen.width / scale ? SharedPresetsWindow.RightEdge : 10f;
            Rect panel = new Rect(left, 10f, PanelWidth, height);
            Fill(panel, new Color(0.04f, 0.05f, 0.07f, 0.82f));

            float x = panel.x + Pad;
            float graphX = x + LabelWidth;
            float y = panel.y + Pad;
            Label(new Rect(x, y, PanelWidth - 2f * Pad, 20f), header, _text, Color.white);
            y += 22f;

            float spb = Mathf.Max(0.0001f, secondsPerBeat);
            float timelineTop = y;
            if (eventCount > 0)
            {
                Label(new Rect(x, y, LabelWidth, 16f), "EVENTS", _text, new Color(1f, 1f, 1f, 0.5f));
                y += 18f;
                foreach (DirectorEvent e in timeline.Events)
                {
                    DrawEventRow(e, x, graphX, y, beat, spb, hasBeat);
                    y += EventRowHeight;
                }
            }

            Label(new Rect(x, y, LabelWidth, 16f), "SETTINGS OVER TIME", _text, new Color(1f, 1f, 1f, 0.5f));
            DrawTimeScale(graphX, y, spb);
            y += 18f;
            float graphsTop = y;
            for (int i = 0; i < Metrics.Length; i++)
            {
                DrawGraphRow(i, x, graphX, y);
                y += GraphRowHeight;
            }

            // yellow lines = where F6 / F7 jump to
            if (jumpBeats != null)
            {
                foreach (float jump in jumpBeats)
                    Fill(new Rect(graphX + Mathf.Clamp01(jump / _endBeat) * GraphWidth - 1f, timelineTop, 2f, y - timelineTop), new Color(1f, 0.85f, 0.2f, 0.7f));
            }

            if (hasBeat)
            {
                float headX = graphX + Mathf.Clamp01(beat / _endBeat) * GraphWidth;
                Fill(new Rect(headX - 1f, timelineTop, 2f, y - timelineTop), new Color(1f, 0.25f, 0.25f, 0.95f));
            }

            Label(new Rect(x, y + 4f, PanelWidth - 2f * Pad, 16f), "F9 start   F6 / F7 jump (yellow lines)   F8 marker   F10 abort   F11 hide      orange = jumps (whole numbers), blue/green = glides", _text, new Color(1f, 1f, 1f, 0.55f));

            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }

        private void DrawEventRow(DirectorEvent e, float x, float graphX, float y, float beat, float spb, bool hasBeat)
        {
            float startX = graphX + Mathf.Clamp01(e.Beat / _endBeat) * GraphWidth;
            float endX = graphX + Mathf.Clamp01((e.Beat + e.Duration) / _endBeat) * GraphWidth;
            Color color = EventColor(e);
            bool running = hasBeat && beat >= e.Beat && beat < e.Beat + e.Duration;
            bool done = hasBeat && beat >= e.Beat + e.Duration;

            Fill(new Rect(graphX, y + 1f, GraphWidth, EventRowHeight - 2f), new Color(1f, 1f, 1f, 0.05f));
            string status;
            if (e.Duration <= 0f)
            {
                Fill(new Rect(startX - 1.5f, y, 3f, EventRowHeight), color);
                status = !hasBeat ? "at " + Seconds(e.Beat * spb) : done ? "done" : "in " + Seconds((e.Beat - beat) * spb);
            }
            else
            {
                Fill(new Rect(startX, y + 2f, Mathf.Max(3f, endX - startX), EventRowHeight - 4f), new Color(color.r, color.g, color.b, 0.4f));
                if (hasBeat)
                {
                    float progress = Mathf.Clamp01((beat - e.Beat) / e.Duration);
                    Fill(new Rect(startX, y + 2f, Mathf.Max(0f, (endX - startX) * progress), EventRowHeight - 4f), color);
                }

                status = !hasBeat ? Seconds(e.Beat * spb) + " + " + Seconds(e.Duration * spb) : done ? "done" : running ? Running(e, beat, spb) : "in " + Seconds((e.Beat - beat) * spb);
                if (e is MorphEvent morph)
                {
                    float switchBeat = e.Beat + SwitchProgress(morph) * e.Duration;
                    float switchX = graphX + Mathf.Clamp01(switchBeat / _endBeat) * GraphWidth;
                    Fill(new Rect(switchX - 1f, y, 2f, EventRowHeight), Color.white);
                }
            }

            Label(new Rect(x, y - 1f, LabelWidth - 4f, EventRowHeight), e.Describe(), _text, running ? Color.white : new Color(1f, 1f, 1f, 0.7f));
            Label(new Rect(graphX + GraphWidth + 8f, y - 1f, ValueWidth, EventRowHeight), status, _text, running ? Color.white : new Color(1f, 1f, 1f, 0.6f));
        }

        /// <summary>The text of a running event: how far it is, and for a morph the time until the jump of the whole-number values.</summary>
        private static string Running(DirectorEvent e, float beat, float spb)
        {
            float progress = Mathf.Clamp01((beat - e.Beat) / e.Duration);
            string text = Mathf.RoundToInt(progress * 100f) + " %";
            if (e is MorphEvent morph)
            {
                float switchBeat = e.Beat + SwitchProgress(morph) * e.Duration;
                text += beat < switchBeat ? "  jump in " + Seconds((switchBeat - beat) * spb) : "  jumped";
            }
            return text;
        }

        /// <summary>The morph hands the eased progress to the blend, so the jump comes when the eased value reaches SwitchAt.</summary>
        private static float SwitchProgress(MorphEvent morph)
        {
            float low = 0f, high = 1f;
            for (int i = 0; i < 20; i++)
            {
                float mid = (low + high) * 0.5f;
                if (DirectorEvent.Apply(morph.Ease, mid) >= morph.SwitchAt)
                    high = mid;
                else
                    low = mid;
            }
            return high;
        }

        private void DrawTimeScale(float graphX, float y, float spb)
        {
            for (int b = 0; b <= _endBeat; b += 8)
            {
                float tx = graphX + b / _endBeat * GraphWidth;
                Label(new Rect(tx - 20f, y, 60f, 16f), Seconds(b * spb), _text, new Color(1f, 1f, 1f, 0.45f));
            }
        }

        private void DrawGraphRow(int index, float x, float graphX, float y)
        {
            Metric metric = Metrics[index];
            Rect area = new Rect(graphX, y + 1f, GraphWidth, GraphRowHeight - 2f);
            Fill(area, new Color(1f, 1f, 1f, 0.06f));
            for (int b = 0; b <= _endBeat; b += 4)
                Fill(new Rect(graphX + b / _endBeat * GraphWidth, area.y, 1f, area.height), new Color(1f, 1f, 1f, b % 8 == 0 ? 0.12f : 0.05f));

            List<float> values = _values[index];
            float min = _min[index], max = _max[index];
            bool hasRange = !float.IsInfinity(min);
            if (hasRange && max - min < 0.0001f)
            {
                min -= 0.5f;
                max += 0.5f;
            }

            // A line with a vertical piece wherever the value changes, so a jump shows as a step and a glide as a slope.
            float previousY = float.NaN;
            for (int i = 0; i < values.Count && hasRange; i++)
            {
                if (float.IsNaN(values[i]))
                    continue;
                float sx = graphX + Mathf.Clamp01(_beats[i] / _endBeat) * GraphWidth;
                float next = i + 1 < values.Count ? graphX + Mathf.Clamp01(_beats[i + 1] / _endBeat) * GraphWidth : sx + 1f;
                float width = Mathf.Max(1f, next - sx);
                float level = Mathf.Clamp01((values[i] - min) / (max - min));
                float lineY = area.yMax - 3f - level * (area.height - 6f);
                if (!float.IsNaN(previousY) && Mathf.Abs(lineY - previousY) > 1f)
                    Fill(new Rect(sx, Mathf.Min(lineY, previousY), 2f, Mathf.Abs(lineY - previousY) + 2f), metric.Color);
                Fill(new Rect(sx, lineY, width, 2f), metric.Color);
                previousY = lineY;
            }

            string current = values.Count > 0 && !float.IsNaN(values[values.Count - 1]) ? Format(values[values.Count - 1], metric.Whole) : "";
            string range = hasRange ? "  (" + Format(_min[index], metric.Whole) + " to " + Format(_max[index], metric.Whole) + ")" : "";
            Label(new Rect(x, y, LabelWidth - 4f, GraphRowHeight), metric.Label, _text, Color.white);
            Label(new Rect(graphX + GraphWidth + 8f, y, ValueWidth, GraphRowHeight), current + range, _text, Color.white);
        }
        #endregion


        #region Helpers
        private static Color EventColor(DirectorEvent e)
        {
            if (e is MorphEvent) return new Color(1f, 0.6f, 0.2f);
            if (e is CutEvent) return new Color(1f, 0.35f, 0.45f);
            if (e is FadeEvent) return new Color(0.7f, 0.7f, 0.75f);
            if (e is MenuEvent) return new Color(0.4f, 0.85f, 0.9f);
            if (e is CameraEvent) return new Color(0.6f, 0.9f, 0.5f);
            return new Color(0.95f, 0.85f, 0.35f);
        }

        private static string Format(float value, bool whole) => whole ? Mathf.RoundToInt(value).ToString() : value.ToString("0.00");

        private static string Seconds(float seconds) => seconds.ToString("0.0") + " s";

        private static void Fill(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
        }

        private void Label(Rect rect, string text, GUIStyle style, Color color)
        {
            GUI.color = color;
            GUI.Label(rect, text, style);
        }

        private void EnsureStyles()
        {
            if (_text != null)
                return;
            _text = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = false,
                clipping = TextClipping.Clip,
                padding = new RectOffset(0, 0, 0, 0),
                alignment = TextAnchor.MiddleLeft
            };
            _text.normal.textColor = Color.white;
        }
        #endregion
    }
}
