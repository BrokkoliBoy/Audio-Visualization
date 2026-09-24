using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi.Modules.MathModules.FloatModules.FloatArrayToFloatModules
{
    public class FloatArrayToFloatSumModule : FloatArrayToFloatModule
    {
        protected override void Recalculate()
        {
            base.Recalculate();
            if (!IsValid())
            {
                _storedValue = _defaultValue;
                return;
            }

            float[] inputValue = _inputValue.GetValue();
            float value = 0;

            for (int i = 0; i < inputValue.Length; i++)
            {
                value += inputValue[i];
            }
            _storedValue = value;
        }

        protected override string GetName()
        {
            return "FloatArrayToFloatSumModule";
        }
    }
}