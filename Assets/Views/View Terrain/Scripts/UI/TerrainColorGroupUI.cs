using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;

namespace Gavi
{
    /// <summary>
    /// Color tab of the terrain menu. Four colors (hue, saturation, intensity) are written into the keys of the
    /// terrain height gradient. Each color owns a few keys of the gradient. The keys of a color share its hue and keep their saturation
    /// and intensity relative to the first (reference) key of their color, so hue 0 makes all its keys the same pure red,
    /// and the palette made from the reference keys matches the gradient from the scene in hue.
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
        [Tooltip("The first key of the gradient (the lowest vertices) never gets brighter than this exposure value, whatever the base color is set to. The gradient then rises to the chosen brightness.")]
        [SerializeField] private float _startKeyMaxIntensity = -4.5f;

        private Gradient _gradient;
        private GradientColorKey[] _originalColorKeys;
        private GradientAlphaKey[] _originalAlphaKeys;
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
        private float BloomThreshold => _sliderBloomThreshold != null ? _sliderBloomThreshold.Value : TerrainColorPalette.DefaultBloomThreshold;
        private float BloomIntensity => _sliderBloomIntensity != null ? _sliderBloomIntensity.Value : TerrainColorPalette.DefaultBloomIntensity;
        private float BloomScatter => _sliderBloomScatter != null ? _sliderBloomScatter.Value : TerrainColorPalette.DefaultBloomScatter;
        private float DirtStrength => _sliderDirtStrength != null ? _sliderDirtStrength.Value : TerrainLensDirt.DefaultStrength;
        private float DirtBeat => _sliderDirtBeat != null ? _sliderDirtBeat.Value : TerrainLensDirt.DefaultBeat;
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
            ListenToSlider(_sliderBloomThreshold, TerrainColorPalette.DefaultBloomThreshold, OnBloomChanged);
            ListenToSlider(_sliderBloomIntensity, TerrainColorPalette.DefaultBloomIntensity, OnBloomChanged);
            ListenToSlider(_sliderBloomScatter, TerrainColorPalette.DefaultBloomScatter, OnBloomChanged);
            ListenToSlider(_sliderDirtStrength, TerrainLensDirt.DefaultStrength, OnDirtChanged);
            ListenToSlider(_sliderDirtBeat, TerrainLensDirt.DefaultBeat, OnDirtChanged);
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
            for (int c = 0; c < _entries.Count; c++)
                _entries[c].SetValues(profile.ColorValues[c].x, profile.ColorValues[c].y, profile.ColorValues[c].z);
            _suppress = false;

            _baseName = palette != null ? palette.Name : null;
            _modified = palette != null && !Matches(palette, profile);
            ShowNameInInputField();
            ApplyEntriesToGradient();
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
                && Mathf.Abs(palette.BloomScatter - profile.BloomScatter) < 0.001f;
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
            for (int c = 0; c < _entries.Count && c < _referenceHue.Length; c++)
                _entries[c].SetValues(_referenceHue[c], _referenceSaturation[c], TerrainColorMapping.IntensityToMenu(_referenceIntensity[c]));
            _suppress = false;

            _baseName = null;
            _modified = false;
            ApplyEntriesToGradient();
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
            for (int c = 0; c < _entries.Count && c < palette.Colors.Count; c++)
                _entries[c].SetValues(palette.Colors[c].x, palette.Colors[c].y, palette.Colors[c].z);
            _suppress = false;

            _baseName = palette.Name;
            _modified = false;
            ShowNameInInputField();
            ApplyEntriesToGradient();
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
                BloomScatter = BloomScatter
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
            ApplyEntriesToGradient();
            ApplyBloom();
            OnUIValueChanged();
        }

        private void OnOverallIntensityChanged(float value)
        {
            if (!_ready || _suppress)
                return;

            MarkModified();
            ApplyEntriesToGradient();
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

        private void ApplyEntriesToGradient()
        {
            var keys = new GradientColorKey[_originalColorKeys.Length];
            float overall = OverallIntensity;

            for (int i = 0; i < keys.Length; i++)
            {
                int c = _keyColorIndex[i];
                TerrainColorEntryUI entry = _entries[c];

                float hue = entry.Hue;
                float saturation = Mathf.Clamp01(entry.Saturation + _saturationOffset[i]);
                // The intensity is an exposure value, so a factor becomes log2 of it.
                float intensity = TerrainColorMapping.MenuToIntensity(entry.MenuIntensity) + _intensityOffset[i]
                    + Mathf.Log(ColorCompensation(entry), 2f);
                if (i == 0)
                    intensity = Mathf.Min(intensity, _startKeyMaxIntensity);

                Color color = TerrainColorMapping.ToHdrColor(hue, saturation, intensity);
                keys[i] = new GradientColorKey(new Color(color.r * overall, color.g * overall, color.b * overall, 1f), _originalColorKeys[i].time);
            }

            _gradient.SetKeys(keys, _originalAlphaKeys);
            ApplyBackground();
        }

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
