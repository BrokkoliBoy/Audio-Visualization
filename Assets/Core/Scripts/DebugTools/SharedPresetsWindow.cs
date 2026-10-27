using System;
using UnityEngine;

namespace Gavi
{
    /// <summary>
    /// The debug window (F12). Only there in debug mode: in the Editor, in a development build, or after Ctrl+F12 in any build.
    /// <list type="bullet">
    /// <item>Preset files of the menu: a build uses its own files or those of the Unity project (see <see cref="ProjectPresets"/>).</item>
    /// <item>Live link: one program sends its settings whenever they change, the other receives them (a few times a second).</item>
    /// </list>
    /// </summary>
    public class SharedPresetsWindow : MonoBehaviour
    {
        private enum LiveMode { Off, Send, Receive }

        private const KeyCode WindowKey = KeyCode.F12;
        private const float PollSeconds = 0.25f;
        private const int WindowId = 7801;

        /// <summary>Right edge of the window in GUI units (0 when it is hidden), so the info panel can sit next to it instead of under it.</summary>
        public static float RightEdge { get; private set; }

        private RectTransform _blocker;
        private float _lastHeight = 10f;
        private float _scale = 1f;

        private readonly string _id = Guid.NewGuid().ToString("N");

        private bool _visible;
        private bool _placed;
        private Rect _rect;
        private GUISkin _skin;
        private string _status = "";

        private LiveMode _live = LiveMode.Off;
        private DateTime _liveStamp = DateTime.MinValue;
        private string _lastLiveJson = "";
        private float _nextPoll;

        private TerrainSettingsPreset Current => TerrainSettingsManager.Instance != null ? TerrainSettingsManager.Instance.CurrentPreset : null;


        private static SharedPresetsWindow _instance;

        /// <summary>Created from code when the scene has loaded, so no scene file holds it.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (_instance != null)
                return;
            GameObject window = new GameObject("Shared Presets Window");
            _instance = window.AddComponent<SharedPresetsWindow>();
        }

        #region Mono
        private void Start()
        {
            ProjectPresets.WriteEditorPath();
        }

        private void Update()
        {
            if (!UiPointer.IsTyping && Input.GetKeyDown(WindowKey))
            {
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    SharedPresets.ToggleDebug();
                else if (SharedPresets.DebugOn)
                    _visible = !_visible;
            }

            if (!SharedPresets.DebugOn)
                _visible = false;
            UpdateBlocker();
            if (!SharedPresets.DebugOn || _live == LiveMode.Off)
                return;

            if (Time.unscaledTime < _nextPoll || Current == null)
                return;
            _nextPoll = Time.unscaledTime + PollSeconds;
            if (_live == LiveMode.Send)
                SendLive();
            else
                ReceiveLive();
        }
        #endregion


        #region Live link
        private void SendLive()
        {
            Current.ExtractPresetFromUI();
            TerrainSettingsPreset copy = new TerrainSettingsPreset();
            copy.CopyFromPreset(Current);
            copy.Name = "Live";
            copy.FormatVersion = TerrainSettingsPreset.CurrentFormatVersion;
            string json = JsonUtility.ToJson(copy);
            if (json == _lastLiveJson)
                return;
            _lastLiveJson = json;
            try
            {
                SharedPresets.WriteLive(_id, json);
            }
            catch (Exception e)
            {
                _status = "Live send failed: " + e.Message;
            }
        }

        private void ReceiveLive()
        {
            if (!SharedPresets.ReadLive(out DateTime stamp, out string sender, out string json) || stamp == _liveStamp)
                return;
            _liveStamp = stamp;
            if (sender == _id || json == _lastLiveJson)
                return;
            TerrainSettingsPreset preset;
            try
            {
                preset = TerrainSettingsPresetManager.ParsePreset(json);
            }
            catch (ArgumentException)
            {
                return;
            }

            if (preset == null)
                return;
            _lastLiveJson = json;
            Current.CopyFromPreset(preset);
            Current.ApplyPresetToUI();
            _status = "Live setup received " + DateTime.Now.ToString("HH:mm:ss");
        }
        #endregion


        #region Window
        private void OnGUI()
        {
            if (!_visible || !SharedPresets.DebugOn)
                return;

            if (_skin == null)
                _skin = MakeSkin();
            GUISkin oldSkin = GUI.skin;
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.skin = _skin;
            float scale = Mathf.Max(0.75f, Screen.height / 1080f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            if (!_placed)
            {
                _rect = new Rect(10f, 10f, 400f, 10f);
                _placed = true;
            }

            _rect.height = 10f;
            _rect = GUILayout.Window(WindowId, _rect, DrawWindow, "Debug   (F12 hides)");
            RightEdge = _rect.xMax + 10f;
            _lastHeight = _rect.height;
            _scale = scale;
            GUI.matrix = oldMatrix;
            GUI.skin = oldSkin;
        }

        /// <summary>
        /// The camera ignores the mouse while the pointer is over a UI object, but knows nothing of this window (it is drawn by IMGUI). A transparent
        /// UI object over the window tells it, so a click or drag in the window no longer turns the terrain or the camera.
        /// </summary>
        private void UpdateBlocker()
        {
            bool show = _visible && SharedPresets.DebugOn && _placed;
            if (!show)
                RightEdge = 0f;
            if (_blocker == null)
            {
                if (!show)
                    return;
                GameObject canvasObject = new GameObject("Debug Window Blocker");
                canvasObject.transform.SetParent(transform, false);
                Canvas canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = short.MaxValue - 1;
                canvasObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                GameObject imageObject = new GameObject("Blocker");
                imageObject.transform.SetParent(canvasObject.transform, false);
                UnityEngine.UI.Image image = imageObject.AddComponent<UnityEngine.UI.Image>();
                image.color = new Color(0f, 0f, 0f, 0f);
                _blocker = (RectTransform)imageObject.transform;
                _blocker.anchorMin = _blocker.anchorMax = _blocker.pivot = Vector2.zero;
            }

            _blocker.gameObject.SetActive(show);
            if (!show)
                return;
            _blocker.sizeDelta = new Vector2(_rect.width, _lastHeight) * _scale;
            _blocker.anchoredPosition = new Vector2(_rect.x * _scale, Screen.height - (_rect.y + _lastHeight) * _scale);
        }

        private static GUISkin MakeSkin()
        {
            GUISkin skin = Instantiate(GUI.skin);
            foreach (GUIStyle style in new[] { skin.label, skin.button, skin.toggle, skin.window, skin.textField })
                style.fontSize = 13;
            return skin;
        }

        private void DrawWindow(int id)
        {
            DrawProjectFiles();

            GUILayout.Space(6f);
            GUILayout.Label("Live link between Editor and build");
            LiveMode mode = (LiveMode)GUILayout.Toolbar((int)_live, new[] { "Off", "Send my changes", "Receive" });
            if (mode != _live)
            {
                _live = mode;
                _lastLiveJson = "";
                _liveStamp = DateTime.MinValue;
            }

            if (_status.Length > 0)
                GUILayout.Label(_status);
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 22f));
        }

        /// <summary>The switch between the preset files of this build and those of the Unity project. The menu reads its list again at once.</summary>
        private void DrawProjectFiles()
        {
            GUILayout.Label("Preset files of the menu (Save, Load, Delete)");
            if (Application.isEditor)
            {
                GUILayout.Label("Editor: the menu already works in the project files.");
                return;
            }

            bool wanted = ProjectPresets.Wanted;
            int choice = GUILayout.Toolbar(wanted ? 1 : 0, new[] { "This build", "Unity project" });
            if ((choice == 1) != wanted)
            {
                wanted = choice == 1;
                ProjectPresets.SetWanted(wanted);
                if (TerrainGroupUIPresets.Instance != null)
                    TerrainGroupUIPresets.Instance.ReloadPresets();
                _status = "Preset list read again from the " + (ProjectPresets.Root != null ? "Unity project" : "files of this build");
            }

            string active = ProjectPresets.Root;
            if (wanted && active == null)
                GUILayout.Label("Unity project not found, using the files of this build. Build into the project folder, or press Play in the Editor once.");
            else
                GUILayout.Label(active != null ? "Now in the project: " + active : "Now in the files of this build.");
        }
        #endregion
    }
}
