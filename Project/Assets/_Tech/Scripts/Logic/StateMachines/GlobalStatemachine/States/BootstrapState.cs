public class BootstrapState : IState {
    private GameStateMachine _gameStateMachine;

    public BootstrapState(
        GameStateMachine gameStateMachine, ICoroutineService coroutineService, UIRoot uiRoot, 
        AssetsContainer assetsContainer) {
        _gameStateMachine = gameStateMachine;

        RegisterSystems(coroutineService, uiRoot, assetsContainer);
    }

    public void Enter() {
        _gameStateMachine.Enter<LoadLevelState, string>("DefaultScene");
    }

    private void RegisterSystems(ICoroutineService coroutineService, UIRoot uiRoot, AssetsContainer assetsContainer) {
        ServicesContainer.Instance.Set(new LevelsLoadingService(coroutineService));

        uiRoot.Init(
            _gameStateMachine, assetsContainer, coroutineService);
    }

    public void Exit() { }
}