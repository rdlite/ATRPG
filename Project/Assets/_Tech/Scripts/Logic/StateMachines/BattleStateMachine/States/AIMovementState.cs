public class AIMovementState : IPayloadState<UnitBase>, IState {
    private BattleHandler _battleHandler;
    private UIRoot _uiRoot;
    private AIMovementResolver _aiResolver;

    public AIMovementState(
        AIMovementResolver aiResolver, UIRoot uiRoot, BattleHandler battleHandler) {
        _battleHandler = battleHandler;
        _uiRoot = uiRoot;
        _aiResolver = aiResolver;
    }

    public void Enter() { }

    public void Enter(UnitBase unitToMove) {
        _battleHandler.CurrentSelectedUnit?.DestroySelection();
        _battleHandler.CurrentSelectedUnit?.SetActiveOutline(false);
        _uiRoot.GetPanel<BattlePanel>().DisableUnitsPanel(_battleHandler);

        _aiResolver.MoveUnit(unitToMove);
    }

    public void Exit() {

    }
}