using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gavi.Modules.MathModules.IntModules.VoidToIntModules;
using Gavi.Modules.MathModules.IntModules;
using Gavi.Modules.MathModules.FloatModules.VoidToFloatModules;
using Gavi.Modules.MathModules.FloatModules;

namespace Gavi.Modules.MathModules.FloatModules.FloatToFloatModules
{
    public class FloatToFloatSetterModule : FloatToFloatModule
    {
        [Header("- Int -")]
        [Header("--- Float To Float SetValue Module ---")]
        [SerializeField] private VoidToIntSettableModule _targetIntModule;
        [SerializeField] private IntModule _sourceIntModule;
        [Header("- Float -")]
        [SerializeField] private VoidToFloatSettableModule _targetFloatModule;
        [SerializeField] private FloatModule _sourceFloatModule;
        

        protected override void Recalculate()
        {
            base.Recalculate();

            if (!IsValid())
            {
                _storedValue = _defaultValue;
                return;
            }

            //if (_inputValue is FloatToFloatGetSetModule)
            if (_targetIntModule != null && _sourceIntModule != null)
                _targetIntModule.SetValue(_sourceIntModule.GetValue());
            if (_targetFloatModule != null && _sourceFloatModule != null)
                _targetFloatModule.SetValue(_sourceFloatModule.GetValue());

            _storedValue = _inputValue.GetValue();
        }

        protected override string GetName()
        {
            return "FloatToFloatSetterModule";
        }
    }
}