using System.Collections.Generic;
using UnityEngine;

public class IdlePlayerMovementState : IState, IUpdateState {

    private BattleGridData _battleGridData;
    private BattleHandler _battleHandler;
    private BattleStateMachine _battleSM;
    private InputService _inputService;
    private BattleRaycaster _battleRaycaster;
    private bool[] _mouseOverSelectionMap;
    private bool[] _mouseOverDataSelectionMap;
    private const string APPEAR_RANGE = "_AppearRoundRange";

    public IdlePlayerMovementState (
        BattleHandler battleHandler, BattleGridData battleGridData, InputService inputService, 
        BattleRaycaster battleRaycaster, BattleStateMachine battleSM) {
        _battleSM = battleSM;
        _inputService = inputService;
        _battleRaycaster = battleRaycaster;
        _battleGridData = battleGridData;
        _battleHandler = battleHandler;

        _mouseOverSelectionMap = new bool[_battleGridData.Units.Count];
        _mouseOverDataSelectionMap = new bool[_battleGridData.Units.Count];
    }

    public void Enter() {
        _battleHandler.HideWalkingDistance(true);
        _battleHandler.BattleGridDecalProjector.material.SetFloat(APPEAR_RANGE, 0f);
    }

    public void Update() {
        MouseOverSelector();
        SelectionMousePress();
    }

    private void MouseOverSelector() {
        if (Time.frameCount % 4 == 0) {
            _battleHandler.CurrentMouseOverSelectionUnit = _battleRaycaster.GetCurrentMouseOverSelectionUnit();

            if (_battleHandler.CurrentMouseOverSelectionUnit != null && _battleHandler.CurrentMouseOverSelectionUnit.IsDeadOnBattleField) {
                _battleHandler.CurrentMouseOverSelectionUnit = null;
            }

            for (int i = 0; i < _battleGridData.Units.Count; i++) {
                if (_battleGridData.Units[i] == _battleHandler.CurrentMouseOverSelectionUnit && !_mouseOverSelectionMap[i] && !_inputService.IsPointerOverUIObject()) {
                    _mouseOverSelectionMap[i] = true;
                    _battleGridData.Units[i].SetActiveOutline(true);
                } else if (_battleGridData.Units[i] != _battleHandler.CurrentMouseOverSelectionUnit && _mouseOverSelectionMap[i]) {
                    if (!(_battleHandler.CurrentSelectedUnit != null && _battleHandler.CurrentSelectedUnit == _battleGridData.Units[i])) {
                        _mouseOverSelectionMap[i] = false;
                        _battleGridData.Units[i].SetActiveOutline(false);
                    }
                }
            }

            for (int i = 0; i < _battleGridData.Units.Count; i++) {
                if (_battleGridData.Units[i] == _battleHandler.CurrentMouseOverSelectionUnit && !_mouseOverDataSelectionMap[i] && !_inputService.IsPointerOverUIObject()) {
                    _mouseOverDataSelectionMap[i] = true;
                    _battleHandler.ShowOverUnitData(_battleGridData.Units[i]);
                } else if (_battleGridData.Units[i] != _battleHandler.CurrentMouseOverSelectionUnit && _mouseOverDataSelectionMap[i]) {
                    _mouseOverDataSelectionMap[i] = false;
                    _battleHandler.DeactivateOverUnitData(_battleGridData.Units[i], false);
                }
            }
        }
    }

    private void SelectionMousePress() {
        if (Input.GetMouseButtonDown(0) && !_inputService.IsPointerOverUIObject() && _battleHandler.CurrentMouseOverSelectionUnit != null) {
            if (_battleHandler.CurrentMouseOverSelectionUnit != _battleHandler.CurrentSelectedUnit) {
                _battleSM.Enter<UnitSelectionState, (UnitBase, IExitableState)>((_battleHandler.CurrentMouseOverSelectionUnit, _battleSM.GetStateOfType(typeof(IdlePlayerMovementState))));
            }
        }
    }

    public void Exit() { }
}
