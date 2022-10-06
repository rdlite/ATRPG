public class StartBattleState : IState
{
    private BattleTurnsHandler _turnsHandler;
    private UpdateStateMachine _battleSM;

    public StartBattleState(UpdateStateMachine battleSM, BattleTurnsHandler turnsHandler)
    {
        _turnsHandler = turnsHandler;
        _battleSM = battleSM;
    }

    public void Enter()
    {
        _turnsHandler.StartTurns();
    }

    public void Exit()
    {

    }
}