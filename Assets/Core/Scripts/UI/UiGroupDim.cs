using UnityEngine;
using UnityEngine.UI;

namespace Gavi
{
    /// <summary>Dims the rows of a window while the on / off toggle in its header is off (they have no effect then). The header, the accent stripe and the background stay as they are.</summary>
    public class UiGroupDim : MonoBehaviour
    {
        [Tooltip("The on / off toggle in the header of the window.")]
        [SerializeField] private Toggle _toggle;
        [Tooltip("Names of rows that do not depend on the toggle and stay bright.")]
        [SerializeField] private string[] _keepBright;
        [Tooltip("Opacity of the rows while the toggle is off.")]
        [SerializeField] private float _dimmedAlpha = 0.4f;

        private CanvasGroup[] _groups;

        /// <summary>The on / off toggle of the window, null if it has none.</summary>
        public Toggle Toggle => _toggle;
        /// <summary>Opacity of the rows while the toggle is off.</summary>
        public float DimmedAlpha => _dimmedAlpha;

        private void Awake()
        {
            if (_toggle == null)
                return;

            var groups = new System.Collections.Generic.List<CanvasGroup>();
            foreach (Transform child in transform)
            {
                if (child.GetComponentInChildren<Toggle>(true) == _toggle || child.name.StartsWith("Image - ") || System.Array.IndexOf(_keepBright ?? new string[0], child.name) >= 0)
                    continue;
                CanvasGroup group = child.GetComponent<CanvasGroup>();
                if (group == null)
                    group = child.gameObject.AddComponent<CanvasGroup>();
                groups.Add(group);
            }
            _groups = groups.ToArray();
            _toggle.onValueChanged.AddListener(_ => Refresh());
            Refresh();
        }

        private void OnEnable()
        {
            if (_groups != null)
                Refresh();
        }

        private void Refresh()
        {
            float alpha = _toggle.isOn ? 1f : _dimmedAlpha;
            foreach (CanvasGroup group in _groups)
                group.alpha = alpha;
        }
    }
}
