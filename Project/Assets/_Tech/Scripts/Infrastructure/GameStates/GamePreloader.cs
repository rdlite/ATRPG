using UnityEngine;

public class GamePreloader : MonoBehaviour, ICoroutineService {
    private GameService _gameService;

    private void Awake() {
        Application.targetFrameRate = 120;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        DontDestroyOnLoad(gameObject);

        _gameService = new GameService(
            this);
    }

    private void Update() {
        _gameService.GameUpdate();
    }

    private void FixedUpdate() {
        _gameService.GameFixedUpdate();
    }

    private void LateUpdate() {
        _gameService.GameLateUpdate();
    }
}