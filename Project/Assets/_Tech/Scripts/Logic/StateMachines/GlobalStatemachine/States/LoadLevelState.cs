using System;
using UnityEngine;
using Object = UnityEngine.Object;

public class LoadLevelState : IPayloadState<string> {
    private ConfigsContainer _configsContainer;
    private GameStateMachine _globalStatemachine;
    private LevelsLoadingService _levelsLoadingService;

    public LoadLevelState(
        LevelsLoadingService levelsLoadingService, GameStateMachine globalStatemachine, ConfigsContainer configsContainer) {
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
        CharactersGroupContainer charactersGroupContainer = Object.FindObjectOfType<CharactersGroupContainer>();
        SceneAbstractEntitiesMediator abstractEntitiesMediator = new SceneAbstractEntitiesMediator();

        globalGrid.Init();
        battleGridGenerator.Init(globalGrid);

        charactersGroupContainer.Init(
            fieldRaycaster, globalGrid, abstractEntitiesMediator,
            _configsContainer);
        cameraFollower.Init(charactersGroupContainer.MainCharacter.transform, globalGrid.LDPoint, globalGrid.RUPoint);
        fieldRaycaster.Init(mainCamera, globalGrid, charactersGroupContainer);

        BetweenStatesDataContainer statesDataContainer = new BetweenStatesDataContainer(
            fieldRaycaster, charactersGroupContainer, battleGridGenerator,
            cameraFollower);

        EnemyCharacterWalker[] enemyWalkers = Object.FindObjectsOfType<EnemyCharacterWalker>();
        EnemyContainer[] enemyContainers = Object.FindObjectsOfType<EnemyContainer>();

        foreach (EnemyCharacterWalker enemyWalker in enemyWalkers) {
            enemyWalker.Init(fieldRaycaster, abstractEntitiesMediator, _configsContainer);
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