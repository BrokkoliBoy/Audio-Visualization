using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace Gavi
{
    public class SliderG : MonoBehaviour, IScrollHandler
    {
        [SerializeField] private float _minValue = 0;
        [SerializeField] private float _maxValue = 1;
        
        [SerializeField] private bool _isInteger;
        [SerializeField] private int _textDecimals = 1;
        [Tooltip("Optional: below this value the text shows the decimals below instead. -1 = off.")]
        [SerializeField] private float _moreDecimalsBelow = -1f;
        [SerializeField] private int _textDecimalsBelow = 2;
        
        [Header("--- Snap (optional) ---")]
        [Tooltip("Makes a value easy to hit: a small plateau at exactly this value, surrounded by a slow zone for fine values.")]
        [SerializeField] private bool _useSnap;
        [SerializeField] private float _snapValue = 1f;
        [Tooltip("Half width of the plateau at the snap value, as fraction of the slider length.")]
        [SerializeField] private float _snapPlateau = 0.012f;
        [Tooltip("Width of the slow zone on each side of the plateau, as fraction of the slider length.")]
        [SerializeField] private float _snapSlowWidth = 0.07f;
        [Tooltip("The value changes by this much across the slow zone.")]
        [SerializeField] private float _snapSlowDelta = 0.1f;

        [Header("--- Curve (optional, not used together with snap) ---")]
        [Tooltip("Gives small values more room: the curve value sits in the middle of the slider.")]
        [SerializeField] private bool _useCurve;
        [SerializeField] private float _curveMidValue = 0.1f;

        [Header("--- Display range (optional) ---")]
        [Tooltip("The text shows a simple number from the display minimum (slider far left) to the display maximum (far right) instead of the value. The value itself is unchanged.")]
        [SerializeField] private bool _useDisplayRange;
        [SerializeField] private float _displayMin = 1f;
        [SerializeField] private float _displayMax = 10f;

        [Header("--- Mouse wheel ---")]
        [Tooltip("Step of one wheel notch, in the unit of the text: the display range if it is used, otherwise the value. 0 = automatic (1 for whole numbers, otherwise about 1/50 of the range).")]
        [SerializeField] private float _wheelStep;

        [Header("--- References ---")]
        [SerializeField] private Slider _slider;
        [SerializeField] private TextMeshProUGUI _textValue;

        private float Range => Mathf.Max(0.00001f,  RangeMax - RangeMin);
        private float RangeMin => Mathf.Min(_minValue, _maxValue);
        private float RangeMax => Mathf.Max(_minValue, _maxValue);

        public UnityEvent<float> OnValueChangedFloat;
        public UnityEvent<int> OnValueChangedInt;
        private int _lastInt;
        
        public float Value
        {
            get => _isInteger ? Mathf.RoundToInt(PositionToValue(_slider.value)) : PositionToValue(_slider.value);
            set
            {
                if (_isInteger)
                    value = Mathf.RoundToInt(value + 0.000001f);
                _slider.value = ValueToPosition(value);
            }
        }

        public int ValueInt
        {
            get => Mathf.RoundToInt(PositionToValue(_slider.value) + 0.000001f);
            set
            {
                _slider.value = ValueToPosition(value);
            }
        }
        
        
        
        /// <summary>The value the slider gives at a position (0-1), for previews along the track.</summary>
        /// <summary>The slider position (0-1).</summary>
        public float Position => _slider.value;

        public float ValueAtPosition(float position) => PositionToValue(Mathf.Clamp01(position));

        /// <summary>Sends the change event with the current value, for when the value was moved without one.</summary>
        public void NotifyValue() => OnValueChangedFloat.Invoke(Value);

        private void Awake()
        {
            _slider.onValueChanged.AddListener(OnValueChangedM);
            if (_isInteger)
                _slider.SetValueWithoutNotify(SnapToWhole(_slider.value));
            _lastInt = Mathf.RoundToInt(PositionToValue(_slider.value));
            // The text saved in the scene is stale as long as the value never changes.
            ShowText(_slider.value);
        }

        /// <summary>For ranges that are only known at run time. Set the value afterwards, the slider position is not kept.</summary>
        public void SetRange(float min, float max)
        {
            _minValue = min;
            _maxValue = max;
            ShowText(_slider.value);
        }

        private void ShowText(float sliderValue)
        {
            float value = PositionToValue(sliderValue);
            if (_isInteger)
                value = Mathf.RoundToInt(value);
            SetText(_useDisplayRange ? Mathf.Lerp(_displayMin, _displayMax, Mathf.Clamp01(sliderValue)) : value);
        }

        // An integer slider only rests on whole values: the handle snaps to the nearest one.
        private float SnapToWhole(float sliderValue)
        {
            float snapped = ValueToPosition(Mathf.RoundToInt(PositionToValue(sliderValue)));
            if (Mathf.Approximately(snapped, sliderValue))
                return sliderValue;
            _slider.SetValueWithoutNotify(snapped);
            return snapped;
        }

        private void OnValueChangedM(float sliderValue)
        {
            if (_isInteger)
                sliderValue = SnapToWhole(sliderValue);
            ShowText(sliderValue);
            float value = PositionToValue(sliderValue);
            if (_isInteger)
                value = Mathf.RoundToInt(value);
            bool changed = Mathf.RoundToInt(value) != _lastInt;
            // no change event while the handle moves between the same two whole values
            if (_isInteger && !changed)
                return;
            OnValueChangedFloat.Invoke(value);
            if (changed)
            {
                OnValueChangedInt.Invoke(Mathf.RoundToInt(value));
                _lastInt = Mathf.RoundToInt(value);
            }
        }

        #region Mouse wheel
        /// <summary>Holding Shift makes one wheel notch this many times larger.</summary>
        private const float ShiftWheelFactor = 5f;

        private float _wheelSum;
        private float _lastWheelTime;

        /// <summary>Wheel forward = higher value, wheel back = lower value, in steps of the wheel step.</summary>
        public void OnScroll(PointerEventData eventData)
        {
            if (!_slider.interactable)
                return;
            float wheel = eventData.scrollDelta.y;
            if (Mathf.Abs(wheel) < 0.001f)
                return;

            // Some mice send one notch as several small events; they add up to one step.
            if (Time.unscaledTime - _lastWheelTime > 0.4f || Mathf.Sign(wheel) != Mathf.Sign(_wheelSum))
                _wheelSum = 0f;
            _lastWheelTime = Time.unscaledTime;
            _wheelSum += wheel;
            eventData.Use();
            if (Mathf.Abs(_wheelSum) < 0.999f)
                return;
            _wheelSum = 0f;

            int direction = wheel > 0f ? 1 : -1;
            // Shift = large steps
            float boost = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? ShiftWheelFactor : 1f;
            // a short whole-number range has no use for larger steps: Shift jumps to the end of the range
            if (_isInteger && Range < 20f)
            {
                if (boost > 1f)
                {
                    Value = direction > 0 ? RangeMax : RangeMin;
                    eventData.Use();
                    return;
                }
                boost = 1f;
            }
            if (_useDisplayRange)
            {
                float span = Mathf.Abs(_displayMax - _displayMin);
                if (span < 0.00001f)
                    return;
                float shown = Mathf.Lerp(_displayMin, _displayMax, _slider.value);
                float next = SteppedValue(shown, WheelStep(span, false, boost > 1f), direction);
                float nextPosition = Mathf.Clamp01(Mathf.InverseLerp(_displayMin, _displayMax, next));
                // a whole-number slider rests on whole values, so a step that lands on the same one goes one further
                if (_isInteger && Mathf.RoundToInt(PositionToValue(nextPosition)) == Mathf.RoundToInt(PositionToValue(_slider.value)))
                    Value = Mathf.RoundToInt(PositionToValue(_slider.value)) + direction;
                else
                    _slider.value = nextPosition;
            }
            else
            {
                Value = SteppedValue(PositionToValue(_slider.value), WheelStep(Range, _isInteger, boost > 1f), direction);
            }
            eventData.Use();
        }

        private float WheelStep(float range, bool whole, bool shift)
        {
            float step = _wheelStep > 0f ? _wheelStep : whole ? 1f : RoundedFiftieth(range);
            if (shift)
                return step * ShiftWheelFactor;
            // a range of 2 would give 0.04, 0.05 is the nicer step there (Shift keeps the 0.2 steps)
            if (_wheelStep <= 0f && !whole && Mathf.Approximately(step / Mathf.Pow(10f, Mathf.Floor(Mathf.Log10(step))), 4f))
                return step * 1.25f;
            return step;
        }

        // about 1/50 of the range, rounded to one digit
        private static float RoundedFiftieth(float range)
        {
            float raw = range / 50f;
            float magnitude = Mathf.Pow(10f, Mathf.Floor(Mathf.Log10(raw)));
            return Mathf.Round(raw / magnitude) * magnitude;
        }

        /// <summary>The next multiple of the step above or below the value (a value between two multiples goes to the closer one in that direction).</summary>
        private static float SteppedValue(float value, float step, int direction)
        {
            float units = value / step;
            float snapped = direction > 0 ? Mathf.Floor(units + 0.001f) + 1f : Mathf.Ceil(units - 0.001f) - 1f;
            return snapped * step;
        }
        #endregion

        #region Position map
        // Optional: the handle runs from a lower to an upper position of the normal mapping, with a chosen position in the middle.
        // The text then shows the display range along the handle, so the middle reads as the middle of the display range.
        private bool _useMap;
        private float _mapMin;
        private float _mapMiddle;
        private float _mapMax;

        /// <summary>
        /// Limits the slider to the positions min..max of its normal mapping (0-1) and puts <paramref name="middle"/> at the middle of the handle.
        /// The value is kept (and limited to the new range) unless <paramref name="toMiddle"/> is set. No change event.
        /// </summary>
        public void SetPositionMap(float min, float middle, float max, bool toMiddle)
        {
            float value = Value;
            _useMap = true;
            _mapMin = Mathf.Clamp01(min);
            _mapMax = Mathf.Clamp01(max);
            _mapMiddle = Mathf.Clamp(middle, _mapMin, _mapMax);
            _slider.SetValueWithoutNotify(toMiddle ? 0.5f : ValueToPosition(value));
            ShowText(_slider.value);
        }

        private float HandleToBase(float handle)
        {
            if (!_useMap)
                return handle;
            handle = Mathf.Clamp01(handle);
            return handle <= 0.5f ? Mathf.Lerp(_mapMin, _mapMiddle, handle * 2f) : Mathf.Lerp(_mapMiddle, _mapMax, (handle - 0.5f) * 2f);
        }

        private float BaseToHandle(float basePosition)
        {
            if (!_useMap)
                return basePosition;
            if (basePosition <= _mapMiddle)
                return _mapMiddle > _mapMin ? Mathf.Clamp01(Mathf.InverseLerp(_mapMin, _mapMiddle, basePosition) * 0.5f) : 0.5f;
            return _mapMax > _mapMiddle ? Mathf.Clamp01(0.5f + Mathf.InverseLerp(_mapMiddle, _mapMax, basePosition) * 0.5f) : 0.5f;
        }
        #endregion

        #region Mapping
        /// <summary>Slider position (0-1) to value. Linear, or with a snap plateau and slow zone around the snap value.</summary>
        private float PositionToValue(float handle) => BasePositionToValue(HandleToBase(handle));

        private float BasePositionToValue(float position)
        {
            if (!UseSnap(out float p1, out float a, out float b, out float d))
                return UseCurve(out float exponent) ? RangeMin + Range * Mathf.Pow(Mathf.Clamp01(position), exponent) : RangeMin + position * Range;

            float dp = position - p1;
            float sign = Mathf.Sign(dp);
            float distance = Mathf.Abs(dp);
            if (distance <= a)
                return _snapValue;
            if (distance <= b)
                return _snapValue + sign * d * (distance - a) / (b - a);

            float outerLength = (dp > 0 ? 1f - p1 : p1) - b;
            float outerValue = dp > 0 ? RangeMax - _snapValue - d : _snapValue - d - RangeMin;
            return _snapValue + sign * (d + outerValue * (distance - b) / Mathf.Max(0.00001f, outerLength));
        }

        private float ValueToPosition(float value) => BaseToHandle(BaseValueToPosition(value));

        private float BaseValueToPosition(float value)
        {
            if (!UseSnap(out float p1, out float a, out float b, out float d))
            {
                float linear = Utility.Utility.MapValuePercent(RangeMin, RangeMax, value, true);
                return UseCurve(out float exponent) ? Mathf.Pow(linear, 1f / exponent) : linear;
            }

            float dv = value - _snapValue;
            if (Mathf.Approximately(dv, 0f))
                return p1;

            float sign = Mathf.Sign(dv);
            float distance = Mathf.Abs(dv);
            float position;
            if (distance <= d)
            {
                position = p1 + sign * (a + distance / d * (b - a));
            }
            else
            {
                float outerLength = (sign > 0 ? 1f - p1 : p1) - b;
                float outerValue = sign > 0 ? RangeMax - _snapValue - d : _snapValue - d - RangeMin;
                position = p1 + sign * (b + (distance - d) / Mathf.Max(0.00001f, outerValue) * outerLength);
            }
            return Mathf.Clamp01(position);
        }

        private bool UseSnap(out float p1, out float plateau, out float slowEnd, out float delta)
        {
            p1 = (_snapValue - RangeMin) / Range;
            plateau = _snapPlateau;
            slowEnd = _snapPlateau + _snapSlowWidth;
            delta = _snapSlowDelta;
            // The snap needs room on both sides, otherwise the slider stays linear.
            return _useSnap && p1 > slowEnd && p1 < 1f - slowEnd && delta > 0f
                && delta < _snapValue - RangeMin && delta < RangeMax - _snapValue;
        }

        /// <summary>Power curve value = min + range * position^exponent, with the exponent chosen so that the curve value sits at position 0.5.</summary>
        private bool UseCurve(out float exponent)
        {
            float middle = (_curveMidValue - RangeMin) / Range;
            exponent = middle > 0f && middle < 1f ? Mathf.Log(middle) / Mathf.Log(0.5f) : 1f;
            return _useCurve && !_useSnap && middle > 0f && middle < 1f;
        }
        #endregion

        private void SetText(float value)
        {
            int decimals = _moreDecimalsBelow > 0f && value < _moreDecimalsBelow ? _textDecimalsBelow : _textDecimals;
            float factor = Utility.Utility.ExpInt(10, decimals);
            // The small offset keeps values like 2.9999999 (really 3) from being cut down to 2.9.
            float textValue = (int)(value * factor + 0.0001f * Mathf.Sign(value)) / factor;
            _textValue.text = textValue + "";
        }
    }
}