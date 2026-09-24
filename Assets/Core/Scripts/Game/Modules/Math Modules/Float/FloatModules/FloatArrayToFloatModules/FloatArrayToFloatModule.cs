using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gavi.Modules.MathModules.FloatArrayModules;

namespace Gavi.Modules.MathModules.FloatModules.FloatArrayToFloatModules
{
    public class FloatArrayToFloatModule : FloatModule
    {
        [Header("--- Array to Float ---")]
        [SerializeField] protected FloatArrayModule _inputValue;
        [SerializeField] private bool _inputCanBeNull;
        [SerializeField] private bool _inputCanBeEmpty;

        protected override bool IsValid()
        {
            if (_inputValue != null)
            {
                if (!_inputCanBeEmpty && _inputValue.Length() == 0)
                {
                    Utility.Debugger.LogError("Invalid in FloatArrayToFloatModule, " + GetType());
                    return false;
                }
            }
            else if (!_inputCanBeNull)
            {
                Utility.Debugger.LogError("Invalid in FloatArrayToFloatModule, " + GetType());
                return false;
            }

            return base.IsValid();
        }
    }
}