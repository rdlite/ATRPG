using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavAgent : MonoBehaviour {
    public float MovementSpeed;
    public Vector3 Velocity { get; private set; }
    public bool IsWalkingByPath => _isMoving;

    [SerializeField] private float _baseOffset = .85f;
    [SerializeField] private float _faceForwardSpeed = 10f;
    [SerializeField] private float _stoppingDistance = 1f;

    private Vector3[] _path;
    private Coroutine _movementRoutine;
    private Vector3 _prevTargetPoint;
    private Vector3 _prevPosition;
    private int _targetIndex;
    private bool _isMoving;

    private void Start() {
        transform.position = PathRequestManager.GetGroundCharacterPoint(transform.position);
    }

    public void SetDestination(Vector3 destination, bool isMoveToExactPoint, bool isIgnorePenalty, Action<PathCallbackData> foundCallback) {
        PathRequestManager.RequestPath(transform.position, destination, isMoveToExactPoint, isIgnorePenalty,(foundPath, successful) => {
            if (foundPath != null && foundPath.Length > 0) {
                OnPathFound(foundPath, successful);

                Vector3 endPointForwardDirection = Vector3.zero;

                if (foundPath.Length > 1) {
                    endPointForwardDirection = foundPath[^1] - foundPath[^2];
                    endPointForwardDirection.Normalize();
                }

                PathCallbackData pathCallbackData = new PathCallbackData(foundPath[^1], endPointForwardDirection, successful);
                foundCallback?.Invoke(pathCallbackData);
            }
        });
    }

    public void StopMovement() {
        _isMoving = false;
        StopAllCoroutines();
    }

    private void OnPathFound(Vector3[] foundPath, bool successful) {
        if (successful) {
            _path = SmoothPath(foundPath);

            if (_movementRoutine != null) {
                StopCoroutine(_movementRoutine);
            }

            _isMoving = true;

            _movementRoutine = StartCoroutine(FollowPath());
        }
    }

    private Vector3[] SmoothPath(Vector3[] path) {
        List<Vector3> resultPath = new List<Vector3>();

        resultPath.Add(path[0]);

        for (int i = 1; i < path.Length - 1; i += 2) {
            int segments = 4;
            for (int sID = 0; sID < segments; sID++) {
                Vector3[] lerps = new Vector3[3];

                float t = sID / (float)(segments - 1);
                lerps[0] = Vector3.Lerp(path[i - 1], path[i], t);
                lerps[1] = Vector3.Lerp(path[i], path[i + 1], t);
                lerps[2] = Vector3.Lerp(lerps[0], lerps[1], t);

                resultPath.Add(lerps[2]);
            }
        }

        resultPath.Add(path[^1]);

        return resultPath.ToArray();
    }

    public float GetPathLength() {
        if (_path != null && _path.Length >= 2) {
            float length = 0f;

            for (int i = 1; i < _path.Length; i++) {
                length += Vector3.Distance(_path[i - 1], _path[i]);
            }

            return length;
        }

        return 0f;
    }

    private void Update() {
        if (!_isMoving) {
            Velocity = Vector3.Lerp(Velocity, Vector3.zero, Time.deltaTime * 5f);
        }
    }

    private IEnumerator FollowPath() {
        _targetIndex = 0;

        if (_path.Length == 0) {
            FoundTarget();
            yield break;
        }

        Vector3 currentWaypoint = _path[0] + Vector3.up * _baseOffset;

        while (true) {
            bool isFPSBelowCap = (1 / Time.deltaTime) < 40;

            float distanceOffsetLerp = Mathf.Lerp(.25f, .1f, Mathf.InverseLerp(40, 60, 1 / Time.deltaTime));

            if (isFPSBelowCap && (transform.position - currentWaypoint).sqrMagnitude <= .1f || !isFPSBelowCap && (transform.position - currentWaypoint).sqrMagnitude <= distanceOffsetLerp) {
                _targetIndex++;

                if (_targetIndex >= _path.Length) {
                    FoundTarget();
                    yield break;
                }

                currentWaypoint = _path[_targetIndex] + Vector3.up * _baseOffset;
            }

            if (Vector2.Distance(transform.position.GetYRemovedV2(), _path[_path.Length - 1].GetYRemovedV2()) < _stoppingDistance) {
                FoundTarget();
                yield break;
            }

            if (isFPSBelowCap) {
                transform.position = Vector3.MoveTowards(transform.position, currentWaypoint, Time.deltaTime * MovementSpeed);
            } else {
                transform.position += (currentWaypoint - transform.position).normalized * (Time.deltaTime * MovementSpeed);
            }

            Vector3 movementDirection = (currentWaypoint - transform.position);
            movementDirection.y = 0f;
            movementDirection.Normalize();
            if (movementDirection != Vector3.zero) {
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(movementDirection), _faceForwardSpeed * Time.deltaTime);
            }

            Velocity = (transform.position - _prevPosition) * 100f;
            _prevPosition = transform.position;

            yield return null;
        }
    }

    private void FoundTarget() {
        _isMoving = false;
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.white;

        if (_path != null && _path.Length > 2) {
            for (int i = 0; i < _path.Length; i++) {
                Gizmos.DrawSphere(_path[i], .1f);
            }

            for (int i = 1; i < _path.Length; i++) {
                Gizmos.DrawLine(_path[i - 1], _path[i]);
            }
        }
    }
}

public struct PathCallbackData {
    public Vector3 EndWorldPos;
    public Vector3 EndForwardDirection;
    public bool IsSuccessful;

    public PathCallbackData(Vector3 endWorldPos, Vector3 endForwardDirection, bool isSuccessful) {
        EndWorldPos = endWorldPos;
        EndForwardDirection = endForwardDirection;
        IsSuccessful = isSuccessful;
    }
}