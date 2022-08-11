using UnityEngine;

public abstract class CharacterWalker : MonoBehaviour {
    [SerializeField] protected NavAgent _agent;
    [SerializeField] protected Collider _collider;
    [SerializeField] protected Transform _overCharacterPoint;
    [SerializeField] protected GameObject _selectionAboveCharacter;
    [SerializeField] protected float _distanceToStartWalkingAnimation = 5f;

    protected int IS_MOVING_HASH = Animator.StringToHash("IsMoving");
    protected int MOVEMENT_MAGNITUDE = Animator.StringToHash("MovementMagnitude");
    protected Outline[] _childOutlines;
    private ConfigsContainer _configsContainer;
    protected OnFieldRaycaster _fieldRaycaster;
    protected Animator _animator;
    protected SceneAbstractEntitiesMediator _abstractEntityMediator;
    protected GameObject _createdCharacterSelection;
    protected float _defaultSpeed;
    protected bool _isCurrentlyMoving;

    public void Init(
        OnFieldRaycaster fieldRaycaster, SceneAbstractEntitiesMediator abstractEntitiesMediator, ConfigsContainer configsContainer) {
        _configsContainer = configsContainer;
        _fieldRaycaster = fieldRaycaster;
        _animator = GetComponentInChildren<Animator>(true);
        _defaultSpeed = _agent.MovementSpeed;
        _abstractEntityMediator = abstractEntitiesMediator;

        _childOutlines = GetComponentsInChildren<Outline>(true);

        SelectionData selectionData = null;

        if (this is EnemyCharacterWalker) {
            selectionData = _configsContainer.CharactersSelectionData.EnemySelection;
        } else if (this is PlayerCharacterWalker){
            selectionData = _configsContainer.CharactersSelectionData.PlayerSelection;
        }

        for (int i = 0; i < _childOutlines.Length; i++) {
            _childOutlines[i].SetOutlineValues(selectionData);
        }

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

    private void Update() {
        MoveSelection();
    }

    private void MoveSelection() {
        if (_createdCharacterSelection != null) {
            _createdCharacterSelection.transform.position = GetOverCharacterPoint();
        }
    }

    public virtual void AbortMovement() {
        StopMove();
        _agent.StopMovement();
    }

    public void SetActiveOutline(bool value) {
        for (int i = 0; i < _childOutlines.Length; i++) {
            _childOutlines[i].enabled = value;
        }
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

    public Vector3 GetOverCharacterPoint() {
        return _overCharacterPoint.transform.position;
    }

    public void CreateSelectionAbove() {
        DestroySelection();

        _createdCharacterSelection = Instantiate(_selectionAboveCharacter);
    }

    public void DestroySelection() {
        if (_createdCharacterSelection != null) {
            Destroy(_createdCharacterSelection);
        }
    }
}