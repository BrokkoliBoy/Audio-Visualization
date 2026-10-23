using UnityEngine;

namespace Gavi
{
    /// <summary>Moves the amount of the mesh morph over time: a breath on every beat (smooth or like on a spring) and a slow wave in time with the beats. Shares its window with the morph (Expert tab of that window).</summary>
    public class TerrainGroupUIMorphMotion : TerrainGroupUI
    {
        [SerializeField] private SliderG _sliderBeat;
        [SerializeField] private SliderG _sliderAttack;
        [SerializeField] private SliderG _sliderRelease;
        [SerializeField] private SliderG _sliderBounce;
        [SerializeField] private SliderG _sliderWaveDepth;
        [SerializeField] private SliderG _sliderWaveBeats;

        private bool _ready;
        private TerrainSettingsProfile _shownProfile;

        private void Awake()
        {
            _sliderBeat.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderAttack.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderRelease.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderBounce.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderWaveDepth.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderWaveBeats.OnValueChangedFloat.AddListener(OnUIValueChanged);

            // The group is switched off while the other tab of the window is shown, so it can be filled with a profile before it wakes up.
            _ready = true;
            if (_shownProfile != null)
                ShowProfile(_shownProfile);
        }

        public override void ExtractProfile(TerrainSettingsProfile profile)
        {
            // before the first Awake the sliders say nothing, the profile keeps its values
            if (!_ready)
                return;
            base.ExtractProfile(profile);
            profile.MotionBeat = _sliderBeat.Value;
            profile.MotionAttack = _sliderAttack.Value;
            profile.MotionRelease = _sliderRelease.Value;
            profile.MotionBounce = _sliderBounce.Value;
            profile.MotionWaveDepth = _sliderWaveDepth.Value;
            profile.MotionWaveBeats = _sliderWaveBeats.Value;
        }

        public override void ShowProfile(TerrainSettingsProfile profile)
        {
            _shownProfile = profile;
            if (!_ready)
                return;
            base.ShowProfile(profile);
            _sliderBeat.Value = profile.MotionBeat;
            _sliderAttack.Value = profile.MotionAttack;
            _sliderRelease.Value = profile.MotionRelease;
            _sliderBounce.Value = profile.MotionBounce;
            _sliderWaveDepth.Value = profile.MotionWaveDepth;
            _sliderWaveBeats.Value = profile.MotionWaveBeats;
        }
    }
}
