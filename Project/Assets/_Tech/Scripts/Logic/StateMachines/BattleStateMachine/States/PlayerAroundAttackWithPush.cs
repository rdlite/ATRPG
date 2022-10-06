using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAroundAttackWithPush : IPayloadState<(BattleFieldActionAbility, bool)>, IUpdateState
{
    private List<(bool, Node, UnitBase)> _unitsToPushAway = new List<(bool, Node, UnitBase)>();
    private CameraSimpleFollower _cameraFollower;
    private ICoroutineService _coroutineService;
    private AssetsContainer _assetsContainer;
    private InputService _inputService;
    private BattleRaycaster _battleRaycaster;
    private ImposedPairsContainer _imposedPairsContainer;
    private BattleGridData _battleGridData;
    private BattleHandler _battleHandler;
    private UpdateStateMachine _battleSM;
    private BattleFieldActionAbility _attackAbility;
    private bool _isPossibilityAttack;

    public PlayerAroundAttackWithPush(
        UpdateStateMachine battleSM, BattleHandler battleHandler, BattleGridData battleGridData,
        ImposedPairsContainer imposedPairsContainer, BattleRaycaster battleRaycaster, InputService inputService,
        CameraSimpleFollower cameraFollower, AssetsContainer assetsContainer, ICoroutineService coroutineService)
    {
        _cameraFollower = cameraFollower;
        _coroutineService = coroutineService;
        _assetsContainer = assetsContainer;
        _inputService = inputService;
        _battleRaycaster = battleRaycaster;
        _imposedPairsContainer = imposedPairsContainer;
        _battleGridData = battleGridData;
        _battleHandler = battleHandler;
        _battleSM = battleSM;
    }

    public void Enter((BattleFieldActionAbility, bool) data)
    {
        _cameraFollower.SetFreeMovement();
        _attackAbility = data.Item1;
        _isPossibilityAttack = data.Item2;
        _unitsToPushAway.Clear();
        ProcessMeleeRadiusAttack();
    }

    public void Exit()
    {
        _battleHandler.DestroyAllPushPointers();

        _battleHandler.StopAttackProcesses();
    }

    public void Update()
    {
        TryAttack();
        AbortAttackProcess();
    }

    private Node[,] _attackRadiusNodesMatrix = new Node[0, 0];
    private void ProcessMeleeRadiusAttack()
    {
        _attackRadiusNodesMatrix = _battleGridData.GlobalGrid.GetNodesInRadiusByMatrix(_battleHandler.CurrentSelectedUnit.transform.position, _battleHandler.CurrentSelectedUnit.transform.forward, 3f, 360f);

        if (_attackRadiusNodesMatrix.GetLength(0) != 0)
        {
            _battleHandler.AttackRangeDecalProjector.gameObject.SetActive(true);
            _battleHandler.AttackRangeDecalProjector.transform.position = _battleHandler.CurrentSelectedUnit.transform.position + Vector3.up * _battleHandler.AttackRangeDecalProjector.size.z / 2f;
            _battleHandler.AttackRangeDecalProjector.size = new Vector3(_attackRadiusNodesMatrix.GetLength(0) * _battleGridData.GlobalGrid.NodeRadius * 2f, _attackRadiusNodesMatrix.GetLength(1) * _battleGridData.GlobalGrid.NodeRadius * 2f, _battleHandler.BattleGridDecalProjector.size.z);

            Texture2D attackRangeDrawingTexture = new Texture2D(_attackRadiusNodesMatrix.GetLength(0) * 2, _attackRadiusNodesMatrix.GetLength(1) * 2);
            Color destinatedColor = Color.black;

            for (int x = 0; x < _attackRadiusNodesMatrix.GetLength(0); x++)
            {
                for (int y = 0; y < _attackRadiusNodesMatrix.GetLength(1); y++)
                {
                    destinatedColor = _attackRadiusNodesMatrix[x, y] == null ? Color.black : Color.white;
                    attackRangeDrawingTexture.SetPixel(x * 2, y * 2, destinatedColor);
                    attackRangeDrawingTexture.SetPixel(x * 2 + 1, y * 2, destinatedColor);
                    attackRangeDrawingTexture.SetPixel(x * 2, y * 2 + 1, destinatedColor);
                    attackRangeDrawingTexture.SetPixel(x * 2 + 1, y * 2 + 1, destinatedColor);
                }
            }

            attackRangeDrawingTexture.Apply();

            _battleHandler.AttackRangeDecalProjector.material.SetTexture("_MainTex", attackRangeDrawingTexture);
        }

        for (int i = 0; i < _battleGridData.Units.Count; i++)
        {
            bool isActivate = false;
            Node unitNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(_battleGridData.Units[i].transform.position);

            for (int x = 0; x < _attackRadiusNodesMatrix.GetLength(0); x++)
            {
                for (int y = 0; y < _attackRadiusNodesMatrix.GetLength(1); y++)
                {
                    if (_attackRadiusNodesMatrix[x, y] != null)
                    {
                        if (unitNode == _attackRadiusNodesMatrix[x, y] && !_battleGridData.Units[i].IsDeadOnBattleField && _battleHandler.CurrentMouseOverSelectionUnit != _battleGridData.Units[i])
                        {
                            isActivate = true;
                        }
                    }
                }
            }

            _battleHandler.SetActiveAttackDecalUnderUnit(i, isActivate);

            if (!isActivate)
            {
                _battleHandler.DeactivateOverUnitData(_battleGridData.Units[i], false);
            }
            else
            {
                _battleGridData.Units[i].ActivateOverUnitData(true, _battleHandler.CurrentSelectedUnit.GetUnitConfig(), _imposedPairsContainer.HasPairWith(_battleGridData.Units[i]));
            }
        }

        _unitsToPushAway = _battleHandler.CalculatePushingDistances(_attackRadiusNodesMatrix, _battleHandler.CurrentSelectedUnit, _battleHandler.CurrentSelectedUnit.transform.position, true);
    }

    private void TryAttack()
    {
        if (Input.GetMouseButtonDown(0) && !_inputService.IsPointerOverUIObject())
        {
            List<UnitBase> unitsToAttackInRange = new List<UnitBase>();

            for (int i = 0; i < _battleGridData.Units.Count; i++)
            {
                if (!_battleGridData.Units[i].IsDeadOnBattleField)
                {
                    Node unitNode = _battleGridData.GlobalGrid.GetNodeFromWorldPoint(_battleGridData.Units[i].transform.position);

                    bool isBreakLoop = false;
                    for (int x = 0; x < _attackRadiusNodesMatrix.GetLength(0); x++)
                    {
                        for (int y = 0; y < _attackRadiusNodesMatrix.GetLength(1); y++)
                        {
                            if (_attackRadiusNodesMatrix[x, y] != null)
                            {
                                if (unitNode == _attackRadiusNodesMatrix[x, y])
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

                _battleGridData.Units[i].DeactivateOverUnitData(false);
                _battleHandler.SetActiveAttackDecalUnderUnit(i, false);
            }

            _battleHandler.AttackRangeDecalProjector.gameObject.SetActive(false);
            TryAttackUnits(_battleHandler.CurrentSelectedUnit, unitsToAttackInRange, _attackAbility);
        }
    }

    private void AbortAttackProcess()
    {
        if (Input.GetMouseButtonDown(1) && !_inputService.IsPointerOverUIObject())
        {
            _battleSM.Enter<IdlePlayerMovementState>();
        }
    }

    private void TryAttackUnits(UnitBase attacker, List<UnitBase> targets, BattleFieldActionAbility ability)
    {
        _battleSM.Enter<AttackSequenceState, (UnitBase, List<UnitBase>, BattleFieldActionAbility, bool, System.Action, System.Action)>((attacker, targets, ability, _isPossibilityAttack, null, () => _battleHandler.PushTargets(_unitsToPushAway, attacker)));
    }
}