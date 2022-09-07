using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIMovementResolver {
    private ICoroutineService _coroutineService;
    private CameraSimpleFollower _camera;
    private BattleGridData _battleGridData;
    private BattleHandler _battleHandler;
    private BattleTurnsHandler _turnsHandler;
    private ImposedPairsContainer _imposedPairsContainer;
    private bool _isAIActing;

    public AIMovementResolver(
        BattleGridData battleGridData, BattleHandler battleHandler, BattleTurnsHandler turnsHandler,
        CameraSimpleFollower camera, ICoroutineService coroutineService, bool isAIActing,
        ImposedPairsContainer imposedPairsContainer) {
        _imposedPairsContainer = imposedPairsContainer;
        _isAIActing = isAIActing;
        _coroutineService = coroutineService;
        _camera = camera;
        _battleGridData = battleGridData;
        _battleHandler = battleHandler;
        _turnsHandler = turnsHandler;
    }

    public void MoveUnit(UnitBase characterToMove) {
        _battleHandler.SetEnemyTurn();
        if (!_isAIActing) {
            _coroutineService.StartCoroutine(FakeTurnSequence(characterToMove));
        } else {
            _coroutineService.StartCoroutine(TurnSequence(characterToMove));
        }
    }

    private IEnumerator MovementRoutine(UnitBase characterToMove, Vector3 endPoint) {
        bool endedWalk = false;
        characterToMove.GoToPoint(endPoint, false, true, null, () => endedWalk = true);
        yield return new WaitWhile(() => !endedWalk);
    }

    private IEnumerator MeleeAttack(UnitBase attacker, UnitBase target, bool isImposedAttack) {
        bool isAttackEnded = false;
        _battleHandler.ProcessAIAttack(attacker, target, () => isAttackEnded = true, isImposedAttack);
        yield return new WaitWhile(() => !isAttackEnded);
        yield return new WaitForSeconds(.5f);
    }

    private IEnumerator TurnSequence(UnitBase unitToMove) {
        _battleHandler.SetRestriction(true);
        _camera.SetTarget(unitToMove.transform);

        List<Node> walkPoints = _battleHandler.GetPossibleWalkNodesForUnit(unitToMove);

        yield return new WaitForSeconds(.5f);

        if (_imposedPairsContainer.HasPairWith(unitToMove)) {
            UnitBase target = _imposedPairsContainer.GetPairFor(unitToMove);
            yield return _coroutineService.StartCoroutine(MeleeAttack(unitToMove, target, true));
        } else {
            Node currentUnitNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(unitToMove.transform.position);

            List<(UnitBase, List<Node>)> unitAttackData = new List<(UnitBase, List<Node>)>();

            for (int j = 0; j < _battleGridData.Units.Count; j++) {
                if (!_battleGridData.Units[j].IsDeadOnBattleField && _battleGridData.Units[j] != unitToMove && _battleGridData.Units[j] is PlayerUnit) {
                    Node targetUnitNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(_battleGridData.Units[j].transform.position);

                    unitAttackData.Add(new(_battleGridData.Units[j], new List<Node>()));

                    for (int x = -1; x < 2; x++) {
                        for (int y = -1; y < 2; y++) {
                            if (x == 0 || y == 0) {
                                if (x == 0 && y == 0) {
                                    continue;
                                }

                                Node nodeToAdd = _battleGridData.GlobalGrid.GetNodesGrid()[targetUnitNode.GridX + x, targetUnitNode.GridY + y];

                                if (walkPoints.Contains(nodeToAdd)) {
                                    unitAttackData[^1].Item2.Add(nodeToAdd);
                                }
                            }
                        }
                    }
                }
            }

            List<(float, UnitBase, Node)> targets = new List<(float, UnitBase, Node)>();

            for (int i = 0; i < unitAttackData.Count; i++) {
                if (unitAttackData[i].Item2.Count > 0) {
                    (bool, float) result = unitAttackData[i].Item1.GetUnitHealthContainer().GedModifiedDamageAmount(unitToMove.GetUnitConfig().DefaultAttackDamage);
                    float tacticalPoints = result.Item1 ? 100000 : result.Item2;

                    Node randomWalkNode = unitAttackData[i].Item2[Random.Range(0, unitAttackData[i].Item2.Count)];

                    targets.Add(new(tacticalPoints, unitAttackData[i].Item1, randomWalkNode));
                }
            }

            float maxTacticalPoints = -Mathf.Infinity;
            (float, UnitBase, Node) target = (maxTacticalPoints, null, null);
            for (int i = 0; i < targets.Count; i++) {
                if (targets[i].Item1 > maxTacticalPoints) {
                    maxTacticalPoints = targets[i].Item1;
                    target = targets[i];
                }
            }

            if (target.Item3 != _battleGridData.GlobalGrid.GetNodeFromWorldPoint(unitToMove.transform.position)) {
                yield return _coroutineService.StartCoroutine(MovementRoutine(unitToMove, target.Item3.WorldPosition));
            }

            yield return _coroutineService.StartCoroutine(MeleeAttack(unitToMove, target.Item2, false));
        }

        // засунуть проверку на атаку

        _turnsHandler.AIEndedTurn();
        _battleHandler.SetRestriction(false);
    }

    private IEnumerator FakeTurnSequence(UnitBase characterToMove) {
        _battleHandler.SetRestriction(true);
        _camera.SetTarget(characterToMove.transform);

        yield return new WaitForSeconds(1f);

        _turnsHandler.AIEndedTurn();
        _battleHandler.SetRestriction(false);
    }
}