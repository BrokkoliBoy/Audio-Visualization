using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gavi
{
    /// <summary>
    /// Asks before the app closes while the terrain settings differ from the loaded or saved profile.
    /// Hooks Application.wantsToQuit, so the Quit button, Alt+F4 and the window close button are all covered.
    /// The dialog builds itself in code and appears only when it is needed.
    /// </summary>
    public class QuitConfirmation : MonoBehaviour
    {
        private static QuitConfirmation _instance;
        private static bool _allowQuit;
        private static int _closedFrame = -1;

        /// <summary>True while the dialog is open (and in the frame it closed), so hotkeys such as Esc do not also act on the menu behind it.</summary>
        public static bool BlocksHotkeys => (_instance != null && _instance._root.activeSelf) || _closedFrame == Time.frameCount;

        private GameObject _root;
        private TextMeshProUGUI _message;

        private static readonly Color PanelColor = new Color(0.08f, 0.09f, 0.11f, 0.98f);
        private static readonly Color ButtonColor = new Color(0.2f, 0.22f, 0.27f, 1f);
        private static readonly Color DangerColor = new Color(0.55f, 0.18f, 0.16f, 1f);


        #region Hook
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            _instance = null;
            _allowQuit = false;
            _closedFrame = -1;
            Application.wantsToQuit -= OnWantsToQuit;
            Application.wantsToQuit += OnWantsToQuit;
        }

        private static bool OnWantsToQuit()
        {
            if (_allowQuit)
                return true;

            TerrainGroupUIProfiles profiles = TerrainGroupUIProfiles.Instance;
            if (profiles == null || !profiles.HasUnsavedChanges)
                return true;

            if (_instance == null)
            {
                _instance = new GameObject("QuitConfirmation").AddComponent<QuitConfirmation>();
                _instance.Build();
            }
            _instance.Open(profiles.BaselineProfileName);
            return false;
        }
        #endregion


        #region Dialog
        private void Update()
        {
            if (_root.activeSelf && Input.GetKeyDown(KeyCode.Escape))
                Close();
        }

        private void Open(string profileName)
        {
            _message.text = string.IsNullOrEmpty(profileName)
                ? "The terrain settings have changed and are not saved in a profile."
                : "The terrain settings have changed since the profile \"" + profileName + "\" was loaded or saved.";
            _root.SetActive(true);
        }

        private void Close()
        {
            _root.SetActive(false);
            _closedFrame = Time.frameCount;
        }

        private void QuitAnyway()
        {
            _allowQuit = true;
            Application.Quit();
        }
        #endregion


        #region Build
        private void Build()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            // The blocker covers the screen so nothing behind the dialog can be clicked.
            _root = CreateChild("Blocker", transform);
            Stretch(_root.GetComponent<RectTransform>());
            _root.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            GameObject panel = CreateChild("Panel", _root.transform);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(620f, 260f);
            panel.AddComponent<Image>().color = PanelColor;

            CreateText("Title", panel.transform, "Unsaved changes", 32f, FontStyles.Bold, new Vector2(0f, 90f), new Vector2(560f, 50f));
            _message = CreateText("Message", panel.transform, "", 22f, FontStyles.Normal, new Vector2(0f, 20f), new Vector2(560f, 90f));
            _message.color = new Color(0.8f, 0.82f, 0.86f, 1f);

            CreateButton("Cancel", panel.transform, "Cancel", ButtonColor, new Vector2(-150f, -85f), Close);
            CreateButton("QuitAnyway", panel.transform, "Quit without saving", DangerColor, new Vector2(150f, -85f), QuitAnyway);

            _root.SetActive(false);
        }

        private static GameObject CreateChild(string childName, Transform parent)
        {
            GameObject child = new GameObject(childName, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static TextMeshProUGUI CreateText(string childName, Transform parent, string text, float size, FontStyles style, Vector2 position, Vector2 boxSize)
        {
            GameObject textObject = CreateChild(childName, parent);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = boxSize;
            TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        private static void CreateButton(string childName, Transform parent, string text, Color color, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObject = CreateChild(childName, parent);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(260f, 56f);
            Image image = buttonObject.AddComponent<Image>();
            image.color = color;
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            CreateText("Label", buttonObject.transform, text, 22f, FontStyles.Normal, Vector2.zero, rect.sizeDelta);
        }
        #endregion
    }
}
