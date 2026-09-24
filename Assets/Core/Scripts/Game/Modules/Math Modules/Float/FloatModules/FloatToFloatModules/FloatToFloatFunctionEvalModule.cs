using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gavi.Modules.MathModules.FunctionModules;

namespace Gavi.Modules.MathModules.FloatModules.FloatToFloatModules
{
    public class FloatToFloatFunctionEvalModule : FloatToFloatModule
    {
        [Header("--- Float to Float Bridge ---")]
        [Tooltip("If _functionModule °=null, _functionModule will be used and _curve will be ignored.")]
        [SerializeField] private AnimationCurve _curve = AnimationCurve.Constant(0, 1, 1);
        [Tooltip("If this is != null, the _curve will be ignored.")]
        [SerializeField] private FunctionModule _functionModule;


        protected override void Recalculate()
        {
            base.Recalculate();

            if (!IsValid())
            {
                _storedValue = _defaultValue;
                return;
            }

            float t = _inputValue.GetValue();
            if(_functionModule == null)
                _storedValue = _curve.Evaluate(t);
            else
                _storedValue = _functionModule.GetValue(t);
        }

        protected override string GetName()
        {
            return "FloatToFloatFunctionEvalModule";
        }
    }
}