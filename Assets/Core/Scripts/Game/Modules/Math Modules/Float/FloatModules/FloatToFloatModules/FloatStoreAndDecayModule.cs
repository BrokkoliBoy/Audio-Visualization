using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi.Modules.MathModules.FloatModules.FloatToFloatModules
{


    public class FloatStoreAndDecayModule : FloatToFloatModule
    {
        // Stores a (current) value and decays it each update/lateUpdate. 
        // The input value will be compared to the current value and the new current value will be evaluated by the specified StoreType.
        // GetValue() returns the current value.

        [Header("--- Float Store and Decay Module ---")]
        [Tooltip("Defines how the current value should be evaluated in respect to the input value." +
            "\n\n" +
            "Constant: Saves the input value without any modification." +
            "Min: Saves the minimum of the input value and the current value.\n" +
            "Max: Saves the maximum of the input value and the current value.")]
        [SerializeField] private StoreType _storeType;
        [Tooltip("Specifies what number the current value is at start.")]
        [SerializeField] private float _initialValue;
        [SerializeField] private float _minValue = 0.5f;
        [SerializeField] private float _maxValue = 1.5f;
        [Tooltip("Specifies the rate in which the current value is decayed in the specified UpdateEvent per second." +
            "\n\n" +
            "Positive values increase the current value.\n" +
            "Negative values decrease the current value.")]
        [SerializeField] private float _decayRate;
        [Tooltip("Specifies in which event the decaying is performed. The decayed value is always multiplied with Time.deltaTime.")]
        [SerializeField] private UpdateEvent _updateEvent;

        private float _currentValue;

        private enum StoreType { Override, Min, Max }
        private enum UpdateEvent { Update, LateUpdate }


        #region Mono
        protected override void Awake()
        {
            base.Awake();
            _currentValue = _initialValue;
        }
        private void Update()
        {
            if (_updateEvent == UpdateEvent.Update)
                ApplyDecay();
        }
        protected override void LateUpdate()
        {
            base.LateUpdate();
            if (_updateEvent == UpdateEvent.LateUpdate)
                ApplyDecay();
        }
        #endregion


        #region Logic
        protected override void Recalculate()
        {
            base.Recalculate();

            if (!IsValid())
            {
                _storedValue = _defaultValue;
                return;
            }
            float inputValue = _inputValue.GetValue();
            float value = _currentValue;

            if (_storeType == StoreType.Override)
                _currentValue = inputValue;
            else if (_storeType == StoreType.Min)
                _currentValue = Mathf.Min(_currentValue, inputValue);
            else /*_storeType == StoreType.Max*/
                _currentValue = Mathf.Max(_currentValue, inputValue);
            _currentValue = Mathf.Max(_currentValue, _minValue);
            _currentValue = Mathf.Min(_currentValue, _maxValue);
            value = _currentValue;

            _storedValue = value;
        }

        private void ApplyDecay()
        {
            _currentValue += _decayRate * Time.deltaTime;
        }
        #endregion


        #region Overrides
        protected override string GetName()
        {
            return "FloatStoreAndDecayModule [" + _storeType.ToString() + " || init: " + _initialValue + " || rate: " + _decayRate + " || " + _updateEvent.ToString() + "]";
        }
        #endregion
    }

}