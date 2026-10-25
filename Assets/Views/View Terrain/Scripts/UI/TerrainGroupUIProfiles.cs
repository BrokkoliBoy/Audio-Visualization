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
    public class TerrainGroupUIProfiles : TerrainGroupUI
    {
        public static TerrainGroupUIProfiles Instance;
        
        [Header("- References -")]
        [SerializeField] private TMP_Dropdown _dropdownProfile;
        [SerializeField] private Button _buttonSave;
        [SerializeField] private Button _buttonLoad;
        [SerializeField] private GameObject _panelButtonsProfileControl;
        [SerializeField] private GameObject _labeledButtonRemove;
        [SerializeField] private Button _buttonAttemptRemove;
        [SerializeField] private Button _buttonConfirmRemove;
        [SerializeField] private Button _buttonRejectRemove;
        [SerializeField] private TMP_InputField _inputFieldNewProfile;
        [SerializeField] private Button _buttonNewProfile;
        [SerializeField] private Button _buttonResetTerrain;

        private const string DefaultProfileName = "Default";
        private const string LastProfileKey = "TerrainProfiles.Last";

        private readonly List<TerrainSettingsProfile> _profiles = new List<TerrainSettingsProfile>();

        // The terrain state at the last load or save, to tell whether the settings were changed since.
        private string _savedSnapshot;
        private string _baselineName;

        /// <summary>True when the terrain settings differ from the profile that was last loaded or saved.</summary>
        public bool HasUnsavedChanges => _savedSnapshot != null && JsonUtility.ToJson(CurrentProfile) != _savedSnapshot;
        /// <summary>Name of the profile that was last loaded or saved.</summary>
        public string BaselineProfileName => _baselineName;

        private TerrainSettingsProfile CurrentProfile => TerrainSettingsManager.Instance.CurrentProfile;
        private TerrainSettingsProfile ActiveProfile => _dropdownProfile.value >= _profiles.Count ? null : _profiles[_dropdownProfile.value];

        
        #region Mono
        private void Awake()
        {
            _buttonSave.onClick.AddListener(OnPressedSave);
            _buttonLoad.onClick.AddListener(OnPressedLoad);
            _buttonAttemptRemove.onClick.AddListener(OnPressedAttemptRemove);
            _buttonConfirmRemove.onClick.AddListener(OnPressedConfirmRemove);
            _buttonRejectRemove.onClick.AddListener(OnPressedRejectRemove);
            _buttonNewProfile.onClick.AddListener(OnPressedNewProfile);
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
        #endregion


        #region Profiles
        public void AddProfile(TerrainSettingsProfile profile, bool exportToDisk = true)
        {
            _profiles.Add(profile);
            _dropdownProfile.options.Add(new TMP_Dropdown.OptionData(profile.Name));
            
            if (exportToDisk)
                TerrainSettingsProfileManager.Instance.ExportProfilesFromJson(profile);
        }

        /// <summary>Loads the profile of the last session (the first one if there is none), so the start state is what the dropdown shows.</summary>
        public void LoadStartProfile()
        {
            if (_profiles.Count == 0)
                return;
            string lastName = PlayerPrefs.GetString(LastProfileKey, "");
            int index = _profiles.FindIndex(p => p.Name == lastName);
            _dropdownProfile.SetValueWithoutNotify(Mathf.Max(index, 0));
            _dropdownProfile.RefreshShownValue();
            OnPressedLoad();
        }

        private void RememberBaseline(string profileName)
        {
            _baselineName = profileName;
            if (!string.IsNullOrEmpty(profileName))
                PlayerPrefs.SetString(LastProfileKey, profileName);
        }

        /// <summary>The loaded values reach the terrain and are read back within a frame or two (limits may adjust them), so the snapshot waits for that.</summary>
        private IEnumerator SnapshotAfterSettle()
        {
            yield return null;
            yield return null;
            _savedSnapshot = JsonUtility.ToJson(CurrentProfile);
        }

        /// <summary>Saves changed colors as a palette named like the profile, then reads the whole menu state into the current profile.</summary>
        private void SavePaletteForProfile(string profileName)
        {
            foreach (TerrainGroupUI group in TerrainSettingsManager.Instance.Groups)
            {
                if (group is TerrainColorGroupUI colors)
                    colors.SavePaletteForProfile(profileName);
            }
            CurrentProfile.ExtractProfileFromUI();
        }

        private void RemoveProfileAtIndex(int profileIndex)
        {
            if (profileIndex >= _profiles.Count || profileIndex < 0)
                return;
            TerrainSettingsProfile profile = _profiles[profileIndex];
            _profiles.RemoveAt(profileIndex);
            _dropdownProfile.options.RemoveAt(profileIndex);
            _dropdownProfile.value = Mathf.Min(_dropdownProfile.options.Count - 1, profileIndex);
            TerrainSettingsProfileManager.Instance.DeleteProfileFromDisk(profile);
        }

        private void CreateNewProfile(string name)
        {
            foreach (TerrainSettingsProfile tmpProfile in _profiles)
            {
                if (tmpProfile.Name == name)
                    return;
            }
            
            // Everything the menu shows goes into the profile, the colors too (the terrain alone does not know them).
            SavePaletteForProfile(name);
            TerrainSettingsProfile profile = new TerrainSettingsProfile();
            profile.Name = name;
            profile.CopyFromProfile(CurrentProfile);
            AddProfile(profile);
            _dropdownProfile.value = _dropdownProfile.options.Count - 1;
            RememberBaseline(profile.Name);
            _savedSnapshot = JsonUtility.ToJson(CurrentProfile);

            // return profile;
        }
        #endregion
        

        #region Button Presses
        private void OnPressedNewProfile()
        {
            CreateNewProfile(_inputFieldNewProfile.text == "" ? "New Profile" : _inputFieldNewProfile.text);
            _inputFieldNewProfile.text = "New Profile";
        }

        private void OnPressedSave()
        {
            if (ActiveProfile == null)
                return;
            SavePaletteForProfile(ActiveProfile.Name);
            ActiveProfile.CopyFromProfile(CurrentProfile);
            TerrainSettingsProfileManager.Instance.ExportProfilesFromJson(ActiveProfile);
            RememberBaseline(ActiveProfile.Name);
            _savedSnapshot = JsonUtility.ToJson(CurrentProfile);
        }

        private void OnPressedLoad()
        {
            CurrentProfile.CopyFromProfile(ActiveProfile);
            //CurrentProfile.ApplyProfileToTerrain();
            CurrentProfile.ApplyProfileToUI();
            if (ActiveProfile == null)
                return;
            RememberBaseline(ActiveProfile.Name);
            TerrainSettingsProfileManager.Instance.StartCoroutine(SnapshotAfterSettle());
        }

        /// <summary>Back to the Default profile (the code defaults if there is none). The profile list and the selection stay as they are.</summary>
        private void OnPressedResetTerrain()
        {
            TerrainSettingsProfile target = _profiles.Find(p => p.Name == DefaultProfileName) ?? new TerrainSettingsProfile();
            CurrentProfile.CopyFromProfile(target);
            CurrentProfile.ApplyProfileToUI();
        }

        #region Sharing
        private GameObject _panelShare;
        private float _shareRowHeight;
        private TMP_Text _labelCopy;
        private TMP_Text _labelPaste;

        /// <summary>
        /// Row under Save / Load: copies the current setup to the clipboard as text, and adds a setup from the clipboard as a new profile.
        /// Made from a copy of the Save / Load row, so it looks the same.
        /// </summary>
        private void BuildShareRow()
        {
            Transform source = _panelButtonsProfileControl.transform;
            _panelShare = Instantiate(_panelButtonsProfileControl, source.parent);
            _panelShare.name = "Panel - Buttons Share";
            _panelShare.transform.SetSiblingIndex(source.GetSiblingIndex() + 1);

            // The rows of the group have fixed places: the new row takes the place of "New profile", which moves down one row,
            // and the group grows by that row (see Start).
            RectTransform newProfileRow = (RectTransform)_inputFieldNewProfile.transform.parent;
            _shareRowHeight = ((RectTransform)source).anchoredPosition.y - newProfileRow.anchoredPosition.y;
            ((RectTransform)_panelShare.transform).anchoredPosition = newProfileRow.anchoredPosition;
            newProfileRow.anchoredPosition -= new Vector2(0f, _shareRowHeight);

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
            CurrentProfile.ExtractProfileFromUI();
            TerrainSettingsProfile profile = new TerrainSettingsProfile();
            profile.CopyFromProfile(CurrentProfile);
            profile.Name = string.IsNullOrEmpty(_baselineName) ? "Shared setup" : _baselineName;
            profile.FormatVersion = TerrainSettingsProfile.CurrentFormatVersion;
            GUIUtility.systemCopyBuffer = JsonUtility.ToJson(profile);
            ShowFeedback(_labelCopy, "Copied!");
        }

        private void OnPressedPasteSetup()
        {
            TerrainSettingsProfile profile = null;
            try
            {
                profile = TerrainSettingsProfileManager.ParseProfile(GUIUtility.systemCopyBuffer);
            }
            catch (ArgumentException)
            {
                // Text that looks like JSON but is none: handled like any other text.
            }

            if (profile == null)
            {
                ShowFeedback(_labelPaste, "No setup copied");
                return;
            }

            profile.Name = UniqueProfileName(profile.Name);
            AddProfile(profile);
            _dropdownProfile.SetValueWithoutNotify(_profiles.Count - 1);
            _dropdownProfile.RefreshShownValue();
            OnPressedLoad();
            ShowFeedback(_labelPaste, "Added!");
        }

        /// <summary>The name becomes a file name, so it loses characters a file name cannot have, and gets a number when it is taken.</summary>
        private string UniqueProfileName(string name)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
                name = name.Replace(invalid.ToString(), "");
            name = name.Trim();
            if (name.Length > 40)
                name = name.Substring(0, 40).Trim();
            if (name.Length == 0)
                name = "Shared setup";

            string candidate = name;
            for (int number = 2; _profiles.Exists(p => p.Name == candidate); number++)
                candidate = name + " (" + number + ")";
            return candidate;
        }

        private void ShowFeedback(TMP_Text label, string text)
        {
            TerrainSettingsProfileManager.Instance.StartCoroutine(FeedbackRoutine(label, text));
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
            _panelButtonsProfileControl.SetActive(false);
            SetShareRowActive(false);
            _labeledButtonRemove.SetActive(true);
            _dropdownProfile.interactable = false;
        }
        
        private void OnPressedConfirmRemove()
        {
            RemoveProfileAtIndex(_dropdownProfile.value);
            _panelButtonsProfileControl.SetActive(true);
            SetShareRowActive(true);
            _labeledButtonRemove.SetActive(false);
            _dropdownProfile.interactable = true;
        }
        
        private void OnPressedRejectRemove()
        {
            _panelButtonsProfileControl.SetActive(true);
            SetShareRowActive(true);
            _labeledButtonRemove.SetActive(false);
            _dropdownProfile.interactable = true;
        }
        #endregion
    }
}
