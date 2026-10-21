using UnityEngine;
using UnityEngine.EventSystems;
using Gavi.Audio;
using Gavi.Modifiables;

namespace Gavi
{
    /// <summary>
    /// Lets the viewer move the active camera with the mouse and keyboard. The camera orbits around the CameraPivot
    /// of the current view, so every view is controlled the same way:
    /// left drag on the background (or middle drag anywhere) = orbit around the view, left drag on the terrain = turn the terrain around its middle (see TerrainRotator),
    /// right drag = move the view sideways and up/down (the left and right mouse button belong to the test signal band editor while that is on),
    /// mouse wheel = zoom towards or away from the view, W/S = move forward and back and A/D = move sideways on the
    /// horizontal plane, Q/E = move down and up,
    /// C = back to the start pose.
    /// One instance in the scene controls whichever view camera is currently active (Camera.main). The field of view is
    /// left alone because the beat-driven zoom already uses it. The pose a camera has when it becomes active is its start pose.
    /// </summary>
    public class CameraControl : MonoBehaviour
    {
        [Header("--- Orbit (left mouse, middle mouse) ---")]
        [SerializeField] private bool _allowOrbit = true;
        [Tooltip("Mouse button that orbits when it is pressed on the background (on the terrain it turns the terrain instead): 0 = left, 1 = right, 2 = middle.")]
        [SerializeField] private int _orbitButton = 0;
        [Tooltip("The middle mouse button also orbits, everywhere on the screen and also while the test signal band editor is on.")]
        [SerializeField] private bool _orbitWithMiddle = true;
        [Tooltip("Degrees per pixel of mouse movement.")]
        [SerializeField] private float _orbitSpeed = 0.25f;
        [Tooltip("How far the camera may tilt up or down from its start pose.")]
        [SerializeField] private float _maxPitch = 80f;
        [Tooltip("The camera does not go lower than this angle above the pivot (or its start angle if that is lower). Negative values let it look at the view from below.")]
        [SerializeField] private float _minElevation = -85f;
        [SerializeField] private float _maxElevation = 85f;

        [Header("--- Move (right mouse, WASD, Q/E) ---")]
        [SerializeField] private bool _allowMove = true;
        [Tooltip("1 = the scene follows the cursor exactly at the distance of the pivot.")]
        [SerializeField] private float _panSpeed = 1f;
        [Tooltip("Fraction of the pivot distance per second for W/A/S/D and Q/E.")]
        [SerializeField] private float _keyMoveSpeed = 0.4f;
        [Tooltip("How far the camera may move sideways, as a multiple of the start distance to the pivot.")]
        [SerializeField] private float _maxMoveDistance = 1.5f;

        [Header("--- Zoom (mouse wheel) ---")]
        [SerializeField] private bool _allowZoom = true;
        [Tooltip("Share of the current distance per mouse wheel notch.")]
        [SerializeField] private float _zoomStep = 0.08f;
        [Tooltip("Smallest and largest distance to the pivot as a multiple of the start distance.")]
        [SerializeField] private float _minDistanceScale = 0.1f;
        [SerializeField] private float _maxDistanceScale = 2.5f;

        [Header("--- Feel ---")]
        [Tooltip("Higher = the camera follows the input faster.")]
        [SerializeField] private float _smoothing = 12f;
        [Tooltip("Smoothing while the camera glides back to its start pose after a reset. Lower = slower.")]
        [SerializeField] private float _resetSmoothing = 3f;
        [SerializeField] private bool _useKeys = true;
        [Tooltip("Reset key.")]
        [SerializeField] private KeyCode _resetKey = KeyCode.C;
        [Tooltip("Keeps the slow sway of the camera rotation (RotationModifiable) while the camera is controlled.")]
        [SerializeField] private bool _keepSway = true;

        private Camera _camera;
        private Vector3 _startPosition;
        private Quaternion _startRotation;
        private Vector3 _pivot;
        private Vector3 _startOffset;
        private float _startElevation;

        // Input state (target) and the smoothed state that is applied to the camera.
        private float _yaw, _pitch, _scale = 1f;
        private Vector3 _move;
        private float _curYaw, _curPitch, _curScale = 1f;
        private Vector3 _curMove;

        private bool _orbiting, _moving;
        // The mouse button that started the orbit.
        private int _orbitHeld;
        // True while the camera glides back after a reset. Any new input takes over again.
        private bool _resetting;
        private Vector2 _lastMouse;

        // The slow sway of the camera rotation (RotationModifiable). It writes the camera rotation every frame; the orbit is applied on top of it.
        private RotationModifiable _sway;

        public bool IsOffsetFromStart
        {
            get { return Mathf.Abs(_yaw) > 0.01f || Mathf.Abs(_pitch) > 0.01f || Mathf.Abs(_scale - 1f) > 0.001f || _move.sqrMagnitude > 0.0001f; }
        }

        private void OnDisable()
        {
            ReleaseCamera();
        }

        // Settings the camera menu changes at runtime.
        public bool KeepSway { get { return _keepSway; } set { _keepSway = value; } }

        public void ResetCamera()
        {
            _yaw = 0f;
            _pitch = 0f;
            _scale = 1f;
            _move = Vector3.zero;
        }

        /// <summary>Back to the start pose with a slow glide. Any new input takes over again.</summary>
        public void ResetSmoothly()
        {
            if (TerrainRotator.Instance != null)
                TerrainRotator.Instance.ResetRotation();
            ResetCamera();
            _resetting = true;
        }

        /// <summary>Puts the controlled camera back to its start pose and lets go of it.</summary>
        private void ReleaseCamera()
        {
            if (_camera != null)
                _camera.transform.SetPositionAndRotation(_startPosition, _startRotation);
            _camera = null;
            _sway = null;
            ResetCamera();
            _curYaw = 0f;
            _curPitch = 0f;
            _curScale = 1f;
            _curMove = Vector3.zero;
            _orbiting = false;
            _moving = false;
            _resetting = false;
            UiPointer.SetLocked(false);
        }

        /// <summary>Follows the active camera. A new camera starts at its own pose with no offset.</summary>
        private void TrackActiveCamera()
        {
            Camera active = Camera.main;
            if (active == _camera)
                return;
            ReleaseCamera();
            if (active == null)
                return;

            _camera = active;
            _sway = null;
            foreach (RotationModifiable modifiable in FindObjectsByType<RotationModifiable>(FindObjectsSortMode.None))
                if (modifiable.TargetTransform == active.transform)
                    _sway = modifiable;
            _startPosition = active.transform.position;
            _startRotation = active.transform.rotation;

            CameraPivot pivot = CameraPivot.FindFor(active);
            // Without a pivot the camera orbits a point straight ahead of it.
            _pivot = pivot != null ? pivot.transform.position : _startPosition + _startRotation * Vector3.forward * 30f;
            _startOffset = _startPosition - _pivot;
            if (_startOffset.sqrMagnitude < 1f)
                _startOffset = -(_startRotation * Vector3.forward) * 1f;
            _startElevation = Elevation(0f);
        }

        /// <summary>Angle in degrees of the camera above the horizontal plane through the pivot, for a given tilt.</summary>
        private float Elevation(float pitch)
        {
            Vector3 offset = Quaternion.AngleAxis(pitch, _startRotation * Vector3.right) * _startOffset;
            return Mathf.Asin(Mathf.Clamp(offset.y / offset.magnitude, -1f, 1f)) * Mathf.Rad2Deg;
        }

        /// <summary>Keeps the tilt within the elevation limits. A higher tilt always means a higher camera.</summary>
        private float ClampPitch(float pitch)
        {
            pitch = Mathf.Clamp(pitch, -_maxPitch, _maxPitch);
            float low = Mathf.Min(_startElevation, _minElevation);
            float high = Mathf.Max(_startElevation, _maxElevation);
            if (Elevation(pitch) < low)
                pitch = SolvePitch(pitch, _maxPitch, low);
            else if (Elevation(pitch) > high)
                pitch = SolvePitch(-_maxPitch, pitch, high);
            return pitch;
        }

        private float SolvePitch(float from, float to, float elevation)
        {
            for (int i = 0; i < 12; i++)
            {
                float mid = (from + to) * 0.5f;
                if (Elevation(mid) < elevation)
                    from = mid;
                else
                    to = mid;
            }
            return (from + to) * 0.5f;
        }

        private void Update()
        {
            TrackActiveCamera();
            if (_camera == null)
                return;

            bool canInteract = UiPointer.IsInsideWindow && !UiPointer.IsTyping;
            if (!canInteract)
            {
                _orbiting = false;
                _moving = false;
            }

            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            float startDistance = _startOffset.magnitude;
            float distance = startDistance * _scale;
            Vector2 mouse = Input.mousePosition;

            if (canInteract)
            {
                if (!overUi)
                {
                    // The band editor of the test signal uses the left and right mouse button.
                    bool editing = AudioTestSignal.IsEditingBands;
                    if (_allowOrbit && !editing && Input.GetMouseButtonDown(_orbitButton))
                    {
                        // On the terrain the mouse turns the terrain, on the background it orbits the camera.
                        if (!BeginTerrainTurn(_orbitButton))
                            BeginOrbit(_orbitButton, mouse);
                    }
                    else if (_allowOrbit && _orbitWithMiddle && _orbitButton != 2 && Input.GetMouseButtonDown(2))
                        BeginOrbit(2, mouse);
                    if (_allowMove && !editing && Input.GetMouseButtonDown(1))
                    {
                        _moving = true;
                        _resetting = false;
                        _lastMouse = mouse;
                    }
                }
                if (_resetKey != KeyCode.None && Input.GetKeyDown(_resetKey))
                    ResetSmoothly();
                if (_orbiting && Input.GetMouseButtonUp(_orbitHeld))
                    _orbiting = false;
                if (Input.GetMouseButtonUp(1))
                    _moving = false;
            }

            Vector2 delta = mouse - _lastMouse;
            _lastMouse = mouse;

            if (_orbiting && Input.GetMouseButton(_orbitHeld))
            {
                // The scene turns with the cursor: drag right = camera goes left, drag up = camera goes down.
                _yaw += delta.x * _orbitSpeed;
                _pitch = ClampPitch(_pitch - delta.y * _orbitSpeed);
            }

            if (_moving && Input.GetMouseButton(1))
            {
                float unitsPerPixel = 2f * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * distance / Mathf.Max(1, Screen.height);
                // The view follows the cursor on the plane the camera sees.
                Transform cam = _camera.transform;
                _move -= (cam.right * delta.x + cam.up * delta.y) * (unitsPerPixel * _panSpeed);
            }

            if (canInteract && _allowZoom && !overUi)
            {
                float wheel = Input.mouseScrollDelta.y;
                if (Mathf.Abs(wheel) > 0.001f)
                {
                    _scale *= 1f - wheel * _zoomStep;
                    _resetting = false;
                }
            }

            if (canInteract && _useKeys && _allowMove)
            {
                // W/S and A/D slide on the horizontal plane in the viewing direction, Q/E go straight down and up.
                Transform cam = _camera.transform;
                Vector3 forward = Vector3.ProjectOnPlane(cam.forward, Vector3.up);
                if (forward.sqrMagnitude < 0.0001f)
                    forward = Vector3.ProjectOnPlane(cam.up, Vector3.up);
                forward.Normalize();
                Vector3 right = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;

                Vector3 key = forward * Axis(KeyCode.W, KeyCode.S) + right * Axis(KeyCode.D, KeyCode.A) + Vector3.up * Axis(KeyCode.E, KeyCode.Q);
                if (key.sqrMagnitude > 0f)
                    _resetting = false;
                _move += key * (distance * _keyMoveSpeed * Time.unscaledDeltaTime);
            }

            _scale = Mathf.Clamp(_scale, _minDistanceScale, _maxDistanceScale);
            float maxMove = _maxMoveDistance * startDistance;
            if (_move.magnitude > maxMove)
                _move = _move.normalized * maxMove;

            // While the mouse steers the camera, the menus and cues act as if the pointer had not moved.
            UiPointer.SetLocked(_orbiting || _moving || TerrainRotator.IsTurning);
        }

        private void BeginOrbit(int button, Vector2 mouse)
        {
            _orbiting = true;
            _orbitHeld = button;
            _resetting = false;
            _lastMouse = mouse;
        }

        /// <summary>Starts turning the terrain when the cursor is on it. False when it is not, then the camera orbits instead.</summary>
        private bool BeginTerrainTurn(int button)
        {
            if (TerrainRotator.Instance == null || _camera == null)
                return false;
            return TerrainRotator.Instance.TryBeginTurn(_camera.ScreenPointToRay(Input.mousePosition), button);
        }

        private static float Axis(KeyCode positive, KeyCode negative)
        {
            return (Input.GetKey(positive) ? 1f : 0f) - (Input.GetKey(negative) ? 1f : 0f);
        }

        private void LateUpdate()
        {
            if (_camera == null)
                return;

            float t = 1f - Mathf.Exp(-(_resetting ? _resetSmoothing : _smoothing) * Time.unscaledDeltaTime);
            _curYaw = Mathf.Lerp(_curYaw, _yaw, t);
            _curPitch = Mathf.Lerp(_curPitch, _pitch, t);
            _curScale = Mathf.Lerp(_curScale, _scale, t);
            _curMove = Vector3.Lerp(_curMove, _move, t);

            // Turn around the up axis first, then tilt around the start pose's right axis, so the horizon stays level.
            Quaternion orbit = Quaternion.AngleAxis(_curYaw, Vector3.up) * Quaternion.AngleAxis(_curPitch, _startRotation * Vector3.right);
            // The views sway their camera slowly (RotationModifiable). Its rotation of this frame is kept and the orbit is applied on top of it.
            Quaternion baseRotation = _startRotation;
            if (_keepSway && _sway != null && _sway.LastWrittenFrame == Time.frameCount)
                baseRotation = _sway.LastWrittenRotation;
            Quaternion rotation = orbit * baseRotation;
            // Moving shifts the pivot, so the orbit and the zoom keep working around the spot the viewer moved to.
            Vector3 position = _pivot + _curMove + orbit * (_startOffset * _curScale);
            _camera.transform.SetPositionAndRotation(position, rotation);
        }
    }
}
