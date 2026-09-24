using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi.Modules.MathModules.FloatModules.FloatToFloatModules
{
    public class FloatDecayModule : FloatToFloatModule
    {
        // Saves a float and decays it over time in each update until it is called with GetValue(). Calling. Decaying can be positive and negative.

        [Header("--- Float Decay Module ---")]
        [Tooltip("Specifies what number the _currentNumber is at start.")]
        [SerializeField] private float _initialValue;
        [Tooltip("Specifies the rate in which the current value is affected in the specified update event. The decay is applied in the specified DecayEvent. " +
            "The decay rate is modified by Time.deltaTime." +
            "\n\n" +
            "Positive numbers increase the current value.\n" +
            "Negative numbers decrease the current value.")]
        [SerializeField] private float _decayRate;
        [Tooltip("Specifies when the decaying is performed. When using Update, there is no guarantee if that happens before or after calls of GetValue().")]
        [SerializeField] private DecayEvent _decayEvent;

        private enum DecayEvent { Update, LateUpdate }

        private float _currentValue;

        #region Mono
        protected override void Awake()
        {
            base.Awake();
            _currentValue = _initialValue;
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();
            if (_decayEvent == DecayEvent.Update)
                ApplyDecay();
        }
        private void Update()
        {
            if (_decayEvent == DecayEvent.LateUpdate)
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
            _currentValue = inputValue;

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
            return "FloatDecayModule [init: " + _initialValue + " || rate: " + _decayRate + " || event: " + _decayEvent.ToString() + "]";
        }
        #endregion
    }
}