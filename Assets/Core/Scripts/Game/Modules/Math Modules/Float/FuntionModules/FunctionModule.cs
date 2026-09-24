using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi.Modules.MathModules.FunctionModules
{
    public class FunctionModule : MathModule
    {
        [Header("--- Function Module ---")]
        [SerializeField] private AnimationCurve _defaultFunction;
        [SerializeField] private float _defaultValue;


        public virtual AnimationCurve GetValue()
        {
            if (!IsValid())
            {
                Utility.Debugger.LogError("!IsValid() in FunctionModule.GetValue(). Returning AnimationCurve.Linear(0, 0, 1, 1).");
                return AnimationCurve.Linear(0, 0, 1, 1);
            }

            return _defaultFunction;
        }
        
        public virtual float GetValue(float t)
        {
            if (!IsValid() || !IsValid(t))
            {
                Utility.Debugger.LogError("!IsValid() || !IsValid(t) in FunctionModule.GetValue(float t). Returning _defaultValue (" + _defaultValue + ").");
                return _defaultValue;
            }
            return GetValue().Evaluate(t);
        }

        protected virtual AnimationCurve DefaultFunction()
        {
            return _defaultFunction;
        }
        protected virtual float DefaultValue()
        {
            return _defaultValue;
        }

        protected virtual bool IsValid(float t)
        {
            return true;
        }

        protected override string GetName()
        {
            return "FunctionModule";
        }
    }
}