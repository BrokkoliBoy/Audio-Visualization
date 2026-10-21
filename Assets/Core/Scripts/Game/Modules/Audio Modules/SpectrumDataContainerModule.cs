using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gavi.Modules.MathModules.FloatArrayModules.VoidToFloatArrayModules;
using Gavi.Audio;

namespace Gavi.Modules.DataModules.ArrayModules.FloatArrayModules.VoidToFloatArrayModules
{
    public class SpectrumDataContainerModule : VoidToFloatArrayModule
    {
        [Header("--- Spectrum Data Container Module ---")]
        [SerializeField] private AudioSampleSettingsPrefab _sampleSettings;
        public AudioSampleSettings Settings => _audioSettings;
        
        public int BandSize => _audioSettings.BandSize;
        public float Average => _average;
        private float _average;
        private AudioSampleSettings _audioSettings = new AudioSampleSettings();

        protected override void Recalculate()
        {
            base.Recalculate();
            if (_sampleSettings == null)
                return;

            // sample settings
            //_finalSampleSettings = new AudioSampleSettings(_sampleSettings.Settings);

            // if no audio clip settings, just generate spectrum data and return
            if (AudioSampler.Instance.AudioPlayer == null ||
                !AudioSampler.Instance.AudioPlayer.IsPlaying ||
                AudioSampler.Instance.AudioPlayer.CurrentlyPlayingClip == null ||
                AudioSampler.Instance.AudioPlayer.CurrentlyPlayingClip.Settings == null)
            {
                _audioSettings.SetData(_sampleSettings.Settings);
                _value = AudioSampler.Instance.GenerateSpectrumData(_audioSettings);
                AudioTestSignal.Override(_value);
                CalculatAverage();
                return;
            }

            // else modify the sample settings with the clipSettings before calculating
            // AudioSampleSettings sampleSettings = null; // TODO: GC?
            if (AudioSampler.Instance.AudioPlayer.CurrentlyPlayingClip.OverrideSampleSettings != null)
            {
                _audioSettings.SetData(AudioSampler.Instance.AudioPlayer.CurrentlyPlayingClip.OverrideSampleSettings.Settings);
                //sampleSettings = new AudioSampleSettings(_audioSettings);
            }
            else // TODO: Why do we not apply the _sampleSettings when applying the clip.OverrideSampleSettings?
            {
                _audioSettings.SetData(_sampleSettings.Settings);
                //sampleSettings = new AudioSampleSettings(_sampleSettings.Settings);
            }
            
            // applying clip settings manually here
            AudioClipSettings clipSettings = new AudioClipSettings(AudioSampler.Instance.AudioPlayer.CurrentlyPlayingClip.Settings);
            //sampleSettings.AmplitudeModifierOverall *= clipSettings.AmplitudeModifierOverall;
            _audioSettings.AmplitudeModifierOverall *= clipSettings.AmplitudeModifierOverall;
            for (int i = 0; i < clipSettings.AmplitudeModifierWeightedList.Count; i++)
                _audioSettings.AmplitudeModifierWeightedList.Add(clipSettings.AmplitudeModifierWeightedList[i]);
                //sampleSettings.AmplitudeModifierWeightedList.Add(clipSettings.AmplitudeModifierWeightedList[i]);
 
            _value = AudioSampler.Instance.GenerateSpectrumData(_audioSettings);
            AudioTestSignal.Override(_value);
            // _value =AudioSampler.Instance.GenerateSpectrumData(sampleSettings);
            CalculatAverage();
        }
        
        private void CalculatAverage()
        {
            _average = 0;
            if (_value == null || _value.Length == 0)
                return;

            foreach (float sample in _value)
                _average += sample;
            _average /= _value.Length;
        }

        
        #region Overrides
        protected override void InitializeArray()
        {
            if (_sampleSettings == null)
                _value = new float[0];
            else
                _value = new float[_audioSettings.BandSize];
            base.InitializeArray();
        }

        public override int Length()
        {
            if (_sampleSettings == null)
                return 0;
            return _audioSettings.BandSize;
        }
        
        protected override bool IsValid()
        {
            if (_sampleSettings == null)
                return false;
            return base.IsValid();
        }

        protected override string GetName()
        {
            return "SpectrumDataContainer";
        }
        #endregion
    }
}