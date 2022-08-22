using System;
using UnityEngine;
using Object = UnityEngine.Object;

public class LoadLevelState : IPayloadState<string> {
    private ICoroutineService _coroutineService;
    private AssetsContainer _assetsContainer;
    private ConfigsContainer _configsContainer;
    private GameStateMachine _globalStatemachine;
    private LevelsLoadingService _levelsLoadingService;

    public LoadLevelState(
        LevelsLoadingService levelsLoadingService, GameStateMachine globalStatemachine, ConfigsContainer configsContainer,
         AssetsContainer assetsContainer, ICoroutineService coroutineService) {
        _coroutineService = coroutineService;
        _assetsContainer = assetsContainer;
        _configsContainer = configsContainer;
        _globalStatemachine = globalStatemachine;
        _levelsLoadingService = levelsLoadingService;
    }

    public void Enter(string sceneName) {
        LoadNextLevel(sceneName, InitLoadedLevel);
    }

    private void LoadNextLevel(string sceneName, Action callback) {
        _levelsLoadingService.LoadSceneAsync(sceneName, callback);
    }

    private void InitLoadedLevel() {
        QualitySettings.SetQualityLevel(3);

        BattleGridGenerator battleGridGenerator = Object.FindObjectOfType<BattleGridGenerator>();
        AStarGrid globalGrid = Object.FindObjectOfType<AStarGrid>();
        OnFieldRaycaster fieldRaycaster = Object.FindObjectOfType<OnFieldRaycaster>();
        CameraSimpleFollower cameraFollower = Object.FindObjectOfType<CameraSimpleFollower>();
        Camera mainCamera = Camera.main;
        PlayerUnitsGroupContainer charactersGroupContainer = Object.FindObjectOfType<PlayerUnitsGroupContainer>();
        SceneAbstractEntitiesMediator abstractEntitiesMediator = new SceneAbstractEntitiesMediator();

        globalGrid.Init();
        battleGridGenerator.Init(globalGrid);

        charactersGroupContainer.Init(
            fieldRaycaster, globalGrid, abstractEntitiesMediator,
            _configsContainer, _assetsContainer, cameraFollower,
            _coroutineService);
        cameraFollower.Init(charactersGroupContainer.MainCharacter.transform, globalGrid.LDPoint, globalGrid.RUPoint);
        fieldRaycaster.Init(mainCamera, globalGrid, charactersGroupContainer);

        BetweenStatesDataContainer statesDataContainer = new BetweenStatesDataContainer(
            fieldRaycaster, charactersGroupContainer, battleGridGenerator,
            cameraFollower);

        EnemyUnit[] enemyWalkers = Object.FindObjectsOfType<EnemyUnit>();
        EnemyContainer[] enemyContainers = Object.FindObjectsOfType<EnemyContainer>();

        foreach (EnemyUnit enemyWalker in enemyWalkers) {
            enemyWalker.Init(
                fieldRaycaster, abstractEntitiesMediator, _configsContainer,
                _assetsContainer, cameraFollower, _coroutineService);
        }
        
        foreach (EnemyContainer enemyContainer in enemyContainers) {
            enemyContainer.Init();
        }

        abstractEntitiesMediator.Init(
            globalGrid, _globalStatemachine, statesDataContainer);

        _globalStatemachine.Enter<WordWalkingState, BetweenStatesDataContainer>(statesDataContainer);
    }

    public void Exit() { }
}

public enum LevelLoadType {
    LastSaved, Restart, Next, First
}