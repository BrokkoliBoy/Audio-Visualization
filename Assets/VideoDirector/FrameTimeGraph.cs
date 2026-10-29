using System;
using UnityEngine;

namespace Gavi.VideoDirector
{
    /// <summary>
    /// Frame time graph for the debug mode: shows with the debug window (F12, same switch), bottom left. Every frame is recorded with its length, the graph shows the
    /// last <see cref="WindowSeconds"/> seconds as bars (height = frame time, a long frame is as wide as it lasted), so a dip shows how deep and how long it was.
    /// One small texture, redrawn once per frame while visible, nothing is recorded while it is hidden.
    /// </summary>
    public class FrameTimeGraph : MonoBehaviour
    {
        private const KeyCode WindowKey = KeyCode.F12;
        private const float WindowSeconds = 5f;
        private const int Width = 500;
        private const int Height = 100;
        private const float MaxMs = 50f;                    // top of the graph (20 fps), longer frames are cut off
        private const int Capacity = 4096;

        private static readonly Color32 Background = new Color32(0, 0, 0, 170);
        private static readonly Color32 GuideColor = new Color32(255, 255, 255, 70);
        private static readonly Color32 Good = new Color32(90, 220, 110, 255);      // up to 60 fps
        private static readonly Color32 Mid = new Color32(240, 200, 60, 255);       // up to 30 fps
        private static readonly Color32 Bad = new Color32(240, 70, 60, 255);

        private readonly float[] _times = new float[Capacity];
        private readonly float[] _lengths = new float[Capacity];
        private readonly float[] _columns = new float[Width];
        private readonly Color32[] _pixels = new Color32[Width * Height];
        private int _head;
        private int _count;
        private bool _visible;
        private Texture2D _texture;
        private string _text = "";
        private GUIStyle _style;

        /// <summary>Created from code when the scene has loaded, so no scene file holds it.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            new GameObject("Frame Time Graph").AddComponent<FrameTimeGraph>();
        }

        private void Update()
        {
            if (!UiPointer.IsTyping && Input.GetKeyDown(WindowKey) && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl) && SharedPresets.DebugOn)
                _visible = !_visible;
            if (!SharedPresets.DebugOn)
                _visible = false;
            if (!_visible)
            {
                _count = 0;
                return;
            }

            float now = Time.unscaledTime;
            _times[_head] = now;
            _lengths[_head] = Time.unscaledDeltaTime;
            _head = (_head + 1) % Capacity;
            _count = Mathf.Min(_count + 1, Capacity);
            Redraw(now);
        }

        private void Redraw(float now)
        {
            if (_texture == null)
            {
                _texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                _texture.hideFlags = HideFlags.HideAndDontSave;
            }

            Array.Clear(_columns, 0, Width);
            float worst = 0f;
            float sum = 0f;
            int frames = 0;
            for (int i = 0; i < _count; i++)
            {
                int index = (_head - 1 - i + Capacity) % Capacity;
                float end = _times[index];
                float length = _lengths[index];
                int last = Column(now - end);
                if (last < 0)
                    break;
                int first = Mathf.Max(0, Column(now - (end - length)));
                for (int c = first; c <= last; c++)
                    _columns[c] = Mathf.Max(_columns[c], length);
                worst = Mathf.Max(worst, length);
                sum += length;
                frames++;
            }

            for (int i = 0; i < _pixels.Length; i++)
                _pixels[i] = Background;
            DrawGuide(1000f / 60f);
            DrawGuide(1000f / 30f);
            for (int c = 0; c < Width; c++)
            {
                float ms = _columns[c] * 1000f;
                if (ms <= 0f)
                    continue;
                int bar = Mathf.Min(Height, Mathf.Max(1, Mathf.RoundToInt(ms / MaxMs * Height)));
                Color32 color = ms <= 1000f / 59f ? Good : ms <= 1000f / 29f ? Mid : Bad;
                for (int y = 0; y < bar; y++)
                    _pixels[y * Width + c] = color;
            }

            _texture.SetPixels32(_pixels);
            _texture.Apply(false);
            _text = frames > 0 ? string.Format("{0:0} fps   worst frame {1:0.0} ms ({2:0} fps)   last {3} s", frames / sum, worst * 1000f, 1f / worst, WindowSeconds) : "";
        }

        /// <summary>Column of a moment that lies this many seconds back (the newest is the last column), below 0 when it is out of the graph.</summary>
        private static int Column(float age) => Width - 1 - Mathf.FloorToInt(age / WindowSeconds * Width);

        private void DrawGuide(float ms)
        {
            int y = Mathf.RoundToInt(ms / MaxMs * Height);
            if (y < 0 || y >= Height)
                return;
            for (int x = 0; x < Width; x++)
                _pixels[y * Width + x] = GuideColor;
        }

        private void OnGUI()
        {
            if (!_visible || _texture == null)
                return;
            if (_style == null)
                _style = new GUIStyle(GUI.skin.label) { fontSize = 12, normal = { textColor = Color.white } };
            float scale = Mathf.Max(0.75f, Screen.height / 1080f);
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float top = Screen.height / scale - Height - 30f;
            GUI.DrawTexture(new Rect(10f, top + 20f, Width, Height), _texture);
            GUI.Label(new Rect(10f, top, Width, 20f), _text, _style);
            GUI.Label(new Rect(Width - 60f, top + 20f + Height - Height * (1000f / 60f) / MaxMs - 16f, 70f, 16f), "60 fps", _style);
            GUI.Label(new Rect(Width - 60f, top + 20f + Height - Height * (1000f / 30f) / MaxMs - 16f, 70f, 16f), "30 fps", _style);
            GUI.matrix = oldMatrix;
        }

        private void OnDestroy()
        {
            if (_texture != null)
                Destroy(_texture);
        }
    }
}
