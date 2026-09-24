using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi.Modules.MathModules.FloatModules.FloatToFloatModules
{
    public class FloatToFloatSubtractModule : FloatToFloatModule
    {
        [Header("--- Float To Float Subtract Module ---")]
        [Tooltip("InputIsSubtrahend - The inputValue modules value is the subtrahend (it is the number being subtracted)" +
            "\n" +
            "InputIsMinuend - The inputValue modules value is the minuend (it is the number being subtracted FROM). " +
            "If this is the case, make sure that your inputValue is positive, since the operation is '(_subtractNumber + _subtractModule.GetValue()) - _inputValue.GetValue()")]
        [SerializeField] private SubtractionMode _subtractionMode;
        [Tooltip("If the _subtractionMode is InputtIsSubtrahent, that means that this value here is going to be the minuent." +
            " If so, make sure this value is positive, since the operation is '_inputValue.GetValue() - (_subtractNumber + _subtractModule.GetValue()).")]
        [SerializeField] private float _subtractNumber;
        [SerializeField] private FloatModule _subtractModule;

        private enum SubtractionMode { InputIsSubtrahend, InputIsMinuend }

        protected override void Recalculate()
        {
            base.Recalculate();
            //if (!IsValid())
            //{
            //    _storedValue = _defaultValue;
            //    return;
            //}
            float value = -1;
            float inputValue = _inputValue.GetValue();
            float subtractionValue = _subtractNumber + (_subtractModule != null ? _subtractModule.GetValue() : 0);

            if (_subtractionMode == SubtractionMode.InputIsSubtrahend)
                value = inputValue - subtractionValue;
            else if (_subtractionMode == SubtractionMode.InputIsMinuend)
                value = subtractionValue - inputValue;

            _storedValue = value;
        }

        protected override string GetName()
        {
            return "FloatToFloatSubtractModule [" + _subtractNumber + " / " + (_subtractModule != null ? "+" : "-") + "]";
        }
    }
}