using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class NavAgent : MonoBehaviour {
    public Vector3 Velocity { get; private set; }
    public bool IsWalkingByPath => _isMoving;

    [SerializeField] private float _baseOffset = .85f;
    [SerializeField] private float _movementSpeed;
    [SerializeField] private float _faceForwardSpeed = 10f;
    [SerializeField] private float _stoppingDistance = 1f;

    private Vector3[] _path;
    private Coroutine _movementRoutine;
    private Vector3 _prevTargetPoint;
    private Vector3 _prevPosition;
    private int _targetIndex;
    private bool _isMoving;

    public void SetDestination(Vector3 destination) {
        PathRequestManager.RequestPath(transform.position, destination, OnPathFound);
    }

    private void OnPathFound(Vector3[] path, bool successful) {
        if (successful) {
            _path = path;

            if (_movementRoutine != null) {
                StopCoroutine(_movementRoutine);
            }
            
            _isMoving = true;

            _movementRoutine = StartCoroutine(FollowPath());
        }
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

        float interpolation = 0f;

        while (true) {
            if (transform.position == currentWaypoint || interpolation >= 1f) {
                //if (_path.Length > 2 && _targetIndex > 0 && _targetIndex < _path.Length - 1) {
                //    _targetIndex++;
                //}

                _targetIndex++;
                interpolation = 0f;

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

            //if (_path.Length > 2 && _targetIndex > 0 && _targetIndex < _path.Length - 1) {

            //    interpolation += Time.deltaTime * _movementSpeed / 5f;

            //    int segments = 6;
            //    for (int sID = 0; sID < segments; sID++) {
            //        Vector3[] lerps = new Vector3[3];

            //        float t = interpolation;
            //        //float t = sID / (float)(segments - 1);

            //        lerps[0] = Vector3.Lerp(_path[_targetIndex - 1], _path[_targetIndex], t);
            //        lerps[1] = Vector3.Lerp(_path[_targetIndex], _path[_targetIndex + 1], t);
            //        lerps[2] = Vector3.Lerp(lerps[0], lerps[1], t);

            //        transform.position = lerps[2] + Vector3.up * _baseOffset;
            //    }
            //} else {
            //    transform.position = Vector3.MoveTowards(transform.position, currentWaypoint, _movementSpeed * Time.deltaTime);
            //}

            transform.position = Vector3.MoveTowards(transform.position, currentWaypoint, _movementSpeed * Time.deltaTime);
            
            Vector3 lookDirection = (currentWaypoint - transform.position);
            lookDirection.y = 0f;
            lookDirection.Normalize();
            if (lookDirection != Vector3.zero) {
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(lookDirection), _faceForwardSpeed * Time.deltaTime);
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

            Vector3 prevPosSegPos = _path[0];
            for (int i = 1; i < _path.Length - 1; i++) {
                int segments = 4;
                for (int sID = 0; sID < segments; sID++) {
                    Vector3[] lerps = new Vector3[3];

                    float t = sID / (float)(segments - 1);
                    lerps[0] = Vector3.Lerp(_path[i - 1], _path[i], t);
                    lerps[1] = Vector3.Lerp(_path[i], _path[i + 1], t);
                    lerps[2] = Vector3.Lerp(lerps[0], lerps[1], t);

                    Gizmos.DrawLine(prevPosSegPos, lerps[2]);

                    prevPosSegPos = lerps[2];
                }
            }
        }
    }
}