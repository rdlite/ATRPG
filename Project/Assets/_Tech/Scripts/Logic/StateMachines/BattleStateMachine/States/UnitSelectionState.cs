using System;
using UnityEngine;

public class UnitSelectionState : IPayloadState<(UnitBase, IExitableState)> {
    private ImposedPairsContainer _imposedPairsContainer;
    private UIRoot _uiRoot;
    private BattleTurnsHandler _turnsHandler;
    private UpdateStateMachine _battleSM;
    private BattleHandler _battleHandler;

    public UnitSelectionState(
        BattleHandler battleHandler, UpdateStateMachine battleSM, BattleTurnsHandler turnsHandler,
        UIRoot uiRoot, ImposedPairsContainer imposedPairsContainer) {
        _imposedPairsContainer = imposedPairsContainer;
        _uiRoot = uiRoot;
        _turnsHandler = turnsHandler;
        _battleSM = battleSM;
        _battleHandler = battleHandler;
    }

    public void Enter((UnitBase, IExitableState) data) {
        _battleHandler.DeactivateMovementLine();

        _battleHandler.CurrentSelectedUnit?.DestroySelection();
        _battleHandler.CurrentSelectedUnit?.SetActiveOutline(false);
        _battleHandler.CurrentSelectedUnit = data.Item1;
        _battleHandler.CurrentSelectedUnit.SetActiveOutline(true);
        _battleHandler.CurrentSelectedUnit.CreateSelectionAbove();

        _battleHandler.StopAttackProcesses();

        UnitPanelState viewState = UnitPanelState.CompletelyDeactivate;

        if (_battleHandler.CurrentSelectedUnit is PlayerUnit && _turnsHandler.IsItCurrentWalkingUnit(_battleHandler.CurrentSelectedUnit) && _turnsHandler.IsCanUnitWalk(_battleHandler.CurrentSelectedUnit)) {
            viewState = UnitPanelState.UseTurn;
        } else {
            viewState = UnitPanelState.ViewTurn;
        }

        _uiRoot.GetPanel<BattlePanel>().DisableUnitsPanel(_battleHandler);
        _uiRoot.GetPanel<BattlePanel>().EnableUnitPanel(
            _battleHandler, _battleHandler.CurrentSelectedUnit, viewState,
            _turnsHandler, _imposedPairsContainer.HasPairWith(_battleHandler.CurrentSelectedUnit));

        if (_turnsHandler.IsHaveCurrentWalkingUnit() && !_turnsHandler.IsItCurrentWalkingUnit(_battleHandler.CurrentSelectedUnit)) {
            _uiRoot.GetPanel<BattlePanel>().SetActiveBackToUnitButton(true);
        } else {
            _uiRoot.GetPanel<BattlePanel>().SetActiveBackToUnitButton(false);
        }

        if (data.Item2 is IdlePlayerMovementState) {
            _battleSM.Enter<IdlePlayerMovementState>();
        }

        if (data.Item1 is EnemyUnit) {
            _battleHandler.ShowUnitWalkingDistance(data.Item1);
        }
    }

    public void Exit() { }
}