using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi.Modules.MathModules.FloatArrayModules.VoidToFloatArrayModules
{
    public class VoidToFloatArrayModule : FloatArrayModule
    {
        // [Header("--- Void to FloatArray Module ---")]
        // [SerializeField] protected bool _recalculateAtStart = true;

        protected override void InitializeArray()
        {
            //if (_isArrayInitialized)
            //    return;
            
            // if (!_recalculateAtStart)
            //     return;

            base.InitializeArray();
            // TODO: This might not be helpful for all child classes, but for SpectrumDataContainerModule it is.
            // Recalculate();
        }
    }
}