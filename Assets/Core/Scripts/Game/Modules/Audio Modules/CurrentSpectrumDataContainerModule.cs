using System.Collections;
using System.Collections.Generic;
using Gavi.Audio;
using Gavi.Modules.MathModules.FloatArrayModules.VoidToFloatArrayModules;
using UnityEngine;

namespace Gavi.Modules
{
    public class CurrentSpectrumDataContainerModule : VoidToFloatArrayModule
    {
        protected override void Recalculate()
        {
            base.Recalculate();
            if (!IsValid())
                return;
            _value = ViewManager.Instance.CurrentMainSamplesContainer.GetValue();
        }
        
        
        #region Overrides
        protected override void InitializeArray()
        {
            if (ViewManager.Instance.CurrentMainSamplesContainer == null)
                _value = new float[0];
            else
                _value = new float[ViewManager.Instance.CurrentMainSamplesContainer.Length()];
            base.InitializeArray();
        }

        public override int Length()
        {
            // return base.Length();
            if (ViewManager.Instance.CurrentMainSamplesContainer == null)
                return 0;
            return ViewManager.Instance.CurrentMainSamplesContainer.Length();
        }

        protected override bool IsValid()
        {
            if (ViewManager.Instance.CurrentMainSamplesContainer == null)
                return false;
            return base.IsValid();
        }

        protected override string GetName()
        {
            return "Current SpectrumDataContainer 2 FA Module";
        }
        #endregion
    }
}
