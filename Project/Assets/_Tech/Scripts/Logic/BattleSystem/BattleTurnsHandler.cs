using System.Collections.Generic;
using UnityEngine;

public class BattleTurnsHandler {
    private List<TurnData> _turnsContainer;
    private Dictionary<CharacterWalker, CurrentRoundUnitsData> _unitsRoundData_map;
    private BattleHandler _battleHandler;
    private UIRoot _uiRoot;
    private BattleGridData _battleGridData;
    private CharacterWalker _currentWalkingUnit;
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

    public void StartTurns() {
        if (_turnsContainer[0].IconType == TurnsUILayout.IconType.Enemy) {
            StartAITurn(_turnsContainer[0].Unit);
        }
    }

    public void CallNextTurn() {
        SetCurrentWalker(null);

        CharacterWalker endCharacter = _turnsContainer[0].Unit;

        _turnsContainer.RemoveAt(0);
        _uiRoot.GetPanel<BattlePanel>().DestroyFirstIcon();

        if (_turnsContainer[0].IconType == TurnsUILayout.IconType.RestartRound) {
            StartNewRound();
        }

        if (_turnsContainer[0].IconType == TurnsUILayout.IconType.Enemy) {
            StartAITurn(_turnsContainer[0].Unit);
            return;
        } else if (_turnsContainer[0].Unit is PlayerCharacterWalker) {
            _battleHandler.FocusCameraToNearestAllyUnit(endCharacter);
        }

        TryFillTurns();
    }

    private void StartAITurn(CharacterWalker unit) {
        _aiMovementResolver.MoveUnit(unit);
    }

    public void AIEndedTurn() {
        CallNextTurn();
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
        _turnsContainer.RemoveAt(0);
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

        List<CharacterWalker> listRandomize = new List<CharacterWalker>(_battleGridData.Units);

        for (int i = 0; i < _battleGridData.Units.Count; i++) {
            CharacterWalker randomCharacter = listRandomize[Random.Range(0, listRandomize.Count)];
            listRandomize.Remove(randomCharacter);

            bool isPlayerTurn = randomCharacter is PlayerCharacterWalker;

            _uiRoot.GetPanel<BattlePanel>().AddTurnIcon(isPlayerTurn ? TurnsUILayout.IconType.Player : TurnsUILayout.IconType.Enemy, _battleHandler, randomCharacter);
            _turnsContainer.Add(new TurnData(isPlayerTurn ? TurnsUILayout.IconType.Player : TurnsUILayout.IconType.Enemy, randomCharacter));
        }

        _uiRoot.GetPanel<BattlePanel>().AddTurnIcon(TurnsUILayout.IconType.RestartRound, null, null);
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