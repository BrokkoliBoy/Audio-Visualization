using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Gavi
{
    /// <summary>
    /// Lets the mouse wheel step through the options of a dropdown while the pointer is on it, without opening it.
    /// Wheel forward = previous option, wheel back = next option, wrapping around at both ends. Does nothing while the list is open (it scrolls itself).
    /// </summary>
    [RequireComponent(typeof(TMP_Dropdown))]
    public class DropdownWheel : MonoBehaviour, IScrollHandler
    {
        private TMP_Dropdown _dropdown;

        public void OnScroll(PointerEventData eventData)
        {
            if (_dropdown == null)
                _dropdown = GetComponent<TMP_Dropdown>();
            if (!_dropdown.interactable || _dropdown.IsExpanded || _dropdown.options.Count == 0)
                return;

            float wheel = eventData.scrollDelta.y;
            if (Mathf.Abs(wheel) < 0.001f)
                return;

            int count = _dropdown.options.Count;
            int next = (_dropdown.value + (wheel > 0f ? -1 : 1) + count) % count;
            if (next != _dropdown.value)
                _dropdown.value = next;
            eventData.Use();
        }
    }
}
