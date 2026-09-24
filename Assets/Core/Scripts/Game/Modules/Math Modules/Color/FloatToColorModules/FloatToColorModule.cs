using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gavi.Modules.MathModules.FloatModules;

namespace Gavi.Modules.MathModules.ColorModules.FloatToColorModules
{
    public class FloatToColorModule : ColorModule
    {
        [Header("--- Float to Color Module ---")]
        [SerializeField] protected FloatModule _inputValue;



        protected override string GetName()
        {
            return "FloatToColorModule";
        }
    }
}
