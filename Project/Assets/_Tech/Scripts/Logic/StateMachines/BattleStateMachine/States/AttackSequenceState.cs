using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class AttackSequenceState : IPayloadState<(UnitBase, List<UnitBase>, BattleFieldActionAbility, bool, Action, Action)>
{
    private CameraSimpleFollower _cameraFollower;
    private UpdateStateMachine _battleSM;
    private BattleTurnsHandler _turnsHandler;
    private ImposedPairsContainer _imposedPairsContainer;
    private BattleHandler _battleHandler;
    private UIRoot _uiRoot;
    private ICoroutineService _coroutineService;

    public AttackSequenceState(
        ICoroutineService coroutineService, UIRoot uiRoot, BattleHandler battleHandler,
        ImposedPairsContainer imposedPairsContainer, BattleTurnsHandler turnsHandler, UpdateStateMachine battleSM,
        CameraSimpleFollower cameraFollower)
    {
        _cameraFollower = cameraFollower;
        _battleSM = battleSM;
        _turnsHandler = turnsHandler;
        _imposedPairsContainer = imposedPairsContainer;
        _battleHandler = battleHandler;
        _uiRoot = uiRoot;
        _coroutineService = coroutineService;
    }

    public void Enter((UnitBase, List<UnitBase>, BattleFieldActionAbility, bool, Action, Action) payload)
    {
        _battleHandler.DeselectAllUnits();
        _coroutineService.StartCoroutine(AttackSequence(payload.Item1, payload.Item2, payload.Item3, payload.Item4, payload.Item5, payload.Item6));
    }

    private IEnumerator AttackSequence(UnitBase attacker, List<UnitBase> targets, BattleFieldActionAbility ability, bool isImposedAttack, Action endAttackCallback, Action onDamageDeliveredCallback)
    {
        _uiRoot.GetPanel<BattlePanel>().DisableUnitsPanel(_battleHandler);

        attacker.IsBusy = true;

        for (int i = 0; i < targets.Count; i++)
        {
            targets[i].IsBusy = true;
        }

        float t = 0f;

        List<UnitBase> unitsToRotateToAttacker = _battleHandler.GetUnitsCanBeImposedWithAttacker(attacker, targets);
        Quaternion[] startTargetRotations = new Quaternion[unitsToRotateToAttacker.Count];
        Quaternion[] endTargetRotations = new Quaternion[unitsToRotateToAttacker.Count];
        for (int i = 0; i < unitsToRotateToAttacker.Count; i++)
        {
            startTargetRotations[i] = unitsToRotateToAttacker[i].transform.rotation;
            endTargetRotations[i] = Quaternion.LookRotation((attacker.transform.position - unitsToRotateToAttacker[i].transform.position).RemoveYCoord());
        }

        if (!isImposedAttack && targets.Count > 0)
        {
            Quaternion startAttackerRotation = attacker.transform.rotation;
            Quaternion endAttackerRotation = Quaternion.LookRotation((targets[0].transform.position - attacker.transform.position).RemoveYCoord());

            _battleHandler.StopAttackProcesses();

            while (t <= 1f)
            {
                t += Time.deltaTime * 5f;

                attacker.transform.rotation = Quaternion.Slerp(startAttackerRotation, endAttackerRotation, t);

                for (int i = 0; i < unitsToRotateToAttacker.Count; i++)
                {
                    unitsToRotateToAttacker[i].transform.rotation = Quaternion.Slerp(startTargetRotations[i], endTargetRotations[i], t);
                }

                yield return null;
            }
        }

        t = 0f;

        if (!isImposedAttack)
        {
            _turnsHandler.UnitUsedAbility(attacker, ability);
        }

        bool endAttack = false;
        List<bool> isDeadMap = new List<bool>();

        for (int i = 0; i < targets.Count; i++)
        {
            isDeadMap.Add(false);
        }

        attacker.PlayAttackAnimation(ability.IsDefaultAttack);
        attacker.GetUnitSkinContainer().SignOnAttackAnimation(() =>
        {
            endAttack = true;

            onDamageDeliveredCallback?.Invoke();

            for (int i = 0; i < targets.Count; i++)
            {
                isDeadMap[i] = _battleHandler.DealDamageOnUnit(targets[i], attacker);
            }
        });
        attacker.GetUnitSkinContainer().SignOnAttackAnimation(() =>
        {
            for (int i = 0; i < targets.Count; i++)
            {
                _battleHandler.CreateBloodDecal(attacker, targets[i]);
            }
        });

        yield return new WaitWhile(() => !endAttack);

        for (int i = 0; i < targets.Count; i++)
        {
            if (!isDeadMap[i])
            {
                targets[i].PlayImpactFromSwordAnimation();
            }
        }

        yield return new WaitForSeconds(isDeadMap.Contains(true) ? 2f : 1f);

        attacker.IsBusy = false;

        for (int i = 0; i < targets.Count; i++)
        {
            targets[i].IsBusy = false;
        }

        for (int i = 0; i < targets.Count; i++)
        {
            if (isDeadMap[i])
            {
                _battleHandler.DeselectAllUnits();
                _battleHandler.UnitDeadEvent(targets[i]);
            }
        }

        if (_battleHandler.IsBattleEnded)
        {
            _battleHandler.DestroyAllStunEffects();

            yield break;
        }

        _turnsHandler.ForceFillTurns();

        if (!isImposedAttack && ability.IsImposeTargetAbility)
        {
            _battleHandler.TryImposeUnitWithCollection(attacker, targets);
        }

        //bool isReactivatePanel = (!isImposedAttack || isImposedAttack && targets.Count == 1 && targets[0] is PlayerUnit && !isDeadMap[0]) && !_uiRoot.GetPanel<BattlePanel>().IsUnitPanelAlreadyEnabled();
        bool isReactivatePanel = false;

        if (targets.Count > 1 && attacker is EnemyUnit)
        {
            int indexOfCurrentUnit = targets.IndexOf(_battleHandler.CurrentSelectedUnit);
            isReactivatePanel = (!isImposedAttack || isImposedAttack && attacker is not PlayerUnit && targets[indexOfCurrentUnit] is PlayerUnit && !isDeadMap[indexOfCurrentUnit]) && !_uiRoot.GetPanel<BattlePanel>().IsUnitPanelAlreadyEnabled();
        }
        else
        {
            isReactivatePanel = (!isImposedAttack || isImposedAttack && attacker is not PlayerUnit && targets[0] is PlayerUnit && !isDeadMap[0]) && !_uiRoot.GetPanel<BattlePanel>().IsUnitPanelAlreadyEnabled();
        }

        if (isReactivatePanel && _battleHandler.CurrentSelectedUnit != null)
        {
            _uiRoot.GetPanel<BattlePanel>().EnableUnitPanel(
                _battleHandler, _battleHandler.CurrentSelectedUnit, UnitPanelState.UseTurn,
                _turnsHandler, _imposedPairsContainer.HasPairWith(_battleHandler.CurrentSelectedUnit));
            _battleSM.Enter<UnitSelectionState, (UnitBase, IExitableState)>((_battleHandler.CurrentSelectedUnit, _battleSM.GetStateOfType(typeof(IdlePlayerMovementState))));
        }

        endAttackCallback?.Invoke();
    }

    public void Exit() { }
}