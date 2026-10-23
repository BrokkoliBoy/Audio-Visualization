using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gavi
{
    /// <summary>
    /// Lets several menu groups share one window: only one group is shown at a time and a row of small tabs inside each group picks
    /// it. Hovering a tab shows its group at once, a click does the same. The new group takes over the fold state of the old one,
    /// so the window stays as it is (open, pinned or opened by hovering). The groups are not part of any tab page list: this switch
    /// turns them on when it is shown and off when it is hidden (e.g. when its menu tab is closed).
    /// </summary>
    public class UiGroupSwitch : MonoBehaviour
    {
        [Serializable]
        public class Entry
        {
            [Tooltip("The group of this layer.")]
            public GameObject Group;
            [Tooltip("The tab buttons inside this group, one per layer in the same order as the entries.")]
            public Button[] Buttons;
            [Tooltip("The small bars of those buttons that show the selected layer.")]
            public Image[] Indicators;
            [Tooltip("Optional: the labels of those buttons (e.g. Base / Expert); the selected one is bright, the others dim.")]
            public TMPro.TMP_Text[] Labels;
        }

        [SerializeField] private List<Entry> _entries = new List<Entry>();
        [SerializeField] private int _startIndex;
        [Tooltip("One single button per group (the first of its Buttons) that shows the next group while the pointer rests on it. Its label (first of Labels) names the group it leads to, its icon (first of Indicators) is tinted with the label.")]
        [SerializeField] private bool _toggleMode;
        [Tooltip("Optional: while this toggle is off, leaving the window shows the first group again (e.g. a window whose second page does nothing while its feature is off).")]
        [SerializeField] private Toggle _firstPageWhenOff;
        [SerializeField] private Color _indicatorActiveColor = new Color(0.1f, 0.83f, 0.88f, 1f);
        [SerializeField] private Color _indicatorInactiveColor = new Color(1f, 1f, 1f, 0.12f);
        [SerializeField] private Color _labelActiveColor = new Color(0.9f, 0.97f, 1f, 1f);
        [SerializeField] private Color _labelInactiveColor = new Color(0.62f, 0.84f, 0.88f, 0.45f);

        [Tooltip("Hovering a tab does not pick its layer while the window slides and for this long (s) afterwards.")]
        [SerializeField] private float _hoverSettleTime = 0.3f;

        private int _current;
        private int _hoverIndex = -1;
        private bool _wired;
        private float _markFade = 1f;
        private bool _toggleArmed;
        private bool _toggleInside;
        private float _spinTime = 1f;
        private float _orbitFrom;
        private float _orbitValue;
        private int _orbitTarget;
        private float _dotValue;
        private float _orbitShow;
        private float _orbitHold;
        [Tooltip("How fast the dot follows the turning icon (the lower, the more it lags behind).")]
        [SerializeField] private float _dotFollow = 6.9f;
        [Tooltip("Seconds the dot and the sockets stay visible after the dot arrived.")]
        [SerializeField] private float _orbitHoldSeconds = 0.4f;
        [Tooltip("Distance (px) of the moving dot from the middle of the toggle icon.")]
        [SerializeField] private float _orbitRadius = 11f;
        [Tooltip("Seconds the icon of a toggle takes for its half turn when the page changes (eased at both ends).")]
        [SerializeField] private float _spinSeconds = 0.435f;

        private void Awake()
        {
            _current = Mathf.Clamp(_startIndex, 0, Mathf.Max(0, _entries.Count - 1));
            _orbitTarget = _current;
            _orbitValue = _current;
            _orbitFrom = _current;
            _dotValue = _current;
        }

        private void Start()
        {
            for (int e = 0; e < _entries.Count; e++)
            {
                Entry entry = _entries[e];
                if (entry.Buttons == null)
                    continue;
                if (_toggleMode)
                {
                    // the pointer is followed in Update, so a button that appears under the pointer does not switch back at once
                    int next = (e + 1) % _entries.Count;
                    if (entry.Buttons.Length > 0 && entry.Buttons[0] != null)
                        entry.Buttons[0].onClick.AddListener(() => { _toggleArmed = false; Select(next); });
                    continue;
                }
                for (int i = 0; i < entry.Buttons.Length; i++)
                {
                    int index = i;
                    Button button = entry.Buttons[i];
                    if (button == null)
                        continue;
                    button.onClick.AddListener(() => Select(index));
                    EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
                    if (trigger == null)
                        trigger = button.gameObject.AddComponent<EventTrigger>();
                    EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                    enter.callback.AddListener(_ => _hoverIndex = index);
                    trigger.triggers.Add(enter);
                    EventTrigger.Entry exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                    exit.callback.AddListener(_ => { if (_hoverIndex == index) _hoverIndex = -1; });
                    trigger.triggers.Add(exit);
                }
            }
            for (int e = 0; e < _entries.Count; e++)
            {
                int index = e;
                UiGroupFold fold = _entries[e].Group != null ? _entries[e].Group.GetComponent<UiGroupFold>() : null;
                if (fold != null)
                    fold.PointerLeft += () => OnGroupLeft(index);
            }
            _wired = true;
        }

        private void OnEnable()
        {
            _hoverIndex = -1;
            _toggleArmed = false;
            Apply(-1);
        }

        // A tab under the pointer is picked by hovering, but not while the window slides: the new layer would take over the fold
        // state at once and cut the slide short. The tab is picked as soon as the window has settled. A folded window shows no tab
        // marks, so there is nothing to pick either.
        private void Update()
        {
            UiGroupFold current = _current >= 0 && _current < _entries.Count && _entries[_current].Group != null
                ? _entries[_current].Group.GetComponent<UiGroupFold>() : null;
            float target = current == null || current.IsOpen ? 1f : 0f;
            _markFade = Mathf.Lerp(_markFade, target, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
            ApplyIndicatorColors();

            if (_toggleMode)
            {
                // the icon of the new page arrives with a half turn (the icon looks the same turned by 180 degrees)
                float dt = Time.unscaledDeltaTime;
                _spinTime = Mathf.Min(1f, _spinTime + dt / _spinSeconds);
                float eased = Mathf.SmoothStep(0f, 1f, _spinTime);
                float spin = 180f * (1f - eased);
                _orbitValue = Mathf.Lerp(_orbitFrom, _orbitTarget, eased);
                // the dot is pulled behind the ring and reaches its socket a little later
                _dotValue = Mathf.Lerp(_dotValue, _orbitValue, 1f - Mathf.Exp(-_dotFollow * dt));
                float gap = _orbitTarget - _dotValue;
                if (gap < 0.003f && _spinTime >= 1f)
                    _dotValue = _orbitTarget;
                if (gap > 0.003f || _spinTime < 1f)
                    _orbitHold = _orbitHoldSeconds;
                else
                    _orbitHold -= dt;
                _orbitShow = Mathf.Lerp(_orbitShow, _orbitHold > 0f ? 1f : 0f, 1f - Mathf.Exp(-12f * dt));
                Entry shown = _current >= 0 && _current < _entries.Count ? _entries[_current] : null;
                if (shown != null && shown.Indicators != null && shown.Indicators.Length > 0 && shown.Indicators[0] != null)
                    shown.Indicators[0].rectTransform.localEulerAngles = new Vector3(0f, 0f, spin);
                UpdateOrbit(shown, Mathf.Max(0f, _orbitTarget - _dotValue));
                UpdateToggle(current, target);
                return;
            }

            if (_hoverIndex < 0 || _hoverIndex == _current || target < 0.5f)
                return;
            if (current != null && current.IsSlidingWithin(_hoverSettleTime))
                return;
            Select(_hoverIndex);
        }

        private void OnGroupLeft(int index)
        {
            if (_firstPageWhenOff != null && !_firstPageWhenOff.isOn && index == _current && _current != 0)
                Select(0);
        }

        // The dot, its trail and the two sockets are only visible while the icon turns and shortly after. The trail grows with the
        // distance the dot still has to go, the socket the dot runs to lights up as the dot gets close.
        private void UpdateOrbit(Entry entry, float gap)
        {
            if (entry == null || entry.Indicators == null || entry.Indicators.Length < 4)
                return;
            float show = _orbitShow * _markFade;
            Color accent = _indicatorActiveColor;
            float trail = Mathf.Min(gap, 0.5f) * 0.5f;
            float arrive = Mathf.SmoothStep(0f, 1f, 1f - Mathf.Clamp01(gap / 0.6f));
            int targetSocket = (_orbitTarget & 1) == 0 ? 2 : 3;
            for (int k = 1; k < entry.Indicators.Length; k++)
            {
                Image mark = entry.Indicators[k];
                if (mark == null)
                    continue;
                Color color = accent;
                float value = _dotValue;
                if (k == 1)
                    color.a = show * (_toggleInside ? 1f : 0.9f);
                else if (k == 2 || k == 3)
                {
                    color = Color.Lerp(_labelInactiveColor, _labelActiveColor, 0.5f);
                    color.a = show * (0.4f + (k == targetSocket ? 0.6f * arrive : 0f));
                    value = k == 2 ? 0f : 1f;
                }
                else
                {
                    int step = k - 3;
                    value = Mathf.Max(_dotValue - trail * step / (entry.Indicators.Length - 4), _orbitFrom);
                    color.a = show * 0.5f / step * Mathf.Clamp01(trail * 10f);
                }
                float angle = (180f - 180f * value) * Mathf.Deg2Rad;
                mark.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _orbitRadius;
                mark.color = color;
            }
        }

        // The button of the shown group switches to the next page while the pointer rests on it. It has to be left once before it
        // switches again, otherwise the button that appears under the pointer would switch back at once.
        private void UpdateToggle(UiGroupFold current, float open)
        {
            Entry entry = _current < _entries.Count ? _entries[_current] : null;
            Button button = entry != null && entry.Buttons != null && entry.Buttons.Length > 0 ? entry.Buttons[0] : null;
            _toggleInside = button != null && UiPointer.IsInsideWindow
                && UiPointer.GetScreenRect((RectTransform)button.transform, Vector4.zero).Contains(UiPointer.Position);
            if (!_toggleInside)
            {
                _toggleArmed = true;
                return;
            }
            // a pointer that only opens the window by resting on the header must not flip its page
            if (open < 0.5f)
            {
                _toggleArmed = false;
                return;
            }
            if (!_toggleArmed || (current != null && current.IsSlidingWithin(_hoverSettleTime)))
                return;
            _toggleArmed = false;
            Select((_current + 1) % _entries.Count);
        }

        private void OnDisable()
        {
            foreach (Entry entry in _entries)
                if (entry.Group != null)
                    entry.Group.SetActive(false);
        }

        /// <summary>The fold of the group that is shown right now.</summary>
        public UiGroupFold CurrentFold => _current >= 0 && _current < _entries.Count && _entries[_current].Group != null ? _entries[_current].Group.GetComponent<UiGroupFold>() : null;

        public void Select(int index)
        {
            if (index == _current && _wired)
                return;
            int previous = _current;
            _current = index;
            if (_toggleMode && _wired)
            {
                _orbitFrom = _orbitValue;
                _orbitTarget++;
                _spinTime = 0f;
            }
            if (isActiveAndEnabled)
                Apply(previous);
        }

        private void Apply(int previous)
        {
            // the window keeps its fold state when another layer takes its place
            if (previous >= 0 && previous < _entries.Count && _current < _entries.Count)
            {
                UiGroupFold from = _entries[previous].Group != null ? _entries[previous].Group.GetComponent<UiGroupFold>() : null;
                UiGroupFold to = _entries[_current].Group != null ? _entries[_current].Group.GetComponent<UiGroupFold>() : null;
                if (from != null && to != null && from != to)
                    to.TakeOver(from);
            }
            for (int i = 0; i < _entries.Count; i++)
            {
                bool active = i == _current;
                if (_entries[i].Group != null && _entries[i].Group.activeSelf != active)
                    _entries[i].Group.SetActive(active);
            }
            ApplyIndicatorColors();
        }

        // The small marks of the tabs show the selected one. They fade out while the window is folded.
        private void ApplyIndicatorColors()
        {
            foreach (Entry entry in _entries)
            {
                if (_toggleMode)
                {
                    // label and icon of the single button: dim, bright while the pointer is on it
                    Color tint = Color.Lerp(_labelInactiveColor, _labelActiveColor, _toggleInside ? 1f : 0f);
                    tint.a = (_toggleInside ? 1f : 0.7f) * _markFade;
                    if (entry.Labels != null && entry.Labels.Length > 0 && entry.Labels[0] != null)
                        entry.Labels[0].color = tint;
                    // only the ring is tinted here, the dot, its trail and the sockets are colored in UpdateOrbit
                    if (entry.Indicators != null && entry.Indicators.Length > 0 && entry.Indicators[0] != null)
                        entry.Indicators[0].color = tint;
                    continue;
                }
                if (entry.Labels != null)
                    for (int k = 0; k < entry.Labels.Length; k++)
                    {
                        if (entry.Labels[k] == null)
                            continue;
                        Color label = k == _current ? _labelActiveColor : _labelInactiveColor;
                        label.a *= _markFade;
                        entry.Labels[k].color = label;
                    }
                if (entry.Indicators == null)
                    continue;
                for (int k = 0; k < entry.Indicators.Length; k++)
                {
                    if (entry.Indicators[k] == null)
                        continue;
                    Color color = k == _current ? _indicatorActiveColor : _indicatorInactiveColor;
                    color.a *= _markFade;
                    entry.Indicators[k].color = color;
                }
            }
        }
    }
}
