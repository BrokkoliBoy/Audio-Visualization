using UnityEngine;
using UnityEngine.EventSystems;
using Gavi.Audio;

namespace Gavi
{
    /// <summary>
    /// Band editor of the test signal: while it is on, the left mouse button raises the band under the cursor and the
    /// right mouse button lowers it. The band is found on the terrain surface as it is drawn.
    /// </summary>
    public class TerrainBandPicker : MonoBehaviour
    {
        [Tooltip("The terrain to pick on. Taken from this object if empty.")]
        [SerializeField] private TerrainGenerator _generator;

        private void Awake()
        {
            if (_generator == null)
                _generator = GetComponent<TerrainGenerator>();
        }

        private void Update()
        {
            if (!AudioTestSignal.IsEditingBands || _generator == null)
                return;

            bool raise = Input.GetMouseButton(0);
            bool lower = Input.GetMouseButton(1);
            if (raise == lower)
                return;
            if (!UiPointer.IsInsideWindow || UiPointer.IsTyping)
                return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Camera camera = Camera.main;
            if (camera == null)
                return;

            if (_generator.TryPickBand(camera.ScreenPointToRay(Input.mousePosition), out int band))
                AudioTestSignal.Instance.EditBand(band, raise ? 1f : -1f, Time.unscaledDeltaTime);
        }
    }
}
