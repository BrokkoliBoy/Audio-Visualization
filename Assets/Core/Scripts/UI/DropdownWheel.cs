using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gavi
{
    /// <summary>
    /// Lets the mouse wheel step through the options of a dropdown while the pointer is on it, without opening it.
    /// Wheel forward = previous option, wheel back = next option, wrapping around at both ends. Does nothing while the list is open (it scrolls itself).
    /// While the list is open, the option under the pointer is shown as a preview (the dropdown takes its value). Closing the list
    /// without picking an option brings the old value back.
    /// </summary>
    [RequireComponent(typeof(TMP_Dropdown))]
    public class DropdownWheel : MonoBehaviour, IScrollHandler
    {
        private TMP_Dropdown _dropdown;
        private bool _wasExpanded;
        private bool _committed;
        private int _originalValue;

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

        private void Update()
        {
            if (_dropdown == null)
                _dropdown = GetComponent<TMP_Dropdown>();
            bool expanded = _dropdown.IsExpanded;
            if (expanded && !_wasExpanded)
            {
                _originalValue = _dropdown.value;
                _committed = false;
            }
            else if (!expanded && _wasExpanded && !_committed && _dropdown.value != _originalValue)
                _dropdown.value = _originalValue;
            _wasExpanded = expanded;
            if (!expanded)
                return;

            int hovered = HoveredOption();
            if (Input.GetMouseButtonUp(0) && hovered >= 0 || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                _committed = true;
            if (hovered >= 0 && hovered != _dropdown.value && !_committed)
                _dropdown.value = hovered;
        }

        // The option whose item lies under the pointer, -1 if none (or if the item is scrolled out of the list).
        private int HoveredOption()
        {
            Canvas canvas = _dropdown.GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector2 pointer = UiPointer.Position;
            ScrollRect scroll = _dropdown.GetComponentInChildren<ScrollRect>();
            if (scroll != null && scroll.viewport != null && !RectTransformUtility.RectangleContainsScreenPoint(scroll.viewport, pointer, camera))
                return -1;
            foreach (Toggle item in _dropdown.GetComponentsInChildren<Toggle>())
            {
                // the items are named "Item <option>: <text>"; the template they were made from has no number
                string name = item.name;
                if (!name.StartsWith("Item ") || !RectTransformUtility.RectangleContainsScreenPoint((RectTransform)item.transform, pointer, camera))
                    continue;
                int end = name.IndexOf(':');
                string number = end > 5 ? name.Substring(5, end - 5) : name.Substring(5);
                if (int.TryParse(number, out int option))
                    return option;
            }
            return -1;
        }
    }
}
