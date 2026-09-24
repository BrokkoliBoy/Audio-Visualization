using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi.Modules.MathModules.IntModules
{
    public class IntModule : MathModule
    {
        [Header("--- Int Module ---")]
        [SerializeField] protected int _defaultValue;
        [SerializeField] protected int _storedValue;

        public virtual int GetValue()
        {
            if (!IsValid())
                return _defaultValue;

            TryRecalculate();
            return _storedValue;
        }
    }
}