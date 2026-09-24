using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi.Modules.MathModules.FloatModules.FloatToFloatModules
{
    public class FloatToFloatModuloModule : FloatToFloatModule
    {
        [Header("--- Float To Float Modulo Module ---")]
        [SerializeField] private FloatModule _moduloModule;

        protected override void Recalculate()
        {
            base.Recalculate();
            if(!IsValid())
            {
                _storedValue = _defaultValue;
                return;
            }

            _storedValue = _inputValue.GetValue() % _moduloModule.GetValue();
        }

        protected override bool IsValid()
        {
            if (_moduloModule == null)
            {
                Utility.Debugger.LogAssertionFail("!IsValid: _moduloModule == null in FloatToFloatModuloModule");
                return false;
            }

            return base.IsValid();
        }

        protected override string GetName()
        {
            return "FloatToFloatModuloModule";
        }
    }
}
