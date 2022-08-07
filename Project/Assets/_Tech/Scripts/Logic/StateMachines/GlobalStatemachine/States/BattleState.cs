using UnityEngine;

public class BattleState : IPayloadState<BetweenStatesDataContainer>, IUpdateState {
    private BetweenStatesDataContainer _sceneData;
    private UIRoot _uiRoot;

    public BattleState(UIRoot uiRoot) {
        _uiRoot = uiRoot;
    }

    public void Enter(BetweenStatesDataContainer sceneData) {
        _sceneData = sceneData;
        sceneData.CharactersGroupContainer.StopCharacters();
        sceneData.BattleGridGenerator.StartBattle(
            sceneData.CharactersGroupContainer, sceneData.EnemyDetected, sceneData.Camera,
            _uiRoot);
        _uiRoot.EnablePanel(UIRoot.UIPanelType.BattleState);
    }

    public void Exit() { }

    public void Update() {
        _sceneData.BattleGridGenerator.Tick();
    }
}