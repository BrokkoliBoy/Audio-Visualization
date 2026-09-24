using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gavi.Modules.MathModules.FloatArrayModules;
using Gavi.Modules.MathModules.FloatModules;

namespace Gavi.Modules.MathModules.FloatCollectionModules.FloatArrayToFloatCollectionModules
{
    public class FloatArrayToFloatCollectionModule : FloatCollectionModule
    {
        [SerializeField] protected FloatArrayModule _inputValue;

        public override List<FloatModule> GetValue()
        {
            Utility.Debugger.LogAssertionFail("In FloatArrayToFloatCollectionModule.GetValue(): This module is not designed to return its _modules. It is designed" +
                " only get the values of a list via its indices (GetValue(int index)).");
            return null;
        }

        public override float GetValue(int index)
        {
            if (!IsValid() || !IsValid(index))
                return _defaultValue;

            return _inputValue.GetValue(index);
        }



        #region Overrides
        public override int Length()
        {
            if (!IsValid())
                return 0;
            return _inputValue.Length();
        }
        // base.IsValid(int index) checks if _modules is valid but not only do we never use _modules which means that they can be invalid without any problems,
        // but we actually want it to be null or a length of 0. The reason is that there is no need to assign anything to _modules in this module but if there
        // was something assigned to it in the inspector, the possibility exists, that the user miss understood the way that this script here is used so I want
        // to inform him on this possible mistake.
        protected override bool IsValid()
        {
            if (_modules != null && base.Length() > 0)
                Utility.Debugger.LogAssertionFail("In FloatArrayToFloatCollectionModule.IsValid(): You have assigned something to _modules, but this module here " +
                    "does not use _modules at all. Make sure that you understood how to use this module. There is currently no way to suppress this warning.");

            if (_inputValue == null)
                return false;
            return /*base.IsValid()*/true;
        }
        protected override bool IsValid(int index)
        {
            return index < Length();
        }
        protected override string GetName()
        {
            return "FA2FC Module";
        }
        #endregion
    }
}