using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gavi.Modules.MathModules.FloatArrayModules.FloatArrayToFloatArrayModules
{
    public class FloatArrayToFloatArrayMedianModule : FloatArrayToFloatArrayModule
    {
        // this module changes the values of its array by averaging it over a variable amount of arrays values of 
        // the past x frames.
        
        [Header("--- FA 2 FA Median Module ---")]
        [SerializeField][Min(1)] private int _numberConsiderFrames = 5;
        [SerializeField] private bool _resetWhenClipChanged = true;

        private Queue<float[]> _queue = new Queue<float[]>();

        protected override void OnEnable()
        {
            base.OnEnable();
            Events.Events.OnClipChanged.AddListener(OnClipChanged);
        }
        protected override void OnDisable()
        {
            base.OnDisable();
            Events.Events.OnClipChanged.RemoveListener(OnClipChanged);
        }

        private void OnClipChanged()
        {
            if (_value == null)
                return;
            if (!_resetWhenClipChanged)
                return;
            for (int i = 0; i < _value.Length; i++)
                _value[i] = 0;
            _queue = new Queue<float[]>();
        }

        protected override void ReInitialize()
        {
            base.ReInitialize();
            // no need to reset _value since this is done in base.ReInitialize().
            _queue = new Queue<float[]>();
        }

        protected override void Recalculate()
        {
            base.Recalculate();

            float[] input = _inputValue.GetValue();
            
            // check if sample size has changed. If so, reset median
            if (_queue.Count > 0 && _queue.Peek().Length != input.Length)
            {
                for (int i = 0; i < _value.Length; i++)
                    _value[i] = 0;
                _queue.Clear();
            }
            
            if (_queue.Count < _numberConsiderFrames)
            {
                for (int i = 0; i < input.Length; i++)
                    _value[i] = (_value[i] / (_queue.Count == 0 ? 1 : _queue.Count) + input[i]) / (_queue.Count + 1);
                float[] tmp = new float[input.Length];
                input.CopyTo(tmp, 0);
                _queue.Enqueue(tmp);
            }
            else if (_queue.Count == _numberConsiderFrames)
            {
                float[] oldInput = _queue.Dequeue();
                // 1.
                // {40, 50, 90} => {50, 90, 70}
                //      60            70
                // 60 + (70-40) / 3 = 70

                // 2.
                // {90, 50, 40} => {50, 40, 30}
                //      60            40
                // 60 + (30-90) / 3 = 40
                for (int i = 0; i < input.Length; i++)
                    _value[i] += (input[i] - oldInput[i]) / (_queue.Count + 1);
                input.CopyTo(oldInput, 0);
                _queue.Enqueue(oldInput);
            }
            else
            {
                while (_queue.Count > _numberConsiderFrames)
                {
                    float[] oldInput = _queue.Dequeue();

                    if(_queue.Count == 0)
                    {
                        Utility.Debugger.LogError("What?");
                        for (int i = 0; i < input.Length; i++)
                            _value[i] = 0;
                        return;
                    }

                    // 1.
                    // {40, 50, 90} => {50, 90}
                    //      60            70
                    // 60 + (60-40) / 2 = 70

                    // 2.
                    // {90, 50, 40} => {50, 40}
                    //      60            45
                    // 60 + (60-90) / 2 = 45

                    // 3.
                    // {0, 50, 40} => {50, 40}
                    //     30           45
                    // 30 + (30 - 0) / 2 = 45

                    for (int i = 0; i < input.Length; i++)
                        _value[i] += (_value[i] - oldInput[i]) / _queue.Count;
                }
            }
        }


        protected override string GetName()
        {
            return "FA 2 FA Median Module (" + _numberConsiderFrames + ")";
        }
    }
}
