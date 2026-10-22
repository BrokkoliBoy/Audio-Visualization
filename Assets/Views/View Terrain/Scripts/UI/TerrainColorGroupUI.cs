using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;

namespace Gavi
{
    /// <summary>
    /// Color tab of the terrain menu. Four colors (hue, saturation, glow) are turned into the color table of the terrain
    /// (see ApplyColorCurve): the glow of every color is exact, the colors blend in a straight line, and flat stretches are
    /// filled with a wave. The gradient of the scene is only the template for the key times (which color owns which stretch)
    /// and for the dark start key.
    /// The bloom (glow) of the terrain volume is part of the look and saved with the palette.
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
        [Tooltip("Evens out the colors between noise patterns, see TerrainGenerator.ColorNoiseBalance. 0 = off.")]
        [SerializeField] private SliderG _sliderPatternBalance;
        [Tooltip("Soft end of the gradient for heights over 1, see TerrainGenerator.ColorHeadroom. 0 = hard end.")]
        [SerializeField] private SliderG _sliderHeadroom;

        [Header("--- Bloom ---")]
        [Tooltip("Volume of the terrain view. Its profile is copied at runtime, so the profile asset stays unchanged.")]
        [SerializeField] private Volume _bloomVolume;
        [SerializeField] private SliderG _sliderBloomThreshold;
        [SerializeField] private SliderG _sliderBloomIntensity;
        [SerializeField] private SliderG _sliderBloomScatter;

        [Header("--- Background ---")]
        [Tooltip("The sky of the bloom volume takes the hue of the base color (the first one). Brightness and saturation come from the sky colors in the volume.")]
        [SerializeField] private Toggle _toggleTintBackground;

        [Header("--- Lens dirt (part of the profile, not of the palette) ---")]
        [SerializeField] private TerrainLensDirt _lensDirt;
        [SerializeField] private SliderG _sliderDirtStrength;
        [SerializeField] private SliderG _sliderDirtBeat;

        [Header("--- Gradient keys ---")]
        [Tooltip("For every key of the terrain gradient: which color (index of the entries) it belongs to.")]
        [SerializeField] private int[] _keyColorIndex = { 0, 0, 1, 2, 3 };
        [Tooltip("For every color: the key that carries the color exactly. The other keys of the color are offsets of it.")]
        [SerializeField] private int[] _referenceKey = { 1, 2, 3, 4 };

        public enum CurveShape { Plateau, ZigZag, SpikesUp, SpikesDown, SawUp, SawDown }

        [Header("--- Color curve ---")]
        [Tooltip("How flat stretches (two keys of one color, or neighbours with nearly the same glow) are filled. Plateau = left flat. Index of CurveShape.")]
        [SerializeField] private TMP_Dropdown _dropdownCurveShape;
        [Tooltip("Slope of the waves in exposure steps per full height of the gradient. A stretch counts as flat when its own slope is under half of this.")]
        [SerializeField] private SliderG _sliderCurveStrength;
        [Tooltip("Glow of the lowest vertices on the menu glow scale (the cap of the first key).")]
        [SerializeField] private SliderG _sliderCurveFloor;
        [Tooltip("Moves the colors along the height: + lower (the hot colors take more area), - higher.")]
        [SerializeField] private SliderG _sliderColorShift;
        [Tooltip("Width of the two long color areas (the keys that hold one color). 1 = as in the scene gradient.")]
        [SerializeField] private SliderG _sliderColorHold;
        [Tooltip("Width of one wave as a share of the gradient height. Narrow stretches get fewer waves, at least one.")]
        [SerializeField] private SliderG _sliderCurveWidth;
        [Tooltip("Rounds the turning points of the zigzag and spike waves: 0 = sharp corners, 1 = fully round. The straight parts keep their slope.")]
        [SerializeField] private SliderG _sliderCurveRounding;
        /// <summary>Smallest distance between two key times, so no stretch collapses.</summary>
        private const float MinKeyGap = 0.01f;

        private Color[] _curveTable;

        /// <summary>The gradient of the scene is only the template: key times and the start key. The table comes from ApplyColorCurve.</summary>
        private Gradient _gradient;
        private GradientColorKey[] _originalColorKeys;
        private float[] _saturationOffset;
        private float[] _intensityOffset;
        private float[] _referenceHue;
        private float[] _referenceSaturation;
        private float[] _referenceIntensity;

        private List<TerrainColorPalette> _palettes = new List<TerrainColorPalette>();
        /// <summary>Palette the current values started from. Null = the values do not come from a saved palette (Custom).</summary>
        private string _baseName;
        /// <summary>True when the values were changed after the palette was selected.</summary>
        private bool _modified;
        private bool _rebuildPending;
        private Bloom _bloom;
        private GradientSky _sky;
        private Color _skyMiddle;
        private Color _skyBottom;
        private bool _ready;
        private bool _suppress;
        private float _deleteConfirmUntil;

        private float OverallIntensity => _sliderOverallIntensity != null ? _sliderOverallIntensity.Value : _defaultOverallIntensity;
        private float Headroom => _sliderHeadroom != null ? _sliderHeadroom.Value : 0f;
        private float PatternBalance => _sliderPatternBalance != null ? _sliderPatternBalance.Value : 1f;
        private float BloomThreshold => _sliderBloomThreshold != null ? _sliderBloomThreshold.Value : TerrainColorPalette.DefaultBloomThreshold;
        private float BloomIntensity => _sliderBloomIntensity != null ? _sliderBloomIntensity.Value : TerrainColorPalette.DefaultBloomIntensity;
        private float BloomScatter => _sliderBloomScatter != null ? _sliderBloomScatter.Value : TerrainColorPalette.DefaultBloomScatter;
        private float DirtStrength => _sliderDirtStrength != null ? _sliderDirtStrength.Value : TerrainLensDirt.DefaultStrength;
        private float DirtBeat => _sliderDirtBeat != null ? _sliderDirtBeat.Value : TerrainLensDirt.DefaultBeat;
        private CurveShape Shape => _dropdownCurveShape != null ? (CurveShape)_dropdownCurveShape.value : (CurveShape)TerrainColorPalette.DefaultCurveShape;
        private float CurveStrength => _sliderCurveStrength != null ? _sliderCurveStrength.Value : TerrainColorPalette.DefaultCurveStrength;
        /// <summary>The first key of the gradient (the lowest vertices) never gets brighter than this glow (menu scale), whatever the base color is set to.</summary>
        private float CurveFloor => _sliderCurveFloor != null ? _sliderCurveFloor.Value : TerrainColorPalette.DefaultCurveFloor;
        private float ColorShift => _sliderColorShift != null ? _sliderColorShift.Value : TerrainColorPalette.DefaultColorShift;
        private float ColorHold => _sliderColorHold != null ? _sliderColorHold.Value : TerrainColorPalette.DefaultColorHold;
        private float CurveWidth => _sliderCurveWidth != null ? _sliderCurveWidth.Value : TerrainColorPalette.DefaultCurveWidth;
        private float CurveRounding => _sliderCurveRounding != null ? _sliderCurveRounding.Value : TerrainColorPalette.DefaultCurveRounding;
        private bool IsSavedPalette => _baseName != null && FindPalette(_baseName) != null;


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
            ListenToSlider(_sliderOverallIntensity, _defaultOverallIntensity, OnOverallIntensityChanged);
            ListenToSlider(_sliderPatternBalance, 1f, OnPatternBalanceChanged);
            ListenToSlider(_sliderHeadroom, 0f, OnPatternBalanceChanged);
            ListenToSlider(_sliderBloomThreshold, TerrainColorPalette.DefaultBloomThreshold, OnBloomChanged);
            ListenToSlider(_sliderBloomIntensity, TerrainColorPalette.DefaultBloomIntensity, OnBloomChanged);
            ListenToSlider(_sliderBloomScatter, TerrainColorPalette.DefaultBloomScatter, OnBloomChanged);
            ListenToSlider(_sliderDirtStrength, TerrainLensDirt.DefaultStrength, OnDirtChanged);
            ListenToSlider(_sliderDirtBeat, TerrainLensDirt.DefaultBeat, OnDirtChanged);
            ListenToSlider(_sliderCurveStrength, TerrainColorPalette.DefaultCurveStrength, OnCurveChanged);
            ListenToSlider(_sliderCurveFloor, TerrainColorPalette.DefaultCurveFloor, OnCurveChanged);
            ListenToSlider(_sliderColorShift, TerrainColorPalette.DefaultColorShift, OnCurveChanged);
            ListenToSlider(_sliderColorHold, TerrainColorPalette.DefaultColorHold, OnCurveChanged);
            ListenToSlider(_sliderCurveWidth, TerrainColorPalette.DefaultCurveWidth, OnCurveChanged);
            ListenToSlider(_sliderCurveRounding, TerrainColorPalette.DefaultCurveRounding, OnCurveChanged);
            if (_dropdownCurveShape != null)
            {
                _dropdownCurveShape.ClearOptions();
                _dropdownCurveShape.AddOptions(new List<string>(System.Enum.GetNames(typeof(CurveShape))));
                _dropdownCurveShape.SetValueWithoutNotify(TerrainColorPalette.DefaultCurveShape);
                _dropdownCurveShape.onValueChanged.AddListener(_ => OnCurveChanged(0f));
            }
            _suppress = false;
            ApplyDirt();

            foreach (TerrainColorEntryUI entry in _entries)
                entry.Changed += OnEntryChanged;

            if (_toggleTintBackground != null)
                _toggleTintBackground.onValueChanged.AddListener(_ => ApplyBackground());
            if (_paletteDropdown != null)
                _paletteDropdown.onValueChanged.AddListener(OnDropdownChanged);
            if (_buttonSave != null)
                _buttonSave.onClick.AddListener(OnPressedSave);
            if (_buttonDelete != null)
                _buttonDelete.onClick.AddListener(OnPressedDelete);

            _palettes = TerrainColorPaletteStore.LoadAll();
            _ready = true;
            SelectStartPalette();
            RebuildDropdown();
        }

        private void Update()
        {
            if (_rebuildPending)
                RebuildDropdown();
            if (_deleteConfirmUntil > 0f && Time.unscaledTime > _deleteConfirmUntil)
                ResetDeleteButton();
        }

        private void OnDestroy()
        {
            foreach (TerrainColorEntryUI entry in _entries)
                if (entry != null)
                    entry.Changed -= OnEntryChanged;
        }

        private static void ListenToSlider(SliderG slider, float startValue, UnityEngine.Events.UnityAction<float> listener)
        {
            if (slider == null)
                return;
            slider.Value = startValue;
            slider.OnValueChangedFloat.AddListener(listener);
        }
        #endregion


        #region Profile
        public override void ExtractProfile(TerrainSettingsProfile profile)
        {
            base.ExtractProfile(profile);
            if (!_ready)
                return;

            profile.ColorPaletteName = _baseName ?? TerrainColorPalette.CustomName;
            profile.ColorOverallIntensity = OverallIntensity;
            profile.CurveShape = (int)Shape;
            profile.CurveStrength = CurveStrength;
            profile.CurveFloor = CurveFloor;
            profile.ColorShift = ColorShift;
            profile.ColorHold = ColorHold;
            profile.CurveWidth = CurveWidth;
            profile.CurveRounding = CurveRounding;
            profile.ColorNoiseBalance = PatternBalance;
            profile.ColorHeadroom = Headroom;
            profile.BloomThreshold = BloomThreshold;
            profile.BloomIntensity = BloomIntensity;
            profile.BloomScatter = BloomScatter;
            profile.DirtStrength = DirtStrength;
            profile.DirtBeat = DirtBeat;
            profile.ColorValues = new List<Vector3>();
            foreach (TerrainColorEntryUI entry in _entries)
                profile.ColorValues.Add(new Vector3(entry.Hue, entry.Saturation, entry.MenuIntensity));
        }

        public override void ShowProfile(TerrainSettingsProfile profile)
        {
            base.ShowProfile(profile);
            if (!_ready)
                return;

            // The lens dirt belongs to the profile whether or not the profile carries its own color values.
            SetDirtSliders(profile.DirtStrength, profile.DirtBeat);
            if (_sliderPatternBalance != null)
            {
                bool wasSuppressed = _suppress;
                _suppress = true;
                _sliderPatternBalance.Value = profile.ColorNoiseBalance;
                if (_sliderHeadroom != null)
                    _sliderHeadroom.Value = profile.ColorHeadroom;
                _suppress = wasSuppressed;
            }
            ApplyDirt();

            string name = profile.ColorPaletteName;
            if (string.IsNullOrEmpty(name) || name == TerrainColorPalette.LegacyDefaultName)
                name = TerrainColorPalette.DefaultName;
            TerrainColorPalette palette = FindPalette(name);
            bool hasValues = profile.ColorValues != null && profile.ColorValues.Count == _entries.Count;

            if (!hasValues)
            {
                // Profiles without own color values use the palette of their name.
                if (palette != null)
                    ApplyPalette(palette);
                else
                    ShowOriginalGradient();
                RebuildDropdown();
                return;
            }

            _suppress = true;
            SetSliders(profile.ColorOverallIntensity, profile.BloomThreshold, profile.BloomIntensity, profile.BloomScatter);
            SetCurveLook(profile.CurveShape, profile.CurveStrength, profile.CurveFloor, profile.ColorShift, profile.ColorHold, profile.CurveWidth, profile.CurveRounding);
            for (int c = 0; c < _entries.Count; c++)
                _entries[c].SetValues(profile.ColorValues[c].x, profile.ColorValues[c].y, profile.ColorValues[c].z);
            _suppress = false;

            _baseName = palette != null ? palette.Name : null;
            _modified = palette != null && !Matches(palette, profile);
            ShowNameInInputField();
            ApplyColors();
            ApplyBloom();
            RebuildDropdown();
        }

        private static bool Matches(TerrainColorPalette palette, TerrainSettingsProfile profile)
        {
            if (palette.Colors.Count != profile.ColorValues.Count)
                return false;
            for (int c = 0; c < palette.Colors.Count; c++)
                if ((palette.Colors[c] - profile.ColorValues[c]).magnitude > 0.001f)
                    return false;
            return Mathf.Abs(palette.OverallIntensity - profile.ColorOverallIntensity) < 0.001f
                && Mathf.Abs(palette.BloomThreshold - profile.BloomThreshold) < 0.001f
                && Mathf.Abs(palette.BloomIntensity - profile.BloomIntensity) < 0.001f
                && Mathf.Abs(palette.BloomScatter - profile.BloomScatter) < 0.001f
                && palette.CurveShape == profile.CurveShape
                && Mathf.Abs(palette.CurveStrength - profile.CurveStrength) < 0.001f
                && Mathf.Abs(palette.CurveFloor - profile.CurveFloor) < 0.001f
                && Mathf.Abs(palette.ColorShift - profile.ColorShift) < 0.001f
                && Mathf.Abs(palette.ColorHold - profile.ColorHold) < 0.001f
                && Mathf.Abs(palette.CurveWidth - profile.CurveWidth) < 0.001f
                && Mathf.Abs(palette.CurveRounding - profile.CurveRounding) < 0.001f;
        }
        #endregion


        #region Template
        private void BuildTemplate()
        {
            _originalColorKeys = _gradient.colorKeys;

            int colorCount = _referenceKey.Length;
            _referenceHue = new float[colorCount];
            _referenceSaturation = new float[colorCount];
            _referenceIntensity = new float[colorCount];
            for (int c = 0; c < colorCount; c++)
                TerrainColorMapping.Decompose(_originalColorKeys[_referenceKey[c]].color, out _referenceHue[c], out _referenceSaturation[c], out _referenceIntensity[c]);

            int keyCount = _originalColorKeys.Length;
            _saturationOffset = new float[keyCount];
            _intensityOffset = new float[keyCount];
            for (int i = 0; i < keyCount; i++)
            {
                int c = _keyColorIndex[i];
                TerrainColorMapping.Decompose(_originalColorKeys[i].color, out _, out float saturation, out float intensity);
                _saturationOffset[i] = saturation - _referenceSaturation[c];
                _intensityOffset[i] = intensity - _referenceIntensity[c];
            }
        }
        #endregion


        #region Palettes
        private TerrainColorPalette FindPalette(string name)
        {
            foreach (TerrainColorPalette palette in _palettes)
                if (palette.Name == name)
                    return palette;
            return null;
        }

        /// <summary>Selects the default palette. Without its file the colors of the scene gradient are shown as Custom.</summary>
        private void SelectStartPalette()
        {
            TerrainColorPalette palette = FindPalette(TerrainColorPalette.DefaultName);
            if (palette != null)
                ApplyPalette(palette);
            else
                ShowOriginalGradient();
        }

        private void ShowOriginalGradient()
        {
            _suppress = true;
            SetSliders(_defaultOverallIntensity, TerrainColorPalette.DefaultBloomThreshold, TerrainColorPalette.DefaultBloomIntensity, TerrainColorPalette.DefaultBloomScatter);
            SetCurveLook(TerrainColorPalette.DefaultCurveShape, TerrainColorPalette.DefaultCurveStrength, TerrainColorPalette.DefaultCurveFloor,
                TerrainColorPalette.DefaultColorShift, TerrainColorPalette.DefaultColorHold, TerrainColorPalette.DefaultCurveWidth, TerrainColorPalette.DefaultCurveRounding);
            for (int c = 0; c < _entries.Count && c < _referenceHue.Length; c++)
                _entries[c].SetValues(_referenceHue[c], _referenceSaturation[c], TerrainColorMapping.IntensityToMenu(_referenceIntensity[c]));
            _suppress = false;

            _baseName = null;
            _modified = false;
            ApplyColors();
            ApplyBloom();
        }

        private void RebuildDropdown()
        {
            _rebuildPending = false;
            if (_paletteDropdown == null)
                return;

            var names = new List<string>();
            foreach (TerrainColorPalette palette in _palettes)
                names.Add(palette.Name);

            // Values that are not from a saved palette show as an extra entry named Custom. A saved palette that was
            // changed keeps its entry and gets a star.
            int index;
            if (_baseName == null)
            {
                names.Add(TerrainColorPalette.CustomName);
                index = names.Count - 1;
            }
            else
            {
                index = _palettes.FindIndex(p => p.Name == _baseName);
                if (_modified && index >= 0)
                    names[index] += " *";
            }

            _paletteDropdown.ClearOptions();
            _paletteDropdown.AddOptions(names);

            _paletteDropdown.SetValueWithoutNotify(Mathf.Max(0, index));
            _paletteDropdown.RefreshShownValue();
            RefreshButtons();
        }

        private void OnDropdownChanged(int index)
        {
            if (!_ready)
                return;

            // The last entry (Custom or modified) is only a display, choosing it changes nothing.
            if (index >= 0 && index < _palettes.Count)
            {
                ApplyPalette(_palettes[index]);
                _rebuildPending = true;
                OnUIValueChanged();
            }
            RefreshButtons();
        }

        private void ApplyPalette(TerrainColorPalette palette)
        {
            _suppress = true;
            SetSliders(palette.OverallIntensity, palette.BloomThreshold, palette.BloomIntensity, palette.BloomScatter);
            SetCurveLook(palette.CurveShape, palette.CurveStrength, palette.CurveFloor, palette.ColorShift, palette.ColorHold, palette.CurveWidth, palette.CurveRounding);
            for (int c = 0; c < _entries.Count && c < palette.Colors.Count; c++)
                _entries[c].SetValues(palette.Colors[c].x, palette.Colors[c].y, palette.Colors[c].z);
            _suppress = false;

            _baseName = palette.Name;
            _modified = false;
            ShowNameInInputField();
            ApplyColors();
            ApplyBloom();
        }

        private void SetSliders(float overall, float bloomThreshold, float bloomIntensity, float bloomScatter)
        {
            bool wasSuppressed = _suppress;
            _suppress = true;
            if (_sliderOverallIntensity != null)
                _sliderOverallIntensity.Value = overall;
            if (_sliderBloomThreshold != null)
                _sliderBloomThreshold.Value = bloomThreshold;
            if (_sliderBloomIntensity != null)
                _sliderBloomIntensity.Value = bloomIntensity;
            if (_sliderBloomScatter != null)
                _sliderBloomScatter.Value = bloomScatter;
            _suppress = wasSuppressed;
        }

        private void SetCurveLook(int shape, float strength, float floor, float shift, float hold, float width, float rounding)
        {
            bool wasSuppressed = _suppress;
            _suppress = true;
            if (_dropdownCurveShape != null)
                _dropdownCurveShape.SetValueWithoutNotify(Mathf.Clamp(shape, 0, System.Enum.GetValues(typeof(CurveShape)).Length - 1));
            if (_sliderCurveStrength != null)
                _sliderCurveStrength.Value = strength;
            if (_sliderCurveFloor != null)
                _sliderCurveFloor.Value = floor;
            if (_sliderColorShift != null)
                _sliderColorShift.Value = shift;
            if (_sliderColorHold != null)
                _sliderColorHold.Value = hold;
            if (_sliderCurveWidth != null)
                _sliderCurveWidth.Value = width;
            if (_sliderCurveRounding != null)
                _sliderCurveRounding.Value = rounding;
            _suppress = wasSuppressed;
        }

        private void SetDirtSliders(float strength, float beat)
        {
            bool wasSuppressed = _suppress;
            _suppress = true;
            if (_sliderDirtStrength != null)
                _sliderDirtStrength.Value = strength;
            if (_sliderDirtBeat != null)
                _sliderDirtBeat.Value = beat;
            _suppress = wasSuppressed;
        }

        private void ShowNameInInputField()
        {
            if (_inputFieldName != null && _baseName != null)
                _inputFieldName.SetTextWithoutNotify(_baseName);
        }
        #endregion


        #region Save and delete
        private void OnPressedSave()
        {
            string name = _inputFieldName != null ? _inputFieldName.text.Trim() : "";
            if (string.IsNullOrEmpty(name))
                name = "My Palette";
            if (name == TerrainColorPalette.CustomName)
                name += " copy";

            var palette = new TerrainColorPalette
            {
                Name = name,
                OverallIntensity = OverallIntensity,
                BloomThreshold = BloomThreshold,
                BloomIntensity = BloomIntensity,
                BloomScatter = BloomScatter,
                CurveShape = (int)Shape,
                CurveStrength = CurveStrength,
                CurveFloor = CurveFloor,
                ColorShift = ColorShift,
                ColorHold = ColorHold,
                CurveWidth = CurveWidth,
                CurveRounding = CurveRounding
            };
            foreach (TerrainColorEntryUI entry in _entries)
                palette.Colors.Add(new Vector3(entry.Hue, entry.Saturation, entry.MenuIntensity));

            TerrainColorPalette existing = FindPalette(name);
            if (existing != null)
                _palettes[_palettes.IndexOf(existing)] = palette;
            else
                _palettes.Add(palette);
            _palettes.Sort((a, b) => string.Compare(a.Name, b.Name, System.StringComparison.OrdinalIgnoreCase));

            TerrainColorPaletteStore.Save(palette);
            _baseName = name;
            _modified = false;
            ShowNameInInputField();
            RebuildDropdown();
            OnUIValueChanged();
        }

        private void OnPressedDelete()
        {
            TerrainColorPalette palette = _baseName != null ? FindPalette(_baseName) : null;
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

            _palettes.Remove(palette);
            TerrainColorPaletteStore.Delete(palette.Name);
            _baseName = null;
            _modified = false;
            ResetDeleteButton();
            RebuildDropdown();
            OnUIValueChanged();
        }

        private void RefreshButtons()
        {
            ResetDeleteButton();
            if (_buttonDelete != null)
                _buttonDelete.interactable = IsSavedPalette;
        }

        private void ResetDeleteButton()
        {
            _deleteConfirmUntil = 0f;
            if (_textDelete != null)
                _textDelete.text = "Delete palette";
        }
        #endregion


        #region Apply
        /// <summary>The values differ from the selected palette now. The dropdown shows that with "(modified)".</summary>
        private void MarkModified()
        {
            if (_baseName != null && !_modified)
            {
                _modified = true;
                _rebuildPending = true;
            }
        }

        private void OnEntryChanged()
        {
            if (_suppress)
                return;

            MarkModified();
            ApplyColors();
            ApplyBloom();
            OnUIValueChanged();
        }

        private void OnOverallIntensityChanged(float value)
        {
            if (!_ready || _suppress)
                return;

            MarkModified();
            ApplyColors();
            OnUIValueChanged();
        }

        private void OnPatternBalanceChanged(float value)
        {
            if (!_ready || _suppress)
                return;

            OnUIValueChanged();
        }

        private void OnBloomChanged(float value)
        {
            if (!_ready || _suppress)
                return;

            MarkModified();
            ApplyBloom();
            OnUIValueChanged();
        }

        private void ApplyColors()
        {
            ApplyColorCurve();
            ApplyBackground();
        }

        #region Color curve
        private void OnCurveChanged(float value)
        {
            if (!_ready || _suppress)
                return;

            MarkModified();
            ApplyColorCurve();
            OnUIValueChanged();
        }

        /// <summary>
        /// Key times with the shift and the hold width applied. The shift bends the times as time^(2^shift), so the ends stay
        /// where they are. The hold width scales the distance of the two keys of a color around their middle, as far as the
        /// neighbours allow.
        /// </summary>
        private float[] BuildKeyTimes()
        {
            int keyCount = _originalColorKeys.Length;
            var time = new float[keyCount];
            float exponent = Mathf.Pow(2f, ColorShift);
            for (int i = 0; i < keyCount; i++)
                time[i] = Mathf.Pow(_originalColorKeys[i].time, exponent);

            float hold = ColorHold;
            for (int i = 1; i < keyCount - 2; i++)
            {
                if (_keyColorIndex[i] != _keyColorIndex[i + 1])
                    continue;
                float middle = (time[i] + time[i + 1]) * 0.5f;
                float half = (time[i + 1] - time[i]) * 0.5f * hold;
                time[i] = Mathf.Max(middle - half, time[i - 1] + MinKeyGap);
                time[i + 1] = Mathf.Min(middle + half, time[i + 2] - MinKeyGap);
                time[i + 1] = Mathf.Max(time[i + 1], time[i] + MinKeyGap);
                i++;
            }
            return time;
        }

        /// <summary>
        /// Calculates the color table straight from the four colors instead of reading the gradient. The glow (exposure) of
        /// every key is exactly the menu value of its color, the color between two keys is a straight blend of the two
        /// colors, and the glow between them runs on a straight line plus a wave where that line is (nearly) flat.
        /// </summary>
        private void ApplyColorCurve()
        {
            if (_terrain == null)
                return;

            int keyCount = _originalColorKeys.Length;
            float[] time = BuildKeyTimes();
            var glow = new float[keyCount];
            var unit = new Color[keyCount];
            for (int i = 0; i < keyCount; i++)
            {
                TerrainColorEntryUI entry = _entries[_keyColorIndex[i]];
                float saturation = i == 0 ? Mathf.Clamp01(entry.Saturation + _saturationOffset[i]) : entry.Saturation;
                unit[i] = TerrainColorMapping.ToHdrColor(entry.Hue, saturation, 0f);
                glow[i] = TerrainColorMapping.MenuToIntensity(entry.MenuIntensity) + Mathf.Log(ColorCompensation(entry), 2f);
                if (i == 0)
                    glow[i] = Mathf.Min(glow[i] + _intensityOffset[i], TerrainColorMapping.MenuToIntensity(CurveFloor));
            }

            int size = TerrainVertexJob.LutSize;
            if (_curveTable == null || _curveTable.Length != size)
                _curveTable = new Color[size];
            float overall = OverallIntensity;
            int segment = 0;
            for (int t = 0; t < size; t++)
            {
                float h = (float)t / (size - 1);
                while (segment < keyCount - 2 && h > time[segment + 1])
                    segment++;
                float width = time[segment + 1] - time[segment];
                float u = width > 0f ? Mathf.Clamp01((h - time[segment]) / width) : 0f;
                float g = Mathf.Lerp(glow[segment], glow[segment + 1], u);
                if (segment > 0)
                    g += CurveWave(h, time[segment], width, glow[segment + 1] - glow[segment]);
                Color c = Color.Lerp(unit[segment], unit[segment + 1], u);
                float factor = Mathf.Pow(2f, g) * overall;
                _curveTable[t] = new Color(c.r * factor, c.g * factor, c.b * factor, 1f);
            }
            _terrain.SetColorTable(_curveTable);
        }

        /// <summary>
        /// Triangle over one period (f 0..1, valley at 0 and 1, peak at 0.5, slope 2). The two turning points are rounded with a
        /// parabola of half-width rounding * 0.25 periods, the straight parts keep their slope. With rounding above 0 the
        /// valley lies at rounding * 0.25 and the peak at 1 minus that.
        /// </summary>
        private static float RoundedTriangle(float f, float rounding)
        {
            float a = rounding * 0.25f;
            float fromPeak = Mathf.Abs(f - 0.5f);
            float fromValley = Mathf.Min(f, 1f - f);
            if (a > 0f && fromPeak < a)
                return 1f - a - fromPeak * fromPeak / a;
            if (a > 0f && fromValley < a)
                return a + fromValley * fromValley / a;
            return 1f - Mathf.Abs(2f * f - 1f);
        }

        /// <summary>Wave in exposure steps added to a stretch of the glow line, 0 at both ends of the stretch.</summary>
        private float CurveWave(float h, float start, float width, float rise)
        {
            CurveShape shape = Shape;
            float steepness = CurveStrength;
            if (shape == CurveShape.Plateau || steepness <= 0f || width <= 0f)
                return 0f;
            if (Mathf.Abs(rise / width) >= steepness * 0.5f)
                return 0f;

            int count = Mathf.Max(1, Mathf.RoundToInt(width / CurveWidth));
            float period = width / count;
            float u = (h - start) / width * count;
            float f = u - Mathf.Floor(u);
            if (h >= start + width)
                f = 0f;

            switch (shape)
            {
                case CurveShape.SpikesUp:
                    return steepness * period * 0.5f * (RoundedTriangle(f, CurveRounding) - CurveRounding * 0.25f);
                case CurveShape.SpikesDown:
                    return -steepness * period * 0.5f * (RoundedTriangle(f, CurveRounding) - CurveRounding * 0.25f);
                case CurveShape.ZigZag:
                    // The same triangle, moved by a quarter period so it swings around 0.
                    float zig = RoundedTriangle(f + 0.25f - Mathf.Floor(f + 0.25f), CurveRounding) * 2f - 1f;
                    return steepness * period * 0.25f * zig;
                case CurveShape.SawUp:
                    return steepness * period * f;
                default:
                    return -steepness * period * f;
            }
        }
        #endregion

        /// <summary>Turns the sky colors by the same angle the base color is turned away from the reference hue.</summary>
        private void ApplyBackground()
        {
            if (_bloomVolume == null)
                return;
            if (_sky == null)
            {
                if (!_bloomVolume.profile.TryGet(out _sky))
                    return;
                _skyMiddle = _sky.middle.value;
                _skyBottom = _sky.bottom.value;
            }

            bool tint = (_toggleTintBackground == null || _toggleTintBackground.isOn) && _entries.Count > 0;
            float hue = _entries.Count > 0 ? _entries[0].Hue : 0f;
            _sky.middle.Override(tint ? WithHue(_skyMiddle, hue) : _skyMiddle);
            _sky.bottom.Override(tint ? WithHue(_skyBottom, hue) : _skyBottom);
        }

        private static Color WithHue(Color color, float degrees)
        {
            Color.RGBToHSV(color, out _, out float s, out float v);
            Color tinted = Color.HSVToRGB(Mathf.Repeat(degrees / 360f, 1f), s, v, true);
            tinted.a = color.a;
            return tinted;
        }

        private void ApplyBloom()
        {
            if (_bloomVolume == null)
                return;
            // Volume.profile is a copy of the shared profile, so the asset on disk is not changed.
            if (_bloom == null && !_bloomVolume.profile.TryGet(out _bloom))
                return;

            _bloom.threshold.Override(BloomThreshold);
            _bloom.intensity.Override(BloomIntensity * SaturationCompensation());
            _bloom.scatter.Override(BloomScatter);
        }

        private void OnDirtChanged(float value)
        {
            if (!_ready || _suppress)
                return;

            ApplyDirt();
            OnUIValueChanged();
        }

        private void ApplyDirt()
        {
            if (_lensDirt == null)
                return;
            _lensDirt.Strength = DirtStrength;
            _lensDirt.BeatAmount = DirtBeat;
        }

        /// <summary>
        /// A color with less saturation is much brighter than the same color at full saturation (pure blue 7 %,
        /// pure red 21 %, white 100 % luminance), and the bloom grows with it. Two hidden corrections keep the
        /// look steady when the saturation drops: the color itself is dimmed by the square root of the luminance
        /// ratio (our eye sees saturated colors brighter than their luminance), and the bloom intensity is lowered
        /// by the same factor again, so the glow energy follows the luminance exactly. Saturation 1 changes nothing.
        /// </summary>
        private static float ColorCompensation(TerrainColorEntryUI entry)
        {
            float full = Luminance(TerrainColorMapping.ToHdrColor(entry.Hue, 1f, 0f));
            float now = Luminance(TerrainColorMapping.ToHdrColor(entry.Hue, entry.Saturation, 0f));
            return Mathf.Clamp(Mathf.Sqrt(full / Mathf.Max(0.0001f, now)), MinColorCompensation, 1f);
        }

        /// <summary>Average of the color compensation over all colors, applied once more to the bloom intensity.</summary>
        private float SaturationCompensation()
        {
            float sum = 0f;
            foreach (TerrainColorEntryUI entry in _entries)
                sum += ColorCompensation(entry);
            return _entries.Count > 0 ? sum / _entries.Count : 1f;
        }

        /// <summary>Lowest factor the compensation may reach, so that even pure white stays visible.</summary>
        private const float MinColorCompensation = 0.15f;

        private static float Luminance(Color color)
        {
            return 0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b;
        }
        #endregion
    }
}
