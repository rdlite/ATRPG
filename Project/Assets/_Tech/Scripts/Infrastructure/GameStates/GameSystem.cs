public class GameService {
    private GameStateMachine _gameStateMachine;

    public GameService(
        ICoroutineService corounieSystem, ConfigsContainer configsContainer, UIRoot uiRoot,
        AssetsContainer assetsContainer) {
        _gameStateMachine = new GameStateMachine(
            corounieSystem, configsContainer, uiRoot,
            assetsContainer);

        _gameStateMachine.Enter<BootstrapState>();
    }

    public void GameUpdate() {
        _gameStateMachine.UpdateState();
    }

    public void GameFixedUpdate() {
        _gameStateMachine.FixedUpdateState();
    }

    public void GameLateUpdate() {
        _gameStateMachine.LateUpdateState();
    }
}