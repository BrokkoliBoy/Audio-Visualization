using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi.Modules.MathModules.FloatModules.FloatToFloatModules
{
    public class FloatToFloatAdditionModule : FloatToFloatModule
    {
        [Header("--- Float To Float Addition Module ---")]
        [SerializeField] private float _addNumber;
        [SerializeField] private FloatModule _addModule;

        protected override void Recalculate()
        {
            base.Recalculate();
            //if (!IsValid())
            //{
            //    _storedValue = _defaultValue;
            //    return;
            //}
            float inputValue = _inputValue.GetValue();
            float value = inputValue + _addNumber;

            if (_addModule != null)
                value += _addModule.GetValue();

            _storedValue = value;
        }

        protected override string GetName()
        {
            return "FloatToFloatAdditionModule [" + _addNumber + " / " + (_addModule != null ? "+" : "-") + "]";
        }
    }
}