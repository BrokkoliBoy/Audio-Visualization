using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gavi.Modules.MathModules.FloatModules;

namespace Gavi.Modules.MathModules.FloatCollectionModules
{
    public class FloatCollectionModule : MathModule
    {
        [Header("--- Float Collection Module ---")]
        [SerializeField] protected List<FloatModule> _modules;
        [SerializeField] protected bool _inputCanBeNull;
        [SerializeField] protected bool _inputCanBeEmpty;
        [Space]
        protected float _defaultValue;

        public virtual List<FloatModule> GetValue()
        {
            if (!IsValid())
                return null;
            return _modules;
        }

        public virtual float GetValue(int index)
        {
            if (!IsValid() || !IsValid(index))
                return _defaultValue;
            
            return GetValue()[index].GetValue();
        }

        public virtual int Length()
        {
            if (_modules == null)
                return 0;
            return _modules.Count;
        }

        protected virtual bool IsValid(int index)
        {
            if (_modules == null || index >= Length())
            {
                Utility.Debugger.LogError("_modules == null || index >= Length() in FloatCollectionModule");
                return false;
            }
            return true;
        }


        #region Overrides
        protected override bool IsValid()
        {
            if (_modules != null)
            {
                if (!_inputCanBeEmpty && _modules.Count == 0)
                {
                    Utility.Debugger.LogError("Invalid in FloatCollectionModule, " + GetType());
                    return false;
                }
            }
            else if (!_inputCanBeNull)
            {
                Utility.Debugger.LogError("Invalid in FloatCollectionModule, " + GetType());
                return false;
            }

            return base.IsValid();
        }

        protected override string GetName()
        {
            return "FloatCollectionModule [" + _modules?.Count + "]";
        }
        #endregion
    }
}
