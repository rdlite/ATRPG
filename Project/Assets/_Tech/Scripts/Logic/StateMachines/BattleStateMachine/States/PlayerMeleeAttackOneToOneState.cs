using System.Collections.Generic;
using UnityEngine;

public class PlayerMeleeAttackOneToOneState : IPayloadState<(BattleFieldActionAbility, bool)>, IUpdateState
{
    private List<UnitBase> _unitsToAttack;
    private UnitBase[] _selectionForAttackMap;
    private bool[] _currentAttackingMap;

    private InputService _inputService;
    private BattleRaycaster _battleRaycaster;
    private ImposedPairsContainer _imposedPairsContainer;
    private BattleGridData _battleGridData;
    private BattleHandler _battleHandler;
    private UpdateStateMachine _battleSM;
    private BattleFieldActionAbility _attackAbility;
    private bool _isPossibilityAttack;

    public PlayerMeleeAttackOneToOneState(
        UpdateStateMachine battleSM, BattleHandler battleHandler, BattleGridData battleGridData,
        ImposedPairsContainer imposedPairsContainer, BattleRaycaster battleRaycaster, InputService inputService)
    {
        _inputService = inputService;
        _battleRaycaster = battleRaycaster;
        _imposedPairsContainer = imposedPairsContainer;
        _battleGridData = battleGridData;
        _battleHandler = battleHandler;
        _battleSM = battleSM;
        _unitsToAttack = new List<UnitBase>();

        _currentAttackingMap = new bool[_battleGridData.Units.Count];
        _selectionForAttackMap = new UnitBase[_battleGridData.Units.Count];
    }

    public void Enter((BattleFieldActionAbility, bool) data)
    {
        _attackAbility = data.Item1;
        _isPossibilityAttack = data.Item2;

        _unitsToAttack.Clear();

        Node currentUnitNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(_battleHandler.CurrentSelectedUnit.transform.position);

        bool hasAtLeastOneTargetInRange = false;

        if (_imposedPairsContainer.HasPairWith(_battleHandler.CurrentSelectedUnit))
        {

            UnitBase imposedUnit = _imposedPairsContainer.GetPairFor(_battleHandler.CurrentSelectedUnit);
            _unitsToAttack.Add(imposedUnit);

            for (int i = 0; i < _battleGridData.Units.Count; i++)
            {
                if (_battleGridData.Units[i] == imposedUnit)
                {
                    _battleHandler.SetActiveAttackDecalUnderUnit(i, true);
                    hasAtLeastOneTargetInRange = true;
                }
            }
        }
        else
        {
            for (int i = 0; i < _battleGridData.Units.Count; i++)
            {
                if (!_battleGridData.Units[i].IsDeadOnBattleField && _battleGridData.Units[i] != _battleHandler.CurrentSelectedUnit && _battleGridData.Units[i] is EnemyUnit)
                {
                    Node targetUnitNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(_battleGridData.Units[i].transform.position);

                    if (_battleGridData.GlobalGrid.IsNodesContacts(currentUnitNode, targetUnitNode))
                    {
                        _battleHandler.SetActiveAttackDecalUnderUnit(i, true);
                        _unitsToAttack.Add(_battleGridData.Units[i]);
                        hasAtLeastOneTargetInRange = true;
                    }
                }
            }
        }

        if (!hasAtLeastOneTargetInRange)
        {
            _battleSM.Enter<IdlePlayerMovementState>();
        }
    }

    public void Update()
    {
        _currentAttackingMap = new bool[_battleGridData.Units.Count];

        ProcessAttack();
        SelectionMousePress();
        AbortAttackProcess();
    }

    public void Exit()
    {
        _battleHandler.StopAttackProcesses();
    }

    private void AbortAttackProcess()
    {
        if (Input.GetMouseButtonDown(1) && !_inputService.IsPointerOverUIObject())
        {
            _battleSM.Enter<IdlePlayerMovementState>();
        }
    }

    private void ProcessAttack()
    {
        UnitBase currentOverSelectedUnitForAttack = _battleRaycaster.GetCurrentMouseOverSelectionUnit();
        bool hasTarget = false;

        for (int i = 0; i < _battleGridData.Units.Count; i++)
        {
            if (
                _battleGridData.Units[i] == currentOverSelectedUnitForAttack &&
                _battleGridData.Units[i] is EnemyUnit &&
                _unitsToAttack.Contains(_battleGridData.Units[i]) &&
                !_inputService.IsPointerOverUIObject())
            {

                _currentAttackingMap[i] = true;
                _selectionForAttackMap[i] = _battleGridData.Units[i];
                hasTarget = true;
            }
        }

        if (!hasTarget)
        {
            DeselectAllAttackUnits();
        }
        else
        {
            ShowUnitsToAttack();
        }
    }

    private void DeselectAllAttackUnits()
    {
        for (int i = 0; i < _selectionForAttackMap.Length; i++)
        {
            if (_selectionForAttackMap[i] != null)
            {
                _selectionForAttackMap[i].SetActiveOutline(false);
                _battleHandler.DeactivateOverUnitData(_selectionForAttackMap[i], false);
                _selectionForAttackMap[i] = null;
                _battleHandler.SetAttackDecalUnderUnitAsDefault(i);
            }
        }
    }

    private void ShowUnitsToAttack()
    {
        for (int i = 0; i < _selectionForAttackMap.Length; i++)
        {
            if (_currentAttackingMap[i])
            {
                _battleHandler.SetAttackDecalUnderUnitAsSelected(i);
                _battleGridData.Units[i].SetActiveOutline(true);
                _battleHandler.ShowOverUnitData(_battleGridData.Units[i], _battleHandler.CurrentSelectedUnit.GetUnitConfig());
            }
        }
    }

    private void SelectionMousePress()
    {
        if (Input.GetMouseButtonDown(0) && !_inputService.IsPointerOverUIObject() && _battleHandler.CurrentMouseOverSelectionUnit != null)
        {
            if (_battleHandler.CurrentMouseOverSelectionUnit != _battleHandler.CurrentSelectedUnit)
            {
                bool isAttackPress = false;

                for (int i = 0; i < _selectionForAttackMap.Length; i++)
                {
                    if (_selectionForAttackMap[i] == _battleHandler.CurrentMouseOverSelectionUnit)
                    {
                        isAttackPress = true;
                        break;
                    }
                }

                if (isAttackPress)
                {
                    TryAttackUnits(_battleHandler.CurrentSelectedUnit, new List<UnitBase>() { _battleHandler.CurrentMouseOverSelectionUnit }, _attackAbility);
                }
                else
                {
                    _battleSM.Enter<UnitSelectionState, (UnitBase, IExitableState)>((_battleHandler.CurrentMouseOverSelectionUnit, _battleSM.GetStateOfType(typeof(IdlePlayerMovementState))));
                }
            }
        }
    }

    private void TryAttackUnits(UnitBase attacker, List<UnitBase> targets, BattleFieldActionAbility ability)
    {
        _battleSM.Enter<AttackSequenceState, (UnitBase, List<UnitBase>, BattleFieldActionAbility, bool, System.Action, System.Action)>((attacker, targets, ability, _isPossibilityAttack, null, null));
    }
}