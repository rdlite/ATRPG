using System;
using UnityEngine;

public abstract class UnitBase : MonoBehaviour {
    public bool IsDeadOnBattleField;
    public bool IsBusy;

    [SerializeField] protected UnitStatsConfig _statsData;
    [SerializeField] protected NavAgent _agent;
    [SerializeField] protected Collider _collider;
    [SerializeField] protected Transform _overUnitPoint;
    [SerializeField] protected Transform _headPoint;
    [SerializeField] protected GameObject _selectionAboveCharacter;
    [SerializeField] protected Collider _characterRaycastDetector;
    [SerializeField] protected float _distanceToStartWalkingAnimation = 5f;
    [SerializeField] protected WeaponPrefabsType _weaponType;
    [SerializeField] protected WeaponAnimationLayerType _weaponLayerType;
    [SerializeField] private Transform _attackPoint;

    protected Outline[] _childOutlines;
    protected ConfigsContainer _configsContainer;
    protected OnFieldRaycaster _fieldRaycaster;
    protected SceneAbstractEntitiesMediator _abstractEntityMediator;
    protected GameObject _createdUnitSelection;
    protected OverUnitWorldDataPanel _createdOverUnitData;
    protected UnitAnimator _animator;
    protected AssetsContainer _assetsContainer;
    private CameraSimpleFollower _mainCamera;
    private SelectionData _selectionData;
    private UnitSkinContainer _skinContainer;
    private UnitWeaponHandler _weaponHandler;
    private UnitHealth _unitHealth;
    protected float _defaultSpeed;
    protected bool _isCurrentlyMoving;

    public void Init(
        OnFieldRaycaster fieldRaycaster, SceneAbstractEntitiesMediator abstractEntitiesMediator, ConfigsContainer configsContainer,
        AssetsContainer assetsContainer, CameraSimpleFollower mainCamera, ICoroutineService coroutineService) {
        _mainCamera = mainCamera;
        _assetsContainer = assetsContainer;
        _configsContainer = configsContainer;
        _fieldRaycaster = fieldRaycaster;
        _defaultSpeed = _statsData.MovementSpeed;
        _abstractEntityMediator = abstractEntitiesMediator;

        _skinContainer = GetComponentInChildren<UnitSkinContainer>(true);
        _childOutlines = GetComponentsInChildren<Outline>(true);

        _unitHealth = new UnitHealth(_statsData);

        _weaponHandler = new UnitWeaponHandler();
        _animator = new UnitAnimator(
            GetComponentInChildren<Animator>(true), coroutineService, _weaponHandler, 
            _skinContainer, _assetsContainer.AnimatorsContainer, _weaponLayerType);

        _weaponHandler.Init(
            assetsContainer, _skinContainer, gameObject.layer);

        if (this is EnemyUnit) {
            _selectionData = _configsContainer.CharactersSelectionData.EnemySelection;
        } else if (this is PlayerUnit) {
            _selectionData = _configsContainer.CharactersSelectionData.PlayerSelection;
        }

        for (int i = 0; i < _childOutlines.Length; i++) {
            _childOutlines[i].SetOutlineValues(_selectionData);
        }

        _weaponHandler.CreateWeapon(_weaponType);

        if (_createdOverUnitData == null) {
            CreateOverUnitUIData();
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
        if (_createdUnitSelection != null) {
            _createdUnitSelection.transform.position = GetOverUnitPoint();
        }
    }

    private void MoveDataAbove() {
        if (_createdOverUnitData != null) {
            _createdOverUnitData.transform.position = Vector3.Lerp(_createdOverUnitData.transform.position, GetOverUnitPoint() + _createdOverUnitData.transform.up * (_createdUnitSelection == null ? .5f : 1f), 13f * Time.deltaTime);
            _createdOverUnitData.transform.forward = _mainCamera.transform.forward;
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

    public Vector3 GetOverUnitPoint() {
        return _overUnitPoint.transform.position;
    }

    public void CreateSelectionAbove() {
        DestroySelection();

        _createdUnitSelection = Instantiate(_selectionAboveCharacter);
    }

    public void DestroySelection() {
        if (_createdUnitSelection != null) {
            Destroy(_createdUnitSelection);
        }
    }

    public Color GetUnitColor() {
        return _selectionData.OutlineColor;
    }

    private void CreateOverUnitUIData() {
        _createdOverUnitData = Instantiate(_assetsContainer.BattleOverUnitDataPrefab);
        _createdOverUnitData.transform.position = GetOverUnitPoint() + Vector3.up * 1.25f;
        _createdOverUnitData.gameObject.SetActive(false);
        _createdOverUnitData.Init(this, _unitHealth);
        _createdOverUnitData.SetActivePanel(OverUnitWorldDataPanel.PanelActivationType.None);
    }

    public void ActivateOverUnitData(bool isSmall, UnitStatsConfig attackerConfig = null, bool imposed = false) {
        _createdOverUnitData.gameObject.SetActive(true);
        _createdOverUnitData.UpdateData(attackerConfig, imposed);
        _createdOverUnitData.SetActivePanel(isSmall ? OverUnitWorldDataPanel.PanelActivationType.Small : OverUnitWorldDataPanel.PanelActivationType.Full);
    }

    public void DeactivateOverUnitData(bool completely) {
        _createdOverUnitData.SetActivePanel(completely ? OverUnitWorldDataPanel.PanelActivationType.None : OverUnitWorldDataPanel.PanelActivationType.Small);
        if (completely) {
            _createdOverUnitData.gameObject.SetActive(false);
        }
    }

    public UnitStatsConfig GetUnitConfig() {
        return _statsData;
    }

    public UnitHealth GetUnitHealthContainer() {
        return _unitHealth;
    }

    public UnitSkinContainer GetUnitSkinContainer() {
        return _skinContainer;
    }

    public float GetMovementLength() {
        return _statsData.MovementLength;
    }

    public bool TakeDamage(float damage) {
        if (_unitHealth.TakeDamage(damage)) {
            OnBattleFieldDieEvent();
            return true;
        }

        return false;
    }

    private void OnBattleFieldDieEvent() {
        PlayDeadAnimation();
        _characterRaycastDetector.gameObject.SetActive(false);
        IsDeadOnBattleField = true;
        _collider.enabled = false;
        _weaponHandler.DeactivateWeapon();
    }

    public void DeactivateWeapon() {
        _animator.SetActiveWeapon(false);
    }

    //ANIMATOR METHODS
    public void WithdrawWeapon() {
        _animator.WithdrawWeapon();
    }

    public void ShealtWeapon() {
        _animator.ShealtWeapon();
    }

    public void PlayAttackAnimation() {
        _animator.PlayAttackAnimation();
    }

    public void PlayImpactFromSwordAnimation() {
        _animator.PlayImpactFromSwordAnimation();
    }

    public void PlayDeadAnimation() {
        _animator.PlayDeadAnimation();
    }

    public void PlayImposedAttackAnimation() {
        _animator.PlayImposedAttackAnimation();
    }

    public void PlayImposedImpactAnimation() {
        _animator.PlayImposedImpactAnimation();
    }

    public Transform GetAttackPoint() {
        return _attackPoint;
    }

    public Transform GetHeadPoint() {
        return _headPoint;
    }

    public StunEffect CreateStunParticle() {
        StunEffect stunParticle = Instantiate(_assetsContainer.StunEffect);
        stunParticle.SnapToPoint(GetHeadPoint());
        return stunParticle;
    }

    public Color GetWalkingGridColor() {
        return _selectionData.WalkingGridColor;
    }

    public void Revive() {
        _characterRaycastDetector.gameObject.SetActive(true);
        IsDeadOnBattleField = false;
        _collider.enabled = true;
        _weaponHandler.SetWeaponIdle();
        _weaponHandler.ActivateWeapon();
        _animator.StandUp();
        _unitHealth.ResetData();
    }
}