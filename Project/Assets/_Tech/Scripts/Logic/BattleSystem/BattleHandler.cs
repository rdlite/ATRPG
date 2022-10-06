using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

public class BattleHandler
{
    public UnitBase CurrentMouseOverSelectionUnit;
    public UnitBase CurrentSelectedUnit;
    public DecalProjector BattleGridDecalProjector;
    public DecalProjector AttackRangeDecalProjector;
    public bool IsBattleEnded;

    private BattleStateMachine _battleSM;
    private LineRenderer _movementLinePrefab;
    private LineRenderer _createdMovementLine;
    private const string APPEAR_CENTER_POINT_UV = "_AppearCenterPointUV";
    private const string APPEAR_RANGE = "_AppearRoundRange";
    private float _currentAppearRange = 0f;

    private List<BloodDecalAppearance> _createdBloodDecals;
    private List<PushPointer> _createdPushPointers;
    private List<StunEffect> _createdStunEffects;
    private DecalProjector[] _createdUnderUnitWalkingDecals;
    private DecalProjector[] _createdUnderUnitAttackDecals;
    private BattleGridData _battleGridData;
    private BattleTurnsHandler _turnsHandler;
    private InputService _inputService;
    private ImposedPairsContainer _imposedPairsContainer;
    private BattleRaycaster _battleRaycaster;
    private ICoroutineService _coroutineService;
    private UIRoot _uiRoot;
    private CameraSimpleFollower _cameraFollower;
    private AssetsContainer _assetsContainer;
    private Transform _bloodDecalsContainer;
    private bool _isCurrentlyShowWalkingDistance;

    public void Init(
        CameraSimpleFollower cameraFollower, BattleGridData battleGridData, DecalProjector decalProjector,
        UIRoot uiRoot, AssetsContainer assetsContainer, LineRenderer movementLinePrefab,
        Transform battleGeneratorTransform, ICoroutineService coroutineService, BattleGridGenerator gridGenerator,
        InputService inputService, bool isAIActing, bool isDebugAIMovementWeights,
        Transform bloodDecalsContainer)
    {
        _coroutineService = coroutineService;
        _uiRoot = uiRoot;
        _cameraFollower = cameraFollower;
        _assetsContainer = assetsContainer;
        _bloodDecalsContainer = bloodDecalsContainer;
        BattleGridDecalProjector = decalProjector;

        _battleGridData = battleGridData;
        _inputService = inputService;
        _movementLinePrefab = movementLinePrefab;

        _battleRaycaster = new BattleRaycaster(
            battleGridData.UnitsLayerMask, cameraFollower, battleGridData.GroundLayerMask);

        _imposedPairsContainer = new ImposedPairsContainer(coroutineService);

        _battleSM = new BattleStateMachine();

        _turnsHandler = new BattleTurnsHandler(
            battleGridData, uiRoot, this,
            coroutineService, cameraFollower, isAIActing,
            _imposedPairsContainer, isDebugAIMovementWeights, _battleSM);

        _battleSM.Init(
            this, battleGridData, inputService,
            _battleRaycaster, _turnsHandler, uiRoot,
            _imposedPairsContainer, movementLinePrefab, assetsContainer,
            coroutineService, cameraFollower, _turnsHandler.GetAIMovementResolver(),
            gridGenerator);

        _createdUnderUnitWalkingDecals = new DecalProjector[battleGridData.Units.Count];
        _createdUnderUnitAttackDecals = new DecalProjector[battleGridData.Units.Count];

        for (int i = 0; i < battleGridData.Units.Count; i++)
        {
            _createdUnderUnitWalkingDecals[i] = Object.Instantiate(assetsContainer.UnderUnitDecalPrefab);
            _createdUnderUnitWalkingDecals[i].transform.SetParent(battleGeneratorTransform);
            _createdUnderUnitWalkingDecals[i].gameObject.SetActive(false);

            _createdUnderUnitAttackDecals[i] = Object.Instantiate(assetsContainer.AttackUnderUnitDecalPrefab);
            _createdUnderUnitAttackDecals[i].transform.SetParent(battleGeneratorTransform);
            SetActiveAttackDecalUnderUnit(i, false);

            battleGridData.Units[i].WithdrawWeapon();
        }

        for (int i = 0; i < battleGridData.Units.Count; i++)
        {
            battleGridData.Units[i].ActivateOverUnitData(true);
        }

        uiRoot.GetPanel<BattlePanel>().SignOnWaitButton(WaitButtonPressed);
        uiRoot.GetPanel<BattlePanel>().SignOnBackButton(BackButtonPressed);
        uiRoot.GetPanel<BattlePanel>().DisableUnitsPanel(this);

        _createdBloodDecals = new List<BloodDecalAppearance>();
        _createdStunEffects = new List<StunEffect>();
        _createdPushPointers = new List<PushPointer>();

        AttackRangeDecalProjector = Object.Instantiate(assetsContainer.AttackRangeDecalPrefab);
        AttackRangeDecalProjector.gameObject.SetActive(false);

        _battleSM.Enter<StartBattleState>();
    }

    public void Tick()
    {
        _battleSM.UpdateState();

        ProcessMovementDecalAppearance();

        if (!_inputService.IsPointerOverUIObject() && (_battleSM.GetActiveState() is IdlePlayerMovementState || _battleSM.GetActiveState() is PlayerMeleeAttackOneToOneState))
        {
            CurrentMouseOverSelectionUnit = _battleRaycaster.GetCurrentMouseOverSelectionUnit();
        }
        else
        {
            CurrentMouseOverSelectionUnit = null;
        }
    }

    private void SetActiveUnderUnitsDecals(bool value)
    {
        if (CurrentSelectedUnit == null)
        {
            return;
        }

        for (int i = 0; i < _battleGridData.Units.Count; i++)
        {
            _createdUnderUnitWalkingDecals[i].gameObject.SetActive(!_battleGridData.Units[i].IsDeadOnBattleField && _battleGridData.Units[i] != CurrentSelectedUnit && value);
            _createdUnderUnitWalkingDecals[i].transform.position = _battleGridData.Units[i].transform.position + Vector3.up / 2f;
            _createdUnderUnitWalkingDecals[i].material = Object.Instantiate(_createdUnderUnitWalkingDecals[i].material);
            _createdUnderUnitWalkingDecals[i].material.SetColor("_Color", _battleGridData.Units[i].GetUnitColor().SetTransparency(1f));
            _createdUnderUnitWalkingDecals[i].material.SetFloat("_IsShowAttackDirection", _imposedPairsContainer.HasPairWith(_battleGridData.Units[i]) ? 1f : 0f);

            Vector2 attackDirectionVector = _imposedPairsContainer.GetAttackDirectionFor(_battleGridData.Units[i]);

            float rotation = 0f;

            if (attackDirectionVector.x != 0f)
            {
                rotation = -90f * attackDirectionVector.x;
            }
            else if (attackDirectionVector.y == 1f)
            {
                rotation = 180f;
            }

            _createdUnderUnitWalkingDecals[i].material.SetFloat("_Rotation", rotation);
        }
    }

    public void StopAttackProcesses()
    {
        AttackRangeDecalProjector.gameObject.SetActive(false);
        for (int i = 0; i < _battleGridData.Units.Count; i++)
        {
            SetActiveAttackDecalUnderUnit(i, false);
            if (_battleGridData.Units[i] != CurrentMouseOverSelectionUnit)
            {
                DeactivateOverUnitData(_battleGridData.Units[i], false);
            }
        }
    }

    public List<Node> GetPossibleWalkNodesForUnitAndSetField(UnitBase unit, bool isShowEnemyWalkingDistance)
    {
        if (!isShowEnemyWalkingDistance && !_turnsHandler.IsUnitHaveLengthToMove(unit))
        {
            return null;
        }

        Vector3 currentUnitPosition = unit.transform.position;

        Node startNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(currentUnitPosition);

        List<Node> possibleNodes = new List<Node>(25);
        Node[] neighbours;
        List<Node> resultNodes = new List<Node>(25);
        possibleNodes.Add(startNode);

        int crushProtection = 0;
        float unitMaxWalkDistance = isShowEnemyWalkingDistance ? _turnsHandler.GetDefaultLengthForUnit(unit) : _turnsHandler.GetLastLengthForUnit(unit);

        for (int x = 0; x < _battleGridData.Width; x++)
        {
            for (int y = 0; y < _battleGridData.Height; y++)
            {
                _battleGridData.WalkableMap[x, y] = false;
                _battleGridData.NodesGrid[x, y].SetPlacedByUnit(false);
            }
        }

        for (int i = 0; i < _battleGridData.Units.Count; i++)
        {
            Node unitNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(_battleGridData.Units[i].transform.position);

            if (unitNode == startNode)
            {
                unitNode.SetPlacedByUnit(false);
            }
            else if (!_battleGridData.Units[i].IsDeadOnBattleField)
            {
                unitNode.SetPlacedByUnit(true);
            }
        }

        while (possibleNodes.Count > 0)
        {
            crushProtection++;

            if (crushProtection > 100000)
            {
                Debug.Log("CRUSHED, DOLBOEB!!!");
                break;
            }

            neighbours = _battleGridData.GlobalGrid.GetNeighbours(possibleNodes[0], true).ToArray();
            possibleNodes.RemoveAt(0);

            foreach (Node neighbour in neighbours)
            {
                if (!resultNodes.Contains(neighbour) &&
                    neighbour.CheckWalkability &&
                    (
                        neighbour.GridX >= _battleGridData.StartNodeIDX &&
                        neighbour.GridX < _battleGridData.StartNodeIDX + _battleGridData.Width) &&
                        (neighbour.GridY >= _battleGridData.StartNodeIDY &&
                        neighbour.GridY < _battleGridData.StartNodeIDY + _battleGridData.Height))
                {
                    if (unitMaxWalkDistance >= _battleGridData.GlobalGrid.GetPathLength(startNode, neighbour))
                    {
                        resultNodes.Add(neighbour);
                        possibleNodes.Add(neighbour);
                        _battleGridData.NodesGrid[neighbour.GridX - _battleGridData.StartNodeIDX, neighbour.GridY - _battleGridData.StartNodeIDY].SetPlacedByUnit(false);
                        _battleGridData.WalkableMap[neighbour.GridX - _battleGridData.StartNodeIDX, neighbour.GridY - _battleGridData.StartNodeIDY] = true;
                    }
                }
            }
        }

        for (int x = 0; x < _battleGridData.Width; x++)
        {
            for (int y = 0; y < _battleGridData.Height; y++)
            {
                bool isBlocked = !_battleGridData.WalkableMap[x, y] || (x == 0 || y == 0 || (x == _battleGridData.Width - 1) || (y == _battleGridData.Height - 1));

                _battleGridData.NodesGrid[x, y].SetPlacedByUnit(isBlocked);
            }
        }

        return resultNodes;
    }

    public void FocusCameraToNearestAllyUnit(UnitBase unitToCheck)
    {
        float nearestLength = Mathf.Infinity;
        UnitBase nearestChar = null;

        for (int i = 0; i < _battleGridData.Units.Count; i++)
        {
            if (_battleGridData.Units[i] is PlayerUnit && !_battleGridData.Units[i].IsDeadOnBattleField && _turnsHandler.IsCanUnitWalk(_battleGridData.Units[i]))
            {
                float distance = Vector3.Distance(_battleGridData.Units[i].transform.position, unitToCheck.transform.position);

                if (distance < nearestLength)
                {
                    nearestLength = distance;
                    nearestChar = _battleGridData.Units[i];
                }
            }
        }

        StartFocusCamera(nearestChar.transform, () => _battleSM.Enter<UnitSelectionState, (UnitBase, IExitableState)>((nearestChar, _battleSM.GetStateOfType(typeof(IdlePlayerMovementState)))));
    }

    public void StartFocusCamera(Transform target, Action callback)
    {
        _battleSM.Enter<CameraFocusState, (Transform, Action)>((target, () =>
            callback?.Invoke()
        ));
    }

    public void StopBattle(bool isPlayerWon)
    {
        _battleSM.Enter<BattleEndState, bool>(isPlayerWon);
    }

    private void AbortImposingButtonPressed()
    {
        StopAttackProcesses();
        UnitBase unitAttacker = _imposedPairsContainer.GetPairFor(CurrentSelectedUnit);

        BattleFieldActionAbility defaultAttackAbility = unitAttacker.GetDefaultUnitAttackAbility();

        List<UnitBase> unitsToAttack = new List<UnitBase>();

        if (defaultAttackAbility.Type == AbilityType.MeleeRangeAttack)
        {
            unitsToAttack.AddRange(GetUnitsWithinAttackRaduisOfMeleeRangeWeapon(unitAttacker, unitAttacker.transform.position, unitAttacker.transform.forward, 3f, 100f));
        } 
        else
        {
            unitsToAttack.Add(CurrentSelectedUnit);
        }

        _battleSM.Enter<AttackSequenceState, (UnitBase, List<UnitBase>, BattleFieldActionAbility, bool, Action, Action)>(
            (unitAttacker, unitsToAttack, unitAttacker.GetDefaultUnitAttackAbility(), true, null, null));
        _imposedPairsContainer.TryRemovePair(CurrentSelectedUnit);
    }

    private bool IsCanUseAbility()
    {
        return (_battleSM.GetActiveState() is not AIMovementState && _battleSM.GetActiveState() is not PlayerUnitMovementAnimationState && _battleSM.GetActiveState() is not AttackSequenceState && _battleSM.GetActiveState() is not CameraFocusState);
    }

    public void AbilityButtonPressed(BattleFieldActionAbility ability, bool imposed)
    {
        if (!IsCanUseAbility())
        {
            return;
        }

        if (ability.Type == AbilityType.Walk)
        {
            if (!imposed)
            {
                SwitchWalking();
            }
            else
            {
                AbortImposingButtonPressed();
            }
        }
        else if (
          ability.Type == AbilityType.MeleeOneToOneAttack ||
          ability.Type == AbilityType.MeleeRangeAttack ||
          ability.Type == AbilityType.ShotAttack ||
          ability.Type == AbilityType.AroundAttackWithPush)
        {
            SwitchAttacking(ability);
        }
    }

    public void AbilityButtonPointerEnter(BattleFieldActionAbility ability, bool imposed)
    {
        if (ability.Type == AbilityType.Walk && IsCanUseAbility() && !imposed && CurrentSelectedUnit is not EnemyUnit)
        {
            WalkingButtonPointerEnter();
        }
    }

    public void AbilityButtonPointerExit(BattleFieldActionAbility ability, bool imposed)
    {
        if (ability.Type == AbilityType.Walk && IsCanUseAbility() && !imposed && CurrentSelectedUnit is not EnemyUnit)
        {
            WalkingButtonPointerExit();
        }
    }

    public void SetAttackDecalUnderUnitAsSelected(int unitID)
    {
        _createdUnderUnitAttackDecals[unitID].material.SetColor("_Color", _createdUnderUnitAttackDecals[unitID].material.GetColor("_SelectedColor"));
    }

    public void SetAttackDecalUnderUnitAsDefault(int unitID)
    {
        _createdUnderUnitAttackDecals[unitID].material.SetColor("_Color", _createdUnderUnitAttackDecals[unitID].material.GetColor("_DefaultColor"));
    }

    public void SetActiveAttackDecalUnderUnit(int decalID, bool value)
    {
        _createdUnderUnitAttackDecals[decalID].gameObject.SetActive(value);

        if (value)
        {
            _createdUnderUnitAttackDecals[decalID].transform.position = _battleGridData.Units[decalID].transform.position + Vector3.up / 3f;
            _createdUnderUnitAttackDecals[decalID].material = Object.Instantiate(_createdUnderUnitAttackDecals[decalID].material);
            _createdUnderUnitAttackDecals[decalID].material.SetColor("_Color", _createdUnderUnitAttackDecals[decalID].material.GetColor("_DefaultColor"));
        }
    }

    private void ProcessMovementDecalAppearance()
    {
        if (CurrentSelectedUnit != null)
        {
            if (_currentAppearRange <= 1f)
            {
                _currentAppearRange += Time.deltaTime;
                BattleGridDecalProjector.material.SetFloat(APPEAR_RANGE, _currentAppearRange);
            }
        }
    }

    public void OnTurnButtonEntered(UnitBase unit)
    {
        unit.SetActiveOutline(true);
        unit.ActivateOverUnitData(false, null, _imposedPairsContainer.HasPairWith(unit));
    }

    public void OnTurnButtonExit(UnitBase unit)
    {
        unit.SetActiveOutline(false);
        unit.DeactivateOverUnitData(false);
    }

    private void WaitButtonPressed()
    {
        if (!IsCanUseAbility())
        {
            return;
        }

        HideWalkingDistance(true);
        StopAttackProcesses();

        _turnsHandler.SetUnitWalked(CurrentSelectedUnit);

        CurrentSelectedUnit?.DestroySelection();
        CurrentSelectedUnit?.SetActiveOutline(false);

        CurrentSelectedUnit = null;
        HideWalkingDistance(true);
        _turnsHandler.CallNextTurn();
    }

    private void BackButtonPressed()
    {
        StartFocusCamera(
            _turnsHandler.GetCurrentUnitWalker().transform,
            () => _battleSM.Enter<UnitSelectionState, (UnitBase, IExitableState)>((_turnsHandler.GetCurrentUnitWalker(), _battleSM.GetStateOfType(typeof(IdlePlayerMovementState)))));
    }

    public void SetActiveBattleGridDecal(bool value)
    {
        BattleGridDecalProjector.gameObject.SetActive(value);
    }

    private void SwitchWalking()
    {
        StopAttackProcesses();

        if (_battleSM.GetActiveState() is not PlayerUnitMovementChoosePathState &&
            _turnsHandler.IsUnitHaveLengthToMove(CurrentSelectedUnit) &&
            _turnsHandler.IsCanUnitWalk(CurrentSelectedUnit) &&
            _turnsHandler.IsItCurrentWalkingUnit(CurrentSelectedUnit))
        {
            _battleSM.Enter<PlayerUnitMovementChoosePathState>();
        }
        else
        {
            _battleSM.Enter<IdlePlayerMovementState>();
        }
    }

    private void SwitchAttacking(BattleFieldActionAbility battleFieldActionAbility)
    {
        HideWalkingDistance(false);

        _battleSM.Enter<AttackTransitionState, (BattleFieldActionAbility, bool)>((battleFieldActionAbility, false));
    }

    public void DestroyAllBloodDecals()
    {
        for (int i = 0; i < _createdBloodDecals.Count; i++)
        {
            _createdBloodDecals[i].DestroyDecal();
        }

        _createdBloodDecals.Clear();
    }

    public void DestroyAllStunEffects()
    {
        foreach (var item in _createdStunEffects)
        {
            Object.Destroy(item.gameObject);
        }

        _createdStunEffects.Clear();
    }

    private void WalkingButtonPointerEnter()
    {
        if (_isCurrentlyShowWalkingDistance || !_turnsHandler.IsCanUnitWalk(CurrentSelectedUnit) || !_turnsHandler.IsItCurrentWalkingUnit(CurrentSelectedUnit))
        {
            return;
        }

        if (_battleSM.GetActiveState() is IdlePlayerMovementState)
        {
            ShowUnitWalkingDistance(CurrentSelectedUnit, false);
        }
    }

    private void WalkingButtonPointerExit()
    {
        if (_isCurrentlyShowWalkingDistance && _battleSM.GetActiveState() is IdlePlayerMovementState)
        {
            HideWalkingDistance(true);
        }
    }

    public void ShowUnitWalkingDistance(UnitBase unitToShow, bool isShowEnemyWalkingDistance)
    {
        if (!isShowEnemyWalkingDistance && (_isCurrentlyShowWalkingDistance || !_turnsHandler.IsUnitHaveLengthToMove(unitToShow)) )
        {
            return;
        }

        _isCurrentlyShowWalkingDistance = true;

        SetActiveUnderUnitsDecals(true);

        _currentAppearRange = 0f;
        SetActiveBattleGridDecal(true);

        GetPossibleWalkNodesForUnitAndSetField(unitToShow, isShowEnemyWalkingDistance);

        Vector3 minPoint = _battleGridData.LDPoint.position;
        Vector3 maxPoint = _battleGridData.RUPoint.position;

        float xLerpPoint = Mathf.InverseLerp(minPoint.x, maxPoint.x, unitToShow.transform.position.x);
        float zLerpPoint = Mathf.InverseLerp(minPoint.z, maxPoint.z, unitToShow.transform.position.z);

        BattleGridDecalProjector.material.SetVector(APPEAR_CENTER_POINT_UV, new Vector2(xLerpPoint, zLerpPoint));
        BattleGridDecalProjector.material.SetColor("_FillColor", unitToShow.GetWalkingGridColor().SetTransparency(1f));
        BattleGridDecalProjector.material.SetColor("_OutlineColor", unitToShow.GetWalkingGridColor());

        SetBattleGridMovementMask();
    }

    public void HideWalkingDistance(bool isHideUnderPointers)
    {
        SetActiveBattleGridDecal(false);
        _isCurrentlyShowWalkingDistance = false;

        if (isHideUnderPointers)
        {
            SetActiveUnderUnitsDecals(false);
        }

        DeactivateMovementLine();
    }

    public LineRenderer CreateMovementLinePrefab(int segmentsAmount)
    {
        LineRenderer createdLine = Object.Instantiate(_movementLinePrefab);
        createdLine.positionCount = segmentsAmount;
        _createdMovementLine = createdLine;
        return createdLine;
    }

    public List<UnitBase> GetUnitsWithinAttackRaduisOfMeleeRangeWeapon(UnitBase unitAttacker, Vector3 startPos, Vector3 direction, float range, float angle)
    {
        List<UnitBase> unitsResult = new List<UnitBase>();
        Node[,] attackRadiusNodesMatrix = _battleGridData.GlobalGrid.GetNodesInRadiusByMatrix(startPos, direction, range, angle);
        int arrayLength0 = attackRadiusNodesMatrix.GetLength(0);
        int arrayLength1 = attackRadiusNodesMatrix.GetLength(1);

        for (int i = 0; i < _battleGridData.Units.Count; i++)
        {
            Node unitNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(_battleGridData.Units[i].transform.position);

            for (int x = 0; x < arrayLength0; x++)
            {
                for (int y = 0; y < arrayLength1; y++)
                {
                    if (attackRadiusNodesMatrix[x, y] != null)
                    {
                        if (unitNode == attackRadiusNodesMatrix[x, y])
                        {
                            if (!_battleGridData.Units[i].IsDeadOnBattleField && _battleGridData.Units[i] != unitAttacker)
                            {
                                unitsResult.Add(_battleGridData.Units[i]);
                            }
                        }
                    }
                }
            }
        }

        return unitsResult;
    }

    public void DeactivateMovementLine()
    {
        if (_createdMovementLine != null)
        {
            Object.Destroy(_createdMovementLine.gameObject);
        }
    }

    public void DeselectAllUnits()
    {
        for (int i = 0; i < _battleGridData.Units.Count; i++)
        {
            _battleGridData.Units[i].SetActiveOutline(false);
            _battleGridData.Units[i].DestroySelection();
            _battleGridData.Units[i].DeactivateOverUnitData(false);
        }
    }

    public void ShowOverUnitData(UnitBase unit, UnitStatsConfig attackerConfig = null)
    {
        unit.ActivateOverUnitData(false, attackerConfig, _imposedPairsContainer.HasPairWith(unit));
    }

    public void DeactivateOverUnitData(UnitBase unit, bool isBattleEnded)
    {
        unit.DeactivateOverUnitData(isBattleEnded);
    }

    public void AddNewStunEffect(StunEffect newStunEffect)
    {
        _createdStunEffects.Add(newStunEffect);
    }

    public void DestroyUnderUnitWalkDecal(int unitID)
    {
        Object.Destroy(_createdUnderUnitWalkingDecals[unitID].gameObject);
    }

    public void DestroyUnderUnitAttackDecal(int unitID)
    {
        Object.Destroy(_createdUnderUnitAttackDecals[unitID].gameObject);
    }

    public void CompletelyDeactivateOverUnitsData()
    {
        for (int i = 0; i < _battleGridData.Units.Count; i++)
        {
            _battleGridData.Units[i].DeactivateOverUnitData(true);
        }
    }

    public void TryImposeUnitWithCollection(UnitBase attacker, List<UnitBase> targets)
    {
        List<UnitBase> targetsToTryImpose = new List<UnitBase>(targets);

        for (int i = targetsToTryImpose.Count - 1; i >= 0; i--)
        {
            if (attacker.GetType() == targetsToTryImpose[i].GetType() || targetsToTryImpose[i].IsDeadOnBattleField || _imposedPairsContainer.HasPairWith(targetsToTryImpose[i]))
            {
                targetsToTryImpose.RemoveAt(i);
            }
        }

        if (targetsToTryImpose.Count != 0)
        {
            UnitBase unitToImpose = null;
            float nearestDotToTarget = -1f;

            for (int i = 0; i < targetsToTryImpose.Count; i++)
            {
                Node attackerNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(attacker.transform.position);
                Node targetNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(targetsToTryImpose[i].transform.position);

                if (_battleGridData.GlobalGrid.IsNodesContacts(targetNode, attackerNode))
                {
                    float dot = Vector3.Dot(attacker.transform.forward, (targetsToTryImpose[i].transform.position - attacker.transform.position).normalized);

                    if (dot > nearestDotToTarget)
                    {
                        nearestDotToTarget = dot;
                        unitToImpose = targetsToTryImpose[i];
                    }
                }
            }

            if (unitToImpose != null)
            {
                _imposedPairsContainer.TryCreateNewPair(attacker, unitToImpose);
            }
        }
    }

    public bool IsCanImposeWithCollection(UnitBase attacker, List<UnitBase> targets)
    {
        if (targets.Count != 0)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                if (attacker.GetType() == targets[i].GetType() || targets[i].IsDeadOnBattleField || _imposedPairsContainer.HasPairWith(targets[i]))
                {
                    continue;
                } 

                if (_battleGridData.GlobalGrid.IsNodesContacts(_battleGridData.GlobalGrid.GetNodeFromWorldPoint(attacker.transform.position), _battleGridData.GlobalGrid.GetNodeFromWorldPoint(targets[i].transform.position)))
                {
                    return true;
                }
            }

        }

        return false;
    }

    public List<UnitBase> GetUnitsCanBeImposedWithAttacker(UnitBase attacker, List<UnitBase> targets)
    {
        List<UnitBase> unitsToReturn = new List<UnitBase>();

        for (int i = 0; i < targets.Count; i++)
        {
            if (attacker.GetType() != targets[i].GetType() && !targets[i].IsDeadOnBattleField && !_imposedPairsContainer.HasPairWith(targets[i]) &&
                _battleGridData.GlobalGrid.IsNodesContacts(_battleGridData.GlobalGrid.GetNodeFromWorldPoint(attacker.transform.position), _battleGridData.GlobalGrid.GetNodeFromWorldPoint(targets[i].transform.position)))
            {
                unitsToReturn.Add(targets[i]);
            }
        }

        return unitsToReturn;
    }

    private void SetBattleGridMovementMask()
    {
        Color blackCol = Color.black;
        Color whiteCol = Color.white;

        for (int x = 0; x < _battleGridData.Width; x++)
        {
            for (int y = 0; y < _battleGridData.Height; y++)
            {
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

    private void SetDecal()
    {
        BattleGridDecalProjector.material.SetTexture("_MainTex", _battleGridData.ViewTexture);
        BattleGridDecalProjector.material.SetTexture("_WalkingPointsMap", _battleGridData.WalkingPointsTexture);
        BattleGridDecalProjector.material.SetFloat("_TextureOffset", 0f);
        BattleGridDecalProjector.material.SetFloat("_WalkPointsTextureOffset", -.0042f);
    }

    public void CreateBloodDecal(UnitBase attacker, UnitBase target)
    {
        BloodDecalAppearance bloodDecal = Object.Instantiate(_assetsContainer.BloodDecal);
        Object.Instantiate(_assetsContainer.BloodImpact, target.GetAttackPoint().position, Quaternion.LookRotation(attacker.transform.forward));
        bloodDecal.ThrowDecalOnSurface(target.GetAttackPoint().position, (target.transform.position - attacker.transform.position).normalized);
        _createdBloodDecals.Add(bloodDecal);
        bloodDecal.transform.SetParent(_bloodDecalsContainer);
    }

    public bool DealDamageOnUnit(UnitBase unit, UnitBase attacker)
    {
        float damage = attacker.GetUnitConfig().DefaultAttackDamage;

        _uiRoot.DynamicObjectsPanel.SpawnDamageNumber(Mathf.RoundToInt(damage), false, unit.transform.position + Vector3.up * 3f, _cameraFollower.Camera);

        return unit.TakeDamage(damage);
    }

    public bool DealDamageOnUnit(UnitBase unit, float damage)
    {
        _uiRoot.DynamicObjectsPanel.SpawnDamageNumber(Mathf.RoundToInt(damage), false, unit.transform.position + Vector3.up * 3f, _cameraFollower.Camera);

        return unit.TakeDamage(damage);
    }

    public void UnitDeadEvent(UnitBase unit)
    {
        if (unit is PlayerUnit)
        {
            AddNewStunEffect(unit.CreateStunParticle());
        }

        unit.DeactivateOverUnitData(true);
        _imposedPairsContainer.TryRemovePair(unit);
        _turnsHandler.MarkUnitAsDead(unit, CurrentSelectedUnit == unit);
    }

    public void DestroyAllPushPointers()
    {
        foreach (var item in _createdPushPointers)
        {
            item.HideLine();
        }

        _createdPushPointers.Clear();
    }

    private UnitBase _distanceAttackerCheckUnit;
    public List<(bool, Node, UnitBase)> CalculatePushingDistances(Node[,] attackRadiusNodesMatrix, UnitBase attacker, Vector3 attackPosition, bool isCreatePushPointers)
    {
        List<UnitBase> unitsToAttackInRange = new List<UnitBase>();
        List<Node> unitsObstacles = new List<Node>();

        for (int i = 0; i < _battleGridData.Units.Count; i++)
        {
            if (!_battleGridData.Units[i].IsDeadOnBattleField)
            {
                Node unitNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(_battleGridData.Units[i].transform.position);

                bool isBreakLoop = false;
                for (int x = 0; x < attackRadiusNodesMatrix.GetLength(0); x++)
                {
                    for (int y = 0; y < attackRadiusNodesMatrix.GetLength(1); y++)
                    {
                        if (attackRadiusNodesMatrix[x, y] != null)
                        {
                            if (unitNode == attackRadiusNodesMatrix[x, y])
                            {
                                unitsToAttackInRange.Add(_battleGridData.Units[i]);
                                isBreakLoop = true;
                            }
                        }

                        if (isBreakLoop)
                        {
                            break;
                        }
                    }

                    if (isBreakLoop)
                    {
                        break;
                    }
                }
            }
        }

        if (unitsToAttackInRange.Count != 0)
        {
            _distanceAttackerCheckUnit = attacker;
            unitsToAttackInRange.Sort(DistancesComparator);
            for (int i = 0; i < _battleGridData.Units.Count; i++)
            {
                if (!_battleGridData.Units[i].IsDeadOnBattleField && !unitsToAttackInRange.Contains(_battleGridData.Units[i]))
                {
                    unitsObstacles.Add(_battleGridData.GlobalGrid.GetNodeFromWorldPoint(_battleGridData.Units[i].transform.position));
                }
            }
        }

        bool isCanDealDamage = false;

        int nodesBoundsOffset = 2;
        Node[,] nodesGrid = _battleGridData.GlobalGrid.GetNodesGrid();
        Vector3 startNodesPos = nodesGrid[_battleGridData.StartNodeIDX + nodesBoundsOffset, _battleGridData.StartNodeIDY + nodesBoundsOffset].WorldPosition;
        Vector3 endNodePos = nodesGrid[_battleGridData.StartNodeIDX + _battleGridData.Width - nodesBoundsOffset, _battleGridData.StartNodeIDY + _battleGridData.Height - nodesBoundsOffset].WorldPosition;
        Vector4 bounds = new Vector4(startNodesPos.x, startNodesPos.z, endNodePos.x, endNodePos.z);

        List<(bool, Node, UnitBase)> unitsToPushAway = new List<(bool, Node, UnitBase)>();

        for (int i = 0; i < unitsToAttackInRange.Count; i++)
        {
            (Node, bool) pushData = _battleGridData.GlobalGrid.GetEndPushingNode(
                unitsToAttackInRange[i].transform.position,
                (unitsToAttackInRange[i].transform.position - attackPosition).normalized,
                3f, bounds,
                unitsObstacles);

            unitsObstacles.Add(pushData.Item1);

            if (isCreatePushPointers)
            {
                PushPointer newPushPointer = Object.Instantiate(_assetsContainer.PushPointer);
                newPushPointer.ShowLine(unitsToAttackInRange[i].transform.position + Vector3.up * .2f, pushData.Item1.WorldPosition + Vector3.up * .22f, pushData.Item2 && isCanDealDamage);
                _createdPushPointers.Add(newPushPointer);
            }

            unitsToPushAway.Add((pushData.Item2 && isCanDealDamage, pushData.Item1, unitsToAttackInRange[i]));
        }

        return unitsToPushAway;
    }

    private int DistancesComparator(UnitBase unit1, UnitBase unit2)
    {
        float distToFisrt = Vector3.Distance(unit1.transform.position, _distanceAttackerCheckUnit.transform.position);
        float distToSecond = Vector3.Distance(unit2.transform.position, _distanceAttackerCheckUnit.transform.position);

        if (distToFisrt == distToSecond)
        {
            return 0;
        }
        else if (distToFisrt > distToSecond)
        {
            return -1;
        }
        else
        {
            return 1;
        }
    }

    public void PushTargets(List<(bool, Node, UnitBase)> unitsToPushAway, UnitBase pusher)
    {
        for (int i = 0; i < unitsToPushAway.Count; i++)
        {
            _coroutineService.StartCoroutine(PushEvent(unitsToPushAway[i].Item3, pusher, unitsToPushAway[i].Item2, unitsToPushAway[i].Item1));
        }
    }

    private IEnumerator PushEvent(UnitBase unitToPush, UnitBase pusher, Node target, bool isTakeDamage)
    {
        float t = 0f;

        Vector3 startPos = unitToPush.transform.position;

        _imposedPairsContainer.TryRemovePair(unitToPush);

        if (_battleGridData.GlobalGrid.GetNodeFromWorldPoint(unitToPush.transform.position) != target)
        {
            while (t <= 1f)
            {
                t += Time.deltaTime * 5f;

                unitToPush.transform.rotation = Quaternion.Lerp(unitToPush.transform.rotation, Quaternion.LookRotation(pusher.transform.position.RemoveYCoord() - startPos.RemoveYCoord(), Vector3.up), Time.deltaTime * 10f);
                unitToPush.transform.position = Vector3.Lerp(startPos, target.WorldPosition, t);

                yield return null;
            }
        }

        yield return new WaitForSeconds(.1f);

        if (!unitToPush.IsDeadOnBattleField && isTakeDamage)
        {
            if (DealDamageOnUnit(unitToPush, 20f))
            {
                UnitDeadEvent(unitToPush);
            }
        }
    }
}