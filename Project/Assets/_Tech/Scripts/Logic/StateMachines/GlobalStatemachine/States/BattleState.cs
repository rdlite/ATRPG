using UnityEngine;

public class BattleState : IPayloadState<BetweenStatesDataContainer>, IUpdateState {
    private BetweenStatesDataContainer _sceneData;
    private AssetsContainer _assetsContainer;
    private UIRoot _uiRoot;

    public BattleState(UIRoot uiRoot, AssetsContainer assetsContainer) {
        _assetsContainer = assetsContainer;
        _uiRoot = uiRoot;
    }

    public void Enter(BetweenStatesDataContainer sceneData) {
        _sceneData = sceneData;
        sceneData.CharactersGroupContainer.StopCharacters();
        sceneData.BattleGridGenerator.StartBattle(
            sceneData.CharactersGroupContainer, sceneData.EnemyDetected, sceneData.Camera,
            _uiRoot, _assetsContainer);
        _uiRoot.EnablePanel(UIRoot.UIPanelType.BattleState);
    }

    public void Exit() { }

    public void Update() {
        _sceneData.BattleGridGenerator.Tick();
    }
}