using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Gavi.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gavi
{
    public class TerrainGroupUIPresets : TerrainGroupUI
    {
        public static TerrainGroupUIPresets Instance;
        
        [Header("- References -")]
        [SerializeField] private TMP_Dropdown _dropdownPreset;
        [SerializeField] private Button _buttonSave;
        [SerializeField] private Button _buttonLoad;
        [SerializeField] private GameObject _panelButtonsPresetControl;
        [SerializeField] private GameObject _labeledButtonRemove;
        [SerializeField] private Button _buttonAttemptRemove;
        [SerializeField] private Button _buttonConfirmRemove;
        [SerializeField] private Button _buttonRejectRemove;
        [SerializeField] private TMP_InputField _inputFieldNewPreset;
        [SerializeField] private Button _buttonNewPreset;
        [SerializeField] private Button _buttonResetTerrain;

        private const string DefaultPresetName = "Default";
        private const string LastPresetKey = "TerrainPresets.Last";

        private readonly List<TerrainSettingsPreset> _presets = new List<TerrainSettingsPreset>();

        // The terrain state at the last load or save, to tell whether the settings were changed since.
        private string _savedSnapshot;
        private string _baselineName;

        /// <summary>True when the terrain settings differ from the preset that was last loaded or saved.</summary>
        public bool HasUnsavedChanges => _savedSnapshot != null && JsonUtility.ToJson(CurrentPreset) != _savedSnapshot;
        /// <summary>Name of the preset that was last loaded or saved.</summary>
        public string BaselinePresetName => _baselineName;

        private TerrainSettingsPreset CurrentPreset => TerrainSettingsManager.Instance.CurrentPreset;
        private TerrainSettingsPreset ActivePreset => _dropdownPreset.value >= _presets.Count ? null : _presets[_dropdownPreset.value];

        
        #region Mono
        private void Awake()
        {
            _buttonSave.onClick.AddListener(OnPressedSave);
            _buttonLoad.onClick.AddListener(OnPressedLoad);
            _buttonAttemptRemove.onClick.AddListener(OnPressedAttemptRemove);
            _buttonConfirmRemove.onClick.AddListener(OnPressedConfirmRemove);
            _buttonRejectRemove.onClick.AddListener(OnPressedRejectRemove);
            _buttonNewPreset.onClick.AddListener(OnPressedNewPreset);
            _dropdownPreset.onValueChanged.AddListener(OnDropdownValueChanged);
            if (_buttonResetTerrain != null)
                _buttonResetTerrain.onClick.AddListener(OnPressedResetTerrain);

            if (Instance != null)
                Debugger.LogInstanceError(GetType());
            Instance = this;
            BuildShareRow();
        }

        private void Start()
        {
            UiGroupFold fold = GetComponent<UiGroupFold>();
            if (fold != null)
                fold.SetExtraOpenHeight(_shareRowHeight);
        }

        private void Update()
        {
            bool open = _dropdownPreset.IsExpanded;
            if (open && !_listWasOpen)
                BeginPreview();
            else if (!open && _listWasOpen)
                StartCoroutine(EndPreviewNextFrame());
            _listWasOpen = open;
        }

        private void OnDisable()
        {
            // A tab switch closes the list without anyone picking an option: the old state stays.
            if (_previewBackup == null)
                return;
            _dropdownPreset.SetValueWithoutNotify(_previewOrigin);
            _dropdownPreset.RefreshShownValue();
            RestoreBackup();
            _previewBackup = null;
            _listWasOpen = false;
        }
        #endregion


        #region Preview
        // While the list is open, the preset under the pointer is shown on the terrain (DropdownWheel puts the hovered option into the
        // dropdown). The state from before is kept, so closing the list without a pick brings it back, unsaved changes included.
        private TerrainSettingsPreset _previewBackup;
        private int _previewOrigin;
        private bool _listWasOpen;

        private void BeginPreview()
        {
            CurrentPreset.ExtractPresetFromUI();
            _previewBackup = new TerrainSettingsPreset();
            _previewBackup.CopyFromPreset(CurrentPreset);
            _previewOrigin = _dropdownPreset.value;
        }

        /// <summary>A frame later, so DropdownWheel has already put the old option back when nothing was picked.</summary>
        private IEnumerator EndPreviewNextFrame()
        {
            yield return null;
            if (_previewBackup == null)
                yield break;
            bool picked = _dropdownPreset.value != _previewOrigin;
            _previewBackup = null;
            if (picked)
                FinishLoad();
        }

        private void RestoreBackup()
        {
            CurrentPreset.CopyFromPreset(_previewBackup);
            CurrentPreset.ApplyPresetToUI();
        }

        /// <summary>A new option in the dropdown loads it: by a click, by the wheel on the closed dropdown, or as a preview while the list is open.</summary>
        private void OnDropdownValueChanged(int index)
        {
            if (_previewBackup == null)
                LoadActivePreset(true);
            else if (index == _previewOrigin)
                RestoreBackup();
            else
                LoadActivePreset(false);
        }
        #endregion


        #region Presets
        public void AddPreset(TerrainSettingsPreset preset, bool exportToDisk = true)
        {
            _presets.Add(preset);
            _dropdownPreset.options.Add(new TMP_Dropdown.OptionData(preset.Name));
            
            if (exportToDisk)
                TerrainSettingsPresetManager.Instance.ExportPresetsFromJson(preset);
        }

        /// <summary>
        /// Reads the preset and palette files again, for when the folder they come from changed (debug window). The setup on the terrain stays as it
        /// is. The dropdown shows the preset of the same name; when the new folder has none, an unsaved copy of the setup under that name, so Save
        /// never overwrites a different preset.
        /// </summary>
        public void ReloadPresets()
        {
            CurrentPreset.ExtractPresetFromUI();
            string shown = _baselineName;
            _presets.Clear();
            _dropdownPreset.options.Clear();
            TerrainSettingsPresetManager.Instance.ImportPresetsFromJson();

            int index = _presets.FindIndex(p => p.Name == shown);
            if (index < 0 && !string.IsNullOrEmpty(shown))
            {
                TerrainSettingsPreset copy = new TerrainSettingsPreset();
                copy.CopyFromPreset(CurrentPreset);
                copy.Name = shown;
                AddPreset(copy, false);
                index = _presets.Count - 1;
            }
            _dropdownPreset.SetValueWithoutNotify(Mathf.Max(index, 0));
            _dropdownPreset.RefreshShownValue();

            foreach (TerrainGroupUI group in TerrainSettingsManager.Instance.Groups)
            {
                if (group is TerrainColorGroupUI colors)
                    colors.ReloadPalettes();
            }
        }

        /// <summary>Loads the preset of the last session (the first one if there is none), so the start state is what the dropdown shows.</summary>
        public void LoadStartPreset()
        {
            if (_presets.Count == 0)
                return;
            string lastName = PlayerPrefs.GetString(LastPresetKey, "");
            int index = _presets.FindIndex(p => p.Name == lastName);
            _dropdownPreset.SetValueWithoutNotify(Mathf.Max(index, 0));
            _dropdownPreset.RefreshShownValue();
            OnPressedLoad();
        }

        private void RememberBaseline(string presetName)
        {
            _baselineName = presetName;
            if (!string.IsNullOrEmpty(presetName))
                PlayerPrefs.SetString(LastPresetKey, presetName);
        }

        /// <summary>The loaded values reach the terrain and are read back within a frame or two (limits may adjust them), so the snapshot waits for that.</summary>
        private IEnumerator SnapshotAfterSettle()
        {
            yield return null;
            yield return null;
            _savedSnapshot = JsonUtility.ToJson(CurrentPreset);
        }

        /// <summary>Saves changed colors as a palette named like the preset, then reads the whole menu state into the current preset.</summary>
        private void SavePaletteForPreset(string presetName)
        {
            foreach (TerrainGroupUI group in TerrainSettingsManager.Instance.Groups)
            {
                if (group is TerrainColorGroupUI colors)
                    colors.SavePaletteForPreset(presetName);
            }
            CurrentPreset.ExtractPresetFromUI();
        }

        private void RemovePresetAtIndex(int presetIndex)
        {
            if (presetIndex >= _presets.Count || presetIndex < 0)
                return;
            TerrainSettingsPreset preset = _presets[presetIndex];
            _presets.RemoveAt(presetIndex);
            _dropdownPreset.options.RemoveAt(presetIndex);
            _dropdownPreset.SetValueWithoutNotify(Mathf.Max(0, Mathf.Min(_dropdownPreset.options.Count - 1, presetIndex)));
            _dropdownPreset.RefreshShownValue();
            TerrainSettingsPresetManager.Instance.DeletePresetFromDisk(preset);
        }

        private void CreateNewPreset(string name)
        {
            foreach (TerrainSettingsPreset tmpPreset in _presets)
            {
                if (tmpPreset.Name == name)
                    return;
            }
            
            // Everything the menu shows goes into the preset, the colors too (the terrain alone does not know them).
            SavePaletteForPreset(name);
            TerrainSettingsPreset preset = new TerrainSettingsPreset();
            preset.Name = name;
            preset.CopyFromPreset(CurrentPreset);
            AddPreset(preset);
            _dropdownPreset.SetValueWithoutNotify(_dropdownPreset.options.Count - 1);
            _dropdownPreset.RefreshShownValue();
            RememberBaseline(preset.Name);
            _savedSnapshot = JsonUtility.ToJson(CurrentPreset);

            // return preset;
        }
        #endregion
        

        #region Button Presses
        private void OnPressedNewPreset()
        {
            CreateNewPreset(_inputFieldNewPreset.text == "" ? "New Preset" : _inputFieldNewPreset.text);
            _inputFieldNewPreset.text = "New Preset";
        }

        private void OnPressedSave()
        {
            if (ActivePreset == null)
                return;
            SavePaletteForPreset(ActivePreset.Name);
            ActivePreset.CopyFromPreset(CurrentPreset);
            TerrainSettingsPresetManager.Instance.ExportPresetsFromJson(ActivePreset);
            RememberBaseline(ActivePreset.Name);
            _savedSnapshot = JsonUtility.ToJson(CurrentPreset);
        }

        private void OnPressedLoad()
        {
            LoadActivePreset(true);
        }

        /// <param name="final">False for a preview: the preset only shows, it does not become the one the settings are compared to.</param>
        private void LoadActivePreset(bool final)
        {
            if (ActivePreset == null)
                return;
            CurrentPreset.CopyFromPreset(ActivePreset);
            CurrentPreset.ApplyPresetToUI();
            if (final)
                FinishLoad();
        }

        private void FinishLoad()
        {
            if (ActivePreset == null)
                return;
            RememberBaseline(ActivePreset.Name);
            TerrainSettingsPresetManager.Instance.StartCoroutine(SnapshotAfterSettle());
        }

        /// <summary>Back to the Default preset (the code defaults if there is none). The preset list and the selection stay as they are.</summary>
        private void OnPressedResetTerrain()
        {
            TerrainSettingsPreset target = _presets.Find(p => p.Name == DefaultPresetName) ?? new TerrainSettingsPreset();
            CurrentPreset.CopyFromPreset(target);
            CurrentPreset.ApplyPresetToUI();
        }

        #region Sharing
        private GameObject _panelShare;
        private float _shareRowHeight;
        private TMP_Text _labelCopy;
        private TMP_Text _labelPaste;

        /// <summary>
        /// Row under Save / Load: copies the current setup to the clipboard as text, and adds a setup from the clipboard as a new preset.
        /// Made from a copy of the Save / Load row, so it looks the same.
        /// </summary>
        private void BuildShareRow()
        {
            Transform source = _panelButtonsPresetControl.transform;
            _panelShare = Instantiate(_panelButtonsPresetControl, source.parent);
            _panelShare.name = "Panel - Buttons Share";
            _panelShare.transform.SetSiblingIndex(source.GetSiblingIndex() + 1);

            // The rows of the group have fixed places: the new row takes the place of "New preset", which moves down one row,
            // and the group grows by that row (see Start).
            RectTransform newPresetRow = (RectTransform)_inputFieldNewPreset.transform.parent;
            _shareRowHeight = ((RectTransform)source).anchoredPosition.y - newPresetRow.anchoredPosition.y;
            ((RectTransform)_panelShare.transform).anchoredPosition = newPresetRow.anchoredPosition;
            newPresetRow.anchoredPosition -= new Vector2(0f, _shareRowHeight);

            Button[] buttons = _panelShare.GetComponentsInChildren<Button>(true);
            for (int i = buttons.Length - 1; i >= 2; i--)
                Destroy(buttons[i].gameObject);

            // Two buttons instead of three share the row.
            HorizontalLayoutGroup layout = _panelShare.GetComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            _labelCopy = SetupShareButton(buttons[0], "Copy setup", OnPressedCopySetup);
            _labelPaste = SetupShareButton(buttons[1], "Paste setup", OnPressedPasteSetup);
        }

        private static TMP_Text SetupShareButton(Button button, string text, UnityEngine.Events.UnityAction onClick)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            label.text = text;
            return label;
        }

        private void SetShareRowActive(bool active)
        {
            if (_panelShare != null)
                _panelShare.SetActive(active);
        }

        private void OnPressedCopySetup()
        {
            CurrentPreset.ExtractPresetFromUI();
            TerrainSettingsPreset preset = new TerrainSettingsPreset();
            preset.CopyFromPreset(CurrentPreset);
            preset.Name = string.IsNullOrEmpty(_baselineName) ? "Shared setup" : _baselineName;
            preset.FormatVersion = TerrainSettingsPreset.CurrentFormatVersion;
            GUIUtility.systemCopyBuffer = JsonUtility.ToJson(preset);
            ShowFeedback(_labelCopy, "Copied!");
        }

        private void OnPressedPasteSetup()
        {
            TerrainSettingsPreset preset = null;
            try
            {
                preset = TerrainSettingsPresetManager.ParsePreset(GUIUtility.systemCopyBuffer);
            }
            catch (ArgumentException)
            {
                // Text that looks like JSON but is none: handled like any other text.
            }

            if (preset == null)
            {
                ShowFeedback(_labelPaste, "No setup copied");
                return;
            }

            preset.Name = UniquePresetName(preset.Name);
            AddPreset(preset);
            _dropdownPreset.SetValueWithoutNotify(_presets.Count - 1);
            _dropdownPreset.RefreshShownValue();
            OnPressedLoad();
            ShowFeedback(_labelPaste, "Added!");
        }

        /// <summary>The name becomes a file name, so it loses characters a file name cannot have, and gets a number when it is taken.</summary>
        private string UniquePresetName(string name)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
                name = name.Replace(invalid.ToString(), "");
            name = name.Trim();
            if (name.Length > 40)
                name = name.Substring(0, 40).Trim();
            if (name.Length == 0)
                name = "Shared setup";

            string candidate = name;
            for (int number = 2; _presets.Exists(p => p.Name == candidate); number++)
                candidate = name + " (" + number + ")";
            return candidate;
        }

        private void ShowFeedback(TMP_Text label, string text)
        {
            TerrainSettingsPresetManager.Instance.StartCoroutine(FeedbackRoutine(label, text));
        }

        private IEnumerator FeedbackRoutine(TMP_Text label, string text)
        {
            string original = label == _labelCopy ? "Copy setup" : "Paste setup";
            label.text = text;
            yield return new WaitForSecondsRealtime(1.5f);
            if (label != null)
                label.text = original;
        }
        #endregion


        private void OnPressedAttemptRemove()
        {
            _panelButtonsPresetControl.SetActive(false);
            SetShareRowActive(false);
            _labeledButtonRemove.SetActive(true);
            _dropdownPreset.interactable = false;
        }
        
        private void OnPressedConfirmRemove()
        {
            RemovePresetAtIndex(_dropdownPreset.value);
            _panelButtonsPresetControl.SetActive(true);
            SetShareRowActive(true);
            _labeledButtonRemove.SetActive(false);
            _dropdownPreset.interactable = true;
        }
        
        private void OnPressedRejectRemove()
        {
            _panelButtonsPresetControl.SetActive(true);
            SetShareRowActive(true);
            _labeledButtonRemove.SetActive(false);
            _dropdownPreset.interactable = true;
        }
        #endregion
    }
}
