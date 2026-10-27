using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi
{
    /// <summary>
    /// The debug window (F12) for the shared space: save the current setup into the shared folder, load and delete shared presets, and keep the Editor
    /// and a build in step. Only there in debug mode: in the Editor, in a development build, or after Ctrl+F12 in any build.
    /// <list type="bullet">
    /// <item>Follow file: when the file of the loaded preset changes in the shared folder, the setup is applied again.</item>
    /// <item>Live link: one side sends its settings whenever they change, the other receives them (a few times a second).</item>
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
        private List<SharedPresets.Entry> _entries = new List<SharedPresets.Entry>();

        private bool _visible;
        private bool _placed;
        private Rect _rect;
        private Vector2 _scroll;
        private GUISkin _skin;
        private string _status = "";

        private LiveMode _live = LiveMode.Off;
        private bool _follow = true;
        private string _currentName = "";
        private string _lastBaseline;
        private DateTime _followStamp = DateTime.MinValue;
        private DateTime _liveStamp = DateTime.MinValue;
        private string _lastLiveJson = "";
        private float _nextPoll;
        private float _nextList;
        private string _confirmDelete = "";
        private float _confirmUntil;

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
        private IEnumerator Start()
        {
            ProjectPresets.WriteEditorPath();
            // The menu loads the preset of the last session a frame after the scene started; a shared version of that preset replaces it.
            // Not when the menu works in the project files: those are the truth then, an older shared file must not replace them.
            yield return new WaitForSecondsRealtime(1f);
            string baseline = TerrainGroupUIPresets.Instance != null ? TerrainGroupUIPresets.Instance.BaselinePresetName : null;
            if (!SharedPresets.DebugOn || ProjectPresets.Root != null || Current == null || string.IsNullOrEmpty(baseline))
                yield break;
            TerrainSettingsPreset shared = SharedPresets.TryLoad(baseline);
            if (shared != null)
            {
                Apply(shared);
                SetCurrent(baseline);
            }
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
            if (!SharedPresets.DebugOn)
                return;

            if (Time.unscaledTime < _nextPoll || Current == null)
                return;
            _nextPoll = Time.unscaledTime + PollSeconds;
            Poll();
        }
        #endregion


        #region Keeping in step
        private void Poll()
        {
            string baseline = TerrainGroupUIPresets.Instance != null ? TerrainGroupUIPresets.Instance.BaselinePresetName : null;
            if (baseline != _lastBaseline)
            {
                _lastBaseline = baseline;
                if (!string.IsNullOrEmpty(baseline))
                    SetCurrent(baseline);
            }

            if (_live == LiveMode.Send)
                SendLive();
            else if (_live == LiveMode.Receive)
                ReceiveLive();
            else if (_follow)
                FollowFile();
        }

        private void SetCurrent(string name)
        {
            _currentName = name;
            _followStamp = SharedPresets.StampOf(name);
        }

        private void FollowFile()
        {
            if (_currentName.Length == 0)
                return;
            DateTime stamp = SharedPresets.StampOf(_currentName);
            if (stamp == DateTime.MinValue || stamp == _followStamp)
                return;
            _followStamp = stamp;
            TerrainSettingsPreset preset = SharedPresets.TryLoad(_currentName);
            if (preset == null)
                return;
            Apply(preset);
            _status = "Applied '" + _currentName + "' (file changed) " + DateTime.Now.ToString("HH:mm:ss");
        }

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
            Apply(preset);
            _status = "Live setup received " + DateTime.Now.ToString("HH:mm:ss");
        }

        private void Apply(TerrainSettingsPreset preset)
        {
            Current.CopyFromPreset(preset);
            Current.ApplyPresetToUI();
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
            _rect = GUILayout.Window(WindowId, _rect, DrawWindow, "Debug: shared presets   (F12 hides)");
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
            GUILayout.Label("Folder: " + SharedPresets.Folder);
            if (GUILayout.Button("Open folder"))
                Application.OpenURL("file:///" + SharedPresets.Folder.Replace('\\', '/'));

            GUILayout.Space(6f);
            GUILayout.Label("Current preset: " + (_currentName.Length > 0 ? _currentName : "(none)"));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save to shared"))
                SaveShared(_currentName.Length > 0 ? _currentName : "Shared setup");
            if (GUILayout.Button("Save as copy"))
                SaveShared(NextFreeName(_currentName.Length > 0 ? _currentName : "Shared setup"));
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
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
            _follow = GUILayout.Toggle(_follow, "Follow the file of the current preset (when live is off)");

            GUILayout.Space(6f);
            GUILayout.Label("Shared presets");
            if (Time.unscaledTime >= _nextList)
            {
                _nextList = Time.unscaledTime + 1f;
                _entries = SharedPresets.List();
            }

            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(Mathf.Min(220f, 28f * Mathf.Max(1, _entries.Count) + 4f)));
            if (_entries.Count == 0)
                GUILayout.Label("Nothing saved yet.");
            foreach (SharedPresets.Entry entry in _entries)
                DrawEntry(entry);
            GUILayout.EndScrollView();

            if (_status.Length > 0)
                GUILayout.Label(_status);
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 22f));
        }

        /// <summary>The switch between the preset files of this build and those of the Unity project (applies at the next start).</summary>
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
            }

            string active = ProjectPresets.Root;
            if (wanted && active == null)
            {
                string found = ProjectPresets.Find();
                GUILayout.Label(found == null
                    ? "Unity project not found. Build into the project folder, or press Play in the Editor once. Applies after a restart."
                    : "Applies after a restart. Project: " + found);
            }
            else if (!wanted && active != null)
                GUILayout.Label("Applies after a restart. Right now: " + active);
            else
                GUILayout.Label(active != null ? "Now in the project: " + active : "Now in the files of this build.");
        }

        private void DrawEntry(SharedPresets.Entry entry)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(entry.Name + "   " + entry.Time.ToString("dd.MM. HH:mm"), GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Load", GUILayout.Width(55f)))
                LoadShared(entry.Name);
            bool confirming = _confirmDelete == entry.Name && Time.unscaledTime < _confirmUntil;
            if (GUILayout.Button(confirming ? "Sure?" : "Delete", GUILayout.Width(60f)))
            {
                if (confirming)
                {
                    SharedPresets.Delete(entry.Name);
                    _status = "Deleted '" + entry.Name + "'";
                    _nextList = 0f;
                    _confirmDelete = "";
                }
                else
                {
                    _confirmDelete = entry.Name;
                    _confirmUntil = Time.unscaledTime + 3f;
                }
            }
            GUILayout.EndHorizontal();
        }

        private void SaveShared(string name)
        {
            if (Current == null)
                return;
            Current.ExtractPresetFromUI();
            TerrainSettingsPreset preset = new TerrainSettingsPreset();
            preset.CopyFromPreset(Current);
            preset.Name = name;
            try
            {
                SharedPresets.Save(preset);
            }
            catch (Exception e)
            {
                _status = "Save failed: " + e.Message;
                return;
            }

            SetCurrent(name);
            _nextList = 0f;
            _status = "Saved '" + name + "' to the shared folder " + DateTime.Now.ToString("HH:mm:ss");
        }

        private void LoadShared(string name)
        {
            TerrainSettingsPreset preset = SharedPresets.TryLoad(name);
            if (preset == null || Current == null)
            {
                _status = "Could not read '" + name + "'";
                return;
            }

            Apply(preset);
            SetCurrent(name);
            _status = "Loaded '" + name + "'";
        }

        private static string NextFreeName(string baseName)
        {
            for (int number = 2; ; number++)
            {
                string candidate = baseName + " " + number;
                if (!SharedPresets.Exists(candidate))
                    return candidate;
            }
        }
        #endregion
    }
}
