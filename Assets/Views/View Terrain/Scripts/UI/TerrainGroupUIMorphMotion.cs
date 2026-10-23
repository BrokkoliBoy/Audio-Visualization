using UnityEngine;

namespace Gavi
{
    /// <summary>Moves the amount of the mesh morph over time: a breath on every beat (smooth or like on a spring) and a slow wave in time with the beats.</summary>
    public class TerrainGroupUIMorphMotion : TerrainGroupUI
    {
        [SerializeField] private SliderG _sliderBeat;
        [SerializeField] private SliderG _sliderAttack;
        [SerializeField] private SliderG _sliderRelease;
        [SerializeField] private SliderG _sliderBounce;
        [SerializeField] private SliderG _sliderWaveDepth;
        [SerializeField] private SliderG _sliderWaveBeats;

        private void Awake()
        {
            _sliderBeat.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderAttack.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderRelease.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderBounce.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderWaveDepth.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderWaveBeats.OnValueChangedFloat.AddListener(OnUIValueChanged);
        }

        public override void ExtractProfile(TerrainSettingsProfile profile)
        {
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
