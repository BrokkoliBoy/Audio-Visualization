using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gavi.Modules.MathModules.VectorModules;

namespace Gavi.Modifiables
{
    public class RotationModifiable : Module
    {
        [SerializeField] private RotationTyppe _rotationType;
        [SerializeField] private VectorModule _module;

        [Header("--- References ---")]
        [SerializeField] private Transform _targetTransform;

        private enum RotationTyppe { OverrideRotationWorld, OverrideRotationLocal, OverrideRotationFromStart, ChangeRotationWorld, ChangeRotationLocal }

        Vector3 _startRotation;

        public Transform TargetTransform => _targetTransform;
        /// <summary>The world rotation of the target right after this modifiable wrote it, and the frame that happened in.</summary>
        public Quaternion LastWrittenRotation { get; private set; }
        public int LastWrittenFrame { get; private set; } = -1;

        protected override void Awake()
        {
            base.Awake();
            if (_targetTransform != null)
                _startRotation = _targetTransform.eulerAngles;
        }

        public void Update()
        {
            Vector3 value = _module.GetValue();

            if (_rotationType == RotationTyppe.OverrideRotationWorld)
                _targetTransform.rotation = Quaternion.Euler(value);
            else if (_rotationType == RotationTyppe.OverrideRotationLocal)
                _targetTransform.localRotation = Quaternion.Euler(value);
            else if (_rotationType == RotationTyppe.OverrideRotationFromStart)
                _targetTransform.localRotation = Quaternion.Euler(_startRotation + value);
            else if (_rotationType == RotationTyppe.ChangeRotationWorld)
                _targetTransform.rotation = Quaternion.Euler(value) * _targetTransform.rotation;
            else if (_rotationType == RotationTyppe.ChangeRotationLocal)
                _targetTransform.rotation = _targetTransform.rotation * Quaternion.Euler(value);

            LastWrittenRotation = _targetTransform.rotation;
            LastWrittenFrame = Time.frameCount;
        }

        protected override bool IsValid()
        {
            if (_targetTransform == null)
            {
                Utility.Debugger.LogError("_targetTransform == null in RotationModifiable");
                return false;
            }
            return base.IsValid();
        }

        protected override string GetName()
        {
            return "ModifiablePosition";
        }
    }
}