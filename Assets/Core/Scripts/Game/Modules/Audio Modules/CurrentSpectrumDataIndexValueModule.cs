using System.Collections;
using System.Collections.Generic;
using Gavi.Modules.MathModules.FloatModules;
using Gavi.Modules.MathModules.FloatModules.VoidToFloatModules;
using UnityEngine;

namespace Gavi.Modules
{
    public class CurrentSpectrumDataIndexValueModule : VoidToFloatModule
    {
        [Header("--- Index Module ---")]
        [Tooltip("If the module is not null, the modules value will be used instead of this integer.")]
        [SerializeField] private int _index;
        [Tooltip("If the module is not null, the modules value will be used instead of this integer. The float value of float module will simply be casted to int.")]
        [SerializeField] private FloatModule _indexModule;
        
        protected override void Recalculate()
        {
            base.Recalculate();

            int index = _indexModule != null ? (int) _indexModule.GetValue() : _index;
            _storedValue = ViewManager.Instance.CurrentMainSamplesContainer.GetValue(index);
        }

        protected override bool IsValid()
        {
            if (ViewManager.Instance.CurrentMainSamplesContainer == null)
                return false;
            int index = _indexModule != null ? (int) _indexModule.GetValue() : _index;
            if (index < 0 || _index >= ViewManager.Instance.CurrentMainSamplesContainer.Length())
                return false;
            return base.IsValid();
        }
        
        protected override string GetName()
        {
            return "V2F Current Spectrum Data Index Value [" + _index + "/" + (_indexModule != null ? "+" : "-") + "]";
        }
    }
}
