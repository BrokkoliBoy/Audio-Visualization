using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gavi.Modules.MathModules.FloatCollectionModules;

namespace Gavi.Modules.MathModules.FloatModules.FloatCollectionToFloatModule
{
    public class FloatCollectionToFloatMaxModule : FloatCollectionToFloatModule
    {
        protected override void Recalculate()
        {
            base.Recalculate();
            if(!IsValid())
            {
                _storedValue = _defaultValue;
                return;
            }

            float max = float.MinValue;

            FloatCollectionModule inputValue = _inputValue;
            for (int i = 0; i < inputValue.Length(); i++)
                max = Mathf.Max(max, inputValue.GetValue(i));

            //Debug.Log("FloatCollectionToFloatMaxModule: " + max);
            _storedValue = max;
        }

        protected override string GetName()
        {
            return "FloatCollectionToFloatMaxModule";
        }
    }
}
