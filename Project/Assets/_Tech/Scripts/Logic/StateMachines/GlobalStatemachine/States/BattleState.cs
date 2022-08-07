using UnityEngine;

public class BattleState : IPayloadState<BetweenStatesDataContainer> {
    public BattleState() {

    }

    public void Enter(BetweenStatesDataContainer sceneData) {
        sceneData.CharactersGroupContainer.StopCharacters();
        sceneData.BattleGridGenerator.StartBattle(
            sceneData.CharactersGroupContainer, sceneData.EnemyDetected, sceneData.Camera);
    }

    public void Exit() { }
}