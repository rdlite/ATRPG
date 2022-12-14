using UnityEngine;

public class GamePreloader : MonoBehaviour, ICoroutineService
{
    [SerializeField] private ConfigsContainer _configsContainer;
    [SerializeField] private AssetsContainer _assetsContaner;
    [SerializeField] private UIRoot _uiRoot;

    private GameService _gameService;

    private void Awake()
    {
        Application.targetFrameRate = 120;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        _uiRoot = Instantiate(_uiRoot);
        DontDestroyOnLoad(_uiRoot.gameObject);

        DontDestroyOnLoad(gameObject);

        _gameService = new GameService(
            this, _configsContainer, _uiRoot,
            _assetsContaner);

        //Time.timeScale = 4f;
    }

    private void Update()
    {
        _gameService.GameUpdate();
        if (Input.GetKeyDown(KeyCode.F))
        {
            Application.targetFrameRate -= 10;
            if (Application.targetFrameRate <= 10)
            {
                Application.targetFrameRate = 120;
            }
            print(Application.targetFrameRate);
        }
    }

    private void FixedUpdate()
    {
        _gameService.GameFixedUpdate();
    }

    private void LateUpdate()
    {
        _gameService.GameLateUpdate();
    }
}