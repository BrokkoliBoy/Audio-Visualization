using System;
using System.Collections;
using System.Collections.Generic;
using Gavi.Audio;
using Gavi.Modules.DataModules.ArrayModules.FloatArrayModules.VoidToFloatArrayModules;
using Gavi.Modules.MathModules.FloatArrayModules.VoidToFloatArrayModules;
using UnityEngine;

namespace Gavi.Modules
{
    public class SpectrumDataSliceArrayModule : VoidToFloatArrayModule
    {
        [Header("--- Spectrum Data Slice Array Module ---")]
        [Tooltip("Describes the minimum lower frequency range that a band needs to have to be included. " +
                 "That means that if _minHertz == 300, a band with a frequency range from 200-400 will be excluded.")]
        [SerializeField] private float _minHertz;
        [Tooltip("Describes the maximum upper frequency range that a band should have to be included. " +
                 "That means that if _maxHertz == 2000, a band with a frequency range from 1500-3000 will be excluded.")]
        [SerializeField] private float _maxHertz;
        [Tooltip("[Cannot be null] The SpectrumDataContainerModule that the module should take its band values from.")]
        [SerializeField] private SpectrumDataContainerModule _spectrumDataContainer;
        
        private List<float> _tmpSpectrumData = new List<float>();
        
        protected override void Recalculate()
        {
            base.Recalculate();
            AudioSampleSettings sampleSettings = ViewManager.Instance.CurrentMainSamplesContainer.Settings;
            if (sampleSettings == null)
                return;
            if (_spectrumDataContainer == null)
                return;

            _tmpSpectrumData.Clear();
            for (int bandIndex = 0; bandIndex < AudioSampler.Instance.BandCount; bandIndex++)
            {
                float min = AudioSampler.Instance.HertzRangeOfBand(bandIndex).x;
                float max = AudioSampler.Instance.HertzRangeOfBand(bandIndex).y;
                if (min < _minHertz)
                    continue;
                if (max > _maxHertz)
                    continue;
                
                _tmpSpectrumData.Add(_spectrumDataContainer.GetValue(bandIndex));
            }
            
            _value = _tmpSpectrumData.ToArray();
        }

        #region Overrides

        protected override void InitializeArray()
        {
            base.InitializeArray();
            TryRecalculate();
        }

        public override int Length()
        {
            TryRecalculate();
            return base.Length();
            // if (ViewManager.Instance.CurrentMainSamplesContainer == null)
            //     return 0;
            // return ViewManager.Instance.CurrentMainSamplesContainer.Length();
        }
        
        protected override bool IsValid()
        {
            if (ViewManager.Instance.CurrentMainSamplesContainer == null || 
                ViewManager.Instance.CurrentMainSamplesContainer.Settings == null ||
                _spectrumDataContainer == null)
                return false;
            return base.IsValid();
        }

        protected override string GetName()
        {
            return "Spectrum Data Slice Array Module (" + _minHertz + "/" + _maxHertz + ")";
        }
        #endregion
    }
}
