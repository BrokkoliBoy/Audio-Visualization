using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi.Modules.MathModules.FloatModules.FloatToFloatModules
{
    public class FloatToFloatMultiplyModule : FloatToFloatModule
    {
        [Header("--- Float To Float Multiplication Module ---")]
        [SerializeField] private float _multiplierNumber = 1f;
        [SerializeField] private FloatModule _multiplierModule;

        protected override void Recalculate()
        {
            base.Recalculate();
            //if (!IsValid())
            //{
            //    _storedValue = _defaultValue;
            //    return;
            //}
            float inputValue = _inputValue.GetValue();
            float moduleValue = _multiplierModule != null ? _multiplierModule.GetValue() : 1;
            float value = inputValue * _multiplierNumber * moduleValue;

            //Debug.Log(inputValue + " // " + _multiplierNumber + " // " + moduleValue);

            _storedValue = value;
        }

        protected override string GetName()
        {
            return "FloatToFloatMultiplyModule [" + _multiplierNumber + " / " + (_multiplierModule != null ? "+" : "-") + "]";
        }
    }
}