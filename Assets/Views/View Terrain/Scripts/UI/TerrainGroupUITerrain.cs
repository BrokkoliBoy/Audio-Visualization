using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Gavi
{
    public class TerrainGroupUITerrain : TerrainGroupUI
    {
        [Tooltip("Detail of the terrain: the number of faces along both axes (1 = 20, 7 = 500, 10 = 1000). The upper end is compressed, 500 faces are enough for most patterns.")]
        [FormerlySerializedAs("_sliderFaceCountX")]
        [SerializeField] private SliderG _sliderFaceCount;
        [SerializeField] private SliderG _sliderTerrainHeight;

        private void Awake()
        {
            _sliderFaceCount.OnValueChangedInt.AddListener(OnUIValueChanged);
            _sliderTerrainHeight.OnValueChangedFloat.AddListener(OnUIValueChanged);
        }

        public override void ExtractPreset(TerrainSettingsPreset preset)
        {
            base.ExtractPreset(preset);
            preset.FaceCountX = _sliderFaceCount.ValueInt;
            preset.FaceCountY = _sliderFaceCount.ValueInt;
            preset.TerrainHeight = _sliderTerrainHeight.Value;
        }

        public override void ShowPreset(TerrainSettingsPreset preset)
        {
            base.ShowPreset(preset);
            _sliderFaceCount.ValueInt = preset.FaceCountX;
            _sliderTerrainHeight.Value = preset.TerrainHeight;
        }
    }
}
