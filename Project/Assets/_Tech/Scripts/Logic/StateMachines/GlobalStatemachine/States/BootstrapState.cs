public class BootstrapState : IState {
    private GameStateMachine _gameStateMachine;

    public BootstrapState(
        GameStateMachine gameStateMachine, ICoroutineService coroutineService, UIRoot uiRoot) {
        _gameStateMachine = gameStateMachine;

        RegisterSystems(coroutineService, uiRoot);
    }

    public void Enter() {
        _gameStateMachine.Enter<LoadLevelState, string>("DefaultScene");
    }

    private void RegisterSystems(ICoroutineService coroutineService, UIRoot uiRoot) {
        ServicesContainer.Instance.Set(new LevelsLoadingService(coroutineService));

        uiRoot.Init(_gameStateMachine);
    }

    public void Exit() { }
}