using System;
using System.Collections;
using System.Collections.Generic;
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
            
            TerrainSettingsProfile profile = new TerrainSettingsProfile();
            profile.Name = name;
            profile.CopyFromTerrainSettings();
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
            ActiveProfile?.CopyFromProfile(CurrentProfile);
            if (ActiveProfile == null)
                return;
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

        private void OnPressedAttemptRemove()
        {
            _panelButtonsProfileControl.SetActive(false);
            _labeledButtonRemove.SetActive(true);
            _dropdownProfile.interactable = false;
        }
        
        private void OnPressedConfirmRemove()
        {
            RemoveProfileAtIndex(_dropdownProfile.value);
            _panelButtonsProfileControl.SetActive(true);
            _labeledButtonRemove.SetActive(false);
            _dropdownProfile.interactable = true;
        }
        
        private void OnPressedRejectRemove()
        {
            _panelButtonsProfileControl.SetActive(true);
            _labeledButtonRemove.SetActive(false);
            _dropdownProfile.interactable = true;
        }
        #endregion
    }
}
