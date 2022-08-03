using UnityEngine;

public class TestCharacterWalker : MonoBehaviour {
    [SerializeField] private GameObject _viewFow;
    [SerializeField] private NavAgent _agent;
    [SerializeField] private float _walkDistance = 5f;

    private int IS_MOVING_HASH = Animator.StringToHash("IsMoving");
    private int MOVEMENT_MAGNITUDE = Animator.StringToHash("MovementMagnitude");
    private OnFieldRaycaster _fieldRaycaster;
    private Animator _animator;
    private float _defaultSpeed;
    private bool _isCurrentlyMoving;

    public void Init(OnFieldRaycaster fieldRaycaster) {
        _fieldRaycaster = fieldRaycaster;
        _animator = GetComponentInChildren<Animator>(true);
        _defaultSpeed = _agent.MovementSpeed;
        _viewFow.SetActive(true);
    }

    public void GoToPoint(Vector3 worldPos, System.Action<bool> callback) {
        _agent.SetDestination(worldPos, (val) => {
            callback?.Invoke(val);
            _animator.SetFloat(MOVEMENT_MAGNITUDE, _agent.GetPathLength() > _walkDistance ? 1f : .5f);
            _agent.MovementSpeed = _agent.GetPathLength() > _walkDistance ? _defaultSpeed : _defaultSpeed / 2f;
        });
    }

    private void Update() {
        if (Input.GetMouseButtonDown(1)) {
            _agent.StopMovement();
            _fieldRaycaster.ClearWalkPoints();
        }

        if (!_isCurrentlyMoving && _agent.IsWalkingByPath) {
            StartMove();
        } else if (_isCurrentlyMoving && !_agent.IsWalkingByPath) {
            StopMove();
        }
    }

    private void StartMove() {
        _isCurrentlyMoving = true;
        _animator.SetBool(IS_MOVING_HASH, true);
    }

    private void StopMove() {
        _isCurrentlyMoving = false;
        _animator.SetBool(IS_MOVING_HASH, false);
    }
}