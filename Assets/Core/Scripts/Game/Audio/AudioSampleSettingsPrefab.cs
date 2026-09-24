using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gavi.Utility;

namespace Gavi.Audio
{
    public class AudioSampleSettingsPrefab : MonoBehaviour
    {
        [SerializeField] private AudioSampleSettings _setting;
        public AudioSampleSettings Settings => _setting;
    }

    [System.Serializable]
    public class AudioSampleSettings
    {
        [SerializeField] private AudioSampleSettingsPrefab _prefabContainer;
        public AudioSampleSettings PrefabSettings => _prefabContainer != null ? _prefabContainer.Settings : null;
        //private AudioSampleSettings _prefabSettingsCache;

        public enum Redirection { Prefab, None }
        public enum Operation { Use, MultiplyWith }


        #region 
        public AudioSampleSettings()
        {
            
        }
        public AudioSampleSettings(AudioSampleSettings originalSettings)
        {
            SetData(originalSettings);
        }

        public void SetData(AudioSampleSettings originalSettings)
        {
            if (originalSettings == null)
            {
                Debugger.LogError("originalSettings in AudioSampleSettings constructor is null!");
                return;
            }
            
            SampleSize = originalSettings.SampleSizeRec;
            FftWindow = originalSettings.FftWindowRec;

            UseBands = originalSettings.UseBandsRec;
            //if (!UseBands)
            //    return;
            BandSize = originalSettings.BandSizeRec;

            BandDistribution = originalSettings.BandDistributionRec;
            MinHertz = originalSettings.MinHertzRec;
            MaxHertz = originalSettings.MaxHertzRec;
            BandDistribution = originalSettings.BandDistributionRec;
            // FrequencyCoveredPerBand = new float[BandSize]; // TODO: ???
            FrequencyCoveredPerBand = originalSettings.FrequencyCoveredPerBandRec;

            ApplyAmplitude = originalSettings.ApplyAmplitudeRec;
            AmplitudeModifierOverall = originalSettings.AmplitudeModifierOverallRec;
            AmplitudeModifierWeightedList.Clear();
            originalSettings.AmplitudeModifierWeightedRec(AmplitudeModifierWeightedList);

            UseAveraging = originalSettings.UseAveragingRec;
            AverageLerpFactor = originalSettings.AverageLerpFactorRec;
            AveragingTargetValue = originalSettings.AveragingTargetRec;

            //MinBandSamplesDynamicly = originalSettings.MinBandSamplesDynamiclyRec;
            DevideByTotalBins = originalSettings.DevideByTotalBinsRec;
            NormalizeByVolume = originalSettings.NormalizeByVolumeRec;
        }
        #endregion


        #region General Settings
        [Header("--- Base Settings ---")]

        // Sample Size
        [SerializeField] private Redirection _sampleSizeRedirection = Redirection.Prefab;
        [Tooltip("The targeted size of the sample array which the samples will be cast into from the original audio sequence data over the last frame. It's highly recommended to cast this data once more into an " +
            "even smaller sample data (\"Audio Bands\"). When doing this second cast, several additional options can be used to create customized and advanced sampling data.")]
        public int SampleSize = 256;
        private int SampleSizeRec {
            get {
                if (_sampleSizeRedirection == Redirection.None || PrefabSettings == null)
                    return SampleSize;
                return PrefabSettings.SampleSizeRec;
            }
        }

        // FFTWindow
        [Space]
        [SerializeField] private Redirection _fftWindowRedirection = Redirection.Prefab;
        [Tooltip("Type of spectrum analasys when perform the first cast.")]
        public FFTWindow FftWindow = FFTWindow.Hanning;
        private FFTWindow FftWindowRec {
            get {
                if (_fftWindowRedirection == Redirection.None || PrefabSettings == null)
                    return FftWindow;
                return PrefabSettings.FftWindowRec;
            }
        }
        #endregion


        #region Band Settings
        [Header("--- Bands ---")]

        // Use Bands
        [SerializeField] private Redirection _useBandsRedirection = Redirection.Prefab;
        [Tooltip("When true, a second sampling will be done. Here, several options can be used to create customized sampling behaviour.")]
        public bool UseBands = true;
        private bool UseBandsRec {
            get {
                if (_useBandsRedirection == Redirection.None || PrefabSettings == null)
                    return UseBands;
                return PrefabSettings.UseBandsRec;
            }
        }

        // Band Size
        [Space]
        [SerializeField] private Redirection _bandSizeRedirection = Redirection.Prefab;
        [Tooltip("Size of the target array of the second sample cast.")]
        [Min(2)] public int BandSize = 8;
        private int BandSizeRec {
            get {
                if (_bandSizeRedirection == Redirection.None || PrefabSettings == null)
                    return BandSize;
                return PrefabSettings.BandSizeRec;
            }
        }
        #endregion


        #region Band Bin Distribution
        [Header("--- Bin Distribution ---")]
        [Space]

        // Band Distribution Type
        [SerializeField] private Redirection _bandDistributionRedirection = Redirection.Prefab;
        [Tooltip("Specifies the way in which the frequencies are covered over the bands. Typically you want to have less bands for lower Frequencies." +
             "\n\n" +
             "Contant: Have all bands cover the same amount of frequencies.\n" +
             "Logarithmic: This will increase the frequency window covered with each band. The first band covers MinHertz, the last band up to MaxHertz. The value to increase the " +
             "frequency frame per band is evaluated accordingly.\n" +
             "Direct: You can fill the array BandBinDistribution (see below) and fill it with the bin number to cover. The values represent which frequency should be covered with " +
             "each band, NOT how many frequencies nor how many bins to use per band. For example an array with elements 200 and 600 will have the first band cover frequencies 0-200 and " +
             "the second one cover 200-600. Note, that the arrays length has to fit the BandSize. Otherwise, logarithmic will be used. Those settings will override MinHerz and MaxHertz.\n" +
             "Predefined: This emulates BandDistributionType.Direct with the following 7 elements: 60 (Sub-bass), 250 (bass), 500 (Low Midrange), 2000 (Midrange), 4000 (Upper Midrange), " +
             "6000 (Presence) and the rest (Brilliance). Note that this only works if BandSize==7! If BandSize!=7, logarithmic will be used. Those settings will override MinHerz and MaxHertz.")]
        public BandDistributionType BandDistribution;
        public enum BandDistributionType { Constant, Logarithmic, Direct, Predefined }
        private BandDistributionType BandDistributionRec
        {
            get
            {
                if (_bandDistributionRedirection == Redirection.None || PrefabSettings == null)
                    return BandDistribution;
                return PrefabSettings.BandDistributionRec;
            }
        }

        // Band bin Distribution
        [Space]
        [SerializeField] private Redirection _frequencyCoveredPerBandRedirection = Redirection.Prefab;
        [Tooltip("The values represent which frequency should be covered with " +
            "each band, NOT how many frequencies nor how many bins to use per band. For example an array with elements 200 and 600 will have the first band cover frequencies 0-200 and " +
            "the second one cover 200-600. Note, that the arrays length has to fit the BandSize. Otherwise, logarithmic will be chosen. Unity samples with a sample rate of 48000, so make" +
            " sure to have at max 24000.")]
        public float[] FrequencyCoveredPerBand;
        public float[] FrequencyCoveredPerBandRec {
           get {
                if (_frequencyCoveredPerBandRedirection == Redirection.None || PrefabSettings == null)
                    return FrequencyCoveredPerBand;
                return PrefabSettings.FrequencyCoveredPerBandRec;
           }
        }

        // Min Hertz
        [Space]
        [SerializeField] private Redirection _minHertzRedirection = Redirection.Prefab;
        [SerializeField] private Operation _minHertzOperation = Operation.Use;
        [Tooltip("The minimum amount of Hertz that the bands should at least cover. A value of >= 60 is recommended. This value only comes to use when BandDistribution == BandDistributionType.Logarithmic")]
        [Range(1, 24000)]public int MinHertz = 60;
        private int MinHertzRec
        {
            get
            {
                if (_minHertzRedirection == Redirection.None || PrefabSettings == null)
                    return MinHertz;
                return PrefabSettings.MinHertzRec * (_minHertzOperation == Operation.MultiplyWith ? MinHertz : 1);
            }
        }

        // Max Hertz
        [Space]
        [SerializeField] private Redirection _maxHertzRedirection = Redirection.Prefab;
        [SerializeField] private Operation _maxHertzOperation = Operation.Use;
        [Tooltip("The maximum amount of Hertz that the bins in the last band should cover. A value of <= 20000 is recommended since frequencies beyond that point are not really used in music. This value only comes to" +
            " use BandDistribution == BandDistributionType.Direct || BandDistribution != BandDistributionType.Constant.")]
        [Range(1, 24000)]public int MaxHertz = 20000;
        private int MaxHertzRec
        {
            get
            {
                if (_maxHertzRedirection == Redirection.None || PrefabSettings == null)
                    return MaxHertz;
                return PrefabSettings.MaxHertzRec * (_maxHertzOperation == Operation.MultiplyWith ? MaxHertz : 1);
            }
        }
        #endregion


        #region Amplitude Settings
        [Header("--- Amplitude Multiplier ---")]

        // Apply Amplify
        [SerializeField] private Redirection _applyAmplitudeRedirection = Redirection.Prefab;
        [Tooltip("When true, the gathered values will be multiplied by two factors: First an overall factor which will be applied to all bands, and secondly a multiplier which can variy from band to band.")]
        public bool ApplyAmplitude = true;
        private bool ApplyAmplitudeRec {
            get {
                if (_applyAmplitudeRedirection == Redirection.None || PrefabSettings == null)
                    return ApplyAmplitude;
                return PrefabSettings.ApplyAmplitudeRec;
            }
        }

        // Amply Modifier Overall
        [Space]
        [SerializeField] private Redirection _amplitudeModifierOverallRedirection = Redirection.Prefab;
        [SerializeField] private Operation _amplitudeModifierOverallOperation = Operation.Use;
        [Tooltip("Overall multiplier applied to the values in all bands.")]
        public float AmplitudeModifierOverall = 1;
        private float AmplitudeModifierOverallRec {
            get {
                if (_amplitudeModifierOverallRedirection == Redirection.None || PrefabSettings == null)
                    return AmplitudeModifierOverall;
                return PrefabSettings.AmplitudeModifierOverallRec * (_amplitudeModifierOverallOperation == Operation.MultiplyWith ? AmplitudeModifierOverall : 1);
            }
        }

        // Amply Modifier Weighted
        [Space]
        [SerializeField] private Redirection _amplitudeModifierWeightedRedirection = Redirection.Prefab;
        [SerializeField] private Operation _amplitudeModifierWeightedOperation = Operation.Use;
        [Tooltip("Multiplier which is applied throughout all bands individually via a curve. f(0) is the value applied to the first band, f(1) the value of the last band.")]
        public List<AnimationCurve> AmplitudeModifierWeightedList = new List<AnimationCurve>();
        public float AmplitudeModifierWeighted(int bandIndex)
        {
            float value = 1;
            foreach (AnimationCurve curve in AmplitudeModifierWeightedList)
                value *= curve.Evaluate((float)bandIndex / BandSize);
            return value;
        }
        private void AmplitudeModifierWeightedRec(List<AnimationCurve> curveListToBeFilled, bool isRecursive = true)
        {
            if (_amplitudeModifierWeightedRedirection == Redirection.None || PrefabSettings == null)
            {
                for (int i = 0; i < AmplitudeModifierWeightedList.Count; i++)
                    curveListToBeFilled.Add(AmplitudeModifierWeightedList[i]);
                return;
            }
            // if use prefab one, clear own list, but ONLY if no recursion has been done yet. Otherwise the prefabs list will be altered, which we do not want
            if (_amplitudeModifierWeightedOperation == Operation.Use && !isRecursive)
                AmplitudeModifierWeightedList.Clear();
            // else if it is Operation.Multiply and the function has been called recursively, add the own curves. You do not need to do that if it was not recursive because then 
            // the to be filled List amplitudeModifierWeighted already contains the own curves
            else if (isRecursive)
            {
                for (int i = 0; i < AmplitudeModifierWeightedList.Count; i++)
                    curveListToBeFilled.Add(AmplitudeModifierWeightedList[i]);
            }
            PrefabSettings.AmplitudeModifierWeightedRec(curveListToBeFilled);
        }
        #endregion


        #region Averaging
        [Header("--- Averaging Out ---")]

        // Use Average Out
        [SerializeField] private Redirection _useAveragingRedirection = Redirection.Prefab;
        [Tooltip("When true, a constant factor is applied to all values in all bands which depends on the highest value of all bins.")]
        public bool UseAveraging = false;
        private bool UseAveragingRec {
            get {
                if (_useAveragingRedirection == Redirection.None || PrefabSettings == null)
                    return UseAveraging;
                return PrefabSettings.UseAveragingRec;
            }
        }

        // Average out Lerp Factor
        [Space]
        [SerializeField] private Redirection _averageLerpFactorRedirection = Redirection.Prefab;
        [SerializeField] private Operation _averageLerpFactorOperation = Operation.Use;
        [Tooltip("Each frame, the targeted average value is calculated by lerping between the average this frame and the last frame." +
            " The higher the value, the faster the lerping to the current average.")]
        [Range(0.0001f, 1f)] public float AverageLerpFactor = 0.1f;
        private float AverageLerpFactorRec {
            get {
                if (_averageLerpFactorRedirection == Redirection.None || PrefabSettings == null)
                    return AverageLerpFactor;
                return PrefabSettings.AverageLerpFactorRec * (_averageLerpFactorOperation == Operation.MultiplyWith ? AverageLerpFactor : 1);
            }
        }

        // Average out Lerp Strength
        [Space]
        [SerializeField] private Redirection _averagingTargetValueRedirection = Redirection.Prefab;
        [SerializeField] private Operation _averagingTargetValueOperation = Operation.Use;
        [Tooltip("With this variable, you can choose which value the highest value should become.")]
        public float AveragingTargetValue = 0.5f;
        private float AveragingTargetRec {
            get {
                if (_averagingTargetValueRedirection == Redirection.None || PrefabSettings == null)
                    return AveragingTargetValue;
                return PrefabSettings.AveragingTargetRec * (_averagingTargetValueOperation == Operation.MultiplyWith ? AveragingTargetValue : 1);
            }
        }
        #endregion


        #region Misc
        [Header("--- Misc ---")]

        // Devide by Total Binss
        [Space]
        [SerializeField] private Redirection _devideByTotalBinsRedirection = Redirection.Prefab;
        [Tooltip("When casting to band bins, the value of each band is the average of a couple of bins from the originating bins. When calculating that average, you can either devide the accumulated values by " +
            "the amount of bins used for the accumulation of exactly that band or of by the amound of total bins that were evaluated yet. By setting this variable here to true, the average will be calculated by the " +
            "total bin count to the respective point and not by the amount of bins within the respective band. This would resulate in the 'average' I've spoken so far not really being a mathematically correct average " +
            "but an 'average' which gets smaller the higher the band number." +
            "\n\n" +
            "I saw someone doing it that way and I could not find a reason why he would do that, yet I wanted to give the option to do that. I personally experienced that if I turn it off, the values of higher bands would " +
            "peak quite higher than the ones of lower bands, which I do not find looking good.")]
        public bool DevideByTotalBins = true;
        private bool DevideByTotalBinsRec {
            get {
                if (_devideByTotalBinsRedirection == Redirection.None || PrefabSettings == null)
                    return DevideByTotalBins;
                return PrefabSettings.DevideByTotalBinsRec;
            }
        }

        // Normalize by Volume
        [Space]
        [SerializeField] private Redirection _normalizeByVolumeRedirection = Redirection.Prefab;
        [Tooltip("If set to true, all values of all bands will be multiplied by a factor 1/audioSource.volume [which has a range of 0-1] so the amplitude would not be lowered by a lower volume of the audio source. This does " +
            "NOT normalize audio clips which are quite in volume by nature but only the volume specified by the audio source used.")]
        public bool NormalizeByVolume = true;
        private bool NormalizeByVolumeRec {
            get {
                if (_normalizeByVolumeRedirection == Redirection.None || PrefabSettings == null)
                    return NormalizeByVolume;
                return PrefabSettings.NormalizeByVolumeRec;
            }
        }
        #endregion
    }
}