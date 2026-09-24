using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using Gavi.Modules.MathModules.FloatModules;
using Gavi.Modules.MathModules.VectorModules;

namespace Gavi.Modifiables
{
    public class VFXGraphModifiable : Module
    {
        [SerializeField] private List<VisualEffect> _effects;

        // float
        [System.Serializable]
        private class VFXFloatProperty
        {
            public string Identifier;
            public FloatModule Value;
        }
        [SerializeField] private List<VFXFloatProperty> _floatProperties;

        // Vector
        [System.Serializable]
        private class VFXVectorProperty
        {
            public string Identifier;
            public VectorModule Value;
        }
        [SerializeField] private List<VFXVectorProperty> _vectorProperties;

        //[System.Serializable]
        //private class VFXColorProperty
        //{
        //    public string Identifier;
        //    public Gradient Value;
        //}
        //[SerializeField] private List<VFXColorProperty> _colorProperties;


        private void Update()
        {
            foreach (VisualEffect effect in _effects)
            {
                foreach (VFXFloatProperty property in _floatProperties)
                    effect.SetFloat(property.Identifier, property.Value.GetValue());
                foreach (VFXVectorProperty property in _vectorProperties)
                    effect.SetVector3(property.Identifier, property.Value.GetValue());
                //foreach (VFXColorProperty property in _colorProperties)
                //    effect.SetGradient(property.Identifier, property.Value);
            }
        }

        protected override string GetName()
        {
            return "VisualEffectsGraphModule";
        }
    }
}