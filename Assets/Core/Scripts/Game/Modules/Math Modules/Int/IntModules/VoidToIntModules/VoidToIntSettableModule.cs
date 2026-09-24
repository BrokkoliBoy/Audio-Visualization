using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi.Modules.MathModules.IntModules.VoidToIntModules
{
    public class VoidToIntSettableModule : IntModule
    {
        private int _value;

        public void SetValue(int value)
        {
            _value = value;
        }
        public void SetValue(float value)
        {
            _value = (int)value;
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
            return "VoidToIntSettableModule";
        }
    }
}