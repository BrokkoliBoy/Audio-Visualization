using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gavi.Modules.MathModules.FloatCollectionModules;

namespace Gavi.Modules.MathModules.FloatModules.FloatCollectionToFloatModule
{
    public class FloatCollectionToFloatDivisionModule : FloatCollectionToFloatModule
    {
        protected override void Recalculate()
        {
            base.Recalculate();
            if (!IsValid())
            {
                _storedValue = _defaultValue;
                return;
            }

            float value = _inputValue.GetValue(0);

            FloatCollectionModule inputValue = _inputValue;
            for (int i = 1; i < inputValue.Length(); i++)
            {
                float f = inputValue.GetValue(i);
                if (f == 0)
                {
                    Utility.Debugger.LogError("FloatCollectionToFloatDivisionModule.GetValue(): The element " + i + " was null.");
                    _storedValue = 0;
                }
                value /= f;
            }

            //Debug.Log("FloatCollectionToFloatDivisionModule: " + value);

            _storedValue = value;
        }

        protected override bool IsValid()
        {
            if (_inputValue.Length() < 2)
            {
                Utility.Debugger.LogAssertionFail("FloatCollectionToFloatDivisionModule.GetValue(): The input FloatCollection needs to have at least two elements.");
                return false;
            }
            return base.IsValid();
        }

        protected override string GetName()
        {
            return "FloatCollectionToFloatDivisionModule";
        }
    }
}