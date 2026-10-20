using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gavi
{
    public class TerrainGroupUISmoothing : TerrainGroupUI
    {
        [SerializeField] private Toggle _toggleApplySmoothing;

        private void Awake()
        {
            _toggleApplySmoothing.onValueChanged.AddListener(OnUIValueChanged);
        }

        public override void ExtractProfile(TerrainSettingsProfile profile)
        {
            base.ExtractProfile(profile);
            profile.ApplySmoothing = _toggleApplySmoothing.isOn;
        }

        public override void ShowProfile(TerrainSettingsProfile profile)
        {
            base.ShowProfile(profile);
            _toggleApplySmoothing.isOn = profile.ApplySmoothing;
        }
    }
}
