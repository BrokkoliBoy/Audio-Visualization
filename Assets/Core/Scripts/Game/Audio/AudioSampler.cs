using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gavi.Utility;

namespace Gavi.Audio
{
    public class AudioSampler : MonoBehaviour
    {
        public static AudioSampler Instance;

        [SerializeField] private AudioPlayer _audioPlayer;
        public AudioPlayer AudioPlayer => _audioPlayer;
        //[SerializeField] private AudioSampleSettings _defaultSampleSettings;

        [Header("--- DEBUG ---")]
        [SerializeField] private bool _FORCE_RECALCULATE_ARRAYS;

        // audio
        private float[] _spectrumDataRaw;
        private float[] _spectrumDataBands;
        private float _lastHighestBandValue;
        private int _minBinsPerBand;
        private int[] _binRangesPerBand;
        private float[] _targetFrequencyOnBands;

        [SerializeField] private float[] _BINS_PER_BAND_DEBUG;
        private float _minHertz;
        
        private void Awake()
        {
            if (Instance != null)
                Debugger.LogInstanceError(typeof(AudioSampler));
            Instance = this;
        }

        public float[] GenerateSpectrumData(AudioSampleSettings settings)
        {
            if (_audioPlayer == null)
                return null;

            // TODO: Check if sample have already been generated with those specific settings this frame. If yes, return them immediately.

            CheckArraySizes(settings);
            _minHertz = settings.MinHertz;

            // get spectrum data
            _audioPlayer.GetSpectrumData(_spectrumDataRaw, 0, settings.FftWindow);
            if (!settings.UseBands)
                return _spectrumDataRaw;

            
            // translate raw spectrum data into frequency bands
            float highestBandValue = 0;
            float bandSqrt = 1;//Mathf.Sqrt(settings.BandSize) / 2;

            int currentBinIndex = 0;
            for (int bandIndex = 0; bandIndex < _binRangesPerBand.Length; bandIndex++)
            {
                // get average of current band
                float average = 0;
                for (int binIndex = 0; binIndex < _binRangesPerBand[bandIndex]; binIndex++)
                {
                    float overallModifier = settings.ApplyAmplitude ? settings.AmplitudeModifierOverall : 1;
                    float curveModifier = settings.ApplyAmplitude ? settings.AmplitudeModifierWeighted(bandIndex) : 1; // _bandsMultiplier.Evaluate((float)bandIndex / _frequencyBandSize) : 1;
                    float binCountModifier = currentBinIndex + 1 * (1 + bandSqrt);
                    float volumeModifier = !settings.NormalizeByVolume ? 1 : (_audioPlayer.Volume > 0.001f) ? (1 / _audioPlayer.Volume) : 0;
                    float value = _spectrumDataRaw[currentBinIndex] * overallModifier * curveModifier * binCountModifier * volumeModifier;
                    average += value;
                    currentBinIndex++;
                }
                average /= (settings.DevideByTotalBins ? currentBinIndex : _binRangesPerBand[bandIndex]) + 1;
                highestBandValue = Mathf.Max(average, highestBandValue);
                _spectrumDataBands[bandIndex] = average;
            }

            // apply averaging by adding a factor to all bands which multiplied by the highest value of all bands will equal settings.AveragingTargetValue
            if (settings.UseAveraging && highestBandValue > 0 && settings.AverageLerpFactor > 0)
            {
                // apply average
                //if (highestBandValue < _lastHighestBandValue)
                    _lastHighestBandValue = Mathf.Lerp(_lastHighestBandValue, highestBandValue, Utility.Utility.Smooth(settings.AverageLerpFactor, Utility.Utility.GetFpsEqualizer()));
                //else
                    //_lastHighestBandValue = highestBandValue;
                float averageFactor = 1 / _lastHighestBandValue;
                averageFactor *= settings.AveragingTargetValue;

                for (int bandIndex = 0; bandIndex < _spectrumDataBands.Length; bandIndex++)
                {
                    _spectrumDataBands[bandIndex] *= averageFactor;
                }
            }

            // _spectrumCalculatedThisFrame = true;
            return _spectrumDataBands;
        }

        private void CheckArraySizes(AudioSampleSettings settings)
        {
            bool recalculate = _FORCE_RECALCULATE_ARRAYS;
            if (settings.SampleSize > 0 && (_spectrumDataRaw == null || _spectrumDataRaw.Length != settings.SampleSize))
            {
                _spectrumDataRaw = new float[settings.SampleSize];
                recalculate = true;
            }
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (settings.UseBands && settings.BandSize > 0 && (_spectrumDataBands == null || _spectrumDataBands.Length != settings.BandSize))
            {
                _spectrumDataBands = new float[settings.BandSize];
                recalculate = true;
            }

            if (recalculate /*|| _FORCE_RECALCULATE*/)
            {
                RecalculateMinimumBinCountPerBand(settings);
                RecalculateBandRanges(settings);
            }
        }

        private void RecalculateBandRanges(AudioSampleSettings settings)
        {
            if (_binRangesPerBand == null || _binRangesPerBand.Length != settings.BandSize)
                _binRangesPerBand = new int[settings.BandSize];
#if UNITY_EDITOR
            if (_BINS_PER_BAND_DEBUG == null || _BINS_PER_BAND_DEBUG.Length != settings.BandSize)
                _BINS_PER_BAND_DEBUG = new float[settings.BandSize];
#endif
            // maximum bin number that each band should reach. It does not equal to the count of bin a band should have.
            // the count of bins per band is evaluated by subtracting the target bin number of the current band with the one of the previous band.
            if (_targetFrequencyOnBands == null || _targetFrequencyOnBands.Length != settings.BandSize)
                _targetFrequencyOnBands = new float[settings.BandSize];

            PopulateBandDistribution(settings);
#if UNITY_EDITOR
            _BINS_PER_BAND_DEBUG = _targetFrequencyOnBands;
#endif
            PopulateBandDistributionByFrequency(settings);
        }

 

        #region Band Distribution
        private void PopulateBandDistributionByFrequency(AudioSampleSettings settings)
        {
            int frequency = AudioSettings.outputSampleRate / 2;
            float herzPerBin = frequency / settings.SampleSize;

            int binsCovered = 0; // only used to track down remaining not covered bins and put it into the last band
            for (int bandIndex = 0; bandIndex < settings.BandSize; bandIndex++)
            {
                int binsThisBand = 1;
                if (bandIndex == 0) // in first band put in 
                    binsThisBand = (int)(settings.MinHertz / herzPerBin) + 1;
                else if (bandIndex == settings.BandSize - 1) // just put all remaining bins into the last band
                    binsThisBand = settings.SampleSize - binsCovered;
                else // else subtract this target frequency to last bands target frequency
                    binsThisBand = (int)((_targetFrequencyOnBands[bandIndex] - _targetFrequencyOnBands[bandIndex - 1]) / herzPerBin) + 1;
                binsCovered += binsThisBand;
                _binRangesPerBand[bandIndex] = binsThisBand;
            }
        }
        
        private void PopulateBandDistribution(AudioSampleSettings settings)
        {
            AudioSampleSettings.BandDistributionType distributionType = settings.BandDistribution;
            if (distributionType == AudioSampleSettings.BandDistributionType.Constant)
            {
                PopulateBandDistributionConstant(settings);
            }
            else if (distributionType == AudioSampleSettings.BandDistributionType.Direct)
            {
                if (settings.FrequencyCoveredPerBand.Length != settings.BandSize)
                {
                    Debugger.LogAssertionFail("Using BandDistributionType.Direct but settings.BandBinDistribution.Length != settings.BandSize! (" + settings.FrequencyCoveredPerBand.Length + ", " + settings.BandSize + ")");
                    distributionType = AudioSampleSettings.BandDistributionType.Logarithmic;
                }
                else
                    PopulateBandDistributionDirect(settings);
            }
            else if (distributionType == AudioSampleSettings.BandDistributionType.Predefined)
            {
                if (settings.BandSize != 7)
                {
                    Debugger.LogAssertionFail("Using BandDistributionType.Direct but settings.BandSize != 7! (" + settings.BandSize + ")");
                    distributionType = AudioSampleSettings.BandDistributionType.Logarithmic;
                }
                else
                    PopulateBandDistributionPredefined(settings);
            }
            if (distributionType == AudioSampleSettings.BandDistributionType.Logarithmic)
            {
                PopulateBandDistributionLogarithmic(settings);
            }
        }
        
        private void PopulateBandDistributionConstant(AudioSampleSettings settings)
        {
            int frequencyToCover = Mathf.Min(AudioSettings.outputSampleRate / 2, settings.MaxHertz);
            float herzPerBin = (float)frequencyToCover / settings.SampleSize;
            int binsPerBand = settings.SampleSize / settings.BandSize;
            int binsCovered = 0;
            for (int bandIndex = 0; bandIndex < settings.BandSize; bandIndex++)
            {
                if (bandIndex == settings.BandSize - 1)
                    _targetFrequencyOnBands[bandIndex] = (int)((settings.SampleSize - binsCovered) * herzPerBin);
                else
                    _targetFrequencyOnBands[bandIndex] = (int)(binsPerBand * herzPerBin);
                binsCovered += binsPerBand;
            }
        }
        
        private void PopulateBandDistributionDirect(AudioSampleSettings settings)
        {
            //Debug.Log(settings.BandBinDistribution.Length);
            //Debug.Log(_targetFrequencyOnBands.Length);
            settings.FrequencyCoveredPerBand.CopyTo(_targetFrequencyOnBands, 0);
            //for (int bandIndex = 0; bandIndex < _binsBandValues.Length; bandIndex++)
            //    targetFrequencyOnBands[bandIndex] = settings.BandBinsDistribution[bandIndex];
        }
        
        private void PopulateBandDistributionPredefined(AudioSampleSettings settings)
        {
            _targetFrequencyOnBands[0] = 60f;
            _targetFrequencyOnBands[1] = 250f;
            _targetFrequencyOnBands[2] = 500f;
            _targetFrequencyOnBands[3] = 2000f;
            _targetFrequencyOnBands[4] = 4000f;
            _targetFrequencyOnBands[5] = 6000f;
            _targetFrequencyOnBands[6] = 20000f;
        }
        
        private void PopulateBandDistributionLogarithmic(AudioSampleSettings settings)
        {
            // exponent
            int e = settings.BandSize;
            // base, already covered that it starts at settings.MinHertz
            float b = Mathf.Pow(settings.MaxHertz / settings.MinHertz, (float)1 / e);
            for(int bandIndex = 0; bandIndex < settings.BandSize; bandIndex++)
            {
                float frequencyThisFrame = (int)(settings.MinHertz * Utility.Utility.ExpInt(b, bandIndex + 1));
                //Debug.Log(settings.MinHertz + " * " + b + "^" + bandIndex + " (" + Utility.Utility.ExpInt(b, bandIndex) + ") = " + frequencyThisFrame);
                _targetFrequencyOnBands[bandIndex] = frequencyThisFrame;
            }
            return;

            // get the factor that if you divide the bin range at each band by it, will make the sum of all band ranges result to the SampleSize specified in the settings.
            // Here, we are looking for a base and an exponent, which when you power up the base exactly settings.BandSize times, will result in settings.SampleSize.
            // Example: settings.SampleSize=256, settings.BandSize=8. Question: What base number do you have to power up 8 times in order to get 256? Answer: 2, since 2^8=256.
            // We do this, so we will have a little amount of frequencies summed up into more bands and more frequencies into less bands. For example have the first band contain
            // frequencies 0-200, the second one 200-600, the third one 600 to 2000 and so on. I don't know much about audio theory but the internet says its good this way.
            //float baseValue = Mathf.Pow(settings.SampleSize, 1f / settings.BandSize);
            //int exponent = settings.BandSize;
            //float accumulatedBinCount = 0;
            //float binCountThisBand = 1;
            //for (int i = 0; i < exponent; i++)
            //{
            //    binCountThisBand *= baseValue;
            //    accumulatedBinCount += Mathf.Max(binCountThisBand, _minBinsPerBand);
            //    _targetFrequencyOnBands[i] = (int)(accumulatedBinCount * hertzPerBin);
            //}
            //float bandBinsFactor = accumulatedBinCount / settings.SampleSize;

            //float binCountPerBand = 1;
            //int currentBinIndex = 0;
            //for (int bandIndex = 0; bandIndex < _spectrumDataBands.Length; bandIndex++)
            //{
            //    binCountPerBand *= baseValue;
            //    int binSizeThisBand = (int)(binCountPerBand / bandBinsFactor);
            //    binSizeThisBand = Mathf.Max(binSizeThisBand, _minBinsPerBand);
            //    // if we have arrived at the last band, just take all the remaining bins.
            //    // The math does not necessarily reach exactly the audio clips frequency due to flooring of the bin count multiple times.
            //    if (bandIndex == _spectrumDataBands.Length - 1)
            //        binSizeThisBand = settings.SampleSize - currentBinIndex - 1;
            //    currentBinIndex += binSizeThisBand;
            //    _binRangesPerBand[bandIndex] = binSizeThisBand;
            //    //_BINS_PER_BAND_DEBUG[bandIndex] = binSizeThisBand;
            //}
        }
        
        private void RecalculateMinimumBinCountPerBand(AudioSampleSettings settings)
        {
            int frequency = AudioSettings.outputSampleRate / 2;
            float herzPerBin = frequency / settings.SampleSize;
            _minBinsPerBand = (int)(settings.MinHertz / herzPerBin) + 1;

            //float accumulatedHertz = 0;
            //_minBinsPerBand = 0;
            //while (settings.MinHertz > accumulatedHertz)
            //{
            //    _minBinsPerBand++;
            //    accumulatedHertz += hertzPerBin;
            //}
        }
        #endregion


        #region Public Getters
        public int BandCount => _binRangesPerBand == null ? 0 : _binRangesPerBand.Length;

        public Vector2 HertzRangeOfBand(int bandIndex)
        {
            Vector2 range = new Vector2();
            if (_binRangesPerBand.Length == 0)
                return range;
            
            range.x = bandIndex == 0 ? 0 : _targetFrequencyOnBands[bandIndex - 1];
            range.y = _targetFrequencyOnBands[bandIndex];
            
            return range;
        }
        #endregion
    }
    
}