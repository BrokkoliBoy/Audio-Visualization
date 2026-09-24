using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi.Modules.MathModules.VectorModules.VectorToVectorModules
{
    public class VectorToVectorModule : VectorModule
    {
        [Header("--- Vector to Vecttor Module ---")]
        [SerializeField] protected VectorModule _inputValue;
        [SerializeField] private bool _inputCanBeNull;


        protected override bool IsValid()
        {
            if (!_inputCanBeNull && _inputValue == null)
            {
                Utility.Debugger.LogError("Invalid in VectorToVectorModule, " + GetType());
                return false;
            }
            return base.IsValid();
        }
    }
}
