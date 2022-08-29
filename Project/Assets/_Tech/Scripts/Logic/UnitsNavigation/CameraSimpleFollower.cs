using System;
using UnityEngine;

public class CameraSimpleFollower : MonoBehaviour {
    [field: SerializeField] public Camera Camera { get; private set; }

    [SerializeField] private Vector3 _offset;
    [SerializeField] private Vector3 _targetRotation;
    [Range(.4f, 2f), SerializeField] private float _zoom = 1f;
    [SerializeField] private float _zoomSpeed = 1f, _zoomSmooth = 10f;
    [SerializeField] private float _rotationLerpMovement = 10f;
    [SerializeField] private float _maxZoomXRotation = 60f;
    [SerializeField] private float _minZoomXRotation = 45f;
    [SerializeField] private Vector2 _zoomClamp;
    [SerializeField] private Vector2 _zZoomingPosition;
    [SerializeField] private float _freeMovementSpeed = 5f, _freeMovementLerpSpeed = 2f;

    private const string HORIZONTAL_AXIS = "Horizontal";
    private const string VERTICAL_AXIS = "Vertical";
    private const string MOUSE_SCROLLWHEEL = "Mouse ScrollWheel";
    private Vector3 _cameraGlobalLDPoint;
    private Vector3 _cameraGlobalRUPoint;
    private Vector3 _cameraBattleFieldLDPoint;
    private Vector3 _cameraBattleFieldRUPoint;
    private Transform _target;
    private Transform _freeMovementPoint;
    private Vector2 _freeMovementVelocity;
    private Vector3 _defaultOffset;
    private float _currentZooming;
    private bool _isSnapping;
    private bool _isRestrictedMovement;

    public void Init(Transform target, Vector3 cameraGlobalLDPoint, Vector3 cameraGlobalRUPoint) {
        _defaultOffset = _offset;

        _cameraGlobalLDPoint = cameraGlobalLDPoint;
        _cameraGlobalRUPoint = cameraGlobalRUPoint;

        SetTarget(target);

        _freeMovementVelocity = new Vector2(0f, 0f);

        _currentZooming = _zoom;

        _freeMovementPoint = new GameObject("CameraSnappingPoint").transform;
        _freeMovementPoint.transform.position = _target.transform.position;
    }

    public void SetTarget(Transform newTarget) {
        _target = newTarget;
        _isSnapping = true;
    }

    public void SetMovementRestrictions(Vector3 ldPosition, Vector3 ruPosition) {
        _isRestrictedMovement = true;
        _cameraBattleFieldLDPoint = ldPosition;
        _cameraBattleFieldRUPoint = ruPosition;
    }

    public void SetFreeMovement() {
        _isRestrictedMovement = false;
    }

    private void LateUpdate() {
        if (_isSnapping) {
            if (IsPressedMovementKeys()) {
                _isSnapping = false;
            }

            transform.position = Vector3.Lerp(transform.position, _target.position + _offset * _currentZooming, 15f * Time.deltaTime);
            _freeMovementPoint.transform.position = _target.transform.position;
        } else {
            if (Input.GetKeyDown(KeyCode.C)) {
                _isSnapping = true;
                _freeMovementVelocity = Vector2.zero;
            }

            _freeMovementVelocity = Vector2.Lerp(_freeMovementVelocity, new Vector2(Input.GetAxis(HORIZONTAL_AXIS), Input.GetAxis(VERTICAL_AXIS)), _freeMovementLerpSpeed * Time.deltaTime);
            _freeMovementPoint.position -= new Vector3(_freeMovementVelocity.x, 0f, _freeMovementVelocity.y) * Time.deltaTime * _freeMovementSpeed;

            float minXPos = _isRestrictedMovement ? _cameraBattleFieldLDPoint.x : _cameraGlobalLDPoint.x + 5f;
            float maxXPos = _isRestrictedMovement ? _cameraBattleFieldRUPoint.x : _cameraGlobalRUPoint.x - 5f;
            float minZPos = _isRestrictedMovement ? _cameraBattleFieldLDPoint.z : _cameraGlobalLDPoint.z + _offset.z * 1.5f; 
            float maxZPos = _isRestrictedMovement ? _cameraBattleFieldRUPoint.z : _cameraGlobalRUPoint.z - _offset.z * 1.5f;

            _freeMovementPoint.position = new Vector3(
                    Mathf.Clamp(_freeMovementPoint.position.x, minXPos, maxXPos),
                    _freeMovementPoint.position.y,
                    Mathf.Clamp(_freeMovementPoint.position.z, minZPos, maxZPos));
            transform.position = Vector3.Lerp(transform.position, _freeMovementPoint.position + _offset * _currentZooming, 15f * Time.deltaTime);
        }

        transform.eulerAngles = Vector3.Lerp(transform.eulerAngles, _targetRotation, _rotationLerpMovement * Time.deltaTime);

        _zoom -= Input.GetAxis(MOUSE_SCROLLWHEEL) * _zoomSpeed;
        _zoom = Mathf.Clamp(_zoom, _zoomClamp.x, _zoomClamp.y);
        _currentZooming = Mathf.Lerp(_currentZooming, _zoom, _zoomSmooth * Time.deltaTime);
        _targetRotation = Vector3.Lerp(new Vector3(_minZoomXRotation, _targetRotation.y, _targetRotation.z), new Vector3(_maxZoomXRotation, _targetRotation.y, _targetRotation.z), Mathf.InverseLerp(_zoomClamp.x, _zoomClamp.y, _zoom));
        _offset.z = _defaultOffset.z + Mathf.Lerp(_zZoomingPosition.x, _zZoomingPosition.y, Mathf.InverseLerp(_zoomClamp.x, _zoomClamp.y, _zoom));
    }

    private bool IsPressedMovementKeys() {
        return Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D) ||
            Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.DownArrow);
    }
}