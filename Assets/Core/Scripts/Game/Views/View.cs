using System.Collections;
using System.Collections.Generic;
using Gavi.Audio;
using Gavi.Modules.DataModules.ArrayModules.FloatArrayModules.VoidToFloatArrayModules;
using Gavi.Utility;
using UnityEngine;

namespace Gavi
{
    public class View : MonoBehaviour
    {
        public ViewTypeEnum ViewType => _viewType;
        [Tooltip("To identify which view is currently showing. It enables other scripts to perform special behaviour " +
                 "depending on whether a certain view is showing.")]
        [SerializeField] private ViewTypeEnum _viewType;
        [SerializeField] private GameObject _viewObject;
        [SerializeField] private SpectrumDataContainerModule _mainSamplesContainer;
        public SpectrumDataContainerModule MainSamplesContainer => _mainSamplesContainer;

        private bool _isActive;

        public enum ViewTypeEnum
        {
            Unity = 0,
            Terrain = 1,
            Dumbbell = 2
        }
        
        public void StartView()
        {
            if (_isActive)
            {
                Debugger.LogError("Tried to start view though it is already active on " + gameObject.name);
                return;
            }

            _isActive = true;
            _viewObject.SetActive(true); 
        }

        public void StopView()
        {
            _isActive = false;
            _viewObject.SetActive(false); 
        }

        public void RestartView()
        {
            if (!_isActive)
            {
                Debugger.LogError("Tried to restart view though it is not active on " + gameObject.name);
                return;
            }
            
            AudioPlayer.Instance.Replay();
        }
    }
}
