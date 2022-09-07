using System;
using System.Collections.Generic;

public class GameStateMachine : UpdateStateMachine
{
    public GameStateMachine(
        ICoroutineService coroutineService, ConfigsContainer configsContainer, UIRoot uiRoot,
        AssetsContainer assetsContainer)
    {
        _states = new Dictionary<Type, IExitableState>() {
            [typeof(BootstrapState)] = new BootstrapState(
                this, coroutineService, uiRoot,
                assetsContainer),
            [typeof(LoadLevelState)] = new LoadLevelState(
                ServicesContainer.Instance.Get<LevelsLoadingService>(), this, configsContainer,
                assetsContainer, coroutineService),
            [typeof(WordWalkingState)] = new WordWalkingState(
                this, uiRoot),
            [typeof(BattleState)] = new BattleState(
                uiRoot, assetsContainer, coroutineService,
                configsContainer)
        };
    }
}

public class BetweenStatesDataContainer {
    public OnFieldRaycaster FieldRaycaster;
    public PlayerUnitsGroupContainer CharactersGroupContainer;
    public EnemyUnit EnemyDetected;
    public BattleGridGenerator BattleGridGenerator;
    public CameraSimpleFollower Camera;

    public BetweenStatesDataContainer(
        OnFieldRaycaster fieldRaycaster, PlayerUnitsGroupContainer charactersGroupContainer, BattleGridGenerator battleGridGenerator,
        CameraSimpleFollower camera) {
        Camera = camera;
        CharactersGroupContainer = charactersGroupContainer;
        FieldRaycaster = fieldRaycaster;
        BattleGridGenerator = battleGridGenerator;
    }
}