using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi
{
    public class TerrainGroupUITerrain : TerrainGroupUI
    {
        [SerializeField] private SliderG _sliderFaceCountX;
        [SerializeField] private SliderG _sliderFaceCountY;
        [SerializeField] private SliderG _sliderTerrainHeight;

        private void Awake()
        {
            _sliderFaceCountX.OnValueChangedInt.AddListener(OnUIValueChanged);
            _sliderFaceCountY.OnValueChangedInt.AddListener(OnUIValueChanged);
            _sliderTerrainHeight.OnValueChangedFloat.AddListener(OnUIValueChanged);
        }

        public override void ExtractProfile(TerrainSettingsProfile profile)
        {
            base.ExtractProfile(profile);
            profile.FaceCountX = _sliderFaceCountX.ValueInt;
            profile.FaceCountY = _sliderFaceCountY.ValueInt;
            profile.TerrainHeight = _sliderTerrainHeight.Value;
        }

        public override void ShowProfile(TerrainSettingsProfile profile)
        {
            base.ShowProfile(profile);
            _sliderFaceCountX.ValueInt = profile.FaceCountX;
            _sliderFaceCountY.ValueInt = profile.FaceCountY;
            _sliderTerrainHeight.Value = profile.TerrainHeight;
        }
    }
}
