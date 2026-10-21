using UnityEngine;

namespace Gavi
{
    /// <summary>
    /// Lets the viewer turn the terrain: pressing the mouse button on the terrain and dragging rotates it around its
    /// middle, like a trackball (the spot under the cursor follows it). <see cref="CameraControl"/> decides whether a click
    /// goes to the terrain or to the camera and calls <see cref="TryBeginTurn"/>. The rotation glides, and it goes back
    /// slowly with <see cref="ResetRotation"/> (reset key of the camera).
    /// </summary>
    public class TerrainRotator : MonoBehaviour
    {
        public static TerrainRotator Instance { get; private set; }
        /// <summary>True while the mouse turns the terrain.</summary>
        public static bool IsTurning => Instance != null && Instance._turning;

        [Tooltip("The terrain to turn. Taken from this object if empty.")]
        [SerializeField] private TerrainGenerator _generator;
        [Tooltip("Degrees per pixel of mouse movement.")]
        [SerializeField] private float _degreesPerPixel = 0.3f;
        [Tooltip("Higher = the terrain follows the mouse faster.")]
        [SerializeField] private float _smoothing = 10f;
        [Tooltip("Smoothing while the terrain glides back after a reset. Lower = slower.")]
        [SerializeField] private float _resetSmoothing = 3f;

        private bool _turning;
        private bool _resetting;
        private int _button;
        private Vector2 _lastMouse;

        // Rotation (in world space) the viewer asked for and the smoothed one that is applied.
        private Quaternion _target = Quaternion.identity;
        private Quaternion _current = Quaternion.identity;
        // True while the terrain is rotated away from its own pose, which is kept in the parent's space.
        private bool _applied;
        private Vector3 _baseLocalPosition;
        private Quaternion _baseLocalRotation;

        private void Awake()
        {
            Instance = this;
            if (_generator == null)
                _generator = GetComponent<TerrainGenerator>();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void OnDisable()
        {
            _turning = false;
            Restore();
            _target = Quaternion.identity;
            _current = Quaternion.identity;
        }

        /// <summary>Starts turning when the ray hits the terrain. False otherwise, then the click belongs to the camera.</summary>
        public bool TryBeginTurn(Ray ray, int button)
        {
            Transform terrain = _generator != null ? _generator.TerrainTransform : null;
            if (!isActiveAndEnabled || terrain == null || !terrain.gameObject.activeInHierarchy)
                return false;
            if (!_generator.TryHitTerrain(ray))
                return false;

            if (!_applied)
            {
                _baseLocalPosition = terrain.localPosition;
                _baseLocalRotation = terrain.localRotation;
                _applied = true;
            }
            _turning = true;
            _resetting = false;
            _button = button;
            _lastMouse = Input.mousePosition;
            return true;
        }

        /// <summary>Back to the original pose with a slow glide.</summary>
        public void ResetRotation()
        {
            _target = Quaternion.identity;
            _resetting = true;
        }

        private void Update()
        {
            if (!_turning)
                return;
            if (!Input.GetMouseButton(_button))
            {
                _turning = false;
                return;
            }

            Vector2 mouse = Input.mousePosition;
            Vector2 delta = mouse - _lastMouse;
            _lastMouse = mouse;

            Turn(delta, Camera.main);
        }

        /// <summary>Turns the terrain as if the mouse had moved by this many pixels.</summary>
        public void Turn(Vector2 delta, Camera camera)
        {
            if (camera == null || delta.sqrMagnitude < 0.0001f)
                return;
            Transform view = camera.transform;

            // The spot under the cursor follows it: drag right = the near side goes right, drag up = the near side goes up.
            Vector3 near = -view.forward - view.up;
            float yawSign = Vector3.Dot(Quaternion.AngleAxis(1f, Vector3.up) * near - near, view.right) >= 0f ? 1f : -1f;
            Vector3 front = -view.forward;
            float pitchSign = Vector3.Dot(Quaternion.AngleAxis(1f, view.right) * front - front, view.up) >= 0f ? 1f : -1f;

            Quaternion turn = Quaternion.AngleAxis(delta.x * _degreesPerPixel * yawSign, Vector3.up)
                * Quaternion.AngleAxis(delta.y * _degreesPerPixel * pitchSign, view.right);
            _target = turn * _target;
            _resetting = false;
        }

        private void LateUpdate()
        {
            if (!_applied)
                return;

            Transform terrain = _generator != null ? _generator.TerrainTransform : null;
            if (terrain == null)
                return;

            float t = 1f - Mathf.Exp(-(_resetting ? _resetSmoothing : _smoothing) * Time.unscaledDeltaTime);
            _current = Quaternion.Slerp(_current, _target, t);

            if (!_turning && Quaternion.Angle(_target, Quaternion.identity) < 0.01f && Quaternion.Angle(_current, Quaternion.identity) < 0.05f)
            {
                _current = Quaternion.identity;
                Restore();
                return;
            }

            Transform parent = terrain.parent;
            Vector3 basePosition = parent != null ? parent.TransformPoint(_baseLocalPosition) : _baseLocalPosition;
            Quaternion baseRotation = parent != null ? parent.rotation * _baseLocalRotation : _baseLocalRotation;

            // turn around the middle of the terrain box
            Vector3 center = basePosition + baseRotation * Vector3.Scale(_generator.LocalCenter, terrain.lossyScale);
            terrain.SetPositionAndRotation(center + _current * (basePosition - center), _current * baseRotation);
        }

        /// <summary>Puts the terrain back to its own pose.</summary>
        private void Restore()
        {
            if (!_applied)
                return;
            _applied = false;
            Transform terrain = _generator != null ? _generator.TerrainTransform : null;
            if (terrain != null)
                terrain.SetLocalPositionAndRotation(_baseLocalPosition, _baseLocalRotation);
        }
    }
}
