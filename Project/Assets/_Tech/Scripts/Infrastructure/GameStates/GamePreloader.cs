using UnityEngine;

public class GamePreloader : MonoBehaviour, ICoroutineService {
    [SerializeField] private ConfigsContainer _configsContainer;
    
    private GameService _gameService;

    private void Awake() {
        Application.targetFrameRate = 120;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        DontDestroyOnLoad(gameObject);

        _gameService = new GameService(
            this, _configsContainer);
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