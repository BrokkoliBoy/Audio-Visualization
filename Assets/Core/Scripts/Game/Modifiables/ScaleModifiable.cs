using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gavi.Modules.MathModules.FloatModules;

namespace Gavi.Modifiables
{
    public class ScaleModifiable : Module
    {
        [SerializeField] private ScaleType _scaleType;
        [SerializeField] private FloatModule _module;

        [Header("--- References ---")]
        [SerializeField] private Transform _targetTransform;

        private enum ScaleType { Override, Multiply }

        private float _currentScaleFactor;
        private Vector3 _defaultScale;

        protected override void Awake()
        {
            if (_targetTransform == null)
                _targetTransform = transform;

            _defaultScale = _targetTransform.localScale;
        }

        public void Update()
        {
            if(_module == null)
            {
                Utility.Debugger.LogError("_module == null in ScaleModifiable");
                return;
            }
            _currentScaleFactor = _module.GetValue();

            if(_scaleType == ScaleType.Override)
                _targetTransform.localScale = Utility.Utility.ScaleVector(Vector3.one, _currentScaleFactor);
            else if (_scaleType == ScaleType.Multiply)
                _targetTransform.localScale =  Utility.Utility.ScaleVector(_defaultScale, _currentScaleFactor);
        }

        protected override string GetName()
        {
            return "ModifiableScale";
        }
    }
}