using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi.Modules.MathModules.VectorModules.VoidToVectorModules
{
    public class VoidToVectorConstantModule : VoidToVectorModule
    {
        [Header("--- Void to Vector Constant Module ---")]
        [SerializeField] private Vector3 _value;

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
            return "VoidToVectorConstantModule [" + _value.x + "/" + _value.y + "/" + _value.z + "]";
        }
    }
}