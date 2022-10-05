using System;
using System.Collections.Generic;
using UnityEngine;

public class BattleStateMachine : UpdateStateMachine {
    public void Init(
        BattleHandler battleHandler, BattleGridData battleGridData, InputService inputService,
        BattleRaycaster battleRaycaster, BattleTurnsHandler turnsHandler, UIRoot uiRoot,
        ImposedPairsContainer imposedPairsContainer, LineRenderer movementLinePrefab, AssetsContainer assetsContainer,
        ICoroutineService coroutineService, CameraSimpleFollower cameraFollower, AIMovementResolver aiResolver,
        BattleGridGenerator gridGenerator) {
        _states = new Dictionary<Type, IExitableState>() {
            [typeof(StartBattleState)] = new StartBattleState(this, turnsHandler),
            [typeof(CameraFocusState)] = new CameraFocusState(cameraFollower),
            [typeof(IdlePlayerMovementState)] = new IdlePlayerMovementState(
                battleHandler, battleGridData, inputService,
                battleRaycaster, this),
            [typeof(PlayerUnitMovementChoosePathState)] = new PlayerUnitMovementChoosePathState(
                battleHandler, inputService, this,
                battleRaycaster, battleGridData, movementLinePrefab, assetsContainer),
            [typeof(PlayerUnitMovementAnimationState)] = new PlayerUnitMovementAnimationState(
                battleGridData, coroutineService, imposedPairsContainer,
                battleHandler, turnsHandler, cameraFollower,
                this),
            [typeof(AIMovementState)] = new AIMovementState(
                aiResolver, uiRoot, battleHandler),
            [typeof(AttackTransitionState)] = new AttackTransitionState(
                this, battleHandler),
            [typeof(AttackSequenceState)] = new AttackSequenceState(
                coroutineService, uiRoot, battleHandler,
                imposedPairsContainer, turnsHandler, this, 
                cameraFollower),

            [typeof(PlayerMeleeAttackOneToOneState)] = new PlayerMeleeAttackOneToOneState(
                this, battleHandler, battleGridData,
                imposedPairsContainer, battleRaycaster, inputService),
            [typeof(PlayerAttackMeleeRangeState)] = new PlayerAttackMeleeRangeState(
                this, battleHandler, battleGridData,
                imposedPairsContainer, battleRaycaster, inputService,
                cameraFollower),
            [typeof(PlayerAroundAttackWithPush)] = new PlayerAroundAttackWithPush(
                this, battleHandler, battleGridData,
                imposedPairsContainer, battleRaycaster, inputService,
                cameraFollower),
            
            [typeof(UnitSelectionState)] = new UnitSelectionState(
                battleHandler, this, turnsHandler,
                uiRoot, imposedPairsContainer),
            [typeof(StateMachineIdleState)] = new StateMachineIdleState(),
            [typeof(BattleEndState)] = new BattleEndState(
                battleHandler, turnsHandler, battleGridData, 
                gridGenerator),
        };
    }
}