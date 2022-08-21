using System;
using UnityEngine;

public abstract class CharacterWalker : MonoBehaviour {
    [SerializeField] protected CharacterStatsConfig _statsData;
    [SerializeField] protected NavAgent _agent;
    [SerializeField] protected Collider _collider;
    [SerializeField] protected Transform _overCharacterPoint;
    [SerializeField] protected GameObject _selectionAboveCharacter;
    [SerializeField] protected float _distanceToStartWalkingAnimation = 5f;

    protected Outline[] _childOutlines;
    protected ConfigsContainer _configsContainer;
    protected OnFieldRaycaster _fieldRaycaster;
    protected SceneAbstractEntitiesMediator _abstractEntityMediator;
    protected GameObject _createdCharacterSelection;
    protected OverCharacterWorldDataPanel _createdOverCharacterData;
    protected CharacterAnimator _animator;
    protected AssetsContainer _assetsContainer;
    protected float _defaultSpeed;
    protected bool _isCurrentlyMoving;
    private CameraSimpleFollower _mainCamera;
    private SelectionData _selectionData;

    public void Init(
        OnFieldRaycaster fieldRaycaster, SceneAbstractEntitiesMediator abstractEntitiesMediator, ConfigsContainer configsContainer,
        AssetsContainer assetsContainer, CameraSimpleFollower mainCamera, ICoroutineService coroutineService) {
        _mainCamera = mainCamera;
        _assetsContainer = assetsContainer;
        _configsContainer = configsContainer;
        _fieldRaycaster = fieldRaycaster;
        _animator = new CharacterAnimator(GetComponentInChildren<Animator>(true), coroutineService);
        _defaultSpeed = _statsData.MovementSpeed;
        _abstractEntityMediator = abstractEntitiesMediator;

        _childOutlines = GetComponentsInChildren<Outline>(true);

        if (this is EnemyCharacterWalker) {
            _selectionData = _configsContainer.CharactersSelectionData.EnemySelection;
        } else if (this is PlayerCharacterWalker) {
            _selectionData = _configsContainer.CharactersSelectionData.PlayerSelection;
        }

        for (int i = 0; i < _childOutlines.Length; i++) {
            _childOutlines[i].SetOutlineValues(_selectionData);
        }

        LocalInit();
    }

    protected abstract void LocalInit();

    public void GoToPoint(
        Vector3 worldPos, bool isMoveToExactPoint, bool isIgnorePenalty = false,
        Action<PathCallbackData> callback = null, Action onReachCallback = null) {
        _agent.SetDestination(worldPos, isMoveToExactPoint, isIgnorePenalty, (val) => {
            callback?.Invoke(val);
            _animator.SetMovementMagnitude(_agent.GetPathLength() > _distanceToStartWalkingAnimation ? 1f : .5f);
            _agent.MovementSpeed = _agent.GetPathLength() > _distanceToStartWalkingAnimation ? _defaultSpeed : _defaultSpeed / 2f;
        }, onReachCallback);
    }

    protected virtual void Update() {
        if (!_isCurrentlyMoving && _agent.IsWalkingByPath) {
            StartMove();
        } else if (_isCurrentlyMoving && !_agent.IsWalkingByPath) {
            StopMove();
        }

        MoveSelection();
        MoveDataAbove();
    }

    private void MoveSelection() {
        if (_createdCharacterSelection != null) {
            _createdCharacterSelection.transform.position = GetOverCharacterPoint();
        }
    }

    private void MoveDataAbove() {
        if (_createdOverCharacterData != null) {
            _createdOverCharacterData.transform.position = Vector3.Lerp(_createdOverCharacterData.transform.position, GetOverCharacterPoint() + Vector3.up * (_createdCharacterSelection == null ? .5f : 1f), 13f * Time.deltaTime);
            _createdOverCharacterData.transform.forward = _mainCamera.transform.forward;
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

    public virtual void StartMove() {
        _collider.enabled = false;
        _isCurrentlyMoving = true;
        _animator.SetMovementAnimation(true);
    }

    public virtual void StopMove() {
        _collider.enabled = true;
        _isCurrentlyMoving = false;
        _animator.SetMovementAnimation(false);
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

    public CharacterStatsConfig GetStatsConfig() {
        return _statsData;
    }

    public Color GetCharacterColor() {
        return _selectionData.OutlineColor;
    }

    public void CreateOverUnitData() {
        if (_createdOverCharacterData == null) {
            _createdOverCharacterData = Instantiate(_assetsContainer.BattleOverCharacterDataPrefab);
            _createdOverCharacterData.transform.position = GetOverCharacterPoint() + Vector3.up * 1.25f;
            _createdOverCharacterData.Init(this);
        }
    }

    public void DestroyOverUnitData() {
        if (_createdOverCharacterData != null) {
            Destroy(_createdOverCharacterData.gameObject);
        }
    }

    //ANIMATOR METHODS
    public void WithdrawWeapon() {
        _animator.WithdrawWeapon();
    }
}