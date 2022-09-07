using UnityEngine;

public class BattleState : IPayloadState<BetweenStatesDataContainer>, IUpdateState {
    private BetweenStatesDataContainer _sceneData;
    private ConfigsContainer _configsContainer;
    private ICoroutineService _coroutineService;
    private AssetsContainer _assetsContainer;
    private UIRoot _uiRoot;

    public BattleState(
        UIRoot uiRoot, AssetsContainer assetsContainer, ICoroutineService coroutineService,
        ConfigsContainer configsContainer) {
        _configsContainer = configsContainer;
        _coroutineService = coroutineService;
        _assetsContainer = assetsContainer;
        _uiRoot = uiRoot;
    }

    public void Enter(BetweenStatesDataContainer sceneData) {
        _sceneData = sceneData;
        sceneData.CharactersGroupContainer.StopUnits();
        sceneData.BattleGridGenerator.StartBattle(
            sceneData.CharactersGroupContainer, sceneData.EnemyDetected, sceneData.Camera,
            _uiRoot, _assetsContainer, _coroutineService,
            _configsContainer);
        _uiRoot.EnablePanel(UIRoot.UIPanelType.BattleState);
    }

    public void Exit() { }

    public void Update() {
        _sceneData.BattleGridGenerator.Tick();
    }
}