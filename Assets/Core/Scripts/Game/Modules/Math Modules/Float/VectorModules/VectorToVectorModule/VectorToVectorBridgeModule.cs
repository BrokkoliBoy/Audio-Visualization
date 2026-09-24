using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi.Modules.MathModules.VectorModules.VectorToVectorModules
{
    public class VectorToVectorBridgeModule : VectorToVectorModule
    {
        [Header("--- Vector to Vector Bridge ---")]
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
            return "VB: " + _thumnailText;
        }
    }
}