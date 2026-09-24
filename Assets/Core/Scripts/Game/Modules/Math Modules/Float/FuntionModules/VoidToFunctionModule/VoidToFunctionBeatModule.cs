using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gavi.Audio;

namespace Gavi.Modules.MathModules.FunctionModules.VoidToFunctionModules
{
    public class VoidToFunctionBeatModule : VoidToFunctionModule
    {
        [SerializeField] private ReturnType _returnType;
        private enum ReturnType { BeatMap }

        public override AnimationCurve GetValue()
        {
            if (!IsValid())
                return DefaultFunction();
            
            if (_returnType == ReturnType.BeatMap)
                return AudioPlayer.Instance.CurrentlyPlayingClip.BeatMapModule.GetValue();
            return DefaultFunction();
        }

        protected override bool IsValid()
        {
            if (AudioPlayer.Instance != null && AudioPlayer.Instance.CurrentlyPlayingClip != null && AudioPlayer.Instance.CurrentlyPlayingClip.BeatMapModule != null)
                return base.IsValid();
            Utility.Debugger.LogAssertionFail("!IsValid() in VoidToFunctionBeatModule!");
            return false;
        }

        protected override string GetName()
        {
            return "VoidToFunctionBeatModule [" + _returnType.ToString() + "]";
        }
    }
}
