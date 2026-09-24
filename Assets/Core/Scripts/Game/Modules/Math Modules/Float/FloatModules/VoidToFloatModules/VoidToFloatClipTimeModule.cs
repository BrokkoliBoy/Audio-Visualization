using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gavi.Audio;

namespace Gavi.Modules.MathModules.FloatModules.VoidToFloatModules
{
    public class VoidToFloatClipTimeModule : VoidToFloatModule
    {
        [SerializeField] private TimeInfo _timeInfo;
        private enum TimeInfo { TimeTotal, TimePlayed, TimePlayedRate, TimeLeft }

        protected override void Recalculate()
        {
            base.Recalculate();

            if (_timeInfo == TimeInfo.TimeTotal)
                _storedValue = AudioPlayer.Instance.CurrentlyPlayingClip.Clip.length;
            else if (_timeInfo == TimeInfo.TimePlayed)
                _storedValue = AudioPlayer.Instance.CurrentPlaybackTime;
            else if (_timeInfo == TimeInfo.TimePlayedRate)
                _storedValue = AudioPlayer.Instance.CurrentPlaybackTime / AudioPlayer.Instance.CurrentClipLength;
            else if (_timeInfo == TimeInfo.TimeLeft)
                _storedValue = AudioPlayer.Instance.CurrentlyPlayingClip.Clip.length - AudioPlayer.Instance.CurrentPlaybackTime;
        }

        protected override bool IsValid()
        {
            if (AudioPlayer.Instance == null || AudioPlayer.Instance.CurrentlyPlayingClip == null || AudioPlayer.Instance.CurrentlyPlayingClip.Clip == null)
                return false;

            if ((_timeInfo == TimeInfo.TimeLeft || _timeInfo == TimeInfo.TimePlayed || _timeInfo == TimeInfo.TimePlayedRate) && AudioPlayer.Instance.IsPlaying)
                return false;
            
            return base.IsValid();
        }

        protected override string GetName()
        {
            return "VoidToFloatClipTimeModule [" + _timeInfo.ToString() + "]";
        }
    }
}
