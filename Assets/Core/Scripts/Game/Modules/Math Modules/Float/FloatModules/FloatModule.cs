using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi.Modules.MathModules.FloatModules
{
    public class FloatModule : MathModule
    {
        [Header("--- Float Module ---")]
        [SerializeField] protected float _defaultValue;

        // recalculation
        [SerializeField] protected float _storedValue;
        
        public virtual float GetValue()
        {
            if (!IsValid())
                return _defaultValue;

            TryRecalculate();
            return _storedValue;
        }

    }
}
