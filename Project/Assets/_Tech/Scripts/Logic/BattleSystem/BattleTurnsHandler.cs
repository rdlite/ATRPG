using System.Collections.Generic;
using UnityEngine;

public class BattleTurnsHandler {
    private List<TurnData> _turnsContainer;
    private Dictionary<CharacterWalker, CurrentRoundUnitsData> _unitsRoundData_map;
    private BattleHandler _battleHandler;
    private UIRoot _uiRoot;
    private BattleGridData _battleGridData;
    private CharacterWalker _currentWalkingUnit;

    public BattleTurnsHandler(
        BattleGridData battleGridData, UIRoot ui, BattleHandler battleHandler) {
        _battleHandler = battleHandler;
        _uiRoot = ui;
        _battleGridData = battleGridData;
        _turnsContainer = new List<TurnData>();
        TryFillTurns();
        RefreshUnitsData();
    }

    public void StartTurns() {
        if (_turnsContainer[0].IconType == TurnsUILayout.IconType.Enemy) {
            AIEndedTurn();
        }
    }

    public void WaitButtonPressed() {
        SetCurrentWalker(null);

        if (_turnsContainer[1].IconType == TurnsUILayout.IconType.RestartRound) {
            StartNewRound();
        }

        if (_turnsContainer[1].IconType == TurnsUILayout.IconType.Enemy) {
            _turnsContainer.RemoveAt(1);
            _uiRoot.GetPanel<BattlePanel>().DestroyFirstIcon();
            AIEndedTurn();
            return;
        } else if (_turnsContainer[1].Unit is PlayerCharacterWalker) {
            _battleHandler.FocusCameraToNearestAllyUnit(_turnsContainer[0].Unit);
        }

        _turnsContainer.RemoveAt(0);
        _uiRoot.GetPanel<BattlePanel>().DestroyFirstIcon();

        TryFillTurns();
    }

    public void AIEndedTurn() {
        WaitButtonPressed();
    }

    public void SetCurrentWalker(CharacterWalker unit) {
        _currentWalkingUnit = unit;
    }

    public CharacterWalker GetCurrentUnitWalker() {
        return _currentWalkingUnit;
    }

    public bool IsHaveCurrentWalkingUnit() {
        return _currentWalkingUnit != null;
    }

    public bool IsItCurrentWalkingUnit(CharacterWalker unit) {
        return _currentWalkingUnit == null || _currentWalkingUnit == unit;
    }

    private void StartNewRound() {
        _turnsContainer.RemoveAt(1);
        _uiRoot.GetPanel<BattlePanel>().DestroyFirstIcon();
        RefreshUnitsData();
    }

    private void TryFillTurns() {
        while (_uiRoot.GetPanel<BattlePanel>().CheckIfNeedNewTurnIcons()) {
            GenerateOneRound();
        }
    }

    public bool IsCanUnitWalk(CharacterWalker unit) {
        return _unitsRoundData_map[unit].IsCanWalk;
    }

    public void SetUnitWalked(CharacterWalker unit) {
        _unitsRoundData_map[unit].IsCanWalk = false;
    }

    public void RemovePossibleLengthForUnit(CharacterWalker unit, float length) {
        _unitsRoundData_map[unit].MovementLengthLast -= length;
    }

    public float GetLastLengthForUnit(CharacterWalker unit) {
        return _unitsRoundData_map[unit].MovementLengthLast;
    }

    private void RefreshUnitsData() {
        if (_unitsRoundData_map == null) {
            _unitsRoundData_map = new Dictionary<CharacterWalker, CurrentRoundUnitsData>();

            for (int i = 0; i < _battleGridData.Units.Count; i++) {
                _unitsRoundData_map.Add(_battleGridData.Units[i], new CurrentRoundUnitsData(_battleGridData.Units[i].GetStatsConfig().MovementLength));
            }
        }

        foreach (var unitData in _unitsRoundData_map) {
            unitData.Value.IsCanWalk = true;
            unitData.Value.MovementLengthLast = unitData.Key.GetStatsConfig().MovementLength;
        }
    }

    private void GenerateOneRound() {
        List<CharacterWalker> listRandomize = new List<CharacterWalker>(_battleGridData.Units);

        for (int i = 0; i < _battleGridData.Units.Count; i++) {
            CharacterWalker randomCharacter = listRandomize[Random.Range(0, listRandomize.Count)];
            listRandomize.Remove(randomCharacter);

            bool isPlayerTurn = randomCharacter is PlayerCharacterWalker;

            _uiRoot.GetPanel<BattlePanel>().AddTurnIcon(isPlayerTurn ? TurnsUILayout.IconType.Player : TurnsUILayout.IconType.Enemy);
            _turnsContainer.Add(new TurnData(isPlayerTurn ? TurnsUILayout.IconType.Player : TurnsUILayout.IconType.Enemy, randomCharacter));
        }

        _uiRoot.GetPanel<BattlePanel>().AddTurnIcon(TurnsUILayout.IconType.RestartRound);
        _turnsContainer.Add(new TurnData(TurnsUILayout.IconType.RestartRound, null));
    }

    private class TurnData {
        public TurnsUILayout.IconType IconType;
        public CharacterWalker Unit;

        public TurnData(TurnsUILayout.IconType iconType, CharacterWalker unit) {
            IconType = iconType;
            Unit = unit;
        }
    }

    private class CurrentRoundUnitsData {
        public float MovementLengthLast;
        public bool IsCanWalk;

        public CurrentRoundUnitsData(float movementRangeLast) {
            MovementLengthLast = movementRangeLast;
            IsCanWalk = true;
        }
    }
}