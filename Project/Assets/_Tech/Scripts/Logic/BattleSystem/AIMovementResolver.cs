using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AIMovementResolver
{
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
        ImposedPairsContainer imposedPairsContainer, bool isDebugAIMovementWeights, UpdateStateMachine battleSM)
    {
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

    public void MoveUnit(UnitBase characterToMove)
    {
        if (!_isAIActing)
        {
            _coroutineService.StartCoroutine(FakeTurnSequence(characterToMove));
        }
        else
        {
            _coroutineService.StartCoroutine(TurnSequence(characterToMove));
        }
    }

    private IEnumerator MovementRoutine(UnitBase unitToMove, Vector3 endPoint)
    {
        Node startMovementNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(unitToMove.transform.position);
        Node endMovementNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(endPoint);
        _turnsHandler.SetCurrentWalker(unitToMove);
        _turnsHandler.RemovePossibleLengthForUnit(unitToMove, _battleGridData.GlobalGrid.GetPathLength(startMovementNode, endMovementNode));
        bool endedWalk = false;
        unitToMove.GoToPoint(endPoint, false, true, null, () => endedWalk = true);

        yield return new WaitWhile(() => !endedWalk);
    }

    private IEnumerator MeleeAttack(UnitBase attacker, List<UnitBase> targets, BattleFieldActionAbility ability, bool isImposedAttack)
    {
        bool isAttackEnded = false;

        _turnsHandler.UnitUsedAbility(attacker, ability);

        _battleSM.Enter<AttackSequenceState, (UnitBase, List<UnitBase>, BattleFieldActionAbility, bool, System.Action callback)>(
            (attacker, targets, ability, isImposedAttack,
            () =>
            {
                _battleSM.Enter<AIMovementState>();
                isAttackEnded = true;
            }
        ));

        yield return new WaitWhile(() => !isAttackEnded);
        yield return new WaitForSeconds(.5f);
    }

    private IEnumerator TurnSequence(UnitBase unitToMove)
    {
        _battleHandler.StartFocusCamera(unitToMove.transform, () => _battleSM.Enter<AIMovementState>());

        yield return new WaitForSeconds(1f);

        AIAction bestAIAction = new AIAction();
        List<AIAction> possibleAIActions = new List<AIAction>(unitToMove.GetUnitAbilitites().Count);

        bool hasNoAbilities = unitToMove.GetUnitAbilitites().Count == 0;

        bool isAlreadyMoved = false;

        while (!hasNoAbilities)
        {
            foreach (var ability in unitToMove.GetUnitAbilitites())
            {
                List<Node> walkPoints = _battleHandler.GetPossibleWalkNodesForUnitAndSetField(unitToMove, false);

                if (!isAlreadyMoved && ability.Type == AbilityType.Walk && _turnsHandler.IsUnitHaveLengthToMove(unitToMove) && !_imposedPairsContainer.HasPairWith(unitToMove))
                {
                    AIAction newAction = new AIAction();

                    yield return _coroutineService.StartCoroutine(GetWalkingBestTurn(walkPoints, unitToMove, newAction, ability));

                    possibleAIActions.Add(newAction);
                }
                else if (ability.Type == AbilityType.MeleeOneToOneAttack && !_turnsHandler.IsUnitUsedAbility(unitToMove, ability))
                {
                    AIAction newAction = new AIAction();

                    yield return _coroutineService.StartCoroutine(GetOneToOneMeleeAttackBestTurn(walkPoints, unitToMove, newAction, ability));

                    if (newAction.IsCanBeUsed)
                    {
                        possibleAIActions.Add(newAction);
                    }
                }
                else if (ability.Type == AbilityType.MeleeRangeAttack && !_turnsHandler.IsUnitUsedAbility(unitToMove, ability))
                {
                    AIAction newAction = new AIAction();

                    yield return _coroutineService.StartCoroutine(GetMeleeRangeAttackBestTurn(walkPoints, unitToMove, newAction, ability));

                    if (/*newAction.Points >= 0f &&*/ newAction.IsCanBeUsed)
                    {
                        possibleAIActions.Add(newAction);
                    }
                }

                yield return null;
            }

            if (possibleAIActions.Count > 0)
            {
                bestAIAction = possibleAIActions[0];

                float bestActionPointsAmount = -Mathf.Infinity;

                for (int i = 0; i < possibleAIActions.Count; i++)
                {
                    if (bestActionPointsAmount < possibleAIActions[i].Points)
                    {
                        bestActionPointsAmount = possibleAIActions[i].Points;
                        EqualizeTwoActions(bestAIAction, possibleAIActions[i]);
                        if (bestAIAction.Ability.Type == AbilityType.Walk)
                        {
                            isAlreadyMoved = true;
                        }
                    }
                }

                yield return _coroutineService.StartCoroutine(ImplementAIAction(unitToMove, bestAIAction));
            }

            if (possibleAIActions.Count == 0)
            {
                hasNoAbilities = true;
            }
            else
            {
                possibleAIActions.Clear();
            }
        }

        _turnsHandler.AIEndedTurn();
    }

    private IEnumerator ImplementAIAction(UnitBase unit, AIAction aIAction)
    {
        if (!aIAction.IsAttack)
        {
            yield return _coroutineService.StartCoroutine(MovementRoutine(unit, aIAction.EndNodeWalking.WorldPosition));
        }
        else
        {
            if (aIAction.EndNodeWalking != _battleGridData.GlobalGrid.GetNodeFromWorldPoint(unit.transform.position))
            {
                yield return _coroutineService.StartCoroutine(MovementRoutine(unit, aIAction.EndNodeWalking.WorldPosition));

                if (aIAction.Targets.Count > 1)
                {
                    while (Vector3.Dot(unit.transform.forward, aIAction.AttackDirection) < .95)
                    {
                        unit.transform.rotation = Quaternion.Slerp(unit.transform.rotation, Quaternion.LookRotation(aIAction.AttackDirection, Vector3.up), 10f * Time.deltaTime);
                        yield return null;
                    }
                }
            }

            yield return _coroutineService.StartCoroutine(MeleeAttack(unit, aIAction.Targets, aIAction.Ability, false));
        }

        yield return new WaitForSeconds(1f);
    }

    private IEnumerator GetWalkingBestTurn(List<Node> walkPoints, UnitBase unitToMove, AIAction bestAIAction, BattleFieldActionAbility actionAbility)
    {
        AIAction bestWalkingAction = new AIAction();
        AIAction worstWalkingAction = new AIAction();
        float bestTacticalPoints = -Mathf.Infinity;
        float worstTacticalPoints = Mathf.Infinity;

        if (_isDebugAIMovementWeights)
        {
            bool isFirst = false;
            foreach (var item in Object.FindObjectsOfType<TMPro.TextMeshPro>())
            {
                if (!isFirst)
                {
                    isFirst = true;
                    continue;
                }
                Object.Destroy(item.gameObject);
            }

            Object.FindObjectOfType<TMPro.TextMeshPro>().transform.position = Vector3.one * 10000f;
        }

        foreach (Node node in walkPoints)
        {
            if (!node.CheckWalkability || node == _battleGridData.GlobalGrid.GetNodeFromWorldPoint(unitToMove.transform.position))
            {
                continue;
            }

            float summaryDistanceToUnits = 0f;
            int unitsAmount = 0;
            for (int i = 0; i < _battleGridData.Units.Count; i++)
            {
                if (unitToMove != _battleGridData.Units[i] && !_battleGridData.Units[i].IsDeadOnBattleField && unitToMove.GetType() != _battleGridData.Units[i].GetType())
                {
                    summaryDistanceToUnits += Vector3.Distance(node.WorldPosition.RemoveYCoord(), _battleGridData.Units[i].transform.position.RemoveYCoord());
                    unitsAmount++;
                }
            }

            float tacticalPointForCurrentNode = (float)unitsAmount / summaryDistanceToUnits * Random.Range(.9f, 1.1f) * 10f;

            if (_isDebugAIMovementWeights)
            {
                TMPro.TextMeshPro worldText = Object.Instantiate(Object.FindObjectOfType<TMPro.TextMeshPro>());
                worldText.transform.position = Vector3.up + node.WorldPosition;
                worldText.text = tacticalPointForCurrentNode.ToString("F1");
            }

            if (tacticalPointForCurrentNode < worstTacticalPoints)
            {
                worstTacticalPoints = tacticalPointForCurrentNode;
                worstWalkingAction.Points = worstTacticalPoints;
                worstWalkingAction.EndNodeWalking = node;
            }

            if (tacticalPointForCurrentNode > bestTacticalPoints)
            {
                bestTacticalPoints = tacticalPointForCurrentNode;
                bestWalkingAction.Points = bestTacticalPoints;
                bestWalkingAction.EndNodeWalking = node;
            }
        }

        bestWalkingAction.IsCanBeUsed = true;

        yield return null;

        EqualizeTwoActions(bestAIAction, bestWalkingAction);
        bestAIAction.Ability = actionAbility;
    }

    private IEnumerator GetMeleeRangeAttackBestTurn(List<Node> walkPoints, UnitBase unitToMove, AIAction bestAIAction, BattleFieldActionAbility attackAbility)
    {
        float attackRange = 3f;
        float attackAngle = 100f;

        if (_imposedPairsContainer.HasPairWith(unitToMove))
        {
            List<UnitBase> attackUnits = _battleHandler.GetUnitsWithinAttackRaduisOfMeleeRangeWeapon(unitToMove.transform.position, unitToMove.transform.forward, attackRange, attackAngle);
            float damageToGive = 0f;
            foreach (UnitBase unitInRange in attackUnits)
            {
                float sign = unitInRange.GetType() != unitToMove.GetType() ? 1f : -1f;

                damageToGive += sign * GetDamageOnTarget(unitToMove, unitInRange, attackAbility, false);
            }

            SetAttackActionTemplate(bestAIAction, damageToGive, _battleGridData.GlobalGrid.GetNodeFromWorldPoint(unitToMove.transform.position), attackUnits, unitToMove.transform.forward, attackAbility);

            yield break;
        }

        Node attackerNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(unitToMove.transform.position);
        if (walkPoints == null || walkPoints.Count == 0)
        {
            walkPoints = new List<Node>();
            walkPoints.Add(attackerNode);
        }

        List<(Node, List<UnitBase>, Vector3, float)> unitsAttackData = new List<(Node, List<UnitBase>, Vector3, float)>(walkPoints.Count * 8);

        int callsAmount = 0;

        foreach (Node nodeAttackFrom in walkPoints)
        {
            for (int x = -1; x < 2; x++)
            {
                for (int y = -1; y < 2; y++)
                {
                    if (x == 0 && y == 0) continue;

                    Vector3 attackDir = new Vector3(x, 0f, y).normalized;

                    List<UnitBase> attackUnits = _battleHandler.GetUnitsWithinAttackRaduisOfMeleeRangeWeapon(nodeAttackFrom.WorldPosition, attackDir, attackRange, attackAngle);

                    if (attackUnits.Count != 0)
                    {
                        float damageToGive = 0f;
                        foreach (UnitBase unitInRange in attackUnits)
                        {
                            float sign = unitInRange.GetType() != unitToMove.GetType() ? 1f : -1f;

                            damageToGive += sign * GetDamageOnTarget(unitToMove, unitInRange, attackAbility, false);
                        }
                        unitsAttackData.Add((nodeAttackFrom, attackUnits, attackDir, damageToGive));
                    }
                }
            }

            callsAmount++;
            if (callsAmount % 80 == 0)
            {
                yield return null;
            }
        }

        if (unitsAttackData.Count != 0)
        {
            float maxPoints = -Mathf.Infinity;
            (Node, List<UnitBase>, Vector3, float) bestWayToAttack = unitsAttackData[0];

            for (int i = 0; i < unitsAttackData.Count; i++)
            {
                bool isCanBeImposed = _battleHandler.IsCanImposeWithCollection(unitToMove, bestWayToAttack.Item2);

                if ((unitsAttackData[i].Item4 * (isCanBeImposed ? .5f : 1f)) > maxPoints)
                {
                    maxPoints = unitsAttackData[i].Item4 * (isCanBeImposed ? .5f : 1f);
                    bestWayToAttack = unitsAttackData[i];
                }
            }

            SetAttackActionTemplate(bestAIAction, bestWayToAttack.Item4, bestWayToAttack.Item1, bestWayToAttack.Item2, bestWayToAttack.Item3, attackAbility);
        }
    }

    private IEnumerator GetOneToOneMeleeAttackBestTurn(List<Node> walkPoints, UnitBase unitToMove, AIAction bestAIAction, BattleFieldActionAbility attackAbility)
    {
        if (_imposedPairsContainer.HasPairWith(unitToMove))
        {
            UnitBase target = _imposedPairsContainer.GetPairFor(unitToMove);
            SetAttackActionTemplate(bestAIAction, GetDamageOnTarget(unitToMove, target, attackAbility, false), _battleGridData.GlobalGrid.GetNodeFromWorldPoint(unitToMove.transform.position), new List<UnitBase>() { target }, Vector3.forward, attackAbility);
            yield break;
        }

        yield return null;

        List<(UnitBase, List<Node>)> unitAttackData = new List<(UnitBase, List<Node>)>();
        Node attackerNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(unitToMove.transform.position);

        if (walkPoints == null || walkPoints.Count == 0)
        {
            walkPoints = new List<Node>();
            walkPoints.Add(attackerNode);
        }

        for (int j = 0; j < _battleGridData.Units.Count; j++)
        {
            if (!_battleGridData.Units[j].IsDeadOnBattleField && _battleGridData.Units[j].GetType() != unitToMove.GetType())
            {
                Node targetUnitNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(_battleGridData.Units[j].transform.position);

                unitAttackData.Add(new(_battleGridData.Units[j], new List<Node>()));

                for (int x = -1; x < 2; x++)
                {
                    for (int y = -1; y < 2; y++)
                    {
                        if (x == 0 || y == 0)
                        {
                            if (x == 0 && y == 0)
                            {
                                continue;
                            }

                            Node nodeToAdd = _battleGridData.GlobalGrid.GetNodesGrid()[targetUnitNode.GridX + x, targetUnitNode.GridY + y];

                            if (walkPoints.Contains(nodeToAdd))
                            {
                                unitAttackData[^1].Item2.Add(nodeToAdd);
                            }
                        }
                    }
                }
            }
        }

        if (_isDebugAIMovementWeights)
        {
            bool isFirst = false;
            foreach (var item in Object.FindObjectsOfType<TMPro.TextMeshPro>())
            {
                if (!isFirst)
                {
                    isFirst = true;
                    continue;
                }
                Object.Destroy(item.gameObject);
            }

            Object.FindObjectOfType<TMPro.TextMeshPro>().transform.position = Vector3.one * 10000f;
        }

        List<(float, UnitBase, Node)> targets = new List<(float, UnitBase, Node)>();

        for (int i = 0; i < unitAttackData.Count; i++)
        {
            if (unitAttackData[i].Item2.Count > 0)
            {
                List<(float, Node)> nodesByTacticalPoints = new List<(float, Node)>(unitAttackData[i].Item2.Count);

                Node bestNodeToAttackFrom = unitAttackData[i].Item2[0];

                for (int j = 0; j < unitAttackData[i].Item2.Count; j++)
                {
                    Vector3 attackDirection = (unitAttackData[i].Item1.transform.position.RemoveYCoord() - unitAttackData[i].Item2[j].WorldPosition.RemoveYCoord()).normalized;
                    bool isAttackFromBehind = _imposedPairsContainer.HasPairWith(unitAttackData[i].Item1) && (Vector3.Dot(attackDirection, unitAttackData[i].Item1.transform.forward.RemoveYCoord()) >= .9f);

                    float tacticalPoints = GetDamageOnTarget(unitToMove, unitAttackData[i].Item1, attackAbility, false);

                    // add more tactical points the health is closer to zero
                    float targetUnitHealthCompleteness = unitAttackData[i].Item1.GetUnitHealthContainer().GetHealthCompleteness();
                    float remappedTactitialPointsValue = Mathf.Lerp(tacticalPoints, tacticalPoints * 1.25f, Mathf.InverseLerp(1f, 0f, targetUnitHealthCompleteness));

                    float distToTarget = Vector3.Distance(unitAttackData[i].Item2[j].WorldPosition, unitToMove.transform.position);
                    tacticalPoints -= distToTarget / 8f;

                    nodesByTacticalPoints.Add(new(tacticalPoints, unitAttackData[i].Item2[j]));
                }

                float maxTacticalAttackPoints = -Mathf.Infinity;

                for (int j = 0; j < nodesByTacticalPoints.Count; j++)
                {
                    if (_isDebugAIMovementWeights)
                    {
                        TMPro.TextMeshPro worldText = Object.Instantiate(Object.FindObjectOfType<TMPro.TextMeshPro>());
                        worldText.transform.position = Vector3.up + nodesByTacticalPoints[j].Item2.WorldPosition;
                        worldText.text = nodesByTacticalPoints[j].Item1.ToString("F1");
                    }

                    if (nodesByTacticalPoints[j].Item1 > maxTacticalAttackPoints)
                    {
                        maxTacticalAttackPoints = nodesByTacticalPoints[j].Item1;
                        bestNodeToAttackFrom = nodesByTacticalPoints[j].Item2;
                    }
                }

                targets.Add(new(maxTacticalAttackPoints, unitAttackData[i].Item1, bestNodeToAttackFrom));
            }
        }

        if (targets.Count != 0)
        {
            float maxTacticalPoints = -Mathf.Infinity;
            (float, UnitBase, Node) bestAttackTarget = (maxTacticalPoints, null, null);
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i].Item1 > maxTacticalPoints)
                {
                    maxTacticalPoints = targets[i].Item1;
                    bestAttackTarget = targets[i];
                }
            }

            SetAttackActionTemplate(bestAIAction, maxTacticalPoints, bestAttackTarget.Item3, new List<UnitBase>() { bestAttackTarget.Item2 }, Vector3.forward, attackAbility);
        }
    }

    private IEnumerator OLD_TurnSequence(UnitBase unitToMove)
    {
        _battleHandler.StartFocusCamera(unitToMove.transform, () => _battleSM.Enter<AIMovementState>());

        List<Node> walkPoints = _battleHandler.GetPossibleWalkNodesForUnitAndSetField(unitToMove, false);

        yield return new WaitForSeconds(.5f);

        BattleFieldActionAbility attackAbility = unitToMove.GetUnitAbilitites().Single(ability => ability.Type != AbilityType.Walk);

        if (_imposedPairsContainer.HasPairWith(unitToMove))
        {
            UnitBase target = _imposedPairsContainer.GetPairFor(unitToMove);
            yield return _coroutineService.StartCoroutine(MeleeAttack(unitToMove, new List<UnitBase>() { target }, attackAbility, true));
        }
        else
        {
            Node currentUnitNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(unitToMove.transform.position);

            List<(UnitBase, List<Node>)> unitAttackData = new List<(UnitBase, List<Node>)>();

            for (int j = 0; j < _battleGridData.Units.Count; j++)
            {
                if (!_battleGridData.Units[j].IsDeadOnBattleField && _battleGridData.Units[j] != unitToMove && _battleGridData.Units[j] is PlayerUnit)
                {
                    Node targetUnitNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(_battleGridData.Units[j].transform.position);

                    unitAttackData.Add(new(_battleGridData.Units[j], new List<Node>()));

                    for (int x = -1; x < 2; x++)
                    {
                        for (int y = -1; y < 2; y++)
                        {
                            if (x == 0 || y == 0)
                            {
                                if (x == 0 && y == 0)
                                {
                                    continue;
                                }

                                Node nodeToAdd = _battleGridData.GlobalGrid.GetNodesGrid()[targetUnitNode.GridX + x, targetUnitNode.GridY + y];

                                if (walkPoints.Contains(nodeToAdd))
                                {
                                    unitAttackData[^1].Item2.Add(nodeToAdd);
                                }
                            }
                        }
                    }
                }
            }

            List<(float, UnitBase, Node)> targets = new List<(float, UnitBase, Node)>();

            if (_isDebugAIMovementWeights)
            {
                bool isFirst = false;
                foreach (var item in Object.FindObjectsOfType<TMPro.TextMeshPro>())
                {
                    if (!isFirst)
                    {
                        isFirst = true;
                        continue;
                    }
                    Object.Destroy(item.gameObject);
                }

                Object.FindObjectOfType<TMPro.TextMeshPro>().transform.position = Vector3.one * 10000f;
            }

            for (int i = 0; i < unitAttackData.Count; i++)
            {
                if (unitAttackData[i].Item2.Count > 0)
                {
                    List<(float, Node)> nodesByTacticalPoints = new List<(float, Node)>(unitAttackData[i].Item2.Count);

                    Node bestNodeToAttackFrom = unitAttackData[i].Item2[0];

                    for (int j = 0; j < unitAttackData[i].Item2.Count; j++)
                    {
                        (bool, float) damageResult = unitAttackData[i].Item1.GetUnitHealthContainer().GedModifiedDamageAmount(unitToMove.GetUnitConfig().DefaultAttackDamage);
                        float tacticalPoints = damageResult.Item1 ? 100000 : damageResult.Item2;

                        Vector3 attackDirection = (unitAttackData[i].Item1.transform.position.RemoveYCoord() - unitAttackData[i].Item2[j].WorldPosition.RemoveYCoord()).normalized;

                        if (_imposedPairsContainer.HasPairWith(unitAttackData[i].Item1))
                        {
                            tacticalPoints *= 1.2f;

                            bool isAttackFromBehind = _imposedPairsContainer.HasPairWith(unitAttackData[i].Item1) && (Vector3.Dot(attackDirection, unitAttackData[i].Item1.transform.forward.RemoveYCoord()) >= .9f);

                            if (isAttackFromBehind)
                            {
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

                    for (int j = 0; j < nodesByTacticalPoints.Count; j++)
                    {
                        if (_isDebugAIMovementWeights)
                        {
                            TMPro.TextMeshPro worldText = Object.Instantiate(Object.FindObjectOfType<TMPro.TextMeshPro>());
                            worldText.transform.position = Vector3.up + nodesByTacticalPoints[j].Item2.WorldPosition;
                            worldText.text = nodesByTacticalPoints[j].Item1.ToString("F1");
                        }

                        if (nodesByTacticalPoints[j].Item1 > maxTacticalAttackPoints)
                        {
                            maxTacticalAttackPoints = nodesByTacticalPoints[j].Item1;
                            bestNodeToAttackFrom = nodesByTacticalPoints[j].Item2;
                        }
                    }

                    targets.Add(new(maxTacticalAttackPoints, unitAttackData[i].Item1, bestNodeToAttackFrom));
                }
            }

            float maxTacticalPoints = -Mathf.Infinity;
            (float, UnitBase, Node) target = (maxTacticalPoints, null, null);
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i].Item1 > maxTacticalPoints)
                {
                    maxTacticalPoints = targets[i].Item1;
                    target = targets[i];
                }
            }

            if (target.Item3 != _battleGridData.GlobalGrid.GetNodeFromWorldPoint(unitToMove.transform.position))
            {
                yield return _coroutineService.StartCoroutine(MovementRoutine(unitToMove, target.Item3.WorldPosition));
            }

            yield return _coroutineService.StartCoroutine(MeleeAttack(unitToMove, new List<UnitBase>() { target.Item2 }, attackAbility, false));
        }

        _turnsHandler.AIEndedTurn();
    }

    private float GetDamageOnTarget(UnitBase attacker, UnitBase target, BattleFieldActionAbility ability, bool isAttackFromBehind)
    {
        (bool, float) attackResult = target.GetUnitHealthContainer().GedModifiedDamageAmount(attacker.GetUnitConfig().DefaultAttackDamage);

        float resultDamage = attackResult.Item2;

        if (attackResult.Item1)
        {
            resultDamage = 100000f;
        }
        else
        {
            if (isAttackFromBehind)
            {
                resultDamage += attackResult.Item2 * .2f;
            }
        }

        return resultDamage;
    }

    private IEnumerator FakeTurnSequence(UnitBase unitToMove)
    {
        _battleHandler.StartFocusCamera(unitToMove.transform, () => _battleSM.Enter<AIMovementState>());

        yield return new WaitForSeconds(1f);

        _turnsHandler.AIEndedTurn();
    }

    private void SetAttackActionTemplate(AIAction baseAction, float points, Node endNodeWalking, List<UnitBase> targets, Vector3 attackDirection, BattleFieldActionAbility ability)
    {
        baseAction.Points = points;
        baseAction.EndNodeWalking = endNodeWalking;
        baseAction.Targets = targets;
        baseAction.AttackDirection = attackDirection;
        baseAction.Ability = ability;
        baseAction.IsAttack = true;
        baseAction.IsCanBeUsed = true;
    }

    private void EqualizeTwoActions(AIAction action1, AIAction action2)
    {
        action1.Points = action2.Points;
        action1.EndNodeWalking = action2.EndNodeWalking;
        action1.IsAttack = action2.IsAttack;
        action1.IsCanBeUsed = action2.IsCanBeUsed;
        action1.Targets = action2.Targets;
        action1.AttackDirection = action2.AttackDirection;
        action1.Ability = action2.Ability;
    }

    private class AIAction
    {
        public float Points = -100000f;
        public Node EndNodeWalking;
        public bool IsAttack;
        public bool IsCanBeUsed;
        public List<UnitBase> Targets;
        public Vector3 AttackDirection;
        public BattleFieldActionAbility Ability;
    }
}