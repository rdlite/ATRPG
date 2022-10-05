using UnityEngine;

public class PlayerUnitMovementChoosePathState : IState, IUpdateState {
    private LineRenderer _movementLinePrefab;
    private BattleGridData _battleGridData;
    private BattleRaycaster _battleRaycaster;
    private UpdateStateMachine _battleSM;
    private InputService _inputService;
    private BattleHandler _battleHandler;
    private Node _prevMovementNode;
    private Node _endMovementNode;
    private MovementPointer _movementPointerStart, _movementPointerEnd;

    public PlayerUnitMovementChoosePathState(
        BattleHandler battleHandler, InputService inputService, UpdateStateMachine battleSM,
        BattleRaycaster battleRaycaster, BattleGridData battleGridData, LineRenderer movementLinePrefab,
        AssetsContainer assetsContainer) {
        _movementLinePrefab = movementLinePrefab;
        _battleGridData = battleGridData;
        _battleRaycaster = battleRaycaster;
        _battleSM = battleSM;
        _inputService = inputService;
        _battleHandler = battleHandler;

        _movementPointerStart = Object.Instantiate(assetsContainer.MovementPointer);
        _movementPointerEnd = Object.Instantiate(assetsContainer.MovementPointer);
        _movementPointerStart.gameObject.SetActive(false);
        _movementPointerEnd.gameObject.SetActive(false);
    }

    public void Enter() {
        _battleHandler.ShowUnitWalkingDistance(_battleHandler.CurrentSelectedUnit, false);
    }

    public void Update() {
        if (Input.GetMouseButtonDown(1) && !_inputService.IsPointerOverUIObject() && _battleHandler.CurrentSelectedUnit != null) {
            _battleSM.Enter<IdlePlayerMovementState>();
            return;
        }

        if (Input.GetMouseButtonDown(0) && !_inputService.IsPointerOverUIObject()) {
            SetUnitDestination();
            return;
        } else {
            TryDrawWalkLine();
        }
    }

    private void TryDrawWalkLine() {
        Vector3 groundPoint = _battleRaycaster.GetRaycastPoint();

        Node startNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(_battleHandler.CurrentSelectedUnit.transform.position);
        Node endNodeCheckInWorld = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(groundPoint);

        _battleHandler.CurrentSelectedUnit.transform.rotation = Quaternion.Slerp(_battleHandler.CurrentSelectedUnit.transform.rotation,
            Quaternion.LookRotation(groundPoint.RemoveYCoord() - _battleHandler.CurrentSelectedUnit.transform.position.RemoveYCoord(), Vector3.up),
            20f * Time.deltaTime);

        if (_prevMovementNode != endNodeCheckInWorld) {
            Node endNode = _battleGridData.GlobalGrid.GetFirstNearestWalkableNode(
           endNodeCheckInWorld,
           false,
           _battleGridData.StartNodeIDX, _battleGridData.StartNodeIDX + _battleGridData.Width - 1, _battleGridData.StartNodeIDY, _battleGridData.StartNodeIDY + _battleGridData.Height - 1);

            Vector3[] path = _battleGridData.GlobalGrid.GetPathPoints(startNode, endNode);

            _battleHandler.DeactivateMovementLine();

            if (path.Length != 0) {
                _endMovementNode = endNode;

                LineRenderer createdLine = _battleHandler.CreateMovementLinePrefab(path.Length);

                for (int i = 0; i < path.Length; i++) {
                    createdLine.SetPosition(i, path[i] + Vector3.up * .1f);
                }

                _movementPointerStart.gameObject.SetActive(true);
                _movementPointerEnd.gameObject.SetActive(true);

                _movementPointerStart.transform.position = path[0];
                _movementPointerEnd.transform.position = path[^1];
            }
        }

        _prevMovementNode = endNodeCheckInWorld;
    }

    private void SetUnitDestination() {
        _battleSM.Enter<PlayerUnitMovementAnimationState, Node>(_endMovementNode);
    }

    public void Exit() {
        _movementPointerStart.gameObject.SetActive(false);
        _movementPointerEnd.gameObject.SetActive(false);
        _battleHandler.HideWalkingDistance(false);
    }
}
