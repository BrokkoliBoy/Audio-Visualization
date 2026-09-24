using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using Gavi.Modules.MathModules.FloatCollectionModules;
using Gavi.Modules.MathModules.IntModules;

namespace Gavi.Modules.MathModules.FloatModules.FloatCollectionToFloatModule
{
    public class FloatCollectionToFloatIndexModule : FloatCollectionToFloatModule
    {
        [Header("--- FloatCollection To Float Index Module ---")]
        [SerializeField] private int _index;
        [SerializeField] private IntModule _indexModule;

        protected override void Recalculate()
        {
            base.Recalculate();
            if (!IsValid())
            {
                _storedValue = _defaultValue;
                return;
            }

            int index = _indexModule != null ? _indexModule.GetValue() : _index;
            float value = _inputValue.GetValue(index);

            _storedValue = value;
        }

        protected override bool IsValid()
        {
            if (_inputValue.Length() < 1)
            {
                Utility.Debugger.LogAssertionFail("FloatCollectionToFloatIndexModule.GetValue(): The input FloatCollection needs to have at least one element.");
                return false;
            }
            return base.IsValid();
        }

        protected override string GetName()
        {
            return "FloatCollectionToFloatIndexModule [" + _index + "]";
        }
    }
}