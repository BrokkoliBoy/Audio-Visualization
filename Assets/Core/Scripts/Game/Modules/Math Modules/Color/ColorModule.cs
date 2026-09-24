using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi.Modules.MathModules.ColorModules
{
    public class ColorModule : MathModule
    {
        [Header("--- Color Module ---")]
        [ColorUsage(true, true)][SerializeField] protected Color _defaultValue;

        // recalculation
        [ColorUsage(true, true)] [SerializeField] protected Color _storedValue;

        public virtual Color GetValue()
        {
            if (!IsValid())
                return _defaultValue;

            TryRecalculate();
            return _storedValue;
        }

        protected override void Recalculate()
        {
            base.Recalculate();
            _storedValue = _defaultValue;
        }

        protected override string GetName()
        {
            return "Color Module: [" + _defaultValue.r + " / " + _defaultValue.g + " / " + _defaultValue.b + " / " + _defaultValue.a + "]";
        }
    }
}