using UnityEngine;

public class BattleState : IPayloadState<BetweenStatesDataContainer>, IUpdateState {
    private BetweenStatesDataContainer _sceneData;

    public BattleState() {

    }

    public void Enter(BetweenStatesDataContainer sceneData) {
        _sceneData = sceneData;
        sceneData.CharactersGroupContainer.StopCharacters();
        sceneData.BattleGridGenerator.StartBattle(
            sceneData.CharactersGroupContainer, sceneData.EnemyDetected, sceneData.Camera);
    }

    public void Exit() { }

    public void Update() {
        _sceneData.BattleGridGenerator.Tick();
    }
}