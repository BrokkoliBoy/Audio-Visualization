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

        private readonly List<TerrainSettingsProfile> _profiles = new List<TerrainSettingsProfile>();

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

        /// <summary>Loads the first profile of the list, so the start state is what the dropdown shows.</summary>
        public void LoadFirstProfile()
        {
            if (_profiles.Count == 0)
                return;
            _dropdownProfile.SetValueWithoutNotify(0);
            _dropdownProfile.RefreshShownValue();
            OnPressedLoad();
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
            if (ActiveProfile != null)
                TerrainSettingsProfileManager.Instance.ExportProfilesFromJson(ActiveProfile);
        }
        
        private void OnPressedLoad()
        {
            CurrentProfile.CopyFromProfile(ActiveProfile);
            //CurrentProfile.ApplyProfileToTerrain();
            CurrentProfile.ApplyProfileToUI();
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
