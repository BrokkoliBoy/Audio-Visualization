    using System;
using System.Collections;
using System.Collections.Generic;
    using Gavi.Audio;
    using Gavi.Modules.DataModules.ArrayModules.FloatArrayModules.VoidToFloatArrayModules;
    using Gavi.Utility;
using UnityEngine;

namespace Gavi
{
    public class ViewManager : MonoBehaviour
    {
        public static ViewManager Instance;
        
        [SerializeField] private List<View> _views;
        [SerializeField] private int _startIndex;
        private int _currentIndex = -1;

        [Header("Only two KeyCodes allowed.")]
        [SerializeField] private List<KeyCode> _keyCodesNextView = new List<KeyCode> {KeyCode.V};
        [Header("Only two KeyCodes allowed.")]
        [SerializeField] private List<KeyCode> _keyCodePreviousView = new List<KeyCode> {KeyCode.V, KeyCode.LeftShift};

        [Space]
        [SerializeField] private KeyCode _keyCodeRestartView = KeyCode.R;
        
        public SpectrumDataContainerModule CurrentMainSamplesContainer => _currentIndex >= _views.Count ? null : _views[_currentIndex]?.MainSamplesContainer;

        public View CurrentView => _views[_currentIndex];
        
        private void Awake()
        {
            if (Instance != null)
                Debugger.LogInstanceError(GetType());
            Instance = this;
        }
        
        private void Start()
        {
            SetViewActive(_startIndex);
        }

        private void Update()
        {
            if (_keyCodesNextView != null && _keyCodesNextView.Count == 2 &&
                (Input.GetKeyDown(_keyCodesNextView[0]) && Input.GetKey(_keyCodesNextView[1]) || 
                 Input.GetKey(_keyCodesNextView[0]) && Input.GetKeyDown(_keyCodesNextView[1])))
                NextView();
            else if (_keyCodePreviousView != null && _keyCodePreviousView.Count == 2 &&
                     (Input.GetKeyDown(_keyCodePreviousView[0]) && Input.GetKey(_keyCodePreviousView[1]) ||
                      Input.GetKey(_keyCodePreviousView[0]) && Input.GetKeyDown(_keyCodePreviousView[1])))
                PreviousView();
            else if (_keyCodesNextView != null && _keyCodesNextView.Count == 1 && Input.GetKeyDown(_keyCodesNextView[0]))
                NextView();
            else if (_keyCodePreviousView != null && _keyCodePreviousView.Count == 1 && Input.GetKeyDown(_keyCodePreviousView[0]))
                PreviousView();

            if (Input.GetKeyDown(_keyCodeRestartView))
                RestartView();
        }

        public void NextView()
        {
            int index = _currentIndex + 1;
            if (index >= _views.Count)
                index = 0;
            SetViewActive(index);
        }

        public void PreviousView()
        {
            int index = _currentIndex - 1;
            if (index < 0)
                index = _views.Count - 1;
            SetViewActive(index);
        }

        public void RestartView()
        {
            if (_currentIndex >= _views.Count || _currentIndex < 0)
                return;
            _views[_currentIndex].RestartView();            
        }

        public void SetViewActive(int index)
        {
            if (index == _currentIndex)
                return;
            
            if (_currentIndex >= 0 && _currentIndex < _views.Count)
                _views[_currentIndex].StopView();
            
            if (index < 0 || index >= _views.Count)
            {
                Debugger.LogAssertionFail("index < 0 || index >= _views.Count: " + index + ", " + _views.Count);
                return;
            }

            _currentIndex = index;
            _views[_currentIndex].StartView();
        }
    }
}