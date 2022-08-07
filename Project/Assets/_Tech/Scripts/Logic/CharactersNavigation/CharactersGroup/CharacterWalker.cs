using UnityEngine;

public abstract class CharacterWalker : MonoBehaviour {
    [SerializeField] protected NavAgent _agent;
    [SerializeField] protected Collider _collider;
    [SerializeField] protected float _distanceToStartWalkingAnimation = 5f;

    protected int IS_MOVING_HASH = Animator.StringToHash("IsMoving");
    protected int MOVEMENT_MAGNITUDE = Animator.StringToHash("MovementMagnitude");
    protected OnFieldRaycaster _fieldRaycaster;
    protected Animator _animator;
    protected SceneAbstractEntitiesMediator _abstractEntityMediator;
    protected float _defaultSpeed;
    protected bool _isCurrentlyMoving;

    public void Init(OnFieldRaycaster fieldRaycaster, SceneAbstractEntitiesMediator abstractEntitiesMediator) {
        _fieldRaycaster = fieldRaycaster;
        _animator = GetComponentInChildren<Animator>(true);
        _defaultSpeed = _agent.MovementSpeed;
        _abstractEntityMediator = abstractEntitiesMediator;

        LocalInit();
    }

    protected abstract void LocalInit();

    public void GoToPoint(Vector3 worldPos, bool isMoveToExactPoint, System.Action<PathCallbackData> callback) {
        _agent.SetDestination(worldPos, isMoveToExactPoint, (val) => {
            callback?.Invoke(val);
            _animator.SetFloat(MOVEMENT_MAGNITUDE, _agent.GetPathLength() > _distanceToStartWalkingAnimation ? 1f : .5f);
            _agent.MovementSpeed = _agent.GetPathLength() > _distanceToStartWalkingAnimation ? _defaultSpeed : _defaultSpeed / 2f;
        });
    }

    public virtual void Tick() {
        if (!_isCurrentlyMoving && _agent.IsWalkingByPath) {
            StartMove();
        } else if (_isCurrentlyMoving && !_agent.IsWalkingByPath) {
            StopMove();
        }
    }

    public virtual void AbortMovement() {
        StopMove();
        _agent.StopMovement();
    }

    protected virtual void StartMove() {
        _collider.enabled = false;
        _isCurrentlyMoving = true;
        _animator.SetBool(IS_MOVING_HASH, true);
    }

    protected virtual void StopMove() {
        _collider.enabled = true;
        _isCurrentlyMoving = false;
        _animator.SetBool(IS_MOVING_HASH, false);
    }
}