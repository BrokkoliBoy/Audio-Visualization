using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi.Modules.MathModules.FloatModules.FloatToFloatModules
{
    public class FloatClampModule : FloatToFloatModule
    {
        [Header("--- Float Clamp Module ---")]
        [SerializeField] private bool _useMin = true;
        [SerializeField] private float _minClamp = 0f;
        [SerializeField] private bool _useMax = true;
        [SerializeField] private float _maxClamp = 1f;


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

            if (_useMin)
                value = Mathf.Max(_minClamp, value);
            if (_useMin)
                value = Mathf.Min(_maxClamp, value);

            _storedValue = value;
        }

        protected override string GetName()
        {
            return "FloatClampModule [min: (" + _useMin + "/" + _minClamp + ") || max: (" + _useMax + "/" + _maxClamp + ")]";
        }
    }
}