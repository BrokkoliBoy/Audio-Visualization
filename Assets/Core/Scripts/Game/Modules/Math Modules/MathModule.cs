using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi.Modules.MathModules
{
    public class MathModule : Module
    {
        [Header("--- Math Module ---")]
        [Tooltip("If set to true, the module will perform its Recalculate() function only once per frame. Recommended for most cases where " +
                 "the recalculation is independent on external sources or when the input value doesn't change for sure within that frame.")]
        [SerializeField] protected bool _calculateOncePerFrameOnly = true;
        public bool CalculateOncePerFrameOnly => _calculateOncePerFrameOnly;

        protected bool _calculatedThisFrame;


        protected virtual void LateUpdate()
        {
            _calculatedThisFrame = false;
        }

        protected virtual void Recalculate()
        {
            _calculatedThisFrame = true;
        }

        public virtual void TryRecalculate()
        {
            if (!_calculateOncePerFrameOnly || !_calculatedThisFrame)
                Recalculate();
        }
    }
}