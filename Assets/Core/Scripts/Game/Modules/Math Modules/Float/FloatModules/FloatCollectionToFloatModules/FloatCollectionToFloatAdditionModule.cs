using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gavi.Modules.MathModules.FloatCollectionModules;

namespace Gavi.Modules.MathModules.FloatModules.FloatCollectionToFloatModule
{
    public class FloatCollectionToFloatAdditionModule : FloatCollectionToFloatModule
    {
        protected override void Recalculate()
        {
            base.Recalculate();
            //if (!IsValid())
            //{
            //    _storedValue = _defaultValue;
            //    return;
            //}

            FloatCollectionModule inputValue = _inputValue;
            float value = inputValue.GetValue(0);

            for (int i = 1; i < inputValue.Length(); i++)
            {
                float f = inputValue.GetValue(i);
                
                value += f;
            }

            _storedValue = value;
        }

        protected override bool IsValid()
        {
            if (_inputValue.Length() < 2)
            {
                Utility.Debugger.LogAssertionFail("FloatCollectionToFloatAdditionModule.GetValue(): The input FloatCollection needs to have at least two elements.");
                return false;
            }
            return base.IsValid();
        }

        protected override string GetName()
        {
            return "FloatCollectionToFloatAdditionModule";
        }
    }
}