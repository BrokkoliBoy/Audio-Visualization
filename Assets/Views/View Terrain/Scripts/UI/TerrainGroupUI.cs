using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi
{
    public class TerrainGroupUI : MonoBehaviour
    {
        public virtual void ExtractPreset(TerrainSettingsPreset preset)
        {
            
        }
        
        public virtual void ShowPreset(TerrainSettingsPreset preset)
        {
            
        }

        // The change is applied by the settings manager (see TerrainSettingsManager.LateUpdate), not by the group: a group in a
        // closed tab is inactive, but its sliders can sit in another tab (e.g. height follows zoom in the Expert tab).
        public void OnUIValueChanged()
        {
            MarkChanged();
        }
        public void OnUIValueChanged(float f)
        {
            MarkChanged();
        }
        public void OnUIValueChanged(bool b)
        {
            MarkChanged();
        }
        public void OnUIValueChanged(int i)
        {
            MarkChanged();
        }

        /// <summary>Text for the layer tab of this group: the name of its pattern or shape, or "Off" while the layer is off. Null if the group is no layer.</summary>
        public virtual string LayerLabel => null;

        public const string LayerOffLabel = "Off";

        private static void MarkChanged()
        {
            if (TerrainSettingsManager.Instance != null)
                TerrainSettingsManager.Instance.MarkUIChanged();
        }
    }
}
