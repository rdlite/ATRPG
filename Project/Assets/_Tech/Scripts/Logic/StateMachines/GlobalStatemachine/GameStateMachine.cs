using System;
using System.Collections.Generic;

public class GameStateMachine : UpdateStateMachine
{
    public GameStateMachine(
        ICoroutineService coroutineService)
    {
        _states = new Dictionary<Type, IExitableState>() {
            [typeof(BootstrapState)] = new BootstrapState(
                this, coroutineService),
            [typeof(LoadLevelState)] = new LoadLevelState(
                ServicesContainer.Instance.Get<LevelsLoadingService>(), this),
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

    public BetweenStatesDataContainer(OnFieldRaycaster fieldRaycaster, CharactersGroupContainer charactersGroupContainer) {
        CharactersGroupContainer = charactersGroupContainer;
        FieldRaycaster = fieldRaycaster;
    }
}