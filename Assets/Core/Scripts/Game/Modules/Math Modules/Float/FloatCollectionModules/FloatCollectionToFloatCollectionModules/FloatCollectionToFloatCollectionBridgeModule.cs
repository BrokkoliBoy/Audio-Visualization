using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi.Modules.MathModules.FloatCollectionModules.FloatCollectionToFloatCollectionModules
{
    public class FloatCollectionToFloatCollectionBridgeModule : FloatCollectionToFloatCollectionModule
    {
        [Header("--- FloatCollection to FloatCollection Bridge Module ---")]
        [SerializeField] private string _thumnailText;

        public override float GetValue(int index)
        {
            base.Recalculate();

            if (!IsValid())
            {
                return _defaultValue;
            }

            return _inputValue.GetValue(index);
        }

        protected override string GetName()
        {
            return "FCB: " + _thumnailText;
        }
    }
}