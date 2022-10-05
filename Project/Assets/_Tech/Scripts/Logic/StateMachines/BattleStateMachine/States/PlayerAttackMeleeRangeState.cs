using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackMeleeRangeState : IPayloadState<(BattleFieldActionAbility, bool)>, IUpdateState
{
    private List<UnitBase> _unitsToAttack;
    private UnitBase[] _selectionForAttackMap;
    private bool[] _currentAttackingMap;

    private CameraSimpleFollower _cameraFollower;
    private InputService _inputService;
    private BattleRaycaster _battleRaycaster;
    private ImposedPairsContainer _imposedPairsContainer;
    private BattleGridData _battleGridData;
    private BattleHandler _battleHandler;
    private UpdateStateMachine _battleSM;
    private BattleFieldActionAbility _attackAbility;
    private bool _isPossibilityAttack;

    public PlayerAttackMeleeRangeState(
        UpdateStateMachine battleSM, BattleHandler battleHandler, BattleGridData battleGridData,
        ImposedPairsContainer imposedPairsContainer, BattleRaycaster battleRaycaster, InputService inputService,
        CameraSimpleFollower cameraFollower)
    {
        _cameraFollower = cameraFollower;
        _inputService = inputService;
        _battleRaycaster = battleRaycaster;
        _imposedPairsContainer = imposedPairsContainer;
        _battleGridData = battleGridData;
        _battleHandler = battleHandler;
        _battleSM = battleSM;
        _unitsToAttack = new List<UnitBase>();

        _currentAttackingMap = new bool[_battleGridData.Units.Count];
        _selectionForAttackMap = new UnitBase[_battleGridData.Units.Count];
    }

    public void Enter((BattleFieldActionAbility, bool) data)
    {
        _attackAbility = data.Item1;
        _isPossibilityAttack = data.Item2;
    }

    public void Update()
    {
        ProcessMeleeRadiusAttack();
        AbortAttackProcess();
    }

    public void Exit()
    {
        _battleHandler.StopAttackProcesses();
    }

    private Node[,] _attackRadiusNodesMatrix = new Node[0, 0];
    private void ProcessMeleeRadiusAttack()
    {
        Vector3 lookDirection = (_cameraFollower.Camera.WorldToScreenPoint(_battleHandler.CurrentSelectedUnit.transform.position) - Input.mousePosition).normalized;
        Vector3 targetLookDirection = new Vector3(lookDirection.x, _battleHandler.CurrentSelectedUnit.transform.forward.y, lookDirection.y);
        bool isImposed = _imposedPairsContainer.HasPairWith(_battleHandler.CurrentSelectedUnit);

        if (isImposed || Vector3.Dot(targetLookDirection, _battleHandler.CurrentSelectedUnit.transform.forward) <= .99f)
        {
            if (!isImposed)
            {
                _battleHandler.CurrentSelectedUnit.transform.forward = Vector3.Lerp(_battleHandler.CurrentSelectedUnit.transform.forward, targetLookDirection, 15f * Time.deltaTime);
            }

            if (Time.frameCount % 4 == 0)
            {
                _attackRadiusNodesMatrix = _battleGridData.GlobalGrid.GetNodesInRadiusByMatrix(_battleHandler.CurrentSelectedUnit.transform.position, _battleHandler.CurrentSelectedUnit.transform.forward, 4f, 100f);
                //_attackRadiusNodesMatrix = _battleGridData.GlobalGrid.GetNodesInRadiusByMatrix(_battleHandler.CurrentSelectedUnit.transform.position, _battleHandler.CurrentSelectedUnit.transform.forward, 3f, 100f);

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
                                if (unitNode == _attackRadiusNodesMatrix[x, y])
                                {
                                    isActivate = true;
                                    if (!_battleGridData.Units[i].IsDeadOnBattleField)
                                    {
                                        _battleHandler.ActivateAttackDecalUnderUnit(i);
                                    }
                                }
                            }
                        }
                    }

                    if (!isActivate)
                    {
                        _battleHandler.SetActiveAttackDecalUnderUnit(i, false);
                        _battleHandler.DeactivateOverUnitData(_battleGridData.Units[i], false);
                    }
                    else
                    {
                        if (!_battleGridData.Units[i].IsDeadOnBattleField && _battleHandler.CurrentMouseOverSelectionUnit != _battleGridData.Units[i])
                        {
                            _battleGridData.Units[i].ActivateOverUnitData(true, _battleHandler.CurrentSelectedUnit.GetUnitConfig(), _imposedPairsContainer.HasPairWith(_battleGridData.Units[i]));
                        }
                    }
                }
            }
        }

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
        _battleSM.Enter<AttackSequenceState, (UnitBase, List<UnitBase>, BattleFieldActionAbility, bool, System.Action callback)>((attacker, targets, ability, _isPossibilityAttack, null));
    }
}