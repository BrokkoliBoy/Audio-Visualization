using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gavi.Modules.MathModules.VectorModules;

namespace Gavi.Modifiables
{
    public class PositionModifiable : Module
    {
        [SerializeField] private ScaleType _positionType;
        [SerializeField] private VectorModule _module;

        [Header("--- References ---")]
        [SerializeField] private Transform _targetTransform;

        private enum ScaleType { OverridePositionWorld, OverridePositionLocal, ChangePositionWorld, ChangePositionLocal }


        public void Update()
        {
            Vector3 value = _module.GetValue();

            if (_positionType == ScaleType.OverridePositionWorld)
                _targetTransform.position = value;
            else if (_positionType == ScaleType.OverridePositionLocal)
                _targetTransform.localPosition = value;
            else if (_positionType == ScaleType.ChangePositionWorld)
                _targetTransform.position += value;
            else if (_positionType == ScaleType.ChangePositionLocal)
                _targetTransform.localPosition += value;
        }

        protected override string GetName()
        {
            return "ModifiablePosition";
        }
    }
}