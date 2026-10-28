using System.Reflection;
using UnityEngine;

namespace Gavi.VideoDirector
{
    /// <summary>
    /// Moves the camera through the target values of <see cref="CameraControl"/> (the ones the mouse sets), so the camera glides the same way as
    /// under the mouse. The fields are private, so they are reached through reflection: that keeps <c>CameraControl</c> untouched on this branch.
    /// If the fields get renamed on <c>next</c> the Director logs a warning and the camera events do nothing.
    /// </summary>
    public class DirectorCamera
    {
        private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

        private CameraControl _control;
        private FieldInfo _yaw, _pitch, _scale, _move, _resetting;

        public bool IsAvailable => _control != null && _yaw != null && _pitch != null && _scale != null && _move != null;

        /// <summary>The camera pose as the mouse would set it: rotation around the view (degrees), tilt (degrees), distance as a multiple of the start distance.</summary>
        public struct Pose
        {
            public float Yaw, Pitch, Scale;
        }

        public void Find()
        {
            _control = Object.FindFirstObjectByType<CameraControl>();
            if (_control == null)
            {
                Debug.LogWarning("Video Director: no CameraControl in the scene, camera events are skipped.");
                return;
            }

            System.Type type = typeof(CameraControl);
            _yaw = type.GetField("_yaw", Flags);
            _pitch = type.GetField("_pitch", Flags);
            _scale = type.GetField("_scale", Flags);
            _move = type.GetField("_move", Flags);
            _resetting = type.GetField("_resetting", Flags);
            if (!IsAvailable)
                Debug.LogWarning("Video Director: CameraControl changed (fields _yaw/_pitch/_scale/_move not found), camera events are skipped.");
        }

        public Pose Read()
        {
            if (!IsAvailable)
                return new Pose { Scale = 1f };
            return new Pose { Yaw = (float)_yaw.GetValue(_control), Pitch = (float)_pitch.GetValue(_control), Scale = (float)_scale.GetValue(_control) };
        }

        public void Write(Pose pose)
        {
            if (!IsAvailable)
                return;
            _yaw.SetValue(_control, pose.Yaw);
            _pitch.SetValue(_control, pose.Pitch);
            _scale.SetValue(_control, pose.Scale);
            _resetting?.SetValue(_control, false);
        }

        /// <summary>Back to the start pose at once (and the terrain rotation with it).</summary>
        public void ResetNow()
        {
            if (_control == null)
                return;
            _control.ResetCamera();
            if (TerrainRotator.Instance != null)
                TerrainRotator.Instance.ResetRotation();
        }
    }
}
