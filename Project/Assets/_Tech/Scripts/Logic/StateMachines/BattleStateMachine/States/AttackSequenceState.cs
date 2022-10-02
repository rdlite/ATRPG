using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class AttackSequenceState : IPayloadState<(UnitBase, List<UnitBase>, BattleFieldActionAbility, bool, System.Action callback)> {
    private CameraSimpleFollower _cameraFollower;
    private UpdateStateMachine _battleSM;
    private BattleGridData _battleGridData;
    private AssetsContainer _assetsContainer;
    private BattleTurnsHandler _turnsHandler;
    private ImposedPairsContainer _imposedPairsContainer;
    private BattleHandler _battleHandler;
    private UIRoot _uiRoot;
    private ICoroutineService _coroutineService;

    public AttackSequenceState(
        ICoroutineService coroutineService, UIRoot uiRoot, BattleHandler battleHandler,
        ImposedPairsContainer imposedPairsContainer, BattleTurnsHandler turnsHandler, AssetsContainer assetsContainer,
        BattleGridData battleGridData, UpdateStateMachine battleSM, CameraSimpleFollower cameraFollower) {
        _cameraFollower = cameraFollower;
        _battleSM = battleSM;
        _battleGridData = battleGridData;
        _assetsContainer = assetsContainer;
        _turnsHandler = turnsHandler;
        _imposedPairsContainer = imposedPairsContainer;
        _battleHandler = battleHandler;
        _uiRoot = uiRoot;
        _coroutineService = coroutineService;
    }

    public void Enter((UnitBase, List<UnitBase>, BattleFieldActionAbility, bool, Action callback) payload) {
        _battleHandler.DeselectAllUnits();
        _coroutineService.StartCoroutine(AttackSequence(payload.Item1, payload.Item2, payload.Item3, payload.Item4, payload.Item5));
    }

    private IEnumerator AttackSequence(UnitBase attacker, List<UnitBase> targets, BattleFieldActionAbility ability, bool isImposedAttack, Action callback) {
        _uiRoot.GetPanel<BattlePanel>().DisableUnitsPanel(_battleHandler);

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

            _battleHandler.StopAttackProcesses();

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
                _battleHandler.AddNewBloodDecal(bloodDecal);
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
                _battleHandler.DeselectAllUnits();
                UnitDeadEvent(targets[i]);
            }
        }

        if (_battleHandler.IsBattleEnded) {
            _battleHandler.DestroyAllStunEffects();

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

        bool isReactivatePanel = (!isImposedAttack || isImposedAttack && targets.Count == 1 && targets[0] is PlayerUnit && !isDeadMap[0]) && !_uiRoot.GetPanel<BattlePanel>().IsUnitPanelAlreadyEnabled();

        if (isReactivatePanel && _battleHandler.CurrentSelectedUnit != null) {
            _uiRoot.GetPanel<BattlePanel>().EnableUnitPanel(
                _battleHandler, _battleHandler.CurrentSelectedUnit, UnitPanelState.UseTurn,
                _turnsHandler, _imposedPairsContainer.HasPairWith(_battleHandler.CurrentSelectedUnit));
            _battleSM.Enter<UnitSelectionState, (UnitBase, IExitableState)>((_battleHandler.CurrentSelectedUnit, _battleSM.GetStateOfType(typeof(IdlePlayerMovementState))));
        }

        callback?.Invoke();
    }

    private bool DealDamageOnUnit(UnitBase unit, UnitBase attacker) {
        float damage = attacker.GetUnitConfig().DefaultAttackDamage;

        _uiRoot.DynamicObjectsPanel.SpawnDamageNumber(Mathf.RoundToInt(damage), false, unit.transform.position + Vector3.up * 3f, _cameraFollower.Camera);

        return unit.TakeDamage(damage);
    }

    private void UnitDeadEvent(UnitBase unit) {
        if (unit is PlayerUnit) {
            _battleHandler.AddNewStunEffect(unit.CreateStunParticle());
        }

        unit.DeactivateOverUnitData(true);
        _imposedPairsContainer.TryRemovePair(unit);
        _turnsHandler.MarkUnitAsDead(unit, _battleHandler.CurrentSelectedUnit == unit);
    }

    public void Exit() { }
}