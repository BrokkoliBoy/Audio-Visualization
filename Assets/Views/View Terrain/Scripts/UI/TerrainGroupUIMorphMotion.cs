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

        [Tooltip("Opacity of the sliders while the morph is off (they have no effect then).")]
        [SerializeField] private float _dimmedAlpha = 0.4f;

        private bool _ready;
        private TerrainSettingsProfile _shownProfile;
        private CanvasGroup[] _dimGroups;

        private void Awake()
        {
            _sliderBeat.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderAttack.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderRelease.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderBounce.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderWaveDepth.OnValueChangedFloat.AddListener(OnUIValueChanged);
            _sliderWaveBeats.OnValueChangedFloat.AddListener(OnUIValueChanged);

            SliderG[] sliders = { _sliderBeat, _sliderAttack, _sliderRelease, _sliderBounce, _sliderWaveDepth, _sliderWaveBeats };
            _dimGroups = new CanvasGroup[sliders.Length];
            for (int i = 0; i < sliders.Length; i++)
            {
                _dimGroups[i] = sliders[i].GetComponent<CanvasGroup>();
                if (_dimGroups[i] == null)
                    _dimGroups[i] = sliders[i].gameObject.AddComponent<CanvasGroup>();
            }

            // one switch for the whole window: the copy in this tab sets the real one, which is the one the profile reads
            if (_toggleMirror != null)
                _toggleMirror.onValueChanged.AddListener(isOn => { if (_toggleMorph.isOn != isOn) _toggleMorph.isOn = isOn; });
            _toggleMorph.onValueChanged.AddListener(_ => SyncMirror());

            // The group is switched off while the other tab of the window is shown, so it can be filled with a profile before it wakes up.
            _ready = true;
            if (_shownProfile != null)
                ShowProfile(_shownProfile);
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
            float alpha = _toggleMorph.isOn ? 1f : _dimmedAlpha;
            foreach (CanvasGroup group in _dimGroups)
                group.alpha = alpha;
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
            SyncMirror();
        }
    }
}
