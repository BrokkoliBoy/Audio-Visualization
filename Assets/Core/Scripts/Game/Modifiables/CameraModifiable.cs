using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gavi.Modules.MathModules.FloatModules;


namespace Gavi.Modifiables
{
    public class CameraModifiable : MonoBehaviour
    {
        [SerializeField] private List<Camera> _cameras;

        [SerializeField] private FloatModule _fieldOfViewModule;

        private void Update()
        {
            foreach(Camera camera in _cameras)
            {
                if (_fieldOfViewModule != null)
                    camera.fieldOfView = _fieldOfViewModule.GetValue();
            }
        }
    }
}