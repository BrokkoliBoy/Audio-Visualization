using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gavi.Modules.MathModules.FloatModules;


namespace Gavi.Modifiables
{
    public class CameraModifiable : MonoBehaviour
    {
        /// <summary>Switched by the camera menu. While off, the cameras keep the field of view they started with instead of zooming to the beat.</summary>
        public static bool BeatZoomEnabled { get; set; } = true;

        [SerializeField] private List<Camera> _cameras;

        [SerializeField] private FloatModule _fieldOfViewModule;

        private readonly Dictionary<Camera, float> _baseFieldOfView = new Dictionary<Camera, float>();

        private void Awake()
        {
            foreach (Camera camera in _cameras)
                if (camera != null)
                    _baseFieldOfView[camera] = camera.fieldOfView;
        }

        private void Update()
        {
            foreach(Camera camera in _cameras)
            {
                if (camera == null)
                    continue;
                if (BeatZoomEnabled)
                {
                    if (_fieldOfViewModule != null)
                        camera.fieldOfView = _fieldOfViewModule.GetValue();
                }
                else if (_baseFieldOfView.TryGetValue(camera, out float fieldOfView))
                {
                    camera.fieldOfView = fieldOfView;
                }
            }
        }
    }
}
