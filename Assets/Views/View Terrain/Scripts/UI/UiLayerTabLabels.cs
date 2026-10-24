using TMPro;
using UnityEngine;

namespace Gavi
{
    /// <summary>
    /// The tabs "Layer 1/2/3" at the top of the layer windows (see <see cref="UiGroupSwitch"/>) show what the layer is: the name of its
    /// pattern or shape, or "Off" while the layer is switched off. Every window of the switch has its own copy of the tab strip.
    /// </summary>
    [RequireComponent(typeof(UiGroupSwitch))]
    public class UiLayerTabLabels : MonoBehaviour
    {
        private UiGroupSwitch _switch;
        private GameObject[] _groups;
        private TerrainGroupUI[] _layers;
        private TMP_Text[][] _texts;
        private string[] _shown;

        private void Awake()
        {
            _switch = GetComponent<UiGroupSwitch>();
            var list = new System.Collections.Generic.List<GameObject>(_switch.Groups);
            _groups = list.ToArray();
            _layers = new TerrainGroupUI[_groups.Length];
            _texts = new TMP_Text[_groups.Length][];
            _shown = new string[_groups.Length];
            for (int g = 0; g < _groups.Length; g++)
            {
                _layers[g] = _groups[g].GetComponent<TerrainGroupUI>();
                _texts[g] = new TMP_Text[_groups.Length];
                for (int j = 0; j < _groups.Length; j++)
                {
                    Transform text = _groups[g].transform.Find("Strip - Layers/Tab - Layer " + (j + 1) + "/Text");
                    TMP_Text tmp = text != null ? text.GetComponent<TMP_Text>() : null;
                    if (tmp != null)
                    {
                        tmp.overflowMode = TextOverflowModes.Ellipsis;
                        tmp.enableAutoSizing = true;
                        tmp.fontSizeMax = tmp.fontSize;
                        tmp.fontSizeMin = Mathf.Max(8f, tmp.fontSize * 0.6f);
                    }
                    _texts[g][j] = tmp;
                }
            }
        }

        private void LateUpdate()
        {
            for (int j = 0; j < _layers.Length; j++)
            {
                string label = _layers[j] != null ? _layers[j].LayerLabel : null;
                if (label == null || label == _shown[j])
                    continue;
                _shown[j] = label;
                for (int g = 0; g < _texts.Length; g++)
                    if (_texts[g][j] != null)
                        _texts[g][j].text = label;
            }
        }
    }
}
