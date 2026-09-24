using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi.Modules.MathModules.FloatModules.VoidToFloatModules
{
    public class VoidToFloatSettableModule : VoidToFloatModule
    {
        private float _value;

        public void SetValue(int value)
        {
            _value = value;
        }
        public void SetValue(float value)
        {
            _value = value;
        }


        protected override void Recalculate()
        {
            base.Recalculate();

            if (!IsValid())
            {
                _storedValue = _defaultValue;
                return;
            }

            _storedValue = _value;
        }

        protected override string GetName()
        {
            return "VoidToFloatSettableModule";
        }
    }
}