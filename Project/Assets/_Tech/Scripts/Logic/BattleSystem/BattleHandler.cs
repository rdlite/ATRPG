using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;

public class BattleHandler {
    private const string APPEAR_RANGE = "_AppearRoundRange";
    private const string APPEAR_CENTER_POINT_UV = "_AppearCenterPointUV";
    private DecalProjector[] _createdUnderUnitDecals;
    private DecalProjector[] _createdUnderUnitAttackDecals;
    private bool[] _mouseOverSelectionMap, _mouseOverDataSelectionMap;
    private AssetsContainer _assetsContainer;
    private UIRoot _uiRoot;
    private DecalProjector _decalProjector;
    private BattleGridData _battleGridData;
    private BattleRaycaster _battleRaycaster;
    private UnitBase _currentSelectedUnit;
    private UnitBase _currentMouseOverSelectionUnit;
    private CameraSimpleFollower _cameraFollower;
    private LineRenderer _movementLinePrefab;
    private LineRenderer _createdLinePrefab;
    private BattleTurnsHandler _turnsHandler;
    private float _currentAppearRange = 0f;
    private bool _isCurrentlyShowWalkingDistance;
    private bool _isCurrentlyShowAttacking;
    private bool _isRestrictedForDoAnything;

    public void Init(
        CameraSimpleFollower cameraFollower, BattleGridData battleGridData, DecalProjector decalProjector,
        UIRoot uiRoot, AssetsContainer assetsContainer, LineRenderer movementLinePrefab,
        Transform battleGeneratorTransform, ICoroutineService coroutineService) {
        _cameraFollower = cameraFollower;
        _movementLinePrefab = movementLinePrefab;
        _assetsContainer = assetsContainer;
        _uiRoot = uiRoot;
        _decalProjector = decalProjector;
        _battleGridData = battleGridData;
        _mouseOverSelectionMap = new bool[_battleGridData.Units.Count];
        _mouseOverDataSelectionMap = new bool[_battleGridData.Units.Count];
        _battleRaycaster = new BattleRaycaster(
            _battleGridData.UnitsLayerMask, _cameraFollower, _battleGridData.GroundLayerMask);

        _turnsHandler = new BattleTurnsHandler(
            _battleGridData, _uiRoot, this,
            coroutineService, cameraFollower);

        _createdUnderUnitDecals = new DecalProjector[_battleGridData.Units.Count];
        _createdUnderUnitAttackDecals = new DecalProjector[_battleGridData.Units.Count];

        for (int i = 0; i < _battleGridData.Units.Count; i++) {
            _createdUnderUnitDecals[i] = Object.Instantiate(_assetsContainer.UnderUnitDecalPrefab);
            _createdUnderUnitDecals[i].gameObject.SetActive(false);
            _createdUnderUnitDecals[i].transform.SetParent(battleGeneratorTransform);
            
            _createdUnderUnitAttackDecals[i] = Object.Instantiate(_assetsContainer.AttackUnderUnitDecalPrefab);
            _createdUnderUnitAttackDecals[i].gameObject.SetActive(false);
            _createdUnderUnitAttackDecals[i].transform.SetParent(battleGeneratorTransform);

            _battleGridData.Units[i].WithdrawWeapon();
        }

        _uiRoot.GetPanel<BattlePanel>().SignOnWaitButton(WaitButtonPressed);
        _uiRoot.GetPanel<BattlePanel>().SignOnBackButton(BackButtonPressed);

        _turnsHandler.StartTurns();
    }

    public void Tick() {
        if (Time.frameCount % 4 == 0) {
            _currentMouseOverSelectionUnit = _battleRaycaster.GetCurrentMouseOverSelectionUnit();
            for (int i = 0; i < _battleGridData.Units.Count; i++) {
                if (_battleGridData.Units[i] == _currentMouseOverSelectionUnit && !_mouseOverSelectionMap[i] && !IsPointerOverUIObject()) {
                    _mouseOverSelectionMap[i] = true;
                    _battleGridData.Units[i].SetActiveOutline(true);
                } else if (_battleGridData.Units[i] != _currentMouseOverSelectionUnit && _mouseOverSelectionMap[i]) {
                    if (!(_currentSelectedUnit != null && _currentSelectedUnit == _battleGridData.Units[i])) {
                        _mouseOverSelectionMap[i] = false;
                        _battleGridData.Units[i].SetActiveOutline(false);
                    }
                }
            }

            for (int i = 0; i < _battleGridData.Units.Count; i++) {
                if (_battleGridData.Units[i] == _currentMouseOverSelectionUnit && !_mouseOverDataSelectionMap[i] && !IsPointerOverUIObject()) {
                    _mouseOverDataSelectionMap[i] = true;
                    CreateOverUnitData(_battleGridData.Units[i]);
                } else if (_battleGridData.Units[i] != _currentMouseOverSelectionUnit && _mouseOverDataSelectionMap[i]) {
                    _mouseOverDataSelectionMap[i] = false;
                    DestroyOverUnitData(_battleGridData.Units[i]);
                }
            }
        }

        if (_isRestrictedForDoAnything) {
            return;
        }

        if (_currentSelectedUnit != null) {
            if (_currentAppearRange <= 1f) {
                _currentAppearRange += Time.deltaTime;
                _decalProjector.material.SetFloat(APPEAR_RANGE, _currentAppearRange);
            }
        }

        if (_isCurrentlyShowWalkingDistance) {
            if (Input.GetMouseButtonDown(1) && !IsPointerOverUIObject() && _currentSelectedUnit != null) {
                HideWalkingDistance(true);
                return;
            }

            if (Input.GetMouseButtonDown(0) && !IsPointerOverUIObject()) {
                SetUnitDestination();
                return;
            } else {
                TryDrawWalkLine();
            }
        }

        if (Input.GetMouseButtonDown(0) && !IsPointerOverUIObject() && _currentMouseOverSelectionUnit != null) {
            if (_currentMouseOverSelectionUnit != _currentSelectedUnit) {
                SetUnitSelect(_currentMouseOverSelectionUnit);
            }
        }
    }

    private bool IsPointerOverUIObject() {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }

    public void FocusCameraToNearestAllyUnit(UnitBase unitToCheck) {
        float nearestLength = Mathf.Infinity;
        UnitBase nearestChar = null;

        for (int i = 0; i < _battleGridData.Units.Count; i++) {
            if (_battleGridData.Units[i] is PlayerUnit && _turnsHandler.IsCanUnitWalk(_battleGridData.Units[i])) {
                float distance = Vector3.Distance(_battleGridData.Units[i].transform.position, unitToCheck.transform.position);

                if (distance < nearestLength) {
                    nearestLength = distance;
                    nearestChar = _battleGridData.Units[i];
                }
            }
        }

        SetUnitSelect(nearestChar);
        _cameraFollower.SetTarget(nearestChar.transform);
    }

    // WALK MODULE
    private Node _prevMovementNode;
    private Node _endMovementNode;
    private void TryDrawWalkLine() {
        Vector3 groundPoint = _battleRaycaster.GetRaycastPoint();

        Node startNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(_currentSelectedUnit.transform.position);
        Node endNodeCheckInWorld = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(groundPoint);

        _currentSelectedUnit.transform.rotation = Quaternion.Slerp(_currentSelectedUnit.transform.rotation,
            Quaternion.LookRotation(groundPoint.RemoveYCoord() - _currentSelectedUnit.transform.position.RemoveYCoord(), Vector3.up),
            20f * Time.deltaTime);

        if (_prevMovementNode != endNodeCheckInWorld) {
             Node endNode = _battleGridData.GlobalGrid.GetFirstNearestWalkableNode(
            endNodeCheckInWorld,
            false,
            _battleGridData.StartNodeIDX, _battleGridData.StartNodeIDX + _battleGridData.Width - 1, _battleGridData.StartNodeIDY, _battleGridData.StartNodeIDY + _battleGridData.Height - 1);

            Vector3[] path = _battleGridData.GlobalGrid.GetPathPoints(startNode, endNode);
            if (_createdLinePrefab != null) {
                Object.Destroy(_createdLinePrefab.gameObject);
            }

            if (path.Length != 0) {
                _endMovementNode = endNode;

                _createdLinePrefab = Object.Instantiate(_movementLinePrefab);
                _createdLinePrefab.positionCount = path.Length;

                for (int i = 0; i < path.Length; i++) {
                    _createdLinePrefab.SetPosition(i, path[i] + Vector3.up * .1f);
                }

                for (int x = 0; x < _battleGridData.NodesGrid.GetLength(0); x++) {
                    for (int y = 0; y < _battleGridData.NodesGrid.GetLength(1); y++) {
                        _battleGridData.CurrentMovementPointsTexture.SetPixel(x * 2, y * 2, Color.black);
                    }
                }

                _battleGridData.CurrentMovementPointsTexture.SetPixel((startNode.GridX - _battleGridData.StartNodeIDX) * 2, (startNode.GridY - _battleGridData.StartNodeIDY) * 2, Color.white);
                _battleGridData.CurrentMovementPointsTexture.SetPixel((endNode.GridX - _battleGridData.StartNodeIDX) * 2, (endNode.GridY - _battleGridData.StartNodeIDY) * 2, Color.white);

                _battleGridData.CurrentMovementPointsTexture.Apply();
                SetDecal();
            }
        }
        
        _prevMovementNode = endNodeCheckInWorld;
    }

    public void SwitchWalking() {
        if (_isRestrictedForDoAnything || !_turnsHandler.IsCanUnitWalk(_currentSelectedUnit) || !_turnsHandler.IsItCurrentWalkingUnit(_currentSelectedUnit)) {
            return;
        }

        _isCurrentlyShowWalkingDistance = !_isCurrentlyShowWalkingDistance;

        if (_isCurrentlyShowWalkingDistance && !_decalProjector.gameObject.activeSelf) {
            ShowUnitWalkingDistance();
        }

        if (!_isCurrentlyShowWalkingDistance) {
            HideWalkingDistance(false);
        }
    }

    private void ShowWalkingDistance() {
        if (!_isCurrentlyShowWalkingDistance || !_turnsHandler.IsCanUnitWalk(_currentSelectedUnit)) {
            if (_isCurrentlyShowAttacking) {
                SwitchAttacking();
            }

            ShowUnitWalkingDistance();
        }
    }

    public void WalkingPointerEnter() {
        if (_isRestrictedForDoAnything || !_turnsHandler.IsCanUnitWalk(_currentSelectedUnit) || !_turnsHandler.IsItCurrentWalkingUnit(_currentSelectedUnit)) {
            return;
        }

        ShowWalkingDistance();
    }

    public void WalkingPointerExit() {
        if (_isRestrictedForDoAnything) {
            return;
        }

        if (!_isCurrentlyShowWalkingDistance) {
            HideWalkingDistance(true);
        }
    }

    private void HideWalkingDistance(bool isHideUnderPointers) {
        _isCurrentlyShowWalkingDistance = false;
        _decalProjector.gameObject.SetActive(false);

        if (isHideUnderPointers) {
            SetActiveUnderUnitsDecals(false);
        }

        if (_createdLinePrefab != null) {
            Object.Destroy(_createdLinePrefab.gameObject);
        }
    }

    private void ShowUnitWalkingDistance() {
        SetActiveUnderUnitsDecals(true);
        
        float unitMaxWalkDistance = _turnsHandler.GetLastLengthForUnit(_currentSelectedUnit);

        if (unitMaxWalkDistance < 1f) {
            HideWalkingDistance(false);
            return;
        }
        
        _decalProjector.gameObject.SetActive(true);
        _currentAppearRange = 0f;
        _decalProjector.material.SetFloat(APPEAR_RANGE, 0f);

        Vector3 currentUnitPosition = _currentSelectedUnit.transform.position;

        Node startNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(currentUnitPosition);

        List<Node> possibleNodes = new List<Node>(25);
        Node[] neighbours;
        List<Node> resultNodes = new List<Node>(25);
        possibleNodes.Add(_battleGridData.GlobalGrid.GetNodeFromWorldPoint(currentUnitPosition));

        int crushProtection = 0;

        for (int x = 0; x < _battleGridData.Width; x++) {
            for (int y = 0; y < _battleGridData.Height; y++) {
                _battleGridData.WalkableMap[x, y] = false;
                _battleGridData.NodesGrid[x, y].SetPlacedByUnit(false);
                _battleGridData.CurrentMovementPointsTexture.SetPixel(x * 2, y * 2, Color.black);
            }
        }

        for (int i = 0; i < _battleGridData.Units.Count; i++) {
            Node unitNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(_battleGridData.Units[i].transform.position);

            if (unitNode == startNode) {
                unitNode.SetPlacedByUnit(false);
            } else {
                unitNode.SetPlacedByUnit(true);
            }
        }

        while (possibleNodes.Count > 0) {
            crushProtection++;

            if (crushProtection > 100000) {
                Debug.Log("CRUSHED, DOLBOEB!!!");
                break;
            }

            neighbours = _battleGridData.GlobalGrid.GetNeighbours(possibleNodes[0], true).ToArray();
            possibleNodes.RemoveAt(0);

            foreach (Node neighbour in neighbours) {
                if (!resultNodes.Contains(neighbour) &&
                    neighbour.CheckWalkability &&
                    (
                        neighbour.GridX >= _battleGridData.StartNodeIDX &&
                        neighbour.GridX < _battleGridData.StartNodeIDX + _battleGridData.Width) &&
                        (neighbour.GridY >= _battleGridData.StartNodeIDY &&
                        neighbour.GridY < _battleGridData.StartNodeIDY + _battleGridData.Height)) {
                    if (unitMaxWalkDistance >= _battleGridData.GlobalGrid.GetPathLength(startNode, neighbour)) {
                        resultNodes.Add(neighbour);
                        possibleNodes.Add(neighbour);
                        _battleGridData.NodesGrid[neighbour.GridX - _battleGridData.StartNodeIDX, neighbour.GridY - _battleGridData.StartNodeIDY].SetPlacedByUnit(false);
                        _battleGridData.WalkableMap[neighbour.GridX - _battleGridData.StartNodeIDX, neighbour.GridY - _battleGridData.StartNodeIDY] = true;
                    }
                }
            }
        }

        for (int x = 0; x < _battleGridData.Width; x++) {
            for (int y = 0; y < _battleGridData.Height; y++) {
                bool isBlocked = !_battleGridData.WalkableMap[x, y] || (x == 0 || y == 0 || (x == _battleGridData.Width - 1) || (y == _battleGridData.Height - 1));

                _battleGridData.NodesGrid[x, y].SetPlacedByUnit(isBlocked);
            }
        }

        Vector3 minPoint = _battleGridData.LDPoint.position;
        Vector3 maxPoint = _battleGridData.RUPoint.position;

        float xLerpPoint = Mathf.InverseLerp(minPoint.x, maxPoint.x, currentUnitPosition.x);
        float zLerpPoint = Mathf.InverseLerp(minPoint.z, maxPoint.z, currentUnitPosition.z);

        _decalProjector.material.SetVector(APPEAR_CENTER_POINT_UV, new Vector2(xLerpPoint, zLerpPoint));

        ShowView();
    }

    private void SetActiveUnderUnitsDecals(bool value) {
        for (int i = 0; i < _battleGridData.Units.Count; i++) {
            _createdUnderUnitDecals[i].gameObject.SetActive(_battleGridData.Units[i] != _currentSelectedUnit && value);
            _createdUnderUnitDecals[i].transform.position = _battleGridData.Units[i].transform.position + Vector3.up / 2f;
            _createdUnderUnitDecals[i].material = Object.Instantiate(_createdUnderUnitDecals[i].material);
            _createdUnderUnitDecals[i].material.SetColor("_Color", _battleGridData.Units[i].GetUnitColor().SetTransparency(1f));
        }
    }

    private void SetUnitDestination() {
        _cameraFollower.SetTarget(_currentSelectedUnit.transform);
        _isCurrentlyShowWalkingDistance = false;
        _isRestrictedForDoAnything = true;
        HideWalkingDistance(true);
        _turnsHandler.SetCurrentWalker(_currentSelectedUnit);
        _currentSelectedUnit.StartMove();
        _turnsHandler.RemovePossibleLengthForUnit(_currentSelectedUnit, _battleGridData.GlobalGrid.GetPathLength(_battleGridData.GlobalGrid.GetNodeFromWorldPoint(_currentSelectedUnit.transform.position), _endMovementNode));
        _currentSelectedUnit.GoToPoint(_endMovementNode.WorldPosition, false, true, null, UnitEndedAction);
    }

    private void UnitEndedAction() {
        HideWalkingDistance(true);
        _isRestrictedForDoAnything = false;
    }
    // END WALK MODULE

    // ATTACK MODULE

    public void SwitchAttacking() {
        if (_isRestrictedForDoAnything || !_turnsHandler.IsItCurrentWalkingUnit(_currentSelectedUnit)) {
            return;
        }

        _isCurrentlyShowAttacking = !_isCurrentlyShowAttacking;

        SetActiveUnderUnitsAttackDecals(_isCurrentlyShowAttacking);

        if (_isCurrentlyShowWalkingDistance) {
            WalkingPointerExit();
        }
    }

    private void SetActiveUnderUnitsAttackDecals(bool value) {
        if (value) {
            Node currentUnitNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(_currentSelectedUnit.transform.position);

            for (int x = -1; x < 2; x++) {
                for (int y = -1; y < 2; y++) {
                    if (x == 0 || y == 0) {
                        if (x == 0 && y == 0) {
                            continue;
                        }

                        for (int i = 0; i < _battleGridData.Units.Count; i++) {
                            if (_battleGridData.Units[i] != _currentSelectedUnit && _battleGridData.Units[i] is EnemyUnit) {
                                Node targetUnitNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(_battleGridData.Units[i].transform.position);

                                if (currentUnitNode.GridX + x == targetUnitNode.GridX && currentUnitNode.GridY + y == targetUnitNode.GridY) {
                                    _createdUnderUnitAttackDecals[i].gameObject.SetActive(true);
                                    _createdUnderUnitAttackDecals[i].transform.position = _battleGridData.Units[i].transform.position + Vector3.up / 3f;
                                    _createdUnderUnitAttackDecals[i].material = Object.Instantiate(_createdUnderUnitAttackDecals[i].material);
                                }
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < _battleGridData.Units.Count; i++) {
                //_createdUnderUnitAttackDecals[i].gameObject.SetActive(_battleGridData.Units[i] != _currentSelectedUnit && value);
                //_createdUnderUnitAttackDecals[i].transform.position = _battleGridData.Units[i].transform.position + Vector3.up / 3f;
                //_createdUnderUnitAttackDecals[i].material = Object.Instantiate(_createdUnderUnitAttackDecals[i].material);
            }
        } else {
            for (int i = 0; i < _battleGridData.Units.Count; i++) {
                _createdUnderUnitAttackDecals[i].gameObject.SetActive(false);
            }
        }
    }

    // END ATTACK MODULE

    public void OnTurnButtonEntered(UnitBase unit) {
        unit.SetActiveOutline(true);
        unit.CreateOverUnitData();
    }

    public void OnTurnButtonExit(UnitBase unit) {
        unit.SetActiveOutline(false);
        unit.DestroyOverUnitData();
    }

    private void WaitButtonPressed() {
        if (_isRestrictedForDoAnything) {
            return;
        }

        if (_isCurrentlyShowAttacking) {
            SetActiveUnderUnitsAttackDecals(false);
        }

        _turnsHandler.SetUnitWalked(_currentSelectedUnit);

        DestroySelectionOnCurrentUnit();
        _currentSelectedUnit = null;
        HideWalkingDistance(true);
        _turnsHandler.CallNextTurn();
    }

    private void BackButtonPressed() {
        SetUnitSelect(_turnsHandler.GetCurrentUnitWalker());
        _cameraFollower.SetTarget(_currentSelectedUnit.transform);
    }

    private void SetUnitSelect(UnitBase unit) {
        if (_createdLinePrefab != null) {
            Object.Destroy(_createdLinePrefab.gameObject);
        }

        _decalProjector.gameObject.SetActive(false);

        _currentSelectedUnit?.DestroySelection();
        _currentSelectedUnit?.SetActiveOutline(false);
        _currentSelectedUnit = unit;
        _currentSelectedUnit.SetActiveOutline(true);
        _currentSelectedUnit.CreateSelectionAbove();

        UnitPanelState viewState = UnitPanelState.CompletelyDeactivate;
        
        if (_turnsHandler.IsItCurrentWalkingUnit(_currentSelectedUnit) && _turnsHandler.IsCanUnitWalk(_currentSelectedUnit)) {
            viewState = UnitPanelState.UseTurn;
        } else {
            viewState = UnitPanelState.ViewTurn;
        }

        _uiRoot.GetPanel<BattlePanel>().DisableUnitsPanel(this);
        _uiRoot.GetPanel<BattlePanel>().EnableUnitPanel(this, _currentSelectedUnit, viewState);

        if (_turnsHandler.IsHaveCurrentWalkingUnit() && !_turnsHandler.IsItCurrentWalkingUnit(_currentSelectedUnit)) {
            _uiRoot.GetPanel<BattlePanel>().SetActiveBackToUnitButton(true);
        } else {
            _uiRoot.GetPanel<BattlePanel>().SetActiveBackToUnitButton(false);
        }
    }

    private void DestroySelectionOnCurrentUnit() {
        _currentSelectedUnit?.DestroySelection();
        _currentSelectedUnit?.SetActiveOutline(false);
    }

    private void CreateOverUnitData(UnitBase unit) {
        unit.CreateOverUnitData();
    }

    private void DestroyOverUnitData(UnitBase unit) {
        unit.DestroyOverUnitData();
    }

    public void SetEnemyTurn() {
        _currentSelectedUnit?.DestroySelection();
        _currentSelectedUnit?.SetActiveOutline(false);
        _uiRoot.GetPanel<BattlePanel>().DisableUnitsPanel(this);
    }

    private void ShowView() {
        Color blackCol = Color.black;
        Color whiteCol = Color.white;

        for (int x = 0; x < _battleGridData.Width; x++) {
            for (int y = 0; y < _battleGridData.Height; y++) {
                bool isBorder = x == 0 || y == 0 || x == _battleGridData.Width - 1 || y == _battleGridData.Height - 1;

                //_battleGridData.ViewTexture.SetPixel(x, y, _battleGridData.WalkableMap[x, y] ? whiteCol : blackCol);
                _battleGridData.ViewTexture.SetPixel(x * _battleGridData.ViewTextureResolution, y * _battleGridData.ViewTextureResolution, _battleGridData.WalkableMap[x, y] && !isBorder ? whiteCol : blackCol);
                _battleGridData.ViewTexture.SetPixel(x * _battleGridData.ViewTextureResolution + 1, y * _battleGridData.ViewTextureResolution, _battleGridData.WalkableMap[x, y] && !isBorder ? whiteCol : blackCol);
                _battleGridData.ViewTexture.SetPixel(x * _battleGridData.ViewTextureResolution, y * _battleGridData.ViewTextureResolution + 1, _battleGridData.WalkableMap[x, y] && !isBorder ? whiteCol : blackCol);
                _battleGridData.ViewTexture.SetPixel(x * _battleGridData.ViewTextureResolution + 1, y * _battleGridData.ViewTextureResolution + 1, _battleGridData.WalkableMap[x, y] && !isBorder ? whiteCol : blackCol);

                _battleGridData.CurrentMovementPointsTexture.SetPixel(x * 2, y * 2, Color.black);
            }
        }

        _battleGridData.ViewTexture.Apply();
        _battleGridData.WalkingPointsTexture.Apply();
        _battleGridData.CurrentMovementPointsTexture.Apply();

        SetDecal();
    }

    private void SetDecal() {
        _decalProjector.material.SetTexture("_MainTex", _battleGridData.ViewTexture);
        _decalProjector.material.SetTexture("_WalkingPointsMap", _battleGridData.WalkingPointsTexture);
        _decalProjector.material.SetTexture("_CurrentMovementPointsTexture", _battleGridData.CurrentMovementPointsTexture);
        _decalProjector.material.SetFloat("_TextureOffset", 0f);
        _decalProjector.material.SetFloat("_WalkPointsTextureOffset", -.0042f);
    }
}