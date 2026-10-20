using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace Gavi
{
    public class SliderG : MonoBehaviour
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
        
        
        
        private void Awake()
        {
            _slider.onValueChanged.AddListener(OnValueChangedM);
        }

        private void OnValueChangedM(float sliderValue)
        {
            float value = PositionToValue(sliderValue);
            if (_isInteger)
                value = Mathf.RoundToInt(value);
            SetText(value);
            OnValueChangedFloat.Invoke(value);
            if (Mathf.RoundToInt(value) != _lastInt)
            {
                OnValueChangedInt.Invoke(Mathf.RoundToInt(value));
                _lastInt = Mathf.RoundToInt(value);
            }
        }

        #region Mapping
        /// <summary>Slider position (0-1) to value. Linear, or with a snap plateau and slow zone around the snap value.</summary>
        private float PositionToValue(float position)
        {
            if (!UseSnap(out float p1, out float a, out float b, out float d))
                return RangeMin + position * Range;

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

        private float ValueToPosition(float value)
        {
            if (!UseSnap(out float p1, out float a, out float b, out float d))
                return Utility.Utility.MapValuePercent(RangeMin, RangeMax, value, true);

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