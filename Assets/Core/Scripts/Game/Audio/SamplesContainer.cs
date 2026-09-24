using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi.Audio
{
    public class SamplesContainer : MonoBehaviour
    {
        [SerializeField] private AudioSampleSettingsPrefab _sampleSettings;
        [SerializeField] private bool _recalculateOnUpdate;

        public float[] SpectrumData { get { TryRecalculate(); return _spectrumData; } }
        private float[] _spectrumData;
        private float _average;
        public float Average { get { TryRecalculate(); return _average; } }

        private AudioSampleSettings _finalSampleSettings;
        private bool _recalculate = true;

        public int BandSize { get { TryRecalculate(); return  _finalSampleSettings.BandSize; } }

        //[SerializeField] private AmplitudeModule _amplitudeModule;
        //private float _amplitude; 
        //public float Amplitude { get { CalculateAmplitude(); return _amplitudeModule.GetAmplitude(_spectrumData); } }


        #region Mono
        private void Awake()
        {
            _recalculate = true;
        }
        private void Start()
        {
            TryRecalculate();
        }
        private void LateUpdate()
        {
            _recalculate |= _recalculateOnUpdate;
        }
        #endregion


        //public override float GetFloat(float value)
        //{
        //    return SpectrumData
        //}


        #region Recalculation
        public void TryRecalculate()
        {
            if (_recalculate)
                RecalculateSpectrumData();
        }

        public void ForceRecalculate()
        {
            RecalculateSpectrumData();
        }

        private void RecalculateSpectrumData()
        {
            _recalculate = false;
            if (_sampleSettings == null)
                return;

            // sample settings
            _finalSampleSettings = new AudioSampleSettings(_sampleSettings.Settings);

            // audio clip settings
            if (AudioSampler.Instance.AudioPlayer == null ||
                !AudioSampler.Instance.AudioPlayer.IsPlaying ||
                AudioSampler.Instance.AudioPlayer.CurrentlyPlayingClip == null ||
                AudioSampler.Instance.AudioPlayer.CurrentlyPlayingClip.Settings == null)
            {
                _spectrumData = AudioSampler.Instance.GenerateSpectrumData(_finalSampleSettings);
                CalculatAverage();
                return;
            }

            // else modify the sample settings with the clipSettings before calculating
            AudioClipSettings clipSettings = new AudioClipSettings(AudioSampler.Instance.AudioPlayer.CurrentlyPlayingClip.Settings);
            _finalSampleSettings.AmplitudeModifierOverall *= clipSettings.AmplitudeModifierOverall;
            for (int i = 0; i < clipSettings.AmplitudeModifierWeightedList.Count; i++)
                _finalSampleSettings.AmplitudeModifierWeightedList.Add(clipSettings.AmplitudeModifierWeightedList[i]);

            _spectrumData = AudioSampler.Instance.GenerateSpectrumData(_finalSampleSettings);
            CalculatAverage();
        }

        private void CalculatAverage()
        {
            if (_spectrumData == null || _spectrumData.Length == 0)
                return;

            // average
            _average = 0;
            foreach (float sample in _spectrumData)
                _average += sample;
            _average /= _spectrumData.Length;
        }

        private void CalculateAmplitude()
        {
            TryRecalculate();
        }
        #endregion
    }
}