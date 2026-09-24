using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gavi.Modules.MathModules.FloatModules.VoidToFloatModules
{
    public class VoidToFloatConstantModule : VoidToFloatModule
    {
        [Header("--- Void to Float Constant Module ---")]
        [SerializeField] private float _value;

        protected override void Recalculate()
        {
            base.Recalculate();

            if (!IsValid())
            {
                _storedValue = _defaultValue;
                return;
            }
            _storedValue = _value;
        }

        protected override string GetName()
        {
            return "VoidToFloatConstantModule [" + _value + "]";
        }
    }
}