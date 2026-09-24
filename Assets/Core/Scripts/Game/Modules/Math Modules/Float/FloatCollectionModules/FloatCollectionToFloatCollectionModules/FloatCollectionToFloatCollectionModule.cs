using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi.Modules.MathModules.FloatCollectionModules.FloatCollectionToFloatCollectionModules
{
    public class FloatCollectionToFloatCollectionModule : FloatCollectionModule
    {
        [Header("--- FloatCollection to FloatCollection Module ---")]
        [SerializeField] protected FloatCollectionModule _inputValue;


        public override int Length()
        {
            if (_modules != null && _modules.Count > 0)
            {
                Utility.Debugger.LogAssertionFail("Your variable _modules is not empty. This wont cause problems, but make sure that you are using this script correctly. " +
                    "The designed way of using FloatCollectionToFloatCollectionModule most oftenly is to use another FloatCollectionModule and not single FloatModules.");
            }

            if (_inputValue == null)
                return 0;
            return _inputValue.Length();
        }

        protected override bool IsValid(int index)
        {
            if (_modules != null && _modules.Count > 0)
            {
                Utility.Debugger.LogAssertionFail("Your variable _modules is not empty. This wont cause problems, but make sure that you are using this script correctly. " +
                    "The designed way of using FloatCollectionToFloatCollectionModule most oftenly is to use another FloatCollectionModule and not single FloatModules.");
            }

            if (_inputValue == null || index >= Length())
            {
                Utility.Debugger.LogError("_inputValue == null || index >= Length() in FloatCollectionModule");
                return false;
            }
            return true;
        }

        protected override bool IsValid()
        {
            if (_modules != null && _modules.Count > 0)
            {
                Utility.Debugger.LogAssertionFail("Your variable _modules is not empty. This wont cause problems, but make sure that you are using this script correctly. " +
                    "The designed way of using FloatCollectionToFloatCollectionModule most oftenly is to use another FloatCollectionModule and not single FloatModules.");
            }

            if (_inputValue != null)
            {
                if (!_inputCanBeEmpty && _inputValue.Length() == 0)
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

            return true;
        }

        protected override string GetName()
        {
            return "FloatCollectionToFloatCollectionModule";
        }
    }
}