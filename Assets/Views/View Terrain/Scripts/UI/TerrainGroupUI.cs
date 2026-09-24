using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi
{
    public class TerrainGroupUI : MonoBehaviour
    {
        private bool _wasUpdated;

        private void LateUpdate()
        {
            if (!_wasUpdated) return;
            _wasUpdated = false;
            TerrainSettingsManager.Instance.CurrentProfile.ExtractProfileFromUI();
            TerrainSettingsManager.Instance.CurrentProfile.ApplyProfileToTerrain();
        }


        public virtual void ExtractProfile(TerrainSettingsProfile profile)
        {
            
        }
        
        public virtual void ShowProfile(TerrainSettingsProfile profile)
        {
            
        }

        public void OnUIValueChanged()
        {
            _wasUpdated = true;
        }
        public void OnUIValueChanged(float f)
        {
            _wasUpdated = true;
        }
        public void OnUIValueChanged(bool b)
        {
            _wasUpdated = true;
        }
        public void OnUIValueChanged(int i)
        {
            _wasUpdated = true;
        }
    }
}
