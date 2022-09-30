using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;

public class BattleHandler {
    public bool IsBattleStopped {
        get => _isBattleEnded;
    }

    private const string APPEAR_RANGE = "_AppearRoundRange";
    private const string APPEAR_CENTER_POINT_UV = "_AppearCenterPointUV";
    private List<BloodDecalAppearance> _createdBloodDecals;
    private List<StunEffect> _createdStunEffects;
    private DecalProjector[] _createdUnderUnitWalkingDecals;
    private DecalProjector[] _createdUnderUnitAttackDecals;
    private bool[] _mouseOverSelectionMap, _mouseOverDataSelectionMap, _currentAttackingMap;
    private AssetsContainer _assetsContainer;
    private UIRoot _uiRoot;
    private DecalProjector _decalProjector;
    private DecalProjector _attackRangeDecalProjector;
    private BattleGridData _battleGridData;
    private BattleRaycaster _battleRaycaster;
    private UnitBase[] _selectionForAttackMap;
    private UnitBase _currentSelectedUnit;
    private UnitBase _currentMouseOverSelectionUnit;
    private CameraSimpleFollower _cameraFollower;
    private LineRenderer _movementLinePrefab;
    private LineRenderer _createdLinePrefab;
    private BattleTurnsHandler _turnsHandler;
    private InputService _inputService;
    private ICoroutineService _coroutineService;
    private BattleGridGenerator _gridGenerator;
    private ImposedPairsContainer _imposedPairsContainer;
    private MovementPointer _movementPointerStart, _movementPointerEnd;
    private BattleFieldActionAbility _currentAttackAbility;
    private float _currentAppearRange = 0f;
    private bool _isCurrentlyShowWalkingDistance;
    private bool _isCurrentlyShowAttackingOneToOne;
    private bool _isCurrentlyShowMeleeAttackInRadius;
    private bool _isRestrictedForDoAnything;
    private bool _isBattleEnded;

    public void Init(
        CameraSimpleFollower cameraFollower, BattleGridData battleGridData, DecalProjector decalProjector,
        UIRoot uiRoot, AssetsContainer assetsContainer, LineRenderer movementLinePrefab,
        Transform battleGeneratorTransform, ICoroutineService coroutineService, BattleGridGenerator gridGenerator,
        InputService inputService, bool isAIActing, bool isDebugAIMovementWeights) {
        _inputService = inputService;
        _coroutineService = coroutineService;
        _cameraFollower = cameraFollower;
        _movementLinePrefab = movementLinePrefab;
        _assetsContainer = assetsContainer;
        _uiRoot = uiRoot;
        _gridGenerator = gridGenerator;
        _decalProjector = decalProjector;
        _attackRangeDecalProjector = Object.Instantiate(_assetsContainer.AttackRangeDecalPrefab);
        _attackRangeDecalProjector.gameObject.SetActive(false);
        _battleGridData = battleGridData;
        _mouseOverSelectionMap = new bool[_battleGridData.Units.Count];
        _mouseOverDataSelectionMap = new bool[_battleGridData.Units.Count];
        _currentAttackingMap = new bool[_battleGridData.Units.Count];
        _selectionForAttackMap = new UnitBase[_battleGridData.Units.Count];

        _createdBloodDecals = new List<BloodDecalAppearance>();
        _createdStunEffects = new List<StunEffect>();

        _movementPointerStart = Object.Instantiate(_assetsContainer.MovementPointer);
        _movementPointerEnd = Object.Instantiate(_assetsContainer.MovementPointer);
        _movementPointerStart.gameObject.SetActive(false);
        _movementPointerEnd.gameObject.SetActive(false);

        _imposedPairsContainer = new ImposedPairsContainer(_coroutineService);

        _battleRaycaster = new BattleRaycaster(
            _battleGridData.UnitsLayerMask, _cameraFollower, _battleGridData.GroundLayerMask);

        _turnsHandler = new BattleTurnsHandler(
            _battleGridData, _uiRoot, this,
            _coroutineService, cameraFollower, isAIActing,
            _imposedPairsContainer, isDebugAIMovementWeights);

        _createdUnderUnitWalkingDecals = new DecalProjector[_battleGridData.Units.Count];
        _createdUnderUnitAttackDecals = new DecalProjector[_battleGridData.Units.Count];

        for (int i = 0; i < _battleGridData.Units.Count; i++) {
            _createdUnderUnitWalkingDecals[i] = Object.Instantiate(_assetsContainer.UnderUnitDecalPrefab);
            _createdUnderUnitWalkingDecals[i].gameObject.SetActive(false);
            _createdUnderUnitWalkingDecals[i].transform.SetParent(battleGeneratorTransform);
            
            _createdUnderUnitAttackDecals[i] = Object.Instantiate(_assetsContainer.AttackUnderUnitDecalPrefab);
            _createdUnderUnitAttackDecals[i].gameObject.SetActive(false);
            _createdUnderUnitAttackDecals[i].transform.SetParent(battleGeneratorTransform);

            _battleGridData.Units[i].WithdrawWeapon();
        }

        for (int i = 0; i < _battleGridData.Units.Count; i++) {
            _battleGridData.Units[i].ActivateOverUnitData(true);
        }

        _uiRoot.GetPanel<BattlePanel>().SignOnWaitButton(WaitButtonPressed);
        _uiRoot.GetPanel<BattlePanel>().SignOnBackButton(BackButtonPressed);
        _uiRoot.GetPanel<BattlePanel>().DisableUnitsPanel(this);

        _turnsHandler.StartTurns();
    }

    public void Tick() {
        _currentAttackingMap = new bool[_battleGridData.Units.Count];

        if (_isRestrictedForDoAnything) {
            return;
        }

        if (_isCurrentlyShowAttackingOneToOne) {
            ProcessAttack();
        }

        if (_isCurrentlyShowAttackingOneToOne) {
            if (Input.GetMouseButtonDown(1) && !_inputService.IsPointerOverUIObject() && _currentSelectedUnit != null) {
                SwitchAttacking(AbilityType.None);
                return;
            }
        } else if (_isCurrentlyShowMeleeAttackInRadius) {
            ProcessMeleeRadiusAttack();

            if (Input.GetMouseButtonDown(1) && !_inputService.IsPointerOverUIObject() && _currentSelectedUnit != null) {
                SwitchAttacking(AbilityType.MeleeRangeAttack);
                return;
            }
        } 

        MouseOverSelector();
        ProcessMovementDecalAppearance();

        if (_isCurrentlyShowWalkingDistance) {
            if (Input.GetMouseButtonDown(1) && !_inputService.IsPointerOverUIObject() && _currentSelectedUnit != null) {
                HideWalkingDistance(true);
                return;
            }

            if (Input.GetMouseButtonDown(0) && !_inputService.IsPointerOverUIObject()) {
                SetUnitDestination();
                return;
            } else {
                TryDrawWalkLine();
            }
        }

        SelectionMousePress();
    }

    public void StopBattle(bool isPlayerWon) {
        Object.Destroy(_attackRangeDecalProjector.gameObject);

        _isRestrictedForDoAnything = true;
        _isBattleEnded = true;

        for (int i = 0; i < _createdBloodDecals.Count; i++) {
            _createdBloodDecals[i].DestroyDecal();
        }

        for (int i = 0; i < _createdStunEffects.Count; i++) {
            Object.Destroy(_createdStunEffects[i].gameObject);
        }

        _createdStunEffects.Clear();

        DeselectAllUnits();
        CompletelyDeactivateOverUnitsData();

        _turnsHandler.Cleanup();

        if (isPlayerWon) {
            for (int i = 0; i < _battleGridData.Units.Count; i++) {
                Object.Destroy(_createdUnderUnitWalkingDecals[i].gameObject);
                Object.Destroy(_createdUnderUnitAttackDecals[i].gameObject);

                if (!_battleGridData.Units[i].IsDeadOnBattleField) {
                    if (_battleGridData.Units[i] is PlayerUnit) {
                        _battleGridData.Units[i].HealAfterBattle();
                    }

                    _battleGridData.Units[i].ShealtWeapon();
                } else {
                    if (_battleGridData.Units[i] is EnemyUnit) {
                        _battleGridData.Units[i].DeactivateWeapon();
                    } else {
                        _battleGridData.Units[i].Revive();
                    }
                }
            }

            for (int x = 0; x < _battleGridData.Width; x++) {
                for (int y = 0; y < _battleGridData.Height; y++) {
                    _battleGridData.NodesGrid[x, y].SetPlacedByUnit(false);
                }
            }

            _gridGenerator.StopBattle();
        }
    }

    private void ProcessAttack() {
        UnitBase currentOverSelectedUnitForAttack = _battleRaycaster.GetCurrentMouseOverSelectionUnit();
        bool hasTarget = false;

        for (int i = 0; i < _battleGridData.Units.Count; i++) {
            if (_battleGridData.Units[i] == currentOverSelectedUnitForAttack && _battleGridData.Units[i] is EnemyUnit && _createdUnderUnitAttackDecals[i].gameObject.activeSelf && !_inputService.IsPointerOverUIObject()) {
                _currentAttackingMap[i] = true;
                _selectionForAttackMap[i] = _battleGridData.Units[i];
                hasTarget = true;
            } 
        }

        if (!hasTarget) {
            DeselectAllAttackUnits();
        } else {
            ShowUnitsToAttack();
        }
    }

    private void ProcessMovementDecalAppearance() {
        if (_currentSelectedUnit != null) {
            if (_currentAppearRange <= 1f) {
                _currentAppearRange += Time.deltaTime;
                _decalProjector.material.SetFloat(APPEAR_RANGE, _currentAppearRange);
            }
        }
    }

    private void SelectionMousePress() {
        if (Input.GetMouseButtonDown(0) && !_inputService.IsPointerOverUIObject() && !_isCurrentlyShowMeleeAttackInRadius && _currentMouseOverSelectionUnit != null) {
            if (_currentMouseOverSelectionUnit != _currentSelectedUnit) {
                bool isAttackPress = false;

                for (int i = 0; i < _selectionForAttackMap.Length; i++) {
                    if (_selectionForAttackMap[i] == _currentMouseOverSelectionUnit) {
                        isAttackPress = true;
                        break;
                    }
                }

                if (isAttackPress) {
                    TryAttackUnits(_currentSelectedUnit, new List<UnitBase>() { _currentMouseOverSelectionUnit }, _currentAttackAbility);
                } else {
                    SetUnitSelect(_currentMouseOverSelectionUnit);
                }
            }
        }
    }

    private void MouseOverSelector() {
        if (Time.frameCount % 4 == 0) {
            _currentMouseOverSelectionUnit = _battleRaycaster.GetCurrentMouseOverSelectionUnit();

            if (_currentMouseOverSelectionUnit != null && _currentMouseOverSelectionUnit.IsDeadOnBattleField) {
                _currentMouseOverSelectionUnit = null;
            }

            for (int i = 0; i < _battleGridData.Units.Count; i++) {
                if (!_currentAttackingMap[i] && _battleGridData.Units[i] == _currentMouseOverSelectionUnit && !_mouseOverSelectionMap[i] && !_inputService.IsPointerOverUIObject()) {
                    _mouseOverSelectionMap[i] = true;
                    _battleGridData.Units[i].SetActiveOutline(true);
                } else if (!_currentAttackingMap[i] && _battleGridData.Units[i] != _currentMouseOverSelectionUnit && _mouseOverSelectionMap[i]) {
                    if (!(_currentSelectedUnit != null && _currentSelectedUnit == _battleGridData.Units[i])) {
                        _mouseOverSelectionMap[i] = false;
                        _battleGridData.Units[i].SetActiveOutline(false);
                    }
                }
            }

            for (int i = 0; i < _battleGridData.Units.Count; i++) {
                if (!_currentAttackingMap[i] && _battleGridData.Units[i] == _currentMouseOverSelectionUnit && !_mouseOverDataSelectionMap[i] && !_inputService.IsPointerOverUIObject()) {
                    _mouseOverDataSelectionMap[i] = true;
                    ShowOverUnitData(_battleGridData.Units[i]);
                } else if (!_currentAttackingMap[i] && _battleGridData.Units[i] != _currentMouseOverSelectionUnit && _mouseOverDataSelectionMap[i]) {
                    _mouseOverDataSelectionMap[i] = false;
                    DeactivateOverUnitData(_battleGridData.Units[i], false);
                }
            }
        }
    }

    public void FocusCameraToNearestAllyUnit(UnitBase unitToCheck) {
        float nearestLength = Mathf.Infinity;
        UnitBase nearestChar = null;

        for (int i = 0; i < _battleGridData.Units.Count; i++) {
            if (_battleGridData.Units[i] is PlayerUnit && !_battleGridData.Units[i].IsDeadOnBattleField && _turnsHandler.IsCanUnitWalk(_battleGridData.Units[i])) {
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

    public void AbilityButtonPressed(BattleFieldActionAbility ability, bool imposed) {
        if (ability.Type == AbilityType.Walk) {
            if (!imposed) {
                SwitchWalking();
            } else {
                AbortImposingButtonPressed();
            }
        } else if (
            ability.Type == AbilityType.MeleeOneToOneAttack ||
            ability.Type == AbilityType.MeleeRangeAttack || 
            ability.Type == AbilityType.ShotAttack) {
            _currentAttackAbility = ability;
            SwitchAttacking(ability.Type);
        }
    }

    public void AbilityButtonPointerEnter(BattleFieldActionAbility ability, bool imposed) {
        if (ability.Type == AbilityType.Walk && !imposed) {
            WalkingPointerEnter();
        }
    }

    public void AbilityButtonPointerExit(BattleFieldActionAbility ability, bool imposed) {
        if (ability.Type == AbilityType.Walk && !imposed) {
            WalkingPointerExit();
        }
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

                _movementPointerStart.gameObject.SetActive(true);
                _movementPointerEnd.gameObject.SetActive(true);

                _movementPointerStart.transform.position = path[0];
                _movementPointerEnd.transform.position = path[^1];

                //SetDecal();
            }
        }
        
        _prevMovementNode = endNodeCheckInWorld;
    }

    private void SwitchWalking() {
        if (_isRestrictedForDoAnything || !_turnsHandler.IsCanUnitWalk(_currentSelectedUnit) || !_turnsHandler.IsItCurrentWalkingUnit(_currentSelectedUnit)) {
            return;
        }

        if (_isCurrentlyShowAttackingOneToOne || _isCurrentlyShowMeleeAttackInRadius) {
            StopAttackProcess();
        }

        _isCurrentlyShowWalkingDistance = !_isCurrentlyShowWalkingDistance;

        if (_isCurrentlyShowWalkingDistance && !_decalProjector.gameObject.activeSelf) {
            ShowUnitWalkingDistance(_currentSelectedUnit);
        }

        if (!_isCurrentlyShowWalkingDistance) {
            HideWalkingDistance(false);
        }
    }

    private void ShowWalkingDistance() {
        if (!_isCurrentlyShowWalkingDistance || !_turnsHandler.IsCanUnitWalk(_currentSelectedUnit)) {
            ShowUnitWalkingDistance(_currentSelectedUnit);
        }
    }

    private void WalkingPointerEnter() {
        if (_isRestrictedForDoAnything || _isCurrentlyShowAttackingOneToOne || !_turnsHandler.IsCanUnitWalk(_currentSelectedUnit) || !_turnsHandler.IsItCurrentWalkingUnit(_currentSelectedUnit)) {
            return;
        }

        ShowWalkingDistance();
    }

    private void WalkingPointerExit() {
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

            _movementPointerStart.gameObject.SetActive(false);
            _movementPointerEnd.gameObject.SetActive(false);
        }
    }

    public List<Node> GetPossibleWalkNodesForUnit(UnitBase unit) {
        float unitMaxWalkDistance = _turnsHandler.GetLastLengthForUnit(unit);

        if (unitMaxWalkDistance < 1f) {
            HideWalkingDistance(false);
            return null;
        }

        Vector3 currentUnitPosition = unit.transform.position;

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
            }
        }

        for (int i = 0; i < _battleGridData.Units.Count; i++) {
            Node unitNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(_battleGridData.Units[i].transform.position);

            if (unitNode == startNode) {
                unitNode.SetPlacedByUnit(false);
            } else if (!_battleGridData.Units[i].IsDeadOnBattleField) {
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

        return resultNodes;
    }

    private void ShowUnitWalkingDistance(UnitBase unitToShow) {
        SetActiveUnderUnitsDecals(true);

        float unitMaxWalkDistance = _turnsHandler.GetLastLengthForUnit(unitToShow);

        if (unitMaxWalkDistance < 1f) {
            HideWalkingDistance(false);
            return;
        }
        
        _decalProjector.gameObject.SetActive(true);
        _currentAppearRange = 0f;
        _decalProjector.material.SetFloat(APPEAR_RANGE, 0f);

        Vector3 currentUnitPosition = unitToShow.transform.position;

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
            }
        }

        for (int i = 0; i < _battleGridData.Units.Count; i++) {
            Node unitNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(_battleGridData.Units[i].transform.position);

            if (unitNode == startNode) {
                unitNode.SetPlacedByUnit(false);
            } else if (!_battleGridData.Units[i].IsDeadOnBattleField) {
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
        _decalProjector.material.SetColor("_FillColor", unitToShow.GetWalkingGridColor().SetTransparency(1f));
        _decalProjector.material.SetColor("_OutlineColor", unitToShow.GetWalkingGridColor());

        ShowView();
    }

    private void SetActiveUnderUnitsDecals(bool value) {
        if (_currentSelectedUnit == null || _currentSelectedUnit is EnemyUnit) {
            return;
        }

        for (int i = 0; i < _battleGridData.Units.Count; i++) {
            _createdUnderUnitWalkingDecals[i].gameObject.SetActive(!_battleGridData.Units[i] .IsDeadOnBattleField && _battleGridData.Units[i] != _currentSelectedUnit && value);
            _createdUnderUnitWalkingDecals[i].transform.position = _battleGridData.Units[i].transform.position + Vector3.up / 2f;
            _createdUnderUnitWalkingDecals[i].material = Object.Instantiate(_createdUnderUnitWalkingDecals[i].material);
            _createdUnderUnitWalkingDecals[i].material.SetColor("_Color", _battleGridData.Units[i].GetUnitColor().SetTransparency(1f));
            _createdUnderUnitWalkingDecals[i].material.SetFloat("_IsShowAttackDirection", _imposedPairsContainer.HasPairWith(_battleGridData.Units[i]) ? 1f : 0f);

            Vector2 attackDirectionVector = _imposedPairsContainer.GetAttackDirectionFor(_battleGridData.Units[i]);

            float rotation = 0f;

            if (attackDirectionVector.x != 0f) {
                rotation = -90f * attackDirectionVector.x;
            } else if (attackDirectionVector.y == 1f) {
                rotation = 180f;
            }

            _createdUnderUnitWalkingDecals[i].material.SetFloat("_Rotation", rotation);
        }
    }

    private void SetUnitDestination() {
        Node startMovementNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(_currentSelectedUnit.transform.position);

        if (startMovementNode != _endMovementNode) {
            _cameraFollower.SetTarget(_currentSelectedUnit.transform);
            _isCurrentlyShowWalkingDistance = false;
            _isRestrictedForDoAnything = true;
            HideWalkingDistance(true);
            _turnsHandler.SetCurrentWalker(_currentSelectedUnit);
            _currentSelectedUnit.StartMove();
            _turnsHandler.RemovePossibleLengthForUnit(_currentSelectedUnit, _battleGridData.GlobalGrid.GetPathLength(startMovementNode, _endMovementNode));
            _currentSelectedUnit.GoToPoint(_endMovementNode.WorldPosition, false, true, null, () => UnitEndedWalkAction(_currentSelectedUnit));
        }
    }

    private void UnitEndedWalkAction(UnitBase unitWalked) {
        HideWalkingDistance(true);
        _isRestrictedForDoAnything = false;
        TryTurnEnemiesOnUnitEndWalking(unitWalked);
    }

    private void TryTurnEnemiesOnUnitEndWalking(UnitBase unitWalked) {
        bool isPlayerUnitWalked = unitWalked is PlayerUnit;

        for (int i = 0; i < _battleGridData.Units.Count; i++) {
            if (_battleGridData.Units[i] != unitWalked) {
                if (isPlayerUnitWalked && _battleGridData.Units[i] is EnemyUnit || !isPlayerUnitWalked && _battleGridData.Units[i] is PlayerUnit) {
                    if (IsTargetInMeleeAttackRange(unitWalked, _battleGridData.Units[i]) && !_imposedPairsContainer.HasPairWith(_battleGridData.Units[i])) {
                        _coroutineService.StartCoroutine(RotateUnitToTarget(unitWalked, _battleGridData.Units[i]));
                    }
                } 
            }
        }
    }

    private IEnumerator RotateUnitToTarget(UnitBase toWhomRotate, UnitBase unitNeedsToBeRotated) {
        float t = 0f;

        Quaternion startTargetRotation = unitNeedsToBeRotated.transform.rotation;
        Quaternion endTargetRotation = Quaternion.LookRotation((toWhomRotate.transform.position - unitNeedsToBeRotated.transform.position).RemoveYCoord());

        while (t <= 1f) {
            t += Time.deltaTime * 5f;

            unitNeedsToBeRotated.transform.rotation = Quaternion.Slerp(startTargetRotation, endTargetRotation, t);

            yield return null;
        }
    }
    // END WALK MODULE

    // ATTACK MODULE
    public void SwitchAttacking(AbilityType attackType) {
        if (_isRestrictedForDoAnything || _isCurrentlyShowWalkingDistance || !_turnsHandler.IsItCurrentWalkingUnit(_currentSelectedUnit)) {
            return;
        }

        bool isCurrentlyAttacking = false;

        if (_isCurrentlyShowAttackingOneToOne || _isCurrentlyShowMeleeAttackInRadius) {
            isCurrentlyAttacking = true;

            if (_isCurrentlyShowWalkingDistance) {
                WalkingPointerExit();
            }
        } else {
            for (int i = 0; i < _createdUnderUnitAttackDecals.Length; i++) {
                if (_createdUnderUnitAttackDecals[i] != null) {
                    isCurrentlyAttacking = true;
                    break;
                }
            }
        }

        if (attackType == AbilityType.MeleeOneToOneAttack) {
            if (isCurrentlyAttacking) {
                _isCurrentlyShowAttackingOneToOne = !_isCurrentlyShowAttackingOneToOne;
                FindPossibleTargetsForAttack_MELEE(_isCurrentlyShowAttackingOneToOne);
            }
        } else if (attackType == AbilityType.MeleeRangeAttack) {
            if (isCurrentlyAttacking) {
                _isCurrentlyShowMeleeAttackInRadius = !_isCurrentlyShowMeleeAttackInRadius;
                FindPossibleTargetsForAttack_MELEE_POINTER_RADIUS(_isCurrentlyShowMeleeAttackInRadius);
            } 
        } else if (attackType == AbilityType.ShotAttack){

        }
    }

    private bool IsTargetInMeleeAttackRange(UnitBase unitCentre, UnitBase unitToCheck) {
        Node currentUnitNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(unitCentre.transform.position);

        for (int x = -1; x < 2; x++) {
            for (int y = -1; y < 2; y++) {
                if (x == 0 || y == 0) {
                    if (x == 0 && y == 0) {
                        continue;
                    }

                    for (int i = 0; i < _battleGridData.Units.Count; i++) {
                        if (!_battleGridData.Units[i].IsDeadOnBattleField && _battleGridData.Units[i] != _currentSelectedUnit && _battleGridData.Units[i] is EnemyUnit) {
                            Node targetUnitNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(_battleGridData.Units[i].transform.position);

                            if (currentUnitNode.GridX + x == targetUnitNode.GridX && currentUnitNode.GridY + y == targetUnitNode.GridY) {
                                if (_battleGridData.Units[i] == unitToCheck) {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
        }

        return false;
    }

    private Node[,] _attackRadiusNodesMatrix = new Node[0, 0];
    private void ProcessMeleeRadiusAttack() {
        Vector3 lookDirection = (_cameraFollower.Camera.WorldToScreenPoint(_currentSelectedUnit.transform.position) - Input.mousePosition).normalized;
        Vector3 targetLookDirection = new Vector3(lookDirection.x, _currentSelectedUnit.transform.forward.y, lookDirection.y);
        bool isImposed = _imposedPairsContainer.HasPairWith(_currentSelectedUnit);

        if (isImposed || Vector3.Dot(targetLookDirection, _currentSelectedUnit.transform.forward) <= .99f) {
            if (!isImposed) {
                _currentSelectedUnit.transform.forward = Vector3.Lerp(_currentSelectedUnit.transform.forward, targetLookDirection, 15f * Time.deltaTime);
            }

            if (Time.frameCount % 4 == 0) {
                _attackRadiusNodesMatrix = _battleGridData.GlobalGrid.GetNodesInRadius(_currentSelectedUnit.transform.position, _currentSelectedUnit.transform.forward, 20f, 90f);
                //_attackRadiusNodesMatrix = _battleGridData.GlobalGrid.GetNodesInRadius(_currentSelectedUnit.transform.position, _currentSelectedUnit.transform.forward, 5f, 90f);

                if (_attackRadiusNodesMatrix.GetLength(0) != 0) {
                    _attackRangeDecalProjector.gameObject.SetActive(true);
                    _attackRangeDecalProjector.transform.position = _currentSelectedUnit.transform.position + Vector3.up * _attackRangeDecalProjector.size.z / 2f;
                    _attackRangeDecalProjector.size = new Vector3(_attackRadiusNodesMatrix.GetLength(0) * _battleGridData.GlobalGrid.NodeRadius * 2f, _attackRadiusNodesMatrix.GetLength(1) * _battleGridData.GlobalGrid.NodeRadius * 2f, _decalProjector.size.z);

                    Texture2D attackRangeDrawingTexture = new Texture2D(_attackRadiusNodesMatrix.GetLength(0) * 2, _attackRadiusNodesMatrix.GetLength(1) * 2);
                    Color destinatedColor = Color.black;

                    for (int x = 0; x < _attackRadiusNodesMatrix.GetLength(0); x++) {
                        for (int y = 0; y < _attackRadiusNodesMatrix.GetLength(1); y++) {
                            destinatedColor = _attackRadiusNodesMatrix[x, y] == null ? Color.black : Color.white;
                            attackRangeDrawingTexture.SetPixel(x * 2, y * 2, destinatedColor);
                            attackRangeDrawingTexture.SetPixel(x * 2 + 1, y * 2, destinatedColor);
                            attackRangeDrawingTexture.SetPixel(x * 2, y * 2 + 1, destinatedColor);
                            attackRangeDrawingTexture.SetPixel(x * 2 + 1, y * 2 + 1, destinatedColor);
                        }
                    }

                    attackRangeDrawingTexture.Apply();

                    _attackRangeDecalProjector.material.SetTexture("_MainTex", attackRangeDrawingTexture);
                }

                for (int i = 0; i < _battleGridData.Units.Count; i++) {
                    bool isActivate = false;
                    Node unitNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(_battleGridData.Units[i].transform.position);

                    for (int x = 0; x < _attackRadiusNodesMatrix.GetLength(0); x++) {
                        for (int y = 0; y < _attackRadiusNodesMatrix.GetLength(1); y++) {
                            if (_attackRadiusNodesMatrix[x, y] != null) {
                                if (unitNode == _attackRadiusNodesMatrix[x, y]) {
                                    isActivate = true;
                                    if (!_battleGridData.Units[i].IsDeadOnBattleField) {
                                        ActivateAttackDecalUnderUnit(_createdUnderUnitAttackDecals[i], _battleGridData.Units[i]);
                                    }
                                } 
                            }
                        }
                    }

                    if (!isActivate) {
                        _createdUnderUnitAttackDecals[i].gameObject.SetActive(false);
                        DeactivateOverUnitData(_battleGridData.Units[i], false);
                    } else {
                        if (!_battleGridData.Units[i].IsDeadOnBattleField && _currentMouseOverSelectionUnit != _battleGridData.Units[i]) {
                            _battleGridData.Units[i].ActivateOverUnitData(true, _currentSelectedUnit.GetUnitConfig(), _imposedPairsContainer.HasPairWith(_battleGridData.Units[i]));
                        }
                    }
                }
            }
        }

        if (Input.GetMouseButtonDown(0) && !_inputService.IsPointerOverUIObject()) {
            List<UnitBase> unitsToAttackInRange = new List<UnitBase>();

            for (int i = 0; i < _battleGridData.Units.Count; i++) {
                if (!_battleGridData.Units[i].IsDeadOnBattleField) {
                    Node unitNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(_battleGridData.Units[i].transform.position);

                    bool isBreakLoop = false;
                    for (int x = 0; x < _attackRadiusNodesMatrix.GetLength(0); x++) {
                        for (int y = 0; y < _attackRadiusNodesMatrix.GetLength(1); y++) {
                            if (_attackRadiusNodesMatrix[x, y] != null) {
                                if (unitNode == _attackRadiusNodesMatrix[x, y]) {
                                    unitsToAttackInRange.Add(_battleGridData.Units[i]);
                                    isBreakLoop = true;
                                }
                            }

                            if (isBreakLoop) {
                                break;
                            }
                        }

                        if (isBreakLoop) {
                            break;
                        }
                    }
                }

                _battleGridData.Units[i].DeactivateOverUnitData(false);
                _createdUnderUnitAttackDecals[i].gameObject.SetActive(false);
            }

            _attackRangeDecalProjector.gameObject.SetActive(false);
            TryAttackUnits(_currentSelectedUnit, unitsToAttackInRange, _currentAttackAbility);
        }
    }

    private void FindPossibleTargetsForAttack_MELEE_POINTER_RADIUS(bool value) {
        if (value) {

        } else {
            StopAttackProcess();
        }
    }
    
    private void ActivateAttackDecalUnderUnit(DecalProjector decal, UnitBase unit) {
        decal.gameObject.SetActive(true);
        decal.transform.position = unit.transform.position + Vector3.up / 3f;
        decal.material = Object.Instantiate(decal.material);
        decal.material.SetColor("_Color", decal.material.GetColor("_DefaultColor"));
    }

    private void FindPossibleTargetsForAttack_MELEE(bool value) {
        if (value) {
            Node currentUnitNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(_currentSelectedUnit.transform.position);

            if (_imposedPairsContainer.HasPairWith(_currentSelectedUnit)) {
                UnitBase imposedUnit = _imposedPairsContainer.GetPairFor(_currentSelectedUnit);

                for (int i = 0; i < _battleGridData.Units.Count; i++) {
                    if (_battleGridData.Units[i] == imposedUnit) {
                        ActivateAttackDecalUnderUnit(_createdUnderUnitAttackDecals[i], _battleGridData.Units[i]);
                    }
                }
            } else {
                for (int x = -1; x < 2; x++) {
                    for (int y = -1; y < 2; y++) {
                        if (x == 0 || y == 0) {
                            if (x == 0 && y == 0) {
                                continue;
                            }

                            for (int i = 0; i < _battleGridData.Units.Count; i++) {
                                if (!_battleGridData.Units[i].IsDeadOnBattleField && _battleGridData.Units[i] != _currentSelectedUnit && _battleGridData.Units[i] is EnemyUnit) {
                                    Node targetUnitNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(_battleGridData.Units[i].transform.position);

                                    if (currentUnitNode.GridX + x == targetUnitNode.GridX && currentUnitNode.GridY + y == targetUnitNode.GridY) {
                                        _createdUnderUnitAttackDecals[i].gameObject.SetActive(true);
                                        _createdUnderUnitAttackDecals[i].transform.position = _battleGridData.Units[i].transform.position + Vector3.up / 3f;
                                        _createdUnderUnitAttackDecals[i].material = Object.Instantiate(_createdUnderUnitAttackDecals[i].material);
                                        _createdUnderUnitAttackDecals[i].material.SetColor("_Color", _createdUnderUnitAttackDecals[i].material.GetColor("_DefaultColor"));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        } else {
            StopAttackProcess();
        }
    }

    private void StopAttackProcess() {
        _isCurrentlyShowAttackingOneToOne = false;
        _isCurrentlyShowMeleeAttackInRadius = false;
        _attackRangeDecalProjector.gameObject.SetActive(false);
        for (int i = 0; i < _battleGridData.Units.Count; i++) {
            _createdUnderUnitAttackDecals[i].gameObject.SetActive(false);
            if (_battleGridData.Units[i] != _currentMouseOverSelectionUnit) {
                DeactivateOverUnitData(_battleGridData.Units[i], false);
            }
        }
        DeselectAllAttackUnits();
    }

    public void ProcessAIAttack(UnitBase attacker, List<UnitBase> targets, BattleFieldActionAbility ability, System.Action callback, bool isImposedAttack = false, bool isPossibilityAttack = false) {
        _coroutineService.StartCoroutine(AttackSequence(attacker, targets, ability, isImposedAttack, isPossibilityAttack, callback));
    }

    private void TryAttackUnits(UnitBase attacker, List<UnitBase> targets, BattleFieldActionAbility ability, bool isImposedAttack = false, bool isPossibilityAttack = false) {
        _coroutineService.StartCoroutine(AttackSequence(attacker, targets, ability, isImposedAttack, isPossibilityAttack, null));
    }

    private IEnumerator AttackSequence(UnitBase attacker, List<UnitBase> targets, BattleFieldActionAbility ability, bool isImposedAttack, bool isPossibilityAttack, System.Action callback) {
        _isRestrictedForDoAnything = true;
        _uiRoot.GetPanel<BattlePanel>().DisableUnitsPanel(this);

        attacker.IsBusy = true;

        for (int i = 0; i < targets.Count; i++) {
            targets[i].IsBusy = true;
        }

        float t = 0f;

        if (!isImposedAttack && targets.Count == 1) {
            Quaternion startAttackerRotation = attacker.transform.rotation;
            Quaternion startTargetRotation = targets[0].transform.rotation;
            Quaternion endAttackerRotation = Quaternion.LookRotation((targets[0].transform.position - attacker.transform.position).RemoveYCoord());
            Quaternion endTargetRotation = Quaternion.LookRotation((attacker.transform.position - targets[0].transform.position).RemoveYCoord());

            StopAttackProcess();

            bool isTargetImposed = _imposedPairsContainer.HasPairWith(targets[0]);

            while (t <= 1f) {
                t += Time.deltaTime * 5f;

                attacker.transform.rotation = Quaternion.Slerp(startAttackerRotation, endAttackerRotation, t);

                if (!isTargetImposed) {
                    targets[0].transform.rotation = Quaternion.Slerp(startTargetRotation, endTargetRotation, t);
                }

                yield return null;
            }
        }

        t = 0f;

        if (!isImposedAttack) {
            _turnsHandler.UnitUsedAbility(attacker, ability);
        }

        bool endAttack = false;
        List<bool> isDeadMap = new List<bool>();

        for (int i = 0; i < targets.Count; i++) {
            isDeadMap.Add(false);
        }

        attacker.PlayAttackAnimation();
        attacker.GetUnitSkinContainer().SignOnAttackAnimation(() => {
            endAttack = true;

            for (int i = 0; i < targets.Count; i++) {
                isDeadMap[i] = DealDamageOnUnit(targets[i], attacker);
            }
        });        
        attacker.GetUnitSkinContainer().SignOnAttackAnimation(() => {
            for (int i = 0; i < targets.Count; i++) {
                BloodDecalAppearance bloodDecal = Object.Instantiate(_assetsContainer.BloodDecal);
                _createdBloodDecals.Add(bloodDecal);
                Object.Instantiate(_assetsContainer.BloodImpact, targets[i].GetAttackPoint().position, Quaternion.LookRotation(attacker.transform.forward));
                bloodDecal.ThrowDecalOnSurface(targets[i].GetAttackPoint().position, attacker.transform.forward);
            }
        });

        yield return new WaitWhile(() => !endAttack);

        for (int i = 0; i < targets.Count; i++) {
            if (!isDeadMap[i]) {
                targets[i].PlayImpactFromSwordAnimation();
            }
        }

        yield return new WaitForSeconds(isDeadMap.Contains(true) ? 2f : 1f);

        attacker.IsBusy = false;

        for (int i = 0; i < targets.Count; i++) {
            targets[i].IsBusy = false;
        }

        for (int i = 0; i < targets.Count; i++) {
            if (isDeadMap[i]) {
                DeselectAllUnits();
                UnitDeadEvent(targets[i]);
            }
        }

        if (_isBattleEnded) {
            foreach (var item in _createdStunEffects) {
                Object.Destroy(item.gameObject);
            }

            _createdStunEffects.Clear();

            yield break;
        }

        _turnsHandler.ForceFillTurns();

        if (!isImposedAttack) {
            List<UnitBase> targetsToTryImpose = new List<UnitBase>(targets);

            for (int i = targetsToTryImpose.Count - 1; i >= 0; i--) {
                if (attacker.GetType() == targetsToTryImpose[i].GetType()) {
                    targetsToTryImpose.RemoveAt(i);
                }
            }

            if (targetsToTryImpose.Count != 0) {
                UnitBase unitToImpose = null;
                float nearestDotToTarget = -1f;

                for (int i = 0; i < targetsToTryImpose.Count; i++) {
                    Node attackerNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(attacker.transform.position);
                    Node targetNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(targetsToTryImpose[i].transform.position);

                    if (_battleGridData.GlobalGrid.IsNodesContacts(targetNode, attackerNode)) {
                        float dot = Vector3.Dot(attacker.transform.forward, (targetsToTryImpose[i].transform.position - attacker.transform.position).normalized);

                        if (dot > nearestDotToTarget) {
                            nearestDotToTarget = dot;
                            unitToImpose = targetsToTryImpose[i];
                        }
                    }
                }

                if (unitToImpose != null) {
                    _imposedPairsContainer.TryCreateNewPair(attacker, unitToImpose);
                }
            }
        }

        bool isReactivatePanel = (!isPossibilityAttack || isPossibilityAttack && targets.Count == 1 && targets[0] is PlayerUnit && !isDeadMap[0]) && !_uiRoot.GetPanel<BattlePanel>().IsUnitPanelAlreadyEnabled();

        if (isReactivatePanel && _currentSelectedUnit != null) {
            _uiRoot.GetPanel<BattlePanel>().EnableUnitPanel(
                this, _currentSelectedUnit, UnitPanelState.UseTurn,
                _turnsHandler, _imposedPairsContainer.HasPairWith(_currentSelectedUnit));
            SetUnitSelect(_currentSelectedUnit);
        } 

        callback?.Invoke();
        _isRestrictedForDoAnything = false;
    }

    private bool DealDamageOnUnit(UnitBase unit, UnitBase attacker) {
        float damage = attacker.GetUnitConfig().DefaultAttackDamage;

        _uiRoot.DynamicObjectsPanel.SpawnDamageNumber(Mathf.RoundToInt(damage), false, unit.transform.position + Vector3.up * 3f, _cameraFollower.Camera);

        return unit.TakeDamage(damage);
    }

    private void UnitDeadEvent(UnitBase unit) {
        if (unit is PlayerUnit) {
            _createdStunEffects.Add(unit.CreateStunParticle());
        }

        unit.DeactivateOverUnitData(true);
        _imposedPairsContainer.TryRemovePair(unit);
        _turnsHandler.MarkUnitAsDead(unit, _currentSelectedUnit == unit);
    }

    private void AbortImposingButtonPressed() {
        StopAttackProcess();
        UnitBase unitAttacker = _imposedPairsContainer.GetPairFor(_currentSelectedUnit);
        _imposedPairsContainer.TryRemovePair(_currentSelectedUnit);
        TryAttackUnits(unitAttacker, new List<UnitBase>() { _currentSelectedUnit }, unitAttacker.GetDefaultUnitAttackAbility(), true, true);
    }
    // END ATTACK MODULE

    public void OnTurnButtonEntered(UnitBase unit) {
        unit.SetActiveOutline(true);
        unit.ActivateOverUnitData(false, null, _imposedPairsContainer.HasPairWith(unit));
    }

    public void OnTurnButtonExit(UnitBase unit) {
        unit.SetActiveOutline(false);
        unit.DeactivateOverUnitData(false);
    }

    private void WaitButtonPressed() {
        if (_isRestrictedForDoAnything) {
            return;
        }

        if (_isCurrentlyShowAttackingOneToOne) {
            FindPossibleTargetsForAttack_MELEE(false);
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

            _movementPointerStart.gameObject.SetActive(false);
            _movementPointerEnd.gameObject.SetActive(false);
        }

        _decalProjector.gameObject.SetActive(false);

        _currentSelectedUnit?.DestroySelection();
        _currentSelectedUnit?.SetActiveOutline(false);
        _currentSelectedUnit = unit;
        _currentSelectedUnit.SetActiveOutline(true);
        _currentSelectedUnit.CreateSelectionAbove();

        StopAttackProcess();

        UnitPanelState viewState = UnitPanelState.CompletelyDeactivate;
        
        if (_currentSelectedUnit is PlayerUnit && _turnsHandler.IsItCurrentWalkingUnit(_currentSelectedUnit) && _turnsHandler.IsCanUnitWalk(_currentSelectedUnit)) {
            viewState = UnitPanelState.UseTurn;
        } else {
            viewState = UnitPanelState.ViewTurn;
        }

        _uiRoot.GetPanel<BattlePanel>().DisableUnitsPanel(this);
        _uiRoot.GetPanel<BattlePanel>().EnableUnitPanel(
            this, _currentSelectedUnit, viewState, 
            _turnsHandler, _imposedPairsContainer.HasPairWith(_currentSelectedUnit));

        if (_turnsHandler.IsHaveCurrentWalkingUnit() && !_turnsHandler.IsItCurrentWalkingUnit(_currentSelectedUnit)) {
            _uiRoot.GetPanel<BattlePanel>().SetActiveBackToUnitButton(true);
        } else {
            _uiRoot.GetPanel<BattlePanel>().SetActiveBackToUnitButton(false);
        }

        if (unit is EnemyUnit) {
            ShowUnitWalkingDistance(unit);
        }
    }

    private void DeselectAllUnits() {
        for (int i = 0; i < _battleGridData.Units.Count; i++) {
            _battleGridData.Units[i].SetActiveOutline(false);
            _battleGridData.Units[i].DestroySelection();
        }
    }

    private void ShowUnitsToAttack() {
        for (int i = 0; i < _selectionForAttackMap.Length; i++) {
            if (_currentAttackingMap[i]) {
                _createdUnderUnitAttackDecals[i].material.SetColor("_Color", _createdUnderUnitAttackDecals[i].material.GetColor("_SelectedColor"));
                _battleGridData.Units[i].SetActiveOutline(true);
                ShowOverUnitData(_battleGridData.Units[i], _currentSelectedUnit.GetUnitConfig());
            }
        }
    }

    private void CompletelyDeactivateOverUnitsData() {
        for (int i = 0; i < _battleGridData.Units.Count; i++) {
            _battleGridData.Units[i].DeactivateOverUnitData(true);
        }
    }

    private void DeselectAllAttackUnits() {
        for (int i = 0; i < _selectionForAttackMap.Length; i++) {
            if (_selectionForAttackMap[i] != null) {
                _selectionForAttackMap[i].SetActiveOutline(false);
                DeactivateOverUnitData(_selectionForAttackMap[i], false);
                _selectionForAttackMap[i] = null;
                _createdUnderUnitAttackDecals[i].material.SetColor("_Color", _createdUnderUnitAttackDecals[i].material.GetColor("_DefaultColor"));
            }
        }
    }

    private void DestroySelectionOnCurrentUnit() {
        _currentSelectedUnit?.DestroySelection();
        _currentSelectedUnit?.SetActiveOutline(false);
    }

    private void ShowOverUnitData(UnitBase unit, UnitStatsConfig attackerConfig = null) {
        unit.ActivateOverUnitData(false, attackerConfig, _imposedPairsContainer.HasPairWith(unit));
    }

    private void DeactivateOverUnitData(UnitBase unit, bool isBattleEnded) {
        unit.DeactivateOverUnitData(isBattleEnded);
    }

    public void SetEnemyTurn() {
        _currentSelectedUnit?.DestroySelection();
        _currentSelectedUnit?.SetActiveOutline(false);
        _uiRoot.GetPanel<BattlePanel>().DisableUnitsPanel(this);
    }

    public void SetRestriction(bool value) {
        _isRestrictedForDoAnything = value;
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
            }
        }

        _battleGridData.ViewTexture.Apply();
        _battleGridData.WalkingPointsTexture.Apply();

        SetDecal();
    }

    private void SetDecal() {
        _decalProjector.material.SetTexture("_MainTex", _battleGridData.ViewTexture);
        _decalProjector.material.SetTexture("_WalkingPointsMap", _battleGridData.WalkingPointsTexture);
        _decalProjector.material.SetFloat("_TextureOffset", 0f);
        _decalProjector.material.SetFloat("_WalkPointsTextureOffset", -.0042f);
    }
}