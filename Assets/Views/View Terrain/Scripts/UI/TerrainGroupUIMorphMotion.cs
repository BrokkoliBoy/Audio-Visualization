using UnityEngine;
using UnityEngine.UI;

namespace Gavi
{
    /// <summary>Moves the amount of the mesh morph over time: a breath on every beat (smooth or like on a spring) and a slow wave in time with the beats. Lives in the same window as the morph.</summary>
    public class TerrainGroupUIMorphMotion : TerrainGroupUI
    {
        [Tooltip("The on / off toggle of the morph. Its state is the only one, this window just shows it too.")]
        [SerializeField] private Toggle _toggleMorph;
        [Tooltip("Optional: a copy of that toggle in the header of another page.")]
        [SerializeField] private Toggle _toggleMirror;
        [SerializeField] private SliderG _sliderBeat;
        [SerializeField] private SliderG _sliderAttack;
        [SerializeField] private SliderG _sliderRelease;
        [SerializeField] private SliderG _sliderBounce;
        [SerializeField] private SliderG _sliderWaveDepth;
        [SerializeField] private SliderG _sliderWaveBeats;

        private bool _ready;
        private TerrainSettingsPreset _shownPreset;

        private void Awake()
        {
            _sliderBeat.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderAttack.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderRelease.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderBounce.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderWaveDepth.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderWaveBeats.OnValueChangedFloat.AddListener(OnUIValueChanged);

            // one switch for the whole window: the copy in this tab sets the real one, which is the one the preset reads
            if (_toggleMirror != null)
                _toggleMirror.onValueChanged.AddListener(isOn => { if (_toggleMorph.isOn != isOn) _toggleMorph.isOn = isOn; });
            _toggleMorph.onValueChanged.AddListener(_ => SyncMirror());

            // The group is switched off while the other tab of the window is shown, so it can be filled with a preset before it wakes up.
            _ready = true;
            if (_shownPreset != null)
                ShowPreset(_shownPreset);
        }

        private void OnEnable()
        {
            if (_ready)
                SyncMirror();
        }

        private void SyncMirror()
        {
            if (_toggleMirror != null)
                _toggleMirror.SetIsOnWithoutNotify(_toggleMorph.isOn);
        }

        public override void ExtractPreset(TerrainSettingsPreset preset)
        {
            // before the first Awake the sliders say nothing, the preset keeps its values
            if (!_ready)
                return;
            base.ExtractPreset(preset);
            preset.MotionBeat = _sliderBeat.Value;
            preset.MotionAttack = _sliderAttack.Value;
            preset.MotionRelease = _sliderRelease.Value;
            preset.MotionBounce = _sliderBounce.Value;
            preset.MotionWaveDepth = _sliderWaveDepth.Value;
            preset.MotionWaveBeats = _sliderWaveBeats.Value;
        }

        public override void ShowPreset(TerrainSettingsPreset preset)
        {
            _shownPreset = preset;
            if (!_ready)
                return;
            base.ShowPreset(preset);
            _sliderBeat.Value = preset.MotionBeat;
            _sliderAttack.Value = preset.MotionAttack;
            _sliderRelease.Value = preset.MotionRelease;
            _sliderBounce.Value = preset.MotionBounce;
            _sliderWaveDepth.Value = preset.MotionWaveDepth;
            _sliderWaveBeats.Value = preset.MotionWaveBeats;
            SyncMirror();
        }
    }
}
