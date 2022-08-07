public class GameService {
    private GameStateMachine _gameStateMachine;

    public GameService(
        ICoroutineService corounieSystem) {
        _gameStateMachine = new GameStateMachine(
            corounieSystem);

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