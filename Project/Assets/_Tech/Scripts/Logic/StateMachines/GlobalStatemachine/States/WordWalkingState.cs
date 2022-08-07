using UnityEngine;

public class WordWalkingState : IPayloadState<BetweenStatesDataContainer>, IUpdateState {
    private GameStateMachine _gameStateMachine;
    private BetweenStatesDataContainer _statesData;

    public WordWalkingState(GameStateMachine gameStateMachine) {
        _gameStateMachine = gameStateMachine;
    }

    public void Enter(BetweenStatesDataContainer statesData) {
        _statesData = statesData;
    }

    public void Exit() { }

    public void Update() {
        _statesData.FieldRaycaster.Tick();

        _statesData.CharactersGroupContainer.Tick();
    }
}