using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi.Modules.MathModules.FloatModules.FloatToFloatModules
{
    public class FloatToFloatBridgeModule : FloatToFloatModule
    {
        [Header("--- Float to Float Bridge ---")]
        [SerializeField] private string _thumnailText;
        protected override void Recalculate()
        {
            base.Recalculate();

            if (!IsValid())
            {
                _storedValue = _defaultValue;
                return;
            }

            _storedValue = _inputValue.GetValue();
        }

        protected override string GetName()
        {
            return "FB: " + _thumnailText;
        }
    }
}