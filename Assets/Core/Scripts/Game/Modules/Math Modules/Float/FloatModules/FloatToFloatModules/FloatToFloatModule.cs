using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi.Modules.MathModules.FloatModules.FloatToFloatModules
{
    public class FloatToFloatModule : FloatModule
    {
        [Header("--- Float to Float Module ---")]
        [SerializeField] protected FloatModule _inputValue;
        [SerializeField] private bool _inputCanBeNull;
        

        protected override bool IsValid()
        {
            if (!_inputCanBeNull && _inputValue == null)
            {
                Utility.Debugger.LogError("Invalid in FloatToFloatModule on " + gameObject.name + ", " + GetType());
                return false;
            }
            return base.IsValid();
        }
    }
}