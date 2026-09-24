using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gavi.Modules.MathModules.FloatModules;
using Gavi.Modules.MathModules.ColorModules;
using UnityEngine.Rendering.HighDefinition;

namespace Gavi.Modifiables
{
    public class LightModifiable : Module
    {
        [SerializeField] private List<Light> _lights;
        [SerializeField] private List<HDAdditionalLightData> _lightData;

        [SerializeField] private FloatModule _intensity;
        [SerializeField] private FloatModule _range;
        [SerializeField] private ColorModule _color;

        private void Update()
        {
            if (_intensity != null)
            {
                for (int i = 0; i < _lights.Count; i++)
                    _lights[i].intensity = _intensity.GetValue();
                for (int i = 0; i < _lightData.Count; i++)
                    _lightData[i].intensity = _intensity.GetValue();
            }


            if (_range != null)
            {
                for (int i = 0; i < _lights.Count; i++)
                    _lights[i].range = _range.GetValue();
                for (int i = 0; i < _lightData.Count; i++)
                    _lightData[i].range = _range.GetValue();
            }

            if (_color != null)
            {
                for (int i = 0; i < _lights.Count; i++)
                    _lights[i].color = _color.GetValue();
                for (int i = 0; i < _lightData.Count; i++)
                    _lightData[i].color = _color.GetValue();
            }
        }

        protected override string GetName()
        {
            return "LightSettingsModule";
        }
    }
}