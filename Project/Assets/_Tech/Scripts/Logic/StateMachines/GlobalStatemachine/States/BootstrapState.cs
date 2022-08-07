public class BootstrapState : IState {
    private GameStateMachine _gameStateMachine;

    public BootstrapState(
        GameStateMachine gameStateMachine, ICoroutineService coroutineService) {
        _gameStateMachine = gameStateMachine;

        RegisterSystems(coroutineService);
    }

    public void Enter() {
        _gameStateMachine.Enter<LoadLevelState, string>("DefaultScene");
    }

    private void RegisterSystems(ICoroutineService coroutineService) {
        ServicesContainer.Instance.Set(new LevelsLoadingService(coroutineService));
    }

    public void Exit() { }
}