using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi.Modules.MathModules.FloatModules.FloatToFloatModules
{
    public class FloatToFloatStoreModule : FloatToFloatModule
    {
        [Header("--- Float to Float Store Value ---")]
        [SerializeField] private float _initialValue;
        [SerializeField] private bool _storeOnlyOncePerFrame = true;

        private float _storedValueThis;
        private bool _wasStoredThisFrame;

        protected override void Awake()
        {
            base.Awake();
            _storedValueThis = _initialValue;
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();
            _wasStoredThisFrame = false;
        }

        protected override void Recalculate()
        {
            base.Recalculate();

            if (!IsValid())
            {
                _storedValue = _defaultValue;
                return;
            }

            if (_storeOnlyOncePerFrame && _wasStoredThisFrame)
                _storedValue = _storedValueThis;
            _wasStoredThisFrame = true;

            float value = _storedValueThis;
            _storedValueThis = _inputValue.GetValue();
            _storedValue = value;
        }

        #region Overrides
        protected override string GetName()
        {
            return "FloatToFloatStoreModule [" + _initialValue + "]";
        }
        #endregion
    }
}