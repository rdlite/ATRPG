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
    private UpdateStateMachine _battleSM;
    private ImposedPairsContainer _imposedPairsContainer;
    private bool _isAIActing;
    private bool _isDebugAIMovementWeights;

    public AIMovementResolver(
        BattleGridData battleGridData, BattleHandler battleHandler, BattleTurnsHandler turnsHandler,
        CameraSimpleFollower camera, ICoroutineService coroutineService, bool isAIActing,
        ImposedPairsContainer imposedPairsContainer, bool isDebugAIMovementWeights, UpdateStateMachine battleSM) {
        _battleSM = battleSM;
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
        if (!_isAIActing) {
            _coroutineService.StartCoroutine(FakeTurnSequence(characterToMove));
        } else {
            _coroutineService.StartCoroutine(TurnSequence(characterToMove));
        }
    }

    private IEnumerator MovementRoutine(UnitBase unitToMove, Vector3 endPoint) {
        Node startMovementNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(unitToMove.transform.position);
        Node endMovementNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(endPoint);
        _turnsHandler.SetCurrentWalker(unitToMove);
        _turnsHandler.RemovePossibleLengthForUnit(unitToMove, _battleGridData.GlobalGrid.GetPathLength(startMovementNode, endMovementNode));
        bool endedWalk = false;
        unitToMove.GoToPoint(endPoint, false, true, null, () => endedWalk = true);

        yield return new WaitWhile(() => !endedWalk);
    }

    private IEnumerator MeleeAttack(UnitBase attacker, UnitBase target, BattleFieldActionAbility ability, bool isImposedAttack) {
        bool isAttackEnded = false;
        //_battleHandler.ProcessAIAttack(attacker, new List<UnitBase>() { target }, ability, () => isAttackEnded = true, isImposedAttack);
        yield return new WaitWhile(() => !isAttackEnded);
        yield return new WaitForSeconds(.5f);
    }

    private IEnumerator TurnSequence(UnitBase unitToMove) {
        _battleHandler.StartFocusCamera(unitToMove.transform, () => _battleSM.Enter<AIMovementState>());

        yield return new WaitForSeconds(1f);

        AIAction bestAIAction = new AIAction();
        List<AIAction> possibleAIActions = new List<AIAction>(unitToMove.GetUnitAbilitites().Count);

        bool hasNoAbilities = unitToMove.GetUnitAbilitites().Count == 0;

        while (!hasNoAbilities) {
            List<Node> walkPoints = _battleHandler.GetPossibleWalkNodesForUnitAndSetField(unitToMove);

            foreach (var ability in unitToMove.GetUnitAbilitites()) {
                if (ability.Type == AbilityType.Walk && _turnsHandler.IsUnitHaveLengthToMove(unitToMove)) {
                    AIAction newAction = new AIAction();
                    yield return _coroutineService.StartCoroutine(GetWalkingBestTurn(walkPoints, unitToMove, newAction));
                    possibleAIActions.Add(newAction);
                }

                if (possibleAIActions.Count > 0) {
                    bestAIAction = possibleAIActions[0];

                    float bestActionPointsAmount = -Mathf.Infinity;
                    int removeAtPossibleActions = 0;

                    for (int i = 0; i < possibleAIActions.Count; i++) {
                        if (bestActionPointsAmount > possibleAIActions[i].Points) {
                            EqualizeTwoActions(bestAIAction, possibleAIActions[i]);
                            removeAtPossibleActions = i;
                        }
                    }

                    possibleAIActions.RemoveAt(removeAtPossibleActions);

                    yield return _coroutineService.StartCoroutine(ImplementAIAction(unitToMove, bestAIAction));
                }

                yield return null;
            }

            if (possibleAIActions.Count == 0) {
                hasNoAbilities = true;
            } else {
                possibleAIActions.Clear();
            }
        }

        // пробежатьс€ по всем доступным персонажу абилкам
        // действи€ дл€ каждого движени€ уникальные - разбить по отдельным рутинам
        // движение к ближайшей если нет врагов в ренже
        // дл€ атаки один на один 
            // провер€ть импозед, в общем-то вытащить полностью из текущего алгоритма
            // если нет целей в ренже атаки, то ирдти к ближайшей к врагу ноде (сделать универсальную функцию поиска дл€ переиспользовани€)
            // вообще создать юнита с секирой
        // дл€ атаки мили-ренж
            // провер€ть импозед, если да, то просто атаковать
            // дл€ каждой точки с радиусом до врага смотреть атаку в 8 сторон
            // прибавл€ть очки по урону по врагам, уменьшать по урону по союзникам
            // провер€ть заебываетс€ ли юнит в импозед, если нет, то прибавл€ть к общему числу, то есть отдавать приоритет атакам не вовлекающим в бой
            // если не может ни по кому попасть, то двигать к ближайшей к врагу ноде
        // записать пройденное действие в словарь
        // спрашивать делал ли юнит это действие за этот ход
        // продолжить цикл по возможным абилкам, если нет, то заканчивать ход

        _turnsHandler.AIEndedTurn();
    }

    private IEnumerator ImplementAIAction(UnitBase unit, AIAction aIAction) {
        if (!aIAction.IsAttack) {
            yield return _coroutineService.StartCoroutine(MovementRoutine(unit, aIAction.EndNodeWalking.WorldPosition));
            yield return new WaitForSeconds(1f);
        }
    }

    private IEnumerator GetWalkingBestTurn(List<Node> walkPoints, UnitBase unitToMove, AIAction bestAIAction) {
        AIAction bestWalkingAction = new AIAction();
        AIAction worstWalkingAction = new AIAction();
        float bestTacticalPoints = -Mathf.Infinity;
        float worstTacticalPoints = Mathf.Infinity;

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

        foreach (Node node in walkPoints) {
            if (!node.CheckWalkability || node == _battleGridData.GlobalGrid.GetNodeFromWorldPoint(unitToMove.transform.position)) {
                continue;
            }

            float summaryDistanceToUnits = 0f;
            int unitsAmount = 0;
            for (int i = 0; i < _battleGridData.Units.Count; i++) {
                if (unitToMove != _battleGridData.Units[i] && !_battleGridData.Units[i].IsDeadOnBattleField && unitToMove.GetType() != _battleGridData.Units[i].GetType()) {
                    summaryDistanceToUnits += Vector3.Distance(node.WorldPosition.RemoveYCoord(), _battleGridData.Units[i].transform.position.RemoveYCoord());
                    unitsAmount++;
                }
            }

            float tacticalPointForCurrentNode = (float)unitsAmount / summaryDistanceToUnits * Random.Range(.9f, 1.1f) * 10f;

            if (_isDebugAIMovementWeights) {
                TMPro.TextMeshPro worldText = Object.Instantiate(Object.FindObjectOfType<TMPro.TextMeshPro>());
                worldText.transform.position = Vector3.up + node.WorldPosition;
                worldText.text = tacticalPointForCurrentNode.ToString("F1");
            }

            if (tacticalPointForCurrentNode < worstTacticalPoints) {
                worstTacticalPoints = tacticalPointForCurrentNode;
                worstWalkingAction.Points = worstTacticalPoints;
                worstWalkingAction.EndNodeWalking = node;
            }

            if (tacticalPointForCurrentNode > bestTacticalPoints) {
                bestTacticalPoints = tacticalPointForCurrentNode;
                bestWalkingAction.Points = bestTacticalPoints;
                bestWalkingAction.EndNodeWalking = node;
            }
        }

        yield return null;

        EqualizeTwoActions(bestAIAction, bestWalkingAction);
    }

    private IEnumerator OLD_TurnSequence(UnitBase unitToMove) {
        _battleHandler.StartFocusCamera(unitToMove.transform, () => _battleSM.Enter<AIMovementState>());

        List<Node> walkPoints = _battleHandler.GetPossibleWalkNodesForUnitAndSetField(unitToMove);

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
    }

    private IEnumerator FakeTurnSequence(UnitBase unitToMove) {
        _battleHandler.StartFocusCamera(unitToMove.transform, () => _battleSM.Enter<AIMovementState>());

        yield return new WaitForSeconds(1f);

        _turnsHandler.AIEndedTurn();
    }

    private void EqualizeTwoActions(AIAction action1, AIAction action2) {
        action1.Points = action2.Points;
        action1.EndNodeWalking = action2.EndNodeWalking;
        action1.IsAttack = action2.IsAttack;
        action1.Target = action2.Target;
        action1.AttackDirection = action2.AttackDirection;
        action1.Ability = action2.Ability;
    }

    private class AIAction {
        public float Points = -100000f;
        public Node EndNodeWalking;
        public bool IsAttack;
        public UnitBase Target;
        public Vector3 AttackDirection;
        public BattleFieldActionAbility Ability;
    }
}