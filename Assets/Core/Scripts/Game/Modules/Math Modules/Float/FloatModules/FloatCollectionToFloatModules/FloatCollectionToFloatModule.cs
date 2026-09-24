using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gavi.Modules.MathModules.FloatCollectionModules;

namespace Gavi.Modules.MathModules.FloatModules.FloatCollectionToFloatModule
{
    public class FloatCollectionToFloatModule : FloatModule
    {
        [Header("FloatCollectionToFloatModule")]
        [SerializeField] protected FloatCollectionModule _inputValue;
        [SerializeField] protected bool _inputCanBeNull;
        [SerializeField] protected bool _inputCanBeEmpty;

        protected override bool IsValid()
        {
            if (_inputValue != null)
            {
                if (!_inputCanBeEmpty && _inputValue.Length() == 0)
                    return false;
            }
            else if (!_inputCanBeNull)
                return false;

            if (!_inputCanBeNull && (_inputValue == null))
            {
                Utility.Debugger.LogError("Invalid in FloatCollectionToFloatModule, " + GetType());
                return false;
            }
            return base.IsValid();
        }
    }
}
