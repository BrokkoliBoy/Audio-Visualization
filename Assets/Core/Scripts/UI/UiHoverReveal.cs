using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi
{
    /// <summary>
    /// Decides whether a menu is open: it opens as soon as the mouse pointer comes near it
    /// and closes as soon as the pointer leaves, without any delay.
    /// Optionally fades a CanvasGroup; other scripts can listen to Changed to slide something instead.
    /// </summary>
    public class UiHoverReveal : MonoBehaviour
    {
        [Header("--- Regions ---")]
        [Tooltip("The pointer near one of these opens the menu, even while it is hidden.")]
        [SerializeField] private List<RectTransform> _triggerRegions = new List<RectTransform>();
        [Tooltip("The pointer near one of these keeps the menu open, but only while it is open (e.g. the menu panel itself).")]
        [SerializeField] private List<RectTransform> _stayRegions = new List<RectTransform>();
        [Tooltip("Extra distance around the trigger regions in canvas reference pixels: left, bottom, right, top.")]
        [SerializeField] private Vector4 _triggerPadding = new Vector4(24, 24, 24, 24);
        [Tooltip("Same for the stay regions.")]
        [SerializeField] private Vector4 _stayPadding = new Vector4(24, 24, 24, 24);
        [Tooltip("While the menu is open, every region grows by this fraction of its own size on each side (left, bottom, right, top), so the pointer can leave a bit without closing it.")]
        [SerializeField] private Vector4 _openExtension = new Vector4(0.3f, 0.3f, 0.3f, 0.3f);
        [Tooltip("The menu stays open while any of these is visible (e.g. a popup that belongs to it).")]
        [SerializeField] private List<CanvasGroup> _keepOpenWhileVisible = new List<CanvasGroup>();

        [Header("--- Behaviour ---")]
        [Tooltip("Shown for this many seconds after start so that new users see it.")]
        [SerializeField] private float _showOnStartSeconds = 0f;
        [Tooltip("Does not close while a mouse button is held, e.g. when dragging a slider out of the menu.")]
        [SerializeField] private bool _keepOpenWhileMouseHeld = true;

        [Header("--- Fade (optional) ---")]
        [SerializeField] private CanvasGroup _fadeGroup;
        [SerializeField] private float _fadeSpeed = 10f;
        [Range(0, 1)]
        [SerializeField] private float _hiddenAlpha = 0f;

        public bool IsRevealed { get; private set; }
        /// <summary>Stays open without the pointer being near (set by hotkeys or by clicking the handle).</summary>
        public bool Pinned { get; set; }
        public event Action<bool> Changed;

        private bool _dismissed;
        private float _startUntil;


        #region Mono
        private void Awake()
        {
            _startUntil = Time.unscaledTime + _showOnStartSeconds;
            IsRevealed = _showOnStartSeconds > 0f;
            ApplyFade(true);
        }

        private void Update()
        {
            bool near = IsPointerNear(IsRevealed);

            // After a dismiss the menu stays closed until the pointer has left once.
            if (_dismissed)
            {
                if (!near)
                    _dismissed = false;
                near = false;
            }

            bool want = !_dismissed && (Pinned || near || Time.unscaledTime < _startUntil || IsKeptOpenByOthers());

            if (IsRevealed && !want && !_dismissed && IsStickyActive())
                want = true;

            if (want != IsRevealed)
            {
                IsRevealed = want;
                Changed?.Invoke(want);
            }

            ApplyFade(false);
        }
        #endregion


        #region Control
        /// <summary>Hotkey behaviour: closes the menu when it is open, otherwise opens it and keeps it open.</summary>
        public void ToggleByKey()
        {
            if (IsRevealed)
                Dismiss();
            else
                Open();
        }

        /// <summary>Click behaviour: keeps the menu open or lets go of it again.</summary>
        public void TogglePinned()
        {
            Pinned = !Pinned;
            if (Pinned)
                _dismissed = false;
        }

        public void Open()
        {
            _dismissed = false;
            Pinned = true;
        }

        /// <summary>Closes the menu now and keeps it closed until the pointer has left it once.</summary>
        public void Dismiss()
        {
            Pinned = false;
            _startUntil = 0f;
            _dismissed = true;
        }
        #endregion


        #region Helpers
        private bool IsPointerNear(bool includeStayRegions)
        {
            for (int i = 0; i < _triggerRegions.Count; i++)
                if (UiPointer.IsNear(_triggerRegions[i], _triggerPadding, includeStayRegions ? _openExtension : Vector4.zero))
                    return true;

            if (includeStayRegions)
                for (int i = 0; i < _stayRegions.Count; i++)
                    if (UiPointer.IsNear(_stayRegions[i], _stayPadding, _openExtension))
                        return true;

            return false;
        }

        private bool IsKeptOpenByOthers()
        {
            for (int i = 0; i < _keepOpenWhileVisible.Count; i++)
                if (_keepOpenWhileVisible[i] != null && _keepOpenWhileVisible[i].alpha > 0.05f)
                    return true;
            return false;
        }

        private bool IsStickyActive()
        {
            if (_keepOpenWhileMouseHeld && UiPointer.IsInsideWindow && Input.GetMouseButton(0))
                return true;
            return UiPointer.IsAnyDropdownExpanded();
        }

        private void ApplyFade(bool instant)
        {
            if (_fadeGroup == null)
                return;

            float target = IsRevealed ? 1f : _hiddenAlpha;
            _fadeGroup.alpha = instant ? target : Mathf.MoveTowards(_fadeGroup.alpha, target, _fadeSpeed * Time.unscaledDeltaTime);
            _fadeGroup.interactable = IsRevealed;
            _fadeGroup.blocksRaycasts = IsRevealed;
        }
        #endregion
    }
}
