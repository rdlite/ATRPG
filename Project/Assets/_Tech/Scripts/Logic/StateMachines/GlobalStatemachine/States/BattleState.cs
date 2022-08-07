using UnityEngine;

public class BattleState : IPayloadState<BetweenStatesDataContainer> {
    public BattleState() {

    }

    public void Enter(BetweenStatesDataContainer sceneData) {
        sceneData.CharactersGroupContainer.StopCharacters();
    }

    public void Exit() { }
}