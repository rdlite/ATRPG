using UnityEngine;

public class SceneAbstractEntitiesMediator
{
    private AStarGrid _globalGrid;
    private GameStateMachine _globalStateMachine;
    private BetweenStatesDataContainer _betweenStatesDataContainer;

    public void Init(AStarGrid globalGrid, GameStateMachine globalStateMachine, BetweenStatesDataContainer betweenStatesDataContainer)
    {
        _betweenStatesDataContainer = betweenStatesDataContainer;
        _globalGrid = globalGrid;
        _globalStateMachine = globalStateMachine;
    }

    public void TryEnterToBattleState(EnemyUnit enemyDetected)
    {
        _betweenStatesDataContainer.EnemyDetected = enemyDetected;
        _globalStateMachine.Enter<BattleState, BetweenStatesDataContainer>(_betweenStatesDataContainer);
    }
}