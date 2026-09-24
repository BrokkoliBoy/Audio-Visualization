using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi.Modules.MathModules.FloatArrayModules.FloatArrayToFloatArrayModules
{
    public class FloatArrayToFloatArrayModule : FloatArrayModule
    {
        [Header("--- FA 2 FA Module ---")]
        [Tooltip("The FloatArrayModule which functions as the input value to perform actions with.")]
        [SerializeField] protected FloatArrayModule _inputValue;
        [Tooltip("If set to true and the input values _value arrays length differs from this _value arrays length, the ReInitialize() function will be called which sets this _value to " +
                 "a new float[] with _inputValue's _value arrays length. It also allows deriving classes to perform additional action when reinitializing.")]
        [SerializeField] private bool _reInitializeOnInputLengthDifference = true;

        protected override void InitializeArray()
        {
            //if (_isArrayInitialized)
            //{
            //    //Utility.Debugger.LogAssertionFail("_isArrayInitialized in FloatArrayToFloatArrayModule.InitializeArray(). When calling InitializeArray, at the current implementation the " +
            //    //    "calling method should check")
            //    return;
            //}
            base.InitializeArray();

            if (_value != null)
            {
                Utility.Debugger.LogAssertionFail("_value != null in FloatArrayToFloatArrayModule.Start(). _value should not already have been initialized here?");
                if (_value.Length > 0)
                {
                    Utility.Debugger.LogAssertionFail("_value.Length > 0 in FloatArrayToFloatArrayModule.Start(). Not only has _value been initialized in here, it's length isn't even zero??");
                    return;
                }
            }

            int length = _inputValue.Length();
            if (length == 0)
                Utility.Debugger.LogAssertionFail("length == 0 in FloatArrayToFloatArrayModule.Start(). The length of the input array is 0. Is the input value array not initialized at this point in code?");
            _value = new float[length];
        }

        protected virtual void ReInitialize()
        {
            int length = _inputValue.Length();
            if (length == 0)
                Utility.Debugger.LogAssertionFail("length == 0 in FloatArrayToFloatArrayModule.Start(). The length of the input array is 0. Is the input value array not initialized at this point in code?");
            _value = new float[length];
        }

        protected override void Recalculate()
        {
            base.Recalculate();

            if (!_inputValue.CalculateOncePerFrameOnly)
            {
                Utility.Debugger.LogAssertionFail("_inputValue.CalculateOncePerFrameOnly in FloatArrayToFloatArrayModule.Recalculate(). This poses a problem, since _inputValue.GetValue() might be called " +
                    "twice by one module. Once (here) in the base method, and once inside the overriding method. Path not implemented yet!");
            }

            if (_value == null)
                _value = new float[_inputValue.Length()];
            
            if (_reInitializeOnInputLengthDifference && _inputValue.Length() != _value.Length)
                ReInitialize();

            float[] input = _inputValue.GetValue();
            if (_value.Length != input.Length) // TODO: Should be deprecated by now
            {
                Utility.Debugger.LogError("_value.Length != input.Length in FloatArrayToFloatArrayModule.Recalculate()! Either something is not working correctly or you should set " +
                                          "_reInitializeOnInputLengthDifference to true");
                return;
            }
        }
    }
}
