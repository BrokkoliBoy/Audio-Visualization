using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gavi.Modules.MathModules.FloatModules;

namespace Gavi.Modules.MathModules.VectorModules.VoidToVectorModules
{
    public class VoidToVectorInputModule : VoidToVectorModule
    {
        [SerializeField] private FloatModule _inputX;
        [SerializeField] private FloatModule _inputY;
        [SerializeField] private FloatModule _inputZ;

        protected override void Recalculate()
        {
            base.Recalculate();

            float x = _inputX != null ? _inputX.GetValue() : _defaultValue.x;
            float y = _inputY != null ? _inputY.GetValue() : _defaultValue.y;
            float z = _inputZ != null ? _inputZ.GetValue() : _defaultValue.z;

            _storedValue = new Vector3(x, y, z);
        }

        protected override string GetName()
        {
            return "VoidToVectorInputModule";
        }
    }
}