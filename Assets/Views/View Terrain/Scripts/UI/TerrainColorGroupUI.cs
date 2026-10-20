using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gavi
{
    /// <summary>
    /// Color tab of the terrain menu. Four colors (hue, saturation, intensity) are written into the keys of the
    /// terrain height gradient. Each color owns a few keys of the gradient. The keys keep their hue, saturation
    /// and intensity relative to the first (reference) key of their color, so the default palette reproduces the
    /// gradient from the scene exactly.
    /// Palettes can be saved to disk and are part of the terrain profiles.
    /// </summary>
    public class TerrainColorGroupUI : TerrainGroupUI
    {
        [SerializeField] private TerrainGenerator _terrain;
        [SerializeField] private List<TerrainColorEntryUI> _entries = new List<TerrainColorEntryUI>();
        [SerializeField] private TMP_Dropdown _paletteDropdown;

        [Header("--- Save and delete ---")]
        [SerializeField] private TMP_InputField _inputFieldName;
        [SerializeField] private Button _buttonSave;
        [SerializeField] private Button _buttonDelete;
        [SerializeField] private TMP_Text _textDelete;
        [SerializeField] private float _confirmDeleteSeconds = 3f;

        [Header("--- Intensity ---")]
        [Tooltip("Multiplies the color of the whole gradient. 1 = unchanged.")]
        [SerializeField] private SliderG _sliderOverallIntensity;
        [SerializeField] private float _defaultOverallIntensity = 1f;
        [Tooltip("Debug: Unity intensity (exposure value) that menu intensity 10 stands for.")]
        [SerializeField] private SliderG _sliderMaxIntensity;
        [SerializeField] private float _defaultMaxIntensity = 5f;

        [Header("--- Gradient keys ---")]
        [Tooltip("For every key of the terrain gradient: which color (index of the entries) it belongs to.")]
        [SerializeField] private int[] _keyColorIndex = { 0, 0, 1, 2, 2, 3, 3, 3 };
        [Tooltip("For every color: the key that carries the color exactly. The other keys of the color are offsets of it.")]
        [SerializeField] private int[] _referenceKey = { 1, 2, 3, 6 };

        private Gradient _gradient;
        private GradientColorKey[] _originalColorKeys;
        private GradientAlphaKey[] _originalAlphaKeys;
        private float[] _hueOffset;
        private float[] _saturationOffset;
        private float[] _intensityOffset;
        private float[] _referenceHue;
        private float[] _referenceSaturation;
        private float[] _referenceIntensity;

        private List<TerrainColorPalette> _userPalettes = new List<TerrainColorPalette>();
        private string _paletteName = TerrainColorPalette.DefaultName;
        private bool _ready;
        private bool _suppress;
        private float _deleteConfirmUntil;

        private float MaxIntensity => _sliderMaxIntensity != null ? _sliderMaxIntensity.Value : _defaultMaxIntensity;
        private float OverallIntensity => _sliderOverallIntensity != null ? _sliderOverallIntensity.Value : _defaultOverallIntensity;
        private bool IsDefault => _paletteName == TerrainColorPalette.DefaultName;
        private bool IsUserPalette => FindUserPalette(_paletteName) != null;


        #region Mono
        private void Start()
        {
            if (_terrain == null && TerrainSettingsManager.Instance != null)
                _terrain = TerrainSettingsManager.Instance.Terrain;

            _gradient = _terrain != null ? _terrain.ColorHeightGradient : null;
            if (_gradient == null || _entries.Count == 0 || _gradient.colorKeys.Length != _keyColorIndex.Length)
            {
                Debug.LogWarning("TerrainColorGroupUI: terrain gradient does not match the key setup, color menu is not linked.", this);
                return;
            }

            BuildTemplate();

            _suppress = true;
            if (_sliderMaxIntensity != null)
            {
                _sliderMaxIntensity.Value = _defaultMaxIntensity;
                _sliderMaxIntensity.OnValueChangedFloat.AddListener(OnIntensitySliderChanged);
            }
            if (_sliderOverallIntensity != null)
            {
                _sliderOverallIntensity.Value = _defaultOverallIntensity;
                _sliderOverallIntensity.OnValueChangedFloat.AddListener(OnIntensitySliderChanged);
            }
            _suppress = false;

            foreach (TerrainColorEntryUI entry in _entries)
                entry.Changed += OnEntryChanged;

            if (_paletteDropdown != null)
                _paletteDropdown.onValueChanged.AddListener(OnDropdownChanged);
            if (_buttonSave != null)
                _buttonSave.onClick.AddListener(OnPressedSave);
            if (_buttonDelete != null)
                _buttonDelete.onClick.AddListener(OnPressedDelete);

            _userPalettes = TerrainColorPaletteStore.LoadAll();
            _ready = true;
            SelectDefault();
            RebuildDropdown();
        }

        private void Update()
        {
            if (_deleteConfirmUntil > 0f && Time.unscaledTime > _deleteConfirmUntil)
                ResetDeleteButton();
        }

        private void OnDestroy()
        {
            foreach (TerrainColorEntryUI entry in _entries)
                if (entry != null)
                    entry.Changed -= OnEntryChanged;
        }
        #endregion


        #region Profile
        public override void ExtractProfile(TerrainSettingsProfile profile)
        {
            base.ExtractProfile(profile);
            if (!_ready)
                return;

            profile.ColorPaletteName = _paletteName;
            profile.ColorOverallIntensity = OverallIntensity;
            profile.ColorMaxIntensity = MaxIntensity;
            profile.ColorValues = new List<Vector3>();
            foreach (TerrainColorEntryUI entry in _entries)
                profile.ColorValues.Add(new Vector3(entry.Hue, entry.Saturation, entry.MenuIntensity));
        }

        public override void ShowProfile(TerrainSettingsProfile profile)
        {
            base.ShowProfile(profile);
            if (!_ready)
                return;

            string name = string.IsNullOrEmpty(profile.ColorPaletteName) ? TerrainColorPalette.DefaultName : profile.ColorPaletteName;
            bool hasValues = profile.ColorValues != null && profile.ColorValues.Count == _entries.Count;

            _suppress = true;
            SetIntensitySliders(profile.ColorOverallIntensity, profile.ColorMaxIntensity);

            if (TerrainColorPalette.IsDefault(name) || !hasValues)
            {
                SelectDefault();
            }
            else
            {
                for (int c = 0; c < _entries.Count; c++)
                    _entries[c].SetValues(profile.ColorValues[c].x, profile.ColorValues[c].y, profile.ColorValues[c].z);
                _paletteName = FindUserPalette(name) != null ? name : TerrainColorPalette.CustomName;
                ApplyEntriesToGradient();
            }
            _suppress = false;

            RebuildDropdown();
        }
        #endregion


        #region Template
        private void BuildTemplate()
        {
            _originalColorKeys = _gradient.colorKeys;
            _originalAlphaKeys = _gradient.alphaKeys;

            int colorCount = _referenceKey.Length;
            _referenceHue = new float[colorCount];
            _referenceSaturation = new float[colorCount];
            _referenceIntensity = new float[colorCount];
            for (int c = 0; c < colorCount; c++)
                TerrainColorMapping.Decompose(_originalColorKeys[_referenceKey[c]].color, out _referenceHue[c], out _referenceSaturation[c], out _referenceIntensity[c]);

            int keyCount = _originalColorKeys.Length;
            _hueOffset = new float[keyCount];
            _saturationOffset = new float[keyCount];
            _intensityOffset = new float[keyCount];
            for (int i = 0; i < keyCount; i++)
            {
                int c = _keyColorIndex[i];
                TerrainColorMapping.Decompose(_originalColorKeys[i].color, out float hue, out float saturation, out float intensity);
                _hueOffset[i] = Mathf.DeltaAngle(_referenceHue[c], hue);
                _saturationOffset[i] = saturation - _referenceSaturation[c];
                _intensityOffset[i] = intensity - _referenceIntensity[c];
            }
        }
        #endregion


        #region Palettes
        private TerrainColorPalette FindUserPalette(string name)
        {
            foreach (TerrainColorPalette palette in _userPalettes)
                if (palette.Name == name)
                    return palette;
            return null;
        }

        private void RebuildDropdown()
        {
            if (_paletteDropdown == null)
                return;

            var names = new List<string> { TerrainColorPalette.DefaultName };
            foreach (TerrainColorPalette palette in _userPalettes)
                names.Add(palette.Name);
            names.Add(TerrainColorPalette.CustomName);

            _paletteDropdown.ClearOptions();
            _paletteDropdown.AddOptions(names);

            int index = names.IndexOf(_paletteName);
            _paletteDropdown.SetValueWithoutNotify(index >= 0 ? index : names.Count - 1);
            _paletteDropdown.RefreshShownValue();
            RefreshButtons();
        }

        private void OnDropdownChanged(int index)
        {
            if (!_ready)
                return;

            int userCount = _userPalettes.Count;
            if (index == 0)
            {
                _suppress = true;
                SetIntensitySliders(_defaultOverallIntensity, MaxIntensity);
                SelectDefault();
                _suppress = false;
            }
            else if (index >= 1 && index <= userCount)
            {
                ApplyPalette(_userPalettes[index - 1]);
            }
            else
            {
                _paletteName = TerrainColorPalette.CustomName;
            }

            RefreshButtons();
            OnUIValueChanged();
        }

        private void ApplyPalette(TerrainColorPalette palette)
        {
            _suppress = true;
            SetIntensitySliders(palette.OverallIntensity, palette.MaxIntensity);
            for (int c = 0; c < _entries.Count && c < palette.Colors.Count; c++)
                _entries[c].SetValues(palette.Colors[c].x, palette.Colors[c].y, palette.Colors[c].z);
            _paletteName = palette.Name;
            _suppress = false;
            ApplyEntriesToGradient();
        }

        private void SelectDefault()
        {
            _paletteName = TerrainColorPalette.DefaultName;
            ShowDefaultInEntries();
            RestoreOriginalGradient();
        }

        private void SetIntensitySliders(float overall, float maxIntensity)
        {
            bool wasSuppressed = _suppress;
            _suppress = true;
            if (_sliderOverallIntensity != null)
                _sliderOverallIntensity.Value = overall;
            if (_sliderMaxIntensity != null)
                _sliderMaxIntensity.Value = maxIntensity;
            _suppress = wasSuppressed;
        }

        private void ShowDefaultInEntries()
        {
            for (int c = 0; c < _entries.Count && c < _referenceHue.Length; c++)
                _entries[c].SetValues(_referenceHue[c], _referenceSaturation[c], TerrainColorMapping.IntensityToMenu(_referenceIntensity[c], MaxIntensity));
        }

        private void RestoreOriginalGradient()
        {
            float overall = OverallIntensity;
            if (Mathf.Approximately(overall, 1f))
            {
                _gradient.SetKeys(_originalColorKeys, _originalAlphaKeys);
                return;
            }

            var keys = new GradientColorKey[_originalColorKeys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                Color color = _originalColorKeys[i].color;
                keys[i] = new GradientColorKey(new Color(color.r * overall, color.g * overall, color.b * overall, 1f), _originalColorKeys[i].time);
            }
            _gradient.SetKeys(keys, _originalAlphaKeys);
        }
        #endregion


        #region Save and delete
        private void OnPressedSave()
        {
            string name = _inputFieldName != null ? _inputFieldName.text.Trim() : "";
            if (string.IsNullOrEmpty(name))
                name = "My Palette";
            if (TerrainColorPalette.IsDefault(name) || name == TerrainColorPalette.CustomName)
                name += " copy";

            var palette = new TerrainColorPalette
            {
                Name = name,
                OverallIntensity = OverallIntensity,
                MaxIntensity = MaxIntensity
            };
            foreach (TerrainColorEntryUI entry in _entries)
                palette.Colors.Add(new Vector3(entry.Hue, entry.Saturation, entry.MenuIntensity));

            TerrainColorPalette existing = FindUserPalette(name);
            if (existing != null)
                _userPalettes[_userPalettes.IndexOf(existing)] = palette;
            else
                _userPalettes.Add(palette);
            _userPalettes.Sort((a, b) => string.Compare(a.Name, b.Name, System.StringComparison.OrdinalIgnoreCase));

            TerrainColorPaletteStore.Save(palette);
            _paletteName = name;
            RebuildDropdown();
            OnUIValueChanged();
        }

        private void OnPressedDelete()
        {
            TerrainColorPalette palette = FindUserPalette(_paletteName);
            if (palette == null)
                return;

            // First press asks, second press within a few seconds deletes.
            if (_deleteConfirmUntil <= 0f)
            {
                _deleteConfirmUntil = Time.unscaledTime + _confirmDeleteSeconds;
                if (_textDelete != null)
                    _textDelete.text = "Really delete?";
                return;
            }

            _userPalettes.Remove(palette);
            TerrainColorPaletteStore.Delete(palette.Name);
            _paletteName = TerrainColorPalette.CustomName;
            ResetDeleteButton();
            RebuildDropdown();
            OnUIValueChanged();
        }

        private void RefreshButtons()
        {
            ResetDeleteButton();
            if (_buttonDelete != null)
                _buttonDelete.interactable = IsUserPalette;
        }

        private void ResetDeleteButton()
        {
            _deleteConfirmUntil = 0f;
            if (_textDelete != null)
                _textDelete.text = "Delete palette";
        }
        #endregion


        #region Apply
        private void OnEntryChanged()
        {
            if (_suppress)
                return;

            _paletteName = TerrainColorPalette.CustomName;
            if (_paletteDropdown != null)
            {
                _paletteDropdown.SetValueWithoutNotify(_userPalettes.Count + 1);
                _paletteDropdown.RefreshShownValue();
            }
            RefreshButtons();
            ApplyEntriesToGradient();
            OnUIValueChanged();
        }

        private void OnIntensitySliderChanged(float value)
        {
            if (!_ready || _suppress)
                return;

            if (IsDefault)
            {
                ShowDefaultInEntries();
                RestoreOriginalGradient();
            }
            else
            {
                if (_paletteName != TerrainColorPalette.CustomName)
                {
                    _paletteName = TerrainColorPalette.CustomName;
                    RebuildDropdown();
                }
                ApplyEntriesToGradient();
            }
            OnUIValueChanged();
        }

        private void ApplyEntriesToGradient()
        {
            var keys = new GradientColorKey[_originalColorKeys.Length];
            float maxIntensity = MaxIntensity;
            float overall = OverallIntensity;

            for (int i = 0; i < keys.Length; i++)
            {
                int c = _keyColorIndex[i];
                TerrainColorEntryUI entry = _entries[c];

                float hue = entry.Hue + _hueOffset[i];
                float saturation = Mathf.Clamp01(entry.Saturation + _saturationOffset[i]);
                float intensity = TerrainColorMapping.MenuToIntensity(entry.MenuIntensity, maxIntensity) + _intensityOffset[i];

                Color color = TerrainColorMapping.ToHdrColor(hue, saturation, intensity);
                keys[i] = new GradientColorKey(new Color(color.r * overall, color.g * overall, color.b * overall, 1f), _originalColorKeys[i].time);
            }

            _gradient.SetKeys(keys, _originalAlphaKeys);
        }
        #endregion
    }
}
