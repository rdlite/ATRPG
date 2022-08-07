using System;
using System.Collections.Generic;

public class GameStateMachine : UpdateStateMachine
{
    public GameStateMachine(
        ICoroutineService coroutineService, ConfigsContainer configsContainer)
    {
        _states = new Dictionary<Type, IExitableState>() {
            [typeof(BootstrapState)] = new BootstrapState(
                this, coroutineService),
            [typeof(LoadLevelState)] = new LoadLevelState(
                ServicesContainer.Instance.Get<LevelsLoadingService>(), this, configsContainer),
            [typeof(WordWalkingState)] = new WordWalkingState(
                this),
            [typeof(BattleState)] = new BattleState()
        };
    }
}

public class BetweenStatesDataContainer {
    public OnFieldRaycaster FieldRaycaster;
    public CharactersGroupContainer CharactersGroupContainer;
    public EnemyCharacterWalker EnemyDetected;
    public BattleGridGenerator BattleGridGenerator;
    public CameraSimpleFollower Camera;

    public BetweenStatesDataContainer(
        OnFieldRaycaster fieldRaycaster, CharactersGroupContainer charactersGroupContainer, BattleGridGenerator battleGridGenerator,
        CameraSimpleFollower camera) {
        Camera = camera;
        CharactersGroupContainer = charactersGroupContainer;
        FieldRaycaster = fieldRaycaster;
        BattleGridGenerator = battleGridGenerator;
    }
}