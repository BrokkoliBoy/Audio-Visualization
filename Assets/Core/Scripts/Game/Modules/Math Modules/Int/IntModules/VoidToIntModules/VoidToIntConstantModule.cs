using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi.Modules.MathModules.IntModules.VoidToIntModules
{
    public class VoidToIntConstantModule : VoidToIntModule
    {
        [Header("--- Void to Int Module ---")]
        [SerializeField] private int _value;

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
            return "VoidToIntConstantModule [" + _value + "]";
        }
    }
}