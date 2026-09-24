using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi.Modules.MathModules.FloatArrayModules
{
    public class FloatArrayModule : MathModule
    {
        [Header("--- Float Array Module ---")]
        [Tooltip("If the Recalculate() is not IsValid(), the __defaultValue array will be returned as backup.")]
        [SerializeField] protected float[] __defaultValue;
        [Tooltip("If false, the IsValid() function will return false if this _value is null. Else, the function will ignore that. If you set this to false, make sure your " +
                 "follow up modules can handle that _value is null!")]
        [SerializeField] private bool _inputCanBeNull;
        [Tooltip("If false, the IsValid() function will return false if this _value.Length is 0. Else, the function will ignore that.If you set this to false, make sure your " +
                 "follow up modules can handle that _value.Length is 0!")]
        [SerializeField] private bool _inputCanBeEmpty;
        [Tooltip("When accessing elements of the _value via input, but the IsValid(int index) returns false, then this will be the backup value that will be returned.")]
        [SerializeField] private float _defaultValue;
        [Tooltip("If set to true, the module will try to initialize the _value array in the Start() function. This is required for most VoidToFloatArray modules, since the prior to such a " +
                 "module might require this _value array to be present and/or not null and/or not have a length of 0. The exact implementation of the Initialize() method has " +
                 "to be done in the deriving classes.")]
        [SerializeField] private bool _inititializeAtStart = true;

        protected float[] _value;

        protected bool _isArrayInitialized;

        protected override void Start()
        {
            base.Start();
            if (!_inititializeAtStart)
                return;
            TryInitializeArray();
        }


        public virtual float[] GetValue()
        {
            if (!IsValid())
                return __defaultValue;
            
            TryRecalculate();
            return _value;
        }

        public virtual float GetValue(int index)
        {
            if (!IsValid() || !IsValid(index))
                return _defaultValue;

            TryRecalculate();
            return _value[index];
        }

        public virtual int Length()
        {
            TryInitializeArray();
            if (_value == null)
                return 0;
            return _value.Length;
        }

        protected virtual bool IsValid(int index)
        {
            if (_value == null || index >= Length())
            {
                Utility.Debugger.LogError("_value == null || index >= Length() in FloatArrayModule");
                return false;
            }
            return true;
        }

        private void TryInitializeArray()
        {
            if (_isArrayInitialized)
                return;
            InitializeArray();
        }

        protected virtual void InitializeArray()
        {
            _isArrayInitialized = true;
        }

        #region Overrides 
        protected override bool IsValid()
        {
            if (_value != null)
            {
                if (!_inputCanBeEmpty && _value.Length == 0)
                {
                    Utility.Debugger.LogError("Invalid in FloatArrayModule, " + gameObject.name);
                    return false;
                }
            }
            else if (!_inputCanBeNull)
            {
                Utility.Debugger.LogError("Invalid in FloatArrayModule, " + GetType() + " on " + gameObject.name);
                return false;
            }

            return base.IsValid();
        }
        protected override string GetName()
        {
            return "FloatArrayModule [" + Length() + "]";
        }
        #endregion
    }
}