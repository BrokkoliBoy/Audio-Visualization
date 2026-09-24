using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gavi.Modules.MathModules.FloatModules;
using Gavi.Modules.MathModules.ColorModules;

namespace Gavi.Modifiables
{
    public class MaterialPropertyModifiable : MonoBehaviour
    {
        [SerializeField] private List<Renderer> _renderers;
        [SerializeField] private List<Material> _materials;

        [SerializeField] private List<FloatProperty> _floatProperties;
        [System.Serializable]
        private class FloatProperty
        {
            public string PopertyId;
            public FloatModule FloatValue;
        }

        [SerializeField] private List<ColorProperty> _colorProperties;
        [System.Serializable]
        private class ColorProperty
        {
            public string PopertyId;
            public ColorModule ColorValue;
        }

        private void Update()
        {
            SetFloatProperty();
            SetColorProperty();
        }

        private void SetFloatProperty()
        {
            for (int propertyIndex = 0; propertyIndex < _floatProperties.Count; propertyIndex++) /*FloatProperty property in _floatProperties)*/
            {
                float value = _floatProperties[propertyIndex].FloatValue.GetValue();

                for (int rendererIndex = 0; rendererIndex < _renderers.Count; rendererIndex++)/* Renderer renderer in _renderers)*/
                {
                    for (int materialIndex = 0; materialIndex < _renderers[rendererIndex].materials.Length; materialIndex++)/* (Material material in _renderers[rendererIndex].materials)*/
                    {
                        if (_renderers[rendererIndex] == null)
                            continue;
                        _renderers[rendererIndex].materials[materialIndex].SetFloat(_floatProperties[propertyIndex].PopertyId, value);
                    }
                }

                for (int materialIndex = 0; materialIndex < _materials.Count; materialIndex++)
                {
                    if (_materials[materialIndex] == null)
                        continue;
                    _materials[materialIndex].SetFloat(_floatProperties[propertyIndex].PopertyId, value);
                }
            }
        }

        private void SetColorProperty()
        {
            for (int propertyIndex = 0; propertyIndex < _colorProperties.Count; propertyIndex++) /*ColorProperty property in _colorProperties)*/
            {
                Color value = _colorProperties[propertyIndex].ColorValue.GetValue();

                for (int rendererIndex = 0; rendererIndex < _renderers.Count; rendererIndex++)/* Renderer renderer in _renderers)*/
                {
                    for (int materialIndex = 0; materialIndex < _renderers[rendererIndex].materials.Length; materialIndex++)/* (Material material in _renderers[rendererIndex].materials)*/
                    {
                        _renderers[rendererIndex].materials[materialIndex].SetColor(_colorProperties[propertyIndex].PopertyId, value);
                    }
                }

                for (int materialIndex = 0; materialIndex < _materials.Count; materialIndex++)
                {
                    _materials[materialIndex].SetColor(_colorProperties[propertyIndex].PopertyId, value);
                }
            }
        }
    }

}