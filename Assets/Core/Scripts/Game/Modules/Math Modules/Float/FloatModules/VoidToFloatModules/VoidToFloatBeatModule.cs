using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gavi.Audio;

namespace Gavi.Modules.MathModules.FloatModules.VoidToFloatModules
{
    public class VoidToFloatBeatModule : VoidToFloatModule
    {
        [Header("--- Void To Float Bpm Module ---")]
        [Tooltip("BPM - Beats per Minute" +
            "\n" +
            "BPS - Beats per Second" +
            "\n" +
            "MPB - Minutes per Beat" +
            "\n" +
            "SPB - Seconds per Beat" +
            "\n" +
            "BeatOffset - Offset of the beat in seconds.")]
        [SerializeField] private ReturnType _returnType;
        private enum ReturnType  { BPM, BPS, MPB, SPB, BeatOffset }

        protected override void Recalculate()
        {
            base.Recalculate();
            //if (!IsValid())
            //{
            //    _storedValue = _defaultValue;
            //    return;
            //}

            if (_returnType == ReturnType.BPM)
                _storedValue = AudioPlayer.Instance.CurrentlyPlayingClip.BPM;
            else if (_returnType == ReturnType.BPS)
                _storedValue = AudioPlayer.Instance.CurrentlyPlayingClip.BPM / 60f;
            else if (_returnType == ReturnType.MPB)
                _storedValue = 1f / AudioPlayer.Instance.CurrentlyPlayingClip.BPM;
            else if (_returnType == ReturnType.SPB)
                _storedValue = 1f / (AudioPlayer.Instance.CurrentlyPlayingClip.BPM / 60f);
            else if (_returnType == ReturnType.BeatOffset)
                _storedValue = AudioPlayer.Instance.CurrentlyPlayingClip.BeatOffset;
        }

        protected override bool IsValid()
        {
            if (AudioPlayer.Instance == null || AudioPlayer.Instance.CurrentlyPlayingClip == null)
                return false;
            if (_returnType != ReturnType.BeatOffset && AudioPlayer.Instance.CurrentlyPlayingClip.BPM <= 0)
                return false;

            return base.IsValid();
        }

        protected override string GetName()
        {
            return "VoidToFloatBpmModule [" + _returnType.ToString() + "]";
        }
    }
}
