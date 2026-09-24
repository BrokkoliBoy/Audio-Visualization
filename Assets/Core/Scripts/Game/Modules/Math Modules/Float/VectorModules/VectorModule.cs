using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi.Modules.MathModules.VectorModules
{
    public class VectorModule : MathModule
    {
        [Header("--- Float Module ---")]
        [SerializeField] protected Vector3 _defaultValue;

        // recalculation
        [SerializeField] protected Vector3 _storedValue;

        public virtual Vector3 GetValue()
        {
            if (!IsValid())
                return _defaultValue;

            TryRecalculate();
            return _storedValue;
        }
    }
}