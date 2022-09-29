using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AIMovementResolver {
    private ICoroutineService _coroutineService;
    private CameraSimpleFollower _camera;
    private BattleGridData _battleGridData;
    private BattleHandler _battleHandler;
    private BattleTurnsHandler _turnsHandler;
    private ImposedPairsContainer _imposedPairsContainer;
    private bool _isAIActing;
    private bool _isDebugAIMovementWeights;

    public AIMovementResolver(
        BattleGridData battleGridData, BattleHandler battleHandler, BattleTurnsHandler turnsHandler,
        CameraSimpleFollower camera, ICoroutineService coroutineService, bool isAIActing,
        ImposedPairsContainer imposedPairsContainer, bool isDebugAIMovementWeights) {
        _imposedPairsContainer = imposedPairsContainer;
        _isAIActing = isAIActing;
        _coroutineService = coroutineService;
        _camera = camera;
        _battleGridData = battleGridData;
        _battleHandler = battleHandler;
        _turnsHandler = turnsHandler;
        _isDebugAIMovementWeights = isDebugAIMovementWeights;
    }

    public void MoveUnit(UnitBase characterToMove) {
        _battleHandler.SetEnemyTurn();
        if (!_isAIActing) {
            _coroutineService.StartCoroutine(FakeTurnSequence(characterToMove));
        } else {
            _coroutineService.StartCoroutine(OLD_TurnSequence(characterToMove));
        }
    }

    private IEnumerator MovementRoutine(UnitBase characterToMove, Vector3 endPoint) {
        bool endedWalk = false;
        characterToMove.GoToPoint(endPoint, false, true, null, () => endedWalk = true);
        yield return new WaitWhile(() => !endedWalk);
    }

    private IEnumerator MeleeAttack(UnitBase attacker, UnitBase target, BattleFieldActionAbility ability, bool isImposedAttack) {
        bool isAttackEnded = false;
        _battleHandler.ProcessAIAttack(attacker, new List<UnitBase>() { target }, ability, () => isAttackEnded = true, isImposedAttack);
        yield return new WaitWhile(() => !isAttackEnded);
        yield return new WaitForSeconds(.5f);
    }

    private IEnumerator TurnSequence(UnitBase unitToMove) {
        _battleHandler.SetRestriction(true);
        _camera.SetTarget(unitToMove.transform);

        List<Node> walkPoints = _battleHandler.GetPossibleWalkNodesForUnit(unitToMove);

        yield return new WaitForSeconds(.5f);


    }

    private IEnumerator OLD_TurnSequence(UnitBase unitToMove) {
        _battleHandler.SetRestriction(true);
        _camera.SetTarget(unitToMove.transform);

        List<Node> walkPoints = _battleHandler.GetPossibleWalkNodesForUnit(unitToMove);

        yield return new WaitForSeconds(.5f);

        BattleFieldActionAbility attackAbility = unitToMove.GetUnitAbilitites().Single(ability => ability.Type != AbilityType.Walk);

        if (_imposedPairsContainer.HasPairWith(unitToMove)) {
            UnitBase target = _imposedPairsContainer.GetPairFor(unitToMove);
            yield return _coroutineService.StartCoroutine(MeleeAttack(unitToMove, target, attackAbility, true));
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

            if (_isDebugAIMovementWeights) {
                bool isFirst = false;
                foreach (var item in Object.FindObjectsOfType<TMPro.TextMeshPro>()) {
                    if (!isFirst) {
                        isFirst = true;
                        continue;
                    }
                    Object.Destroy(item.gameObject);
                }

                Object.FindObjectOfType<TMPro.TextMeshPro>().transform.position = Vector3.one * 10000f;
            }

            for (int i = 0; i < unitAttackData.Count; i++) {
                if (unitAttackData[i].Item2.Count > 0) {
                    List<(float, Node)> nodesByTacticalPoints = new List<(float, Node)>(unitAttackData[i].Item2.Count);

                    Node bestNodeToAttackFrom = unitAttackData[i].Item2[0];

                    for (int j = 0; j < unitAttackData[i].Item2.Count; j++) {
                        (bool, float) damageResult = unitAttackData[i].Item1.GetUnitHealthContainer().GedModifiedDamageAmount(unitToMove.GetUnitConfig().DefaultAttackDamage);
                        float tacticalPoints = damageResult.Item1 ? 100000 : damageResult.Item2;

                        Vector3 attackDirection = (unitAttackData[i].Item1.transform.position.RemoveYCoord() - unitAttackData[i].Item2[j].WorldPosition.RemoveYCoord()).normalized;

                        if (_imposedPairsContainer.HasPairWith(unitAttackData[i].Item1)) {
                            tacticalPoints *= 1.2f;

                            bool isAttackFromBehind = _imposedPairsContainer.HasPairWith(unitAttackData[i].Item1) && (Vector3.Dot(attackDirection, unitAttackData[i].Item1.transform.forward.RemoveYCoord()) >= .9f);

                            if (isAttackFromBehind) {
                                tacticalPoints += damageResult.Item2 * .2f;
                            }
                        }

                        // add more tactical points the health is closer to zero
                        float targetUnitHealthCompleteness = unitAttackData[i].Item1.GetUnitHealthContainer().GetHealthCompleteness();
                        float remappedTactitialPointsValue = Mathf.Lerp(tacticalPoints, tacticalPoints * 1.25f, Mathf.InverseLerp(1f, 0f, targetUnitHealthCompleteness));

                        float distToTarget = Vector3.Distance(unitAttackData[i].Item2[j].WorldPosition, unitToMove.transform.position);
                        tacticalPoints -= distToTarget / 8f;

                        nodesByTacticalPoints.Add(new(tacticalPoints, unitAttackData[i].Item2[j]));
                    }

                    float maxTacticalAttackPoints = -Mathf.Infinity;

                    for (int j = 0; j < nodesByTacticalPoints.Count; j++) {
                        if (_isDebugAIMovementWeights) {
                            TMPro.TextMeshPro worldText = Object.Instantiate(Object.FindObjectOfType<TMPro.TextMeshPro>());
                            worldText.transform.position = Vector3.up + nodesByTacticalPoints[j].Item2.WorldPosition;
                            worldText.text = nodesByTacticalPoints[j].Item1.ToString("F1");
                        }

                        if (nodesByTacticalPoints[j].Item1 > maxTacticalAttackPoints) {
                            maxTacticalAttackPoints = nodesByTacticalPoints[j].Item1;
                            bestNodeToAttackFrom = nodesByTacticalPoints[j].Item2;
                        }
                    }

                    targets.Add(new(maxTacticalAttackPoints, unitAttackData[i].Item1, bestNodeToAttackFrom));
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

            yield return _coroutineService.StartCoroutine(MeleeAttack(unitToMove, target.Item2, attackAbility, false));
        }

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