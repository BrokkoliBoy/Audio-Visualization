using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gavi.Utility;

namespace Gavi.Audio
{
    public class AudioClipSettingsPrefab : MonoBehaviour
    {
        [SerializeField] private AudioClipSettings _settings;
        public AudioClipSettings Settings => _settings;
    }

    [System.Serializable]
    public class AudioClipSettings
    {
        [SerializeField] private AudioClipSettingsPrefab _prefabSettings;
        private AudioClipSettings PrefabSettings => _prefabSettings != null ? _prefabSettings.Settings : null;

        public enum Redirection { Prefab, None }
        public enum Operation { Use, MultiplyWith }

        #region Init
        public AudioClipSettings(AudioClipSettings originalSettings)
        {
            if (originalSettings == null)
            {
                Debugger.LogError("originalSettings in AudioClipSettings constructor is null!");
                return;
            }

            AmplitudeModifierOverall = originalSettings.AmplitudeModifierOverallRec;
            originalSettings.AmplitudeModifierWeightedRec(AmplitudeModifierWeightedList);
        }
        #endregion


        // Amply Modifier Overall
        [Space]
        [SerializeField] private Redirection _amplitudeModifierOverallRedirection = Redirection.Prefab;
        [SerializeField] private Operation _amplitudeModifierOverallOperation = Operation.Use;
        public float AmplitudeModifierOverall = 1;
        private float AmplitudeModifierOverallRec {
            get {
                if (_amplitudeModifierOverallRedirection == Redirection.None || _prefabSettings == null)
                    return AmplitudeModifierOverall;
                return PrefabSettings.AmplitudeModifierOverallRec * (_amplitudeModifierOverallOperation == Operation.MultiplyWith ? AmplitudeModifierOverall : 1);
            }
        }


        // Amplitude Modifier Bands
        [Space]
        [SerializeField] private Redirection _amplitudeModifierWeightedRedirection = Redirection.Prefab;
        [SerializeField] private Operation _amplitudeModifierWeightedOperation = Operation.Use;
        public List<AnimationCurve> AmplitudeModifierWeightedList = new List<AnimationCurve>();
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
            PrefabSettings.AmplitudeModifierWeightedRec(curveListToBeFilled, true);
        }

    }
}

