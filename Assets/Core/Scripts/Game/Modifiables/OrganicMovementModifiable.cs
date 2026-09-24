using UnityEngine;
using System.Collections.Generic;
using Gavi.Modules.MathModules.FloatModules;

namespace Gavi.Modifiables
{
    public class OrganicMovementModifiable : MonoBehaviour
    {
        [SerializeField] private List<Transform> _targetTransforms;


        [Header("--- Motion ---")]
        [SerializeField] private bool _applyMovement = true;
        [Header("- X -")]
        [SerializeField] private bool _applyMovementX;
        [SerializeField] private float _movementCurveTimeX = 10f;
        [SerializeField] private Vector2 _movementRangeX = new Vector2(-0.2f, 0.2f);
        [SerializeField] private AnimationCurve _movementCurveX = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0.5f), new Keyframe(0.333f, 1f), new Keyframe(0.667f, 0), new Keyframe(1, 0.5f) } );
        [SerializeField] private float _movementStartTX;
        [SerializeField] private FloatModule _tMovementX;
        private float _currentTMovementX;
        [Header("- Y - ")]
        [SerializeField] private bool _applyMovementY;
        [SerializeField] private float _movementCurveTimeY = 10f;
        [SerializeField] private Vector2 _movementRangeY = new Vector2(-0.2f, 0.2f);
        [SerializeField] private AnimationCurve _movementCurveY = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0.5f), new Keyframe(0.333f, 1f), new Keyframe(0.667f, 0), new Keyframe(1, 0.5f) });
        [SerializeField] private float _movementStartTY;
        [SerializeField] private FloatModule _tMovementY;
        private float _currentTMovementY;
        [Header("- Z - ")]
        [SerializeField] private bool _applyMovementZ;
        [SerializeField] private float _movementCurveTimeZ = 10f;
        [SerializeField] private Vector2 _movementRangeZ = new Vector2(-0.2f, 0.2f);
        [SerializeField] private AnimationCurve _movementCurveZ = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0.5f), new Keyframe(0.333f, 1f), new Keyframe(0.667f, 0), new Keyframe(1, 0.5f) });
        [SerializeField] private float _movementStartTZ;
        [SerializeField] private FloatModule _tMovementZ;
        private float _currentTMovementZ;
        [Header("--- Rotation ---")]
        [SerializeField] private bool _applyRotation = true;
        [Header("- X - ")]
        [SerializeField] private bool _applyRotationX = true;
        [SerializeField] private float _rotationCurveTimeX = 10f;
        [SerializeField] private Vector2 _rotationRangeX = new Vector2(-10f, 10f);
        [SerializeField] private AnimationCurve _rotationCurveX = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0.5f), new Keyframe(0.333f, 1f), new Keyframe(0.667f, 0), new Keyframe(1, 0.5f) });
        [SerializeField] private float _rotationStartTX;
        [SerializeField] private FloatModule _tRotationX;
        private float _currentTRotationX;
        [Header("- Y - ")]
        [SerializeField] private bool _applyRotationY;
        [SerializeField] private float _rotationCurveTimeY = 10f;
        [SerializeField] private Vector2 _rotationRangeY = new Vector2(-10f, 10f);
        [SerializeField] private AnimationCurve _rotationCurveY = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0.5f), new Keyframe(0.333f, 1f), new Keyframe(0.667f, 0), new Keyframe(1, 0.5f) });
        [SerializeField] private float _rotationStartTY;
        [SerializeField] private FloatModule _tRotationY;
        private float _currentTRotationY;
        [Header("- Z - ")]
        [SerializeField] private bool _applyRotationZ;
        [SerializeField] private float _rotationCurveTimeZ = 10f;
        [SerializeField] private Vector2 _rotationRangeZ = new Vector2(-10f, 10f);
        [SerializeField] private AnimationCurve _rotationCurveZ = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0.5f), new Keyframe(0.333f, 1f), new Keyframe(0.667f, 0), new Keyframe(1, 0.5f) });
        [SerializeField] private float _rotationStartTZ;
        [SerializeField] private FloatModule _tRotationZ;
        private float _currentTRotationZ;
        private Dictionary<Transform, Quaternion> _defaultLocalRotation = new Dictionary<Transform, Quaternion>();
        private Dictionary<Transform, Vector3> _defaultLocalPositions = new Dictionary<Transform, Vector3>();



        #region Init
        private void Awake()
        {
            if (_targetTransforms == null || _targetTransforms.Count == 0)
            {
                _targetTransforms = new List<Transform>();
                _targetTransforms.Add(transform);
            }
            AssignDefaults();
            _currentTRotationX = _rotationStartTX;
            _currentTRotationY = _rotationStartTY;
            _currentTRotationZ = _rotationStartTZ;
            _currentTMovementX = _movementStartTX;
            _currentTMovementY = _movementStartTY;
            _currentTMovementZ = _movementStartTZ;
        }
        private void AssignDefaults()
        {
            foreach (Transform t in _targetTransforms)
            {
                _defaultLocalRotation.Add(t, t.localRotation);
                _defaultLocalPositions.Add(t, t.localPosition);
            }
        }
        #endregion
        private void Update()
        {
            if (_targetTransforms == null)
                return;
            ApplyMovement();
            ApplyRotation();
        }
        private void ApplyMovement()
        {
            if (!_applyMovement)
                return;
            Vector3 localDisplacement = Vector3.zero;
            // movement x
            if (_applyMovementX && _movementCurveTimeX > 0)
            {
                _currentTMovementX = _tMovementY != null ? _tMovementX.GetValue() : (_currentTMovementX + 1 / _movementCurveTimeX * Time.deltaTime);
                _currentTMovementX %= 1;
                localDisplacement.x = Mathf.Lerp(_movementRangeX.x, _movementRangeX.y, _movementCurveX.Evaluate(_currentTMovementX));
            }
            // movement y
            if (_applyMovementY && _movementCurveTimeY > 0)
            {
                _currentTMovementY = _tMovementY != null ? _tMovementY.GetValue() : (_currentTMovementY + 1 / _movementCurveTimeY * Time.deltaTime);
                _currentTMovementY %= 1;
                localDisplacement.y = Mathf.Lerp(_movementRangeY.x, _movementRangeY.y, _movementCurveY.Evaluate(_currentTMovementY));
            }
            // movement Z
            if (_applyMovementZ && _movementCurveTimeZ > 0)
            {
                _currentTMovementZ = _tMovementZ != null ? _tMovementZ.GetValue() : (_currentTMovementZ + 1 / _movementCurveTimeZ * Time.deltaTime);
                _currentTMovementZ %= 1;
                localDisplacement.z = Mathf.Lerp(_movementRangeZ.x, _movementRangeZ.y, _movementCurveZ.Evaluate(_currentTMovementZ));
            }

            foreach (Transform t in _targetTransforms)
                t.localPosition = _defaultLocalPositions[t] + localDisplacement;
        }
        private void ApplyRotation()
        {
            if (!_applyRotation)
                return;
            Vector3 localRotation = Vector3.zero;
            // rotation x
            if (_applyRotationX && _rotationCurveTimeX > 0)
            {
                _currentTRotationX = _tRotationX != null ? _tRotationX.GetValue() : (_currentTRotationX + 1 / _rotationCurveTimeX * Time.deltaTime);
                _currentTRotationX %= 1;
                localRotation.x = Mathf.Lerp(_rotationRangeX.x, _rotationRangeX.y,
                    _rotationCurveX.Evaluate(_currentTRotationX));
            }
            // rotation y
            if (_applyRotationY && _rotationCurveTimeY > 0)
            {
                _currentTRotationY = _tRotationY != null ? _tRotationY.GetValue() : (_currentTRotationY + 1 / _rotationCurveTimeY * Time.deltaTime);
                _currentTRotationY %= 1;
                localRotation.y = Mathf.Lerp(_rotationRangeY.x, _rotationRangeY.y,
                    _rotationCurveY.Evaluate(_currentTRotationY));
            }
            // rotation Z
            if (_applyRotationZ && _rotationCurveTimeZ > 0)
            {
                _currentTRotationZ = _tRotationZ != null ? _tRotationZ.GetValue() : (_currentTRotationZ + 1 / _rotationCurveTimeZ * Time.deltaTime);
                _currentTRotationZ %= 1;
                localRotation.z = Mathf.Lerp(_rotationRangeZ.x, _rotationRangeZ.y,
                    _rotationCurveZ.Evaluate(_currentTRotationZ));
            }
            foreach (Transform t in _targetTransforms)
                t.localRotation = _defaultLocalRotation[t] * Quaternion.Euler(localRotation);
        }
    }
}