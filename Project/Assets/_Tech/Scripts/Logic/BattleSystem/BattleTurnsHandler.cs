using System.Collections.Generic;
using UnityEngine;

public class BattleTurnsHandler {
    private List<TurnData> _turnsContainer;
    private Dictionary<UnitBase, CurrentRoundUnitsData> _unitsRoundData_map;
    private BattleHandler _battleHandler;
    private UIRoot _uiRoot;
    private BattleGridData _battleGridData;
    private UnitBase _currentWalkingUnit;
    private AIMovementResolver _aiMovementResolver;

    public BattleTurnsHandler(
        BattleGridData battleGridData, UIRoot ui, BattleHandler battleHandler,
        ICoroutineService coroutineService, CameraSimpleFollower camera) {
        _battleHandler = battleHandler;
        _uiRoot = ui;
        _battleGridData = battleGridData;
        _turnsContainer = new List<TurnData>();

        _aiMovementResolver = new AIMovementResolver(
            battleGridData, battleHandler, this,
            camera, coroutineService);

        TryFillTurns();
        RefreshUnitsData();
    }

    public void Cleanup() {
        _turnsContainer.Clear();
        _unitsRoundData_map.Clear();
        _unitsRoundData_map = null;
        _uiRoot.GetPanel<BattlePanel>().CleanupAfterBattleEnd();
    }

    public void StartTurns() {
        if (_turnsContainer[0].IconType == TurnsUILayout.IconType.Enemy) {
            StartAITurn(_turnsContainer[0].Unit);
        }
    }

    public void CallNextTurn() {
        SetCurrentWalker(null);

        UnitBase endUnit = _turnsContainer[0].Unit;

        _turnsContainer.RemoveAt(0);
        _uiRoot.GetPanel<BattlePanel>().DestroyFirstIcon();

        if (_turnsContainer[0].IconType == TurnsUILayout.IconType.RestartRound) {
            StartNewRound();
        }

        if (_turnsContainer[0].IconType == TurnsUILayout.IconType.Enemy) {
            StartAITurn(_turnsContainer[0].Unit);
            return;
        } else if (_turnsContainer[0].Unit is PlayerUnit) {
            _battleHandler.FocusCameraToNearestAllyUnit(endUnit);
        }

        TryFillTurns();
    }

    private void StartAITurn(UnitBase unit) {
        _aiMovementResolver.MoveUnit(unit);
    }

    public void AIEndedTurn() {
        CallNextTurn();
    }

    public void SetCurrentWalker(UnitBase unit) {
        _currentWalkingUnit = unit;
    }

    public UnitBase GetCurrentUnitWalker() {
        return _currentWalkingUnit;
    }

    public bool IsHaveCurrentWalkingUnit() {
        return _currentWalkingUnit != null;
    }

    public bool IsItCurrentWalkingUnit(UnitBase unit) {
        return _currentWalkingUnit == null || _currentWalkingUnit == unit;
    }

    private void StartNewRound() {
        _turnsContainer.RemoveAt(0);
        _uiRoot.GetPanel<BattlePanel>().DestroyFirstIcon();
        RefreshUnitsData();
    }

    private void TryFillTurns() {
        while (_uiRoot.GetPanel<BattlePanel>().CheckIfNeedNewTurnIcons()) {
            GenerateOneRound();
        }
    }

    public bool IsCanUnitWalk(UnitBase unit) {
        return _unitsRoundData_map[unit].IsCanWalk;
    }

    public void SetUnitWalked(UnitBase unit) {
        _unitsRoundData_map[unit].IsCanWalk = false;
    }

    public void RemovePossibleLengthForUnit(UnitBase unit, float length) {
        _unitsRoundData_map[unit].MovementLengthLast -= length;
    }

    public float GetLastLengthForUnit(UnitBase unit) {
        return _unitsRoundData_map[unit].MovementLengthLast;
    }

    private void RefreshUnitsData() {
        if (_unitsRoundData_map == null) {
            _unitsRoundData_map = new Dictionary<UnitBase, CurrentRoundUnitsData>();

            for (int i = 0; i < _battleGridData.Units.Count; i++) {
                _unitsRoundData_map.Add(_battleGridData.Units[i], new CurrentRoundUnitsData(_battleGridData.Units[i].GetMovementLength()));
            }
        }

        foreach (var unitData in _unitsRoundData_map) {
            unitData.Value.IsCanWalk = true;
            unitData.Value.IsDefaultAttacked = false;
            unitData.Value.MovementLengthLast = unitData.Key.GetMovementLength();
        }
    }

    public void CheckBattleEnd() {
        bool isAllEnemiesDead = true;
        bool isAllPlayerDead = true;

        foreach (var unitData in _unitsRoundData_map) {
            if (isAllPlayerDead && unitData.Key is PlayerUnit && !unitData.Key.IsDeadOnBattleField) {
                isAllPlayerDead = false;
            }

            if (isAllEnemiesDead && unitData.Key is EnemyUnit && !unitData.Key.IsDeadOnBattleField) {
                isAllEnemiesDead = false;
            }
        }

        if (isAllEnemiesDead || isAllPlayerDead) {
            _battleHandler.StopBattle(isAllEnemiesDead);
        }
    }

    private void GenerateOneRound() {
        // FOR DEBUG REASONS
        //_turnsContainer.Add(new TurnData(TurnsUILayout.IconType.Enemy, _battleGridData.Units[^2]));
        //_uiRoot.GetPanel<BattlePanel>().AddTurnIcon(TurnsUILayout.IconType.Enemy, _battleHandler, _battleGridData.Units[^2]);
        //_turnsContainer.Add(new TurnData(TurnsUILayout.IconType.Player, _battleGridData.Units[0]));
        //_uiRoot.GetPanel<BattlePanel>().AddTurnIcon(TurnsUILayout.IconType.Player, _battleHandler, _battleGridData.Units[0]);
        //_turnsContainer.Add(new TurnData(TurnsUILayout.IconType.Player, _battleGridData.Units[1]));
        //_uiRoot.GetPanel<BattlePanel>().AddTurnIcon(TurnsUILayout.IconType.Player, _battleHandler, _battleGridData.Units[1]);
        //_turnsContainer.Add(new TurnData(TurnsUILayout.IconType.Player, _battleGridData.Units[2]));
        //_uiRoot.GetPanel<BattlePanel>().AddTurnIcon(TurnsUILayout.IconType.Player, _battleHandler, _battleGridData.Units[2]);
        //_turnsContainer.Add(new TurnData(TurnsUILayout.IconType.Player, _battleGridData.Units[3]));
        //_uiRoot.GetPanel<BattlePanel>().AddTurnIcon(TurnsUILayout.IconType.Player, _battleHandler, _battleGridData.Units[3]);
        //_turnsContainer.Add(new TurnData(TurnsUILayout.IconType.Enemy, _battleGridData.Units[^1]));
        //_uiRoot.GetPanel<BattlePanel>().AddTurnIcon(TurnsUILayout.IconType.Enemy, _battleHandler, _battleGridData.Units[^2]);
        //_turnsContainer.Add(new TurnData(TurnsUILayout.IconType.RestartRound, null));
        //_uiRoot.GetPanel<BattlePanel>().AddTurnIcon(TurnsUILayout.IconType.RestartRound, null, null);

        List<UnitBase> listRandomize = new List<UnitBase>(_battleGridData.Units);

        int removedUnits = 0;

        for (int i = 0; i < listRandomize.Count; i++) {
            if (listRandomize[i].IsDeadOnBattleField) {
                listRandomize.RemoveAt(i);
                i--;
                removedUnits++;
            }
        }

        for (int i = 0; i < _battleGridData.Units.Count - removedUnits; i++) {
            UnitBase randomUnit = listRandomize[Random.Range(0, listRandomize.Count)];

            listRandomize.Remove(randomUnit);

            bool isPlayerTurn = randomUnit is PlayerUnit;

            _uiRoot.GetPanel<BattlePanel>().AddTurnIcon(isPlayerTurn ? TurnsUILayout.IconType.Player : TurnsUILayout.IconType.Enemy, _battleHandler, randomUnit);
            _turnsContainer.Add(new TurnData(isPlayerTurn ? TurnsUILayout.IconType.Player : TurnsUILayout.IconType.Enemy, randomUnit));
        }

        _uiRoot.GetPanel<BattlePanel>().AddTurnIcon(TurnsUILayout.IconType.RestartRound, null, null);
        _turnsContainer.Add(new TurnData(TurnsUILayout.IconType.RestartRound, null));
    }

    public void SetUnitAttackedDefaultAttack(UnitBase attackedUnit) {
        //_unitsRoundData_map[attackedUnit].IsDefaultAttacked = true;
    }

    public bool IsUnitAttackedDefaultAttack(UnitBase attackedUnit) {
        if (_unitsRoundData_map != null && _unitsRoundData_map.ContainsKey(attackedUnit)) {
            return _unitsRoundData_map[attackedUnit].IsDefaultAttacked;
        }

        return false;
    }

    public void MarkUnitAsDead(UnitBase deadUnit) {
        if (deadUnit is EnemyUnit) {
            for (int i = 0; i < _turnsContainer.Count; i++) {
                if (_turnsContainer[i].Unit == deadUnit) {
                    _uiRoot.GetPanel<BattlePanel>().DestroyIconOfUnit(deadUnit);
                    _turnsContainer.RemoveAt(i);
                    i--;
                }
            }

            TryFillTurns();
        } else {
            RemoveOnePlayerIconFromEachRound();
            CheckBattleEnd();
            CallNextTurn();
        }
    }

    private void RemoveOnePlayerIconFromEachRound() {
        for (int i = 0; i < _turnsContainer.Count; i++) {
            if (_turnsContainer[i].IconType == TurnsUILayout.IconType.RestartRound) {
                for (int j = i; j < _turnsContainer.Count; j++) {
                    if (_turnsContainer[j].IconType == TurnsUILayout.IconType.Player) {
                        _uiRoot.GetPanel<BattlePanel>().DestroyIconAt(j);
                        _turnsContainer.RemoveAt(j);
                        break;
                    }
                }
            }
        }
    }

    private class TurnData {
        public TurnsUILayout.IconType IconType;
        public UnitBase Unit;

        public TurnData(TurnsUILayout.IconType iconType, UnitBase unit) {
            IconType = iconType;
            Unit = unit;
        }
    }

    private class CurrentRoundUnitsData {
        public float MovementLengthLast;
        public bool IsCanWalk;
        public bool IsDefaultAttacked;

        public CurrentRoundUnitsData(float movementRangeLast) {
            MovementLengthLast = movementRangeLast;
            IsCanWalk = true;
            IsDefaultAttacked = false;
        }
    }
}