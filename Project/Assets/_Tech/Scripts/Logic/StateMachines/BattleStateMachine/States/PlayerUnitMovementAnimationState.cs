using System.Collections;
using UnityEngine;

public class PlayerUnitMovementAnimationState : IPayloadState<Node>
{
    private CameraSimpleFollower _cameraFollower;
    private BattleStateMachine _battleSM;
    private BattleHandler _battleHandler;
    private BattleTurnsHandler _turnsHandler;
    private BattleGridData _battleGridData;
    private ICoroutineService _coroutineService;
    private ImposedPairsContainer _imposedPairsContainer;
    private Node _endMovementNode;

    public PlayerUnitMovementAnimationState(
        BattleGridData battleGridData, ICoroutineService coroutineService, ImposedPairsContainer imposedPairsContainer,
        BattleHandler battleHandler, BattleTurnsHandler turnsHandler, CameraSimpleFollower cameraFollower,
        BattleStateMachine battleSM)
    {
        _cameraFollower = cameraFollower;
        _battleSM = battleSM;
        _battleHandler = battleHandler;
        _turnsHandler = turnsHandler;
        _battleGridData = battleGridData;
        _coroutineService = coroutineService;
        _imposedPairsContainer = imposedPairsContainer;
    }

    public void Enter(Node endNode)
    {
        _endMovementNode = endNode;
        StartUnitMovement();
    }

    public void Exit() { }

    private void StartUnitMovement()
    {
        Node startMovementNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(_battleHandler.CurrentSelectedUnit.transform.position);

        if (startMovementNode != _endMovementNode)
        {
            _battleHandler.StartFocusCamera(_battleHandler.CurrentSelectedUnit.transform, null);
            _battleHandler.HideWalkingDistance(true);
            _turnsHandler.SetCurrentWalker(_battleHandler.CurrentSelectedUnit);
            _battleHandler.CurrentSelectedUnit.StartMove();
            _turnsHandler.RemovePossibleLengthForUnit(_battleHandler.CurrentSelectedUnit, _battleGridData.GlobalGrid.GetPathLength(startMovementNode, _endMovementNode));
            _battleHandler.CurrentSelectedUnit.GoToPoint(_endMovementNode.WorldPosition, false, true, null, () => UnitEndedWalkAction(_battleHandler.CurrentSelectedUnit));
        }
    }

    private void UnitEndedWalkAction(UnitBase unitWalked)
    {
        TryTurnEnemiesOnUnitEndWalking(unitWalked);
    }

    private void OnEndedAction()
    {
        _battleSM.Enter<IdlePlayerMovementState>();
    }

    private void TryTurnEnemiesOnUnitEndWalking(UnitBase unitWalked)
    {
        bool hasCharacterToRotate = false;

        for (int i = 0; i < _battleGridData.Units.Count; i++)
        {
            if (!_battleGridData.Units[i].IsDeadOnBattleField && _battleGridData.Units[i] != unitWalked)
            {
                if (unitWalked.GetType() != _battleGridData.Units[i].GetType())
                    if (IsTargetInMeleeAttackRange(unitWalked, _battleGridData.Units[i]) && !_imposedPairsContainer.HasPairWith(_battleGridData.Units[i]))
                    {
                        hasCharacterToRotate = true;
                        _coroutineService.StartCoroutine(RotateUnitToTarget(unitWalked, _battleGridData.Units[i]));
                    }
            }
        }

        if (!hasCharacterToRotate)
        {
            OnEndedAction();
        }
    }

    private bool IsTargetInMeleeAttackRange(UnitBase unitCentre, UnitBase unitToCheck)
    {
        Node currentUnitNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(unitCentre.transform.position);
        Node targetUnitNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(unitToCheck.transform.position);
        return _battleGridData.GlobalGrid.IsNodesContacts(currentUnitNode, targetUnitNode);
    }

    private IEnumerator RotateUnitToTarget(UnitBase toWhomRotate, UnitBase unitNeedsToBeRotated)
    {
        float t = 0f;

        Quaternion startTargetRotation = unitNeedsToBeRotated.transform.rotation;
        Quaternion endTargetRotation = Quaternion.LookRotation((toWhomRotate.transform.position - unitNeedsToBeRotated.transform.position).RemoveYCoord());

        while (t <= 1f)
        {
            t += Time.deltaTime * 5f;

            unitNeedsToBeRotated.transform.rotation = Quaternion.Slerp(startTargetRotation, endTargetRotation, t);

            yield return null;
        }

        OnEndedAction();
    }
}