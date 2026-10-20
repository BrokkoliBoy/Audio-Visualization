using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Gavi
{
    public class TerrainGroupUITerrain : TerrainGroupUI
    {
        [Tooltip("Number of faces along both axes of the terrain.")]
        [FormerlySerializedAs("_sliderFaceCountX")]
        [SerializeField] private SliderG _sliderFaceCount;
        [SerializeField] private SliderG _sliderTerrainHeight;

        private void Awake()
        {
            _sliderFaceCount.OnValueChangedInt.AddListener(OnUIValueChanged);
            _sliderTerrainHeight.OnValueChangedFloat.AddListener(OnUIValueChanged);
        }

        public override void ExtractProfile(TerrainSettingsProfile profile)
        {
            base.ExtractProfile(profile);
            profile.FaceCountX = _sliderFaceCount.ValueInt;
            profile.FaceCountY = _sliderFaceCount.ValueInt;
            profile.TerrainHeight = _sliderTerrainHeight.Value;
        }

        public override void ShowProfile(TerrainSettingsProfile profile)
        {
            base.ShowProfile(profile);
            _sliderFaceCount.ValueInt = profile.FaceCountX;
            _sliderTerrainHeight.Value = profile.TerrainHeight;
        }
    }
}
