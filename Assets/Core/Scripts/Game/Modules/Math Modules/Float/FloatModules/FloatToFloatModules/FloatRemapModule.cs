using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi.Modules.MathModules.FloatModules.FloatToFloatModules
{
    public class FloatRemapModule : FloatToFloatModule
    {
        [Header("--- Float Remap Module ---")]
        [SerializeField] private float _oldMin = 0f;
        [SerializeField] private FloatModule _oldMinFloatModule;
        [SerializeField] private float _oldMax = 1f;
        [SerializeField] private FloatModule _oldMaxFloatModule;
        [Space]
        [SerializeField] private float _newMin = 0.5f;
        [SerializeField] private FloatModule _newMinFloatModule;
        [SerializeField] private float _newMax = 1f;
        [SerializeField] private FloatModule _newMaxFloatModule;
        [Space]
        [Tooltip("Clamps the input value to _oldMin and _oldMax")]
        [SerializeField] private bool _clampInput = true;

        protected override void Recalculate()
        {
            base.Recalculate();

            if (!IsValid())
            {
                _storedValue = _defaultValue;
                return;
            }

            float oldMin = _oldMinFloatModule != null ? _oldMinFloatModule.GetValue() : _oldMin;
            float oldMax = _oldMaxFloatModule != null ? _oldMaxFloatModule.GetValue() : _oldMax;
            float newMin = _newMinFloatModule != null ? _newMinFloatModule.GetValue() : _newMin;
            float newMax = _newMaxFloatModule != null ? _newMaxFloatModule.GetValue() : _newMax;

            float inputValue = _inputValue.GetValue();
            if (_clampInput)
                inputValue = Mathf.Clamp(inputValue, oldMin, oldMax);
            float value = Utility.Utility.Remap(oldMin, oldMax, newMin, newMax, inputValue);


            _storedValue = value;
        }

        protected override string GetName()
        {
            return "FloatRemapModule [old: ("
                + (_oldMinFloatModule != null ? "-" : _oldMin + "") + "/"
                + (_oldMaxFloatModule != null ? "-" : _oldMax + "") + ") || new: (" 
                + (_newMinFloatModule != null ? "-" : _newMin + "") + "/" 
                + (_newMaxFloatModule != null ? "-" : _newMax + "") + ")]";
        }
    }
}
