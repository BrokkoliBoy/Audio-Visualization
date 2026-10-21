using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi.Modules.MathModules.FloatModules.FloatToFloatModules
{
    public class FloatLerpModule : FloatToFloatModule
    {
        [Header("--- Float Lerp Module ---")]
        [Tooltip("Lerp factor to be used when the new value is higher than the last value.")]
        [SerializeField] private float _lerpFactorPos = 0.1f;
        [Tooltip("Lerp factor to be used when the new value is lower than the last value.")]
        [SerializeField] private float _lerpFactorNeg = 0.1f;

        private float _lastValue = float.MaxValue;

        protected override void Recalculate()
        {
            base.Recalculate();

            if (!IsValid())
            {
                _storedValue = _defaultValue;
                return;
            }

            float inputValue = _inputValue.GetValue();
            float value = inputValue;

            // use first incoming inputValue as first _lastValue
            if (_lastValue == float.MaxValue)
                _lastValue = value;

            value = Mathf.Lerp(_lastValue, value, Utility.Utility.Smooth(value > _lastValue ? _lerpFactorPos : _lerpFactorNeg, Utility.Utility.GetFpsEqualizer()));
            _lastValue = value;

            //Debug.Log("FloatLerpModule: " + value);
            _storedValue = value;
        }

        protected override string GetName()
        {
            return "FloatLerpModule [pos: " + _lerpFactorPos + " || neg: " + _lerpFactorNeg + "]";
        }
    }
}