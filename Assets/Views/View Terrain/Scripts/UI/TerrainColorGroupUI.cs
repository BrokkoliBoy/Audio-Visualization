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
    /// (see ApplyColorCurve): the glow of every color follows the menu, the colors blend in a straight line, and the two long
    /// color areas are tilted so the height stays readable. The gradient of the scene is only the template for the key times (which color owns which stretch)
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
        [Tooltip("Optional. Shows the Base color from dark to bright as the track of the Brightness slider.")]
        [SerializeField] private UiSliderGradient _gradientOverallIntensity;
        [Tooltip("Evens out the colors between noise patterns, see TerrainGenerator.ColorNoiseBalance. 0 = off.")]
        [SerializeField] private SliderG _sliderPatternBalance;
        [Tooltip("Soft end of the gradient for heights over 1, see TerrainGenerator.ColorHeadroom. 0 = hard end.")]
        [SerializeField] private SliderG _sliderHeadroom;

        [Header("--- Bloom ---")]
        [Tooltip("Volume of the terrain view. Its profile is copied at runtime, so the profile asset stays unchanged.")]
        [SerializeField] private Volume _bloomVolume;
        [SerializeField] private SliderG _sliderBloomThreshold;
        [SerializeField] private SliderG _sliderBloomIntensity;
        [Tooltip("Optional. Track of the bloom strength slider: the Base color, bleaching out with more bloom.")]
        [SerializeField] private UiSliderGradient _gradientBloomIntensity;
        [SerializeField] private SliderG _sliderBloomScatter;
        [Tooltip("Optional. Header toggle of the glow group: the whole bloom on or off (part of the profile).")]
        [SerializeField] private Toggle _toggleBloom;

        [Header("--- Background ---")]
        [Tooltip("How far the sky of the bloom volume takes the hue of the base color (the first one): 0 = untinted dark gray, 1 = fully tinted. Brightness and saturation come from the sky colors in the volume.")]
        [SerializeField] private SliderG _sliderTintBackground;

        [Header("--- Lens dirt (part of the profile, not of the palette) ---")]
        [SerializeField] private TerrainLensDirt _lensDirt;
        [Tooltip("Optional. Toggle in the lens dirt heading: the whole lens dirt on or off. The tinted background is not affected.")]
        [SerializeField] private Toggle _toggleDirt;
        [SerializeField] private SliderG _sliderDirtStrength;
        [Tooltip("Optional. Track of the dirt strength slider: from a clean dark lens to a dirty, lit one.")]
        [SerializeField] private UiSliderGradient _gradientDirtStrength;
        [SerializeField] private SliderG _sliderDirtBeat;

        [Header("--- Gradient keys ---")]
        [Tooltip("For every key of the terrain gradient: which color (index of the entries) it belongs to.")]
        [SerializeField] private int[] _keyColorIndex = { 0, 0, 1, 2, 3 };
        [Tooltip("For every color: the key that carries the color exactly. The other keys of the color are offsets of it.")]
        [SerializeField] private int[] _referenceKey = { 1, 2, 3, 4 };

        public enum TiltMode { Auto, Rising, Falling, Locked }

        [Header("--- Color curve ---")]
        [Tooltip("Direction the two long color areas are tilted in. Index of TiltMode.")]
        [SerializeField] private TMP_Dropdown _dropdownTiltMode;
        [Tooltip("Brightness of the lowest vertices as a share of the glow of the base color (0-1, in linear brightness).")]
        [SerializeField] private SliderG _sliderCurveFloor;
        [Tooltip("Moves the colors along the height: + lower (the hot colors take more area), - higher.")]
        [SerializeField] private SliderG _sliderColorShift;
        [Tooltip("How strongly the colors blend, 0 to 1. The slider is the opposite of the hold width of the two long color areas (stored as hold: 0.5 = hold 1 = as in the scene gradient, 1 = hold 0.2, 0 = hold 2.5).")]
        [SerializeField] private SliderG _sliderColorHold;
        [Tooltip("Slope of the two long color areas in exposure steps per full height, so the height stays readable. The direction comes from the tilt mode, the slope scales up for bright colors and every stretch of the curve keeps at least this slope (even if a color then misses its menu glow). 0 = flat areas.")]
        [SerializeField] private SliderG _sliderPlateauTilt;
        [Tooltip("Fake light: shades the terrain by the slope of the ground against a fixed light, in exposure steps (see TerrainGenerator.ReliefStrength). Gives contours where the color is flat, on top of the tilt. 0 = off.")]
        [SerializeField] private SliderG _sliderRelief;
        [Tooltip("Evens out how bright the hues look at the same glow: yellow and green are dimmed, red and blue are lifted (up to one exposure step each way, the reference is orange). 0 = off.")]
        [SerializeField] private SliderG _sliderHueBalance;
        /// <summary>Smallest distance between two key times, so no stretch collapses.</summary>
        private const float MinKeyGap = 0.01f;
        /// <summary>Floor share 0 would be black at infinite depth in the exposure scale; this is the lowest share used (about 10 steps down).</summary>
        private const float MinFloorShare = 0.001f;

        private Color[] _curveTable;
        private float[] _glowCurve;
        /// <summary>Slope of the glow (exposure steps per full height) at which the relief light is down to half of its strength.</summary>
        private const float FlatSlope = 8f;
        /// <summary>Tilt directions of the Locked mode: one per color block, then one per stretch between two blocks. Null = take them from the current colors next time.</summary>
        private float[] _lockedDirection;

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
        private bool BloomOn => _toggleBloom == null || _toggleBloom.isOn;
        private bool DirtOn => _toggleDirt == null || _toggleDirt.isOn;
        private float DirtStrength => _sliderDirtStrength != null ? _sliderDirtStrength.Value : TerrainLensDirt.DefaultStrength;
        private float DirtBeat => _sliderDirtBeat != null ? _sliderDirtBeat.Value : TerrainLensDirt.DefaultBeat;
        private float TintBackground => _sliderTintBackground != null ? _sliderTintBackground.Value : 1f;
        private TiltMode Mode => _dropdownTiltMode != null ? (TiltMode)_dropdownTiltMode.value : (TiltMode)TerrainColorPalette.DefaultTiltDirection;
        /// <summary>The lowest vertices get this share of the glow of the base color.</summary>
        private float FloorShare => _sliderCurveFloor != null ? _sliderCurveFloor.Value : TerrainColorPalette.DefaultFloorShare;
        private float ColorShift => _sliderColorShift != null ? _sliderColorShift.Value : TerrainColorPalette.DefaultColorShift;
        private float ColorHold => _sliderColorHold != null ? HoldFromBlend(_sliderColorHold.Value) : TerrainColorPalette.DefaultColorHold;
        private const float MinHold = 0.2f;
        private const float MaxHold = 2.5f;
        private const float BlendAtDefaultHold = 0.5f;

        /// <summary>The blend slider (1 = strongest blending) as the hold width of the long color areas: 0 = widest, 0.5 = as in the scene gradient, 1 = narrowest.</summary>
        private static float HoldFromBlend(float blend)
        {
            blend = Mathf.Clamp01(blend);
            return blend <= BlendAtDefaultHold
                ? Mathf.Lerp(MaxHold, TerrainColorPalette.DefaultColorHold, blend / BlendAtDefaultHold)
                : Mathf.Lerp(TerrainColorPalette.DefaultColorHold, MinHold, (blend - BlendAtDefaultHold) / (1f - BlendAtDefaultHold));
        }

        private static float BlendFromHold(float hold)
        {
            hold = Mathf.Clamp(hold, MinHold, MaxHold);
            return hold >= TerrainColorPalette.DefaultColorHold
                ? Mathf.InverseLerp(MaxHold, TerrainColorPalette.DefaultColorHold, hold) * BlendAtDefaultHold
                : BlendAtDefaultHold + Mathf.InverseLerp(TerrainColorPalette.DefaultColorHold, MinHold, hold) * (1f - BlendAtDefaultHold);
        }
        private float PlateauTilt => _sliderPlateauTilt != null ? _sliderPlateauTilt.Value : TerrainColorPalette.DefaultPlateauTilt;
        private float Relief => _sliderRelief != null ? _sliderRelief.Value : TerrainColorPalette.DefaultRelief;
        private float HueBalance => _sliderHueBalance != null ? _sliderHueBalance.Value : TerrainColorPalette.DefaultHueBalance;
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
            BindSliderPreviews();
            ListenToSlider(_sliderOverallIntensity, _defaultOverallIntensity, OnOverallIntensityChanged);
            ListenToSlider(_sliderPatternBalance, 1f, OnPatternBalanceChanged);
            ListenToSlider(_sliderHeadroom, 0f, OnPatternBalanceChanged);
            ListenToSlider(_sliderBloomThreshold, TerrainColorPalette.DefaultBloomThreshold, OnBloomChanged);
            ListenToSlider(_sliderBloomIntensity, TerrainColorPalette.DefaultBloomIntensity, OnBloomChanged);
            ListenToSlider(_sliderBloomScatter, TerrainColorPalette.DefaultBloomScatter, OnBloomChanged);
            ListenToSlider(_sliderDirtStrength, TerrainLensDirt.DefaultStrength, OnDirtChanged);
            ListenToSlider(_sliderDirtBeat, TerrainLensDirt.DefaultBeat, OnDirtChanged);
            ListenToSlider(_sliderCurveFloor, TerrainColorPalette.DefaultFloorShare, OnCurveChanged);
            ListenToSlider(_sliderColorShift, TerrainColorPalette.DefaultColorShift, OnCurveChanged);
            ListenToSlider(_sliderColorHold, BlendFromHold(TerrainColorPalette.DefaultColorHold), OnCurveChanged);
            ListenToSlider(_sliderPlateauTilt, TerrainColorPalette.DefaultPlateauTilt, OnCurveChanged);
            ListenToSlider(_sliderRelief, TerrainColorPalette.DefaultRelief, OnCurveChanged);
            ListenToSlider(_sliderHueBalance, TerrainColorPalette.DefaultHueBalance, OnCurveChanged);
            if (_dropdownTiltMode != null)
            {
                _dropdownTiltMode.ClearOptions();
                _dropdownTiltMode.AddOptions(new List<string>(System.Enum.GetNames(typeof(TiltMode))));
                _dropdownTiltMode.SetValueWithoutNotify(TerrainColorPalette.DefaultTiltDirection);
                _dropdownTiltMode.onValueChanged.AddListener(_ =>
                {
                    _lockedDirection = null;
                    OnCurveChanged(0f);
                });
            }
            _suppress = false;
            ApplyDirt();

            foreach (TerrainColorEntryUI entry in _entries)
                entry.Changed += OnEntryChanged;

            ListenToSlider(_sliderTintBackground, 1f, _ => ApplyBackground());
            if (_toggleBloom != null)
                _toggleBloom.onValueChanged.AddListener(_ => OnGlowSwitched());
            if (_toggleDirt != null)
                _toggleDirt.onValueChanged.AddListener(_ => OnGlowSwitched());
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
            profile.TiltDirection = (int)Mode;
            profile.FloorShare = FloorShare;
            profile.ColorShift = ColorShift;
            profile.ColorHold = ColorHold;
            profile.PlateauTilt = PlateauTilt;
            profile.ReliefStrength = Relief;
            profile.HueBalance = HueBalance;
            profile.ColorNoiseBalance = PatternBalance;
            profile.ColorHeadroom = Headroom;
            profile.BloomThreshold = BloomThreshold;
            profile.BloomIntensity = BloomIntensity;
            profile.BloomScatter = BloomScatter;
            profile.BloomEnabled = BloomOn;
            profile.DirtEnabled = DirtOn;
            profile.DirtStrength = DirtStrength;
            profile.DirtBeat = DirtBeat;
            profile.TintBackground = TintBackground;
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
            if (_sliderTintBackground != null)
                _sliderTintBackground.Value = profile.TintBackground;
            if (_toggleBloom != null)
                _toggleBloom.SetIsOnWithoutNotify(profile.BloomEnabled);
            if (_toggleDirt != null)
                _toggleDirt.SetIsOnWithoutNotify(profile.DirtEnabled);
            ApplyBloom();
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
            SetCurveLook(profile.TiltDirection, profile.FloorShare, profile.ColorShift, profile.ColorHold, profile.PlateauTilt, profile.ReliefStrength, profile.HueBalance);
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
                && palette.TiltDirection == profile.TiltDirection
                && Mathf.Abs(palette.FloorShare - profile.FloorShare) < 0.001f
                && Mathf.Abs(palette.ColorShift - profile.ColorShift) < 0.001f
                && Mathf.Abs(palette.ColorHold - profile.ColorHold) < 0.001f
                && Mathf.Abs(palette.PlateauTilt - profile.PlateauTilt) < 0.001f
                && Mathf.Abs(palette.ReliefStrength - profile.ReliefStrength) < 0.001f
                && Mathf.Abs(palette.HueBalance - profile.HueBalance) < 0.001f;
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
            SetCurveLook(TerrainColorPalette.DefaultTiltDirection, TerrainColorPalette.DefaultFloorShare, TerrainColorPalette.DefaultColorShift, TerrainColorPalette.DefaultColorHold, TerrainColorPalette.DefaultPlateauTilt, TerrainColorPalette.DefaultRelief, TerrainColorPalette.DefaultHueBalance);
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
            SetCurveLook(palette.TiltDirection, palette.FloorShare, palette.ColorShift, palette.ColorHold, palette.PlateauTilt, palette.ReliefStrength, palette.HueBalance);
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

        private void SetCurveLook(int mode, float floor, float shift, float hold, float tilt, float relief, float balance)
        {
            bool wasSuppressed = _suppress;
            _suppress = true;
            if (_dropdownTiltMode != null)
                _dropdownTiltMode.SetValueWithoutNotify(Mathf.Clamp(mode, 0, System.Enum.GetValues(typeof(TiltMode)).Length - 1));
            if (_sliderCurveFloor != null)
                _sliderCurveFloor.Value = floor;
            if (_sliderColorShift != null)
                _sliderColorShift.Value = shift;
            if (_sliderColorHold != null)
                _sliderColorHold.Value = BlendFromHold(hold);
            if (_sliderPlateauTilt != null)
                _sliderPlateauTilt.Value = tilt;
            if (_sliderRelief != null)
                _sliderRelief.Value = relief;
            if (_sliderHueBalance != null)
                _sliderHueBalance.Value = balance;
            _lockedDirection = null;
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
                TiltDirection = (int)Mode,
                FloorShare = FloorShare,
                ColorShift = ColorShift,
                ColorHold = ColorHold,
                PlateauTilt = PlateauTilt,
                ReliefStrength = Relief,
                HueBalance = HueBalance
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
            RefreshBrightnessPreview();
        }

        private Color BaseColorPreview()
        {
            if (_entries.Count == 0)
                return Color.white;
            TerrainColorEntryUI baseEntry = _entries[0];
            return TerrainColorMapping.Preview(baseEntry.Hue, baseEntry.Saturation, baseEntry.MenuIntensity);
        }

        /// <summary>Binds the slider tracks that show the Base color. They follow the sliders and refresh when the colors change.</summary>
        private void BindSliderPreviews()
        {
            if (_gradientOverallIntensity != null && _sliderOverallIntensity != null)
                _gradientOverallIntensity.Bind(_sliderOverallIntensity, t =>
                {
                    // the Base color times the brightness the slider would give: dark on the left, white-hot over 1
                    float brightness = _sliderOverallIntensity.ValueAtPosition(t);
                    Color baseColor = BaseColorPreview();
                    if (brightness <= 1f)
                        return Color.Lerp(Color.black, baseColor, brightness);
                    return Color.Lerp(baseColor, Color.white, Mathf.Clamp01((brightness - 1f) * 0.5f));
                });
            if (_gradientBloomIntensity != null && _sliderBloomIntensity != null)
                _gradientBloomIntensity.Bind(_sliderBloomIntensity, t => Color.Lerp(BaseColorPreview(), Color.white, 0.85f * t));
            if (_gradientDirtStrength != null && _sliderDirtStrength != null)
                _gradientDirtStrength.Bind(_sliderDirtStrength, t => Color.Lerp(new Color(0.08f, 0.08f, 0.1f), Color.Lerp(BaseColorPreview(), new Color(0.75f, 0.7f, 0.6f), 0.5f), t));
        }

        private void RefreshBrightnessPreview()
        {
            if (_gradientOverallIntensity != null) _gradientOverallIntensity.Refresh();
            if (_gradientBloomIntensity != null) _gradientBloomIntensity.Refresh();
            if (_gradientDirtStrength != null) _gradientDirtStrength.Refresh();
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
        /// colors, and the glow between them runs on a straight line between the (tilted) keys.
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
                glow[i] = TerrainColorMapping.MenuToIntensity(entry.MenuIntensity) + Mathf.Log(ColorCompensation(entry), 2f) + HueOffset(entry);
                if (i == 0)
                    glow[i] += Mathf.Log(Mathf.Max(FloorShare, MinFloorShare), 2f);
            }

            ApplyTilt(time, glow, unit);
            _terrain.ReliefStrength = Relief;

            int size = TerrainVertexJob.LutSize;
            if (_curveTable == null || _curveTable.Length != size)
                _curveTable = new Color[size];
            float overall = OverallIntensity;
            if (_glowCurve == null || _glowCurve.Length != size)
                _glowCurve = new float[size];
            int segment = 0;
            for (int t = 0; t < size; t++)
            {
                float h = (float)t / (size - 1);
                while (segment < keyCount - 2 && h > time[segment + 1])
                    segment++;
                float width = time[segment + 1] - time[segment];
                float u = width > 0f ? Mathf.Clamp01((h - time[segment]) / width) : 0f;
                float g = Mathf.Lerp(glow[segment], glow[segment + 1], u);
                Color c = Color.Lerp(unit[segment], unit[segment + 1], u);
                float factor = Mathf.Pow(2f, g) * overall;
                _glowCurve[t] = g;
                _curveTable[t] = new Color(c.r * factor, c.g * factor, c.b * factor, 1f);
            }
            // The alpha of the table carries how flat the curve is there (1 = flat, towards 0 = steep): the relief light is stronger where
            // the glow gives little contour. TerrainReliefJob reads it and sets the alpha back to 1.
            if (Relief > 0f)
            {
                float step = 1f / (size - 1);
                for (int t = 0; t < size; t++)
                {
                    float slope = Mathf.Abs(_glowCurve[Mathf.Min(t + 1, size - 1)] - _glowCurve[Mathf.Max(t - 1, 0)]) / (step * (Mathf.Min(t + 1, size - 1) - Mathf.Max(t - 1, 0)));
                    Color c = _curveTable[t];
                    _curveTable[t] = new Color(c.r, c.g, c.b, 1f / (1f + slope / FlatSlope));
                }
            }
            _terrain.SetColorTable(_curveTable);
        }


        /// <summary>
        /// Exposure steps that even out how bright a hue looks at the same glow: the eye sees green and yellow much brighter than
        /// red and blue. Relative to orange, half the log ratio of the luminances (like the saturation compensation), at most one
        /// step each way, scaled by the Hue balance slider.
        /// </summary>
        private float HueOffset(TerrainColorEntryUI entry)
        {
            float balance = HueBalance;
            if (balance <= 0f)
                return 0f;
            float luminance = Luminance(TerrainColorMapping.ToHdrColor(entry.Hue, 1f, 0f));
            float reference = Luminance(TerrainColorMapping.ToHdrColor(30f, 1f, 0f));
            return balance * Mathf.Clamp(0.5f * Mathf.Log(reference / Mathf.Max(luminance, 0.01f), 2f), -1f, 1f);
        }

        /// <summary>Largest factor the tilt of a bright color may be scaled up by.</summary>
        private const float MaxTiltBoost = 2f;

        /// <summary>How far (in exposure steps) the push apart of two color areas may move a key from its menu glow.</summary>
        private const float MaxTiltShift = 1f;

        /// <summary>Total slope (steps per full height) of the two neighbours at which a tilt reaches its full direction; below it fades through zero.</summary>
        private const float DirectionRamp = 2f;

        /// <summary>Glow difference of two neighbouring color areas at which the push apart is at full strength; below it fades out, so nothing jumps when two glows cross.</summary>
        private const float GapRamp = 1f;

        /// <summary>
        /// How much more tilt a color needs to show the same step on the screen: the brighter it is (luminance times exposure),
        /// the more the tone mapping flattens a step. 1 for dark colors, up to MaxTiltBoost.
        /// </summary>
        private static float TiltBoost(Color unit, float glow)
        {
            float x = Luminance(unit) * Mathf.Pow(2f, glow);
            float sensitivity = x < 0.001f ? 1f : x / (Mathf.Exp(x) - 1f);
            return Mathf.Clamp(1f / Mathf.Max(sensitivity, 0.01f), 1f, MaxTiltBoost);
        }

        /// <summary>
        /// Tilts the two long color areas (key pairs of one color) around their menu glow so the height stays readable. The
        /// direction depends on the tilt mode: Auto follows the neighbouring colors (at a valley or a peak: the steeper neighbour),
        /// Rising and Falling are fixed, Locked takes the Auto directions once and keeps them while the glows change. Then every
        /// stretch between two areas keeps at least the same slope. If it does not, the areas are pushed apart, which can move
        /// a color away from its menu glow, by at most MaxTiltShift (then the slope stays weaker). Changes the glow in place.
        private void ApplyTilt(float[] time, float[] glow, Color[] unit)
        {
            float tilt = PlateauTilt;
            if (tilt <= 0f)
                return;

            int n = glow.Length;
            var first = new List<int>();
            var last = new List<int>();
            for (int i = 0; i < n;)
            {
                bool pair = i >= 1 && i < n - 2 && _keyColorIndex[i] == _keyColorIndex[i + 1];
                first.Add(i);
                last.Add(pair ? i + 1 : i);
                i += pair ? 2 : 1;
            }

            int blocks = first.Count;
            var center = new float[blocks];
            var half = new float[blocks];
            var boost = new float[blocks];
            var offset = new float[blocks];
            var blockDirection = new float[blocks];
            TiltMode mode = Mode;
            const float budget = MaxTiltShift;
            bool useLock = mode == TiltMode.Locked && _lockedDirection != null && _lockedDirection.Length == blocks * 2 - 1;
            for (int b = 0; b < blocks; b++)
            {
                int s = first[b];
                int e = last[b];
                center[b] = glow[s];
                boost[b] = TiltBoost(unit[s], glow[s]);
                if (e == s)
                    continue;
                float slopeIn = (glow[s] - glow[s - 1]) / Mathf.Max(time[s] - time[s - 1], MinKeyGap);
                float slopeOut = (glow[e + 1] - glow[e]) / Mathf.Max(time[e + 1] - time[e], MinKeyGap);
                float direction;
                if (mode == TiltMode.Rising)
                    direction = 1f;
                else if (mode == TiltMode.Falling)
                    direction = -1f;
                else if (useLock)
                    direction = _lockedDirection[b];
                else
                    direction = Mathf.Clamp((slopeIn + slopeOut) / DirectionRamp, -1f, 1f);
                blockDirection[b] = direction;
                // Tilt fades smoothly: no direction (equal neighbours) = no tilt, and an end goes at most 75 % (plus a little) of the way to a neighbour.
                float limit = tilt * boost[b] * (time[e] - time[s]) * 0.5f;
                limit = Mathf.Min(limit, Mathf.Abs(glow[e + 1] - glow[s]) * 0.75f + 0.25f);
                limit = Mathf.Min(limit, Mathf.Abs(glow[s - 1] - glow[s]) * 0.75f + 0.25f);
                half[b] = direction * limit;
            }

            var gapDirection = new float[blocks - 1];
            var gapMinimum = new float[blocks - 1];
            var gapWeight = new float[blocks - 1];
            for (int b = 0; b < blocks - 1; b++)
            {
                float difference = center[b + 1] - center[b];
                gapDirection[b] = difference > 0f ? 1f : (difference < 0f ? -1f : (b > 0 ? gapDirection[b - 1] : 1f));
                gapWeight[b] = Mathf.Min(1f, Mathf.Abs(difference) / GapRamp);
                gapMinimum[b] = tilt * Mathf.Max(time[first[b + 1]] - time[last[b]], 0f);
                if (useLock)
                    gapDirection[b] = _lockedDirection[blocks + b];
            }

            if (mode == TiltMode.Locked && !useLock)
            {
                _lockedDirection = new float[blocks * 2 - 1];
                blockDirection.CopyTo(_lockedDirection, 0);
                gapDirection.CopyTo(_lockedDirection, blocks);
            }

            for (int pass = 0; pass < 40; pass++)
            {
                for (int b = 0; b < blocks - 1; b++)
                {
                    float gap = (center[b + 1] - half[b + 1] + offset[b + 1]) - (center[b] + half[b] + offset[b]);
                    float deficit = (gapMinimum[b] - gapDirection[b] * gap) * gapWeight[b];
                    if (deficit <= 0f)
                        continue;
                    offset[b + 1] += gapDirection[b] * deficit * 0.5f;
                    offset[b] -= gapDirection[b] * deficit * 0.5f;
                }
                offset[0] = Mathf.Min(offset[0], 0f);
                for (int b = 0; b < blocks; b++)
                    offset[b] = Mathf.Clamp(offset[b], -budget, budget);
            }

            for (int b = 0; b < blocks; b++)
            {
                glow[first[b]] = center[b] - half[b] + offset[b];
                glow[last[b]] = center[b] + half[b] + offset[b];
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

            float tint = _entries.Count > 0 && _sliderTintBackground != null ? _sliderTintBackground.Value : (_entries.Count > 0 ? 1f : 0f);
            float hue = _entries.Count > 0 ? _entries[0].Hue : 0f;
            _sky.middle.Override(Color.Lerp(Neutral(_skyMiddle), WithHue(_skyMiddle, hue), tint));
            _sky.bottom.Override(Color.Lerp(Neutral(_skyBottom), WithHue(_skyBottom, hue), tint));
        }

        // The untinted background is darkened. SDR already looks darker than HDR, so it gets less.
        private const float UntintedDarkenHdr = 0.3f;
        private const float UntintedDarkenSdr = 0.6f;

        /// <summary>A dark gray without any hue: the background when it is not tinted.</summary>
        private static Color Neutral(Color color)
        {
            Color.RGBToHSV(color, out _, out _, out float v);
            bool hdr = UnityEngine.HDROutputSettings.main != null && UnityEngine.HDROutputSettings.main.active;
            Color gray = Color.HSVToRGB(0f, 0f, v * (hdr ? UntintedDarkenHdr : UntintedDarkenSdr), true);
            gray.a = color.a;
            return gray;
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

            _bloom.active = BloomOn;
            _bloom.threshold.Override(BloomThreshold);
            _bloom.intensity.Override(BloomIntensity * SaturationCompensation());
            _bloom.scatter.Override(BloomScatter);
        }

        private void OnGlowSwitched()
        {
            if (!_ready || _suppress)
                return;

            ApplyBloom();
            ApplyDirt();
            OnUIValueChanged();
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
            _lensDirt.Enabled = DirtOn;
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
