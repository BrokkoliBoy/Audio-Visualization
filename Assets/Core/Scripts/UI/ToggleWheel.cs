using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gavi
{
    /// <summary>
    /// Lets the mouse wheel switch a toggle while the pointer is on it: wheel forward = on, wheel back = off.
    /// A button on top of the toggle (e.g. the fold button of a group header) keeps the wheel for itself.
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public class ToggleWheel : MonoBehaviour, IScrollHandler
    {
        private Toggle _toggle;

        public void OnScroll(PointerEventData eventData)
        {
            if (_toggle == null)
                _toggle = GetComponent<Toggle>();
            if (!_toggle.interactable)
                return;

            float wheel = eventData.scrollDelta.y;
            if (Mathf.Abs(wheel) < 0.001f)
                return;

            GameObject hit = eventData.pointerCurrentRaycast.gameObject;
            if (hit != null && hit.GetComponentInParent<Button>() != null)
                return;

            bool on = wheel > 0f;
            if (_toggle.isOn != on)
                _toggle.isOn = on;
            eventData.Use();
        }
    }
}
