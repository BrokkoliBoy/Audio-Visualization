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
            get => _isInteger ? Mathf.RoundToInt(RangeMin + _slider.value * Range) : RangeMin + _slider.value * Range;
            set
            {
                if (_isInteger)
                    value = Mathf.RoundToInt(value + 0.000001f);
                _slider.value = Utility.Utility.MapValuePercent(RangeMin, RangeMax, value, true);
            }
        }

        public int ValueInt
        {
            get => Mathf.RoundToInt(RangeMin + _slider.value * Range + 0.000001f);
            set
            {
                _slider.value = Utility.Utility.MapValuePercent(RangeMin, RangeMax, value, true);
            }
        }
        
        
        
        private void Awake()
        {
            _slider.onValueChanged.AddListener(OnValueChangedM);
        }

        private void OnValueChangedM(float sliderValue)
        {
            float value = RangeMin + Range * sliderValue;
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

        private void SetText(float value)
        {
            float textValue = (int)(value * Utility.Utility.ExpInt(10, _textDecimals)) / (float)Utility.Utility.ExpInt(10, _textDecimals);
            _textValue.text = textValue +"";
        }
    }
}