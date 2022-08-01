using UnityEngine;

public class TestCharacterWalker : MonoBehaviour {
    [SerializeField] private NavAgent _agent;

    private int IS_MOVING_HASH = Animator.StringToHash("IsMoving");
    private Animator _animator;
    private bool _isCurrentlyMoving;

    public void Init() {
        _animator = GetComponentInChildren<Animator>(true);
    }

    public void GoToPoint(Vector3 worldPos) {
        _agent.SetDestination(worldPos);
    }

    private void Update() {
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