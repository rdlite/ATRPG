using UnityEngine;

public class WordWalkingState : IState, IPayloadState<BetweenStatesDataContainer>, IUpdateState {
    private UIRoot _uiRoot;
    private GameStateMachine _gameStateMachine;
    private BetweenStatesDataContainer _statesData;

    public WordWalkingState(
        GameStateMachine gameStateMachine, UIRoot uiRoot) {
        _uiRoot = uiRoot;
        _gameStateMachine = gameStateMachine;
    }

    public void Enter() {
        _uiRoot.EnablePanel(UIRoot.UIPanelType.WorldWalking);
    }

    public void Enter(BetweenStatesDataContainer statesData) {
        _statesData = statesData;
        _uiRoot.EnablePanel(UIRoot.UIPanelType.WorldWalking);
    }

    public void Exit() { }

    public void Update() {
        _statesData.FieldRaycaster.Tick();

        _statesData.CharactersGroupContainer.Tick();
    }
}