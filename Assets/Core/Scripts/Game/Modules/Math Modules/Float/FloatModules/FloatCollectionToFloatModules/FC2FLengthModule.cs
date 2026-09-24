using System.Collections;
using System.Collections.Generic;
using Gavi.Modules.MathModules.FloatModules.FloatCollectionToFloatModule;
using UnityEngine;

namespace Gavi.Modules
{
    public class FC2FLengthModule : FloatCollectionToFloatModule
    {
        protected override void Recalculate()
        {
            base.Recalculate();

            _storedValue = _inputValue.Length();
        }

        protected override string GetName()
        {
            return "FC 2 F Length Module";
        }
    }
}
