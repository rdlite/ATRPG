using System.Collections;
using UnityEngine;

public class AIMovementResolver {
    private ICoroutineService _coroutineService;
    private CameraSimpleFollower _camera;
    private BattleGridData _battleGridData;
    private BattleHandler _battleHandler;
    private BattleTurnsHandler _turnsHandler;

    public AIMovementResolver(
        BattleGridData battleGridData, BattleHandler battleHandler, BattleTurnsHandler turnsHandler,
        CameraSimpleFollower camera, ICoroutineService coroutineService) {
        _coroutineService = coroutineService;
        _camera = camera;
        _battleGridData = battleGridData;
        _battleHandler = battleHandler;
        _turnsHandler = turnsHandler;
    }

    public void MoveUnit(UnitBase characterToMove) {
        _battleHandler.SetEnemyTurn();
        _coroutineService.StartCoroutine(TurnSequence(characterToMove));
    }

    private IEnumerator TurnSequence(UnitBase characterToMove) {
        _battleHandler.SetRestriction(true);
        _camera.SetTarget(characterToMove.transform);

        yield return new WaitForSeconds(1f);

        _turnsHandler.AIEndedTurn();
        _battleHandler.SetRestriction(false);
    }
}