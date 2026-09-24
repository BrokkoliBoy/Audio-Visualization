using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi.Modules.MathModules.ColorModules.FloatToColorModules
{
    public class FloatToColorGradientModule : FloatToColorModule
    {
        [Header("--- FloatToColorGradientModule ---")]
        [GradientUsage(true)][SerializeField] private Gradient _gradient;

        protected override void Recalculate()
        {
            base.Recalculate();
            if (!IsValid())
            {
                _storedValue = _defaultValue;
                return;
            }

            _storedValue = _gradient.Evaluate(_inputValue.GetValue());
        }

        protected override string GetName()
        {
            return "FloatToColorGradientModule";
        }
    }
}