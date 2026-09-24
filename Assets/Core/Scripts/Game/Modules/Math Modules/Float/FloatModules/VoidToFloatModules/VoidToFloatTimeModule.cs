using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi.Modules.MathModules.FloatModules.VoidToFloatModules
{ 
    public class VoidToFloatTimeModule : VoidToFloatModule
    {
        [SerializeField] private TimeType _timeType;

        // add more time functionality if needed
        private enum TimeType { Time, DeltaTime }

        protected override void Recalculate()
        {
            base.Recalculate();
            if (!IsValid())
            {
                _storedValue = _defaultValue;
                return;
            }

            if (_timeType == TimeType.Time)
                _storedValue = Time.time;
            else if (_timeType == TimeType.DeltaTime)
                _storedValue = Time.deltaTime;
        }

        protected override string GetName()
        {
            return "VoidToFloatTimeModule [" + _timeType.ToString()  + "]";
        }
    }
}
