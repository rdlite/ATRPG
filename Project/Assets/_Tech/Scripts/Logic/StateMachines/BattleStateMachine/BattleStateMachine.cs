using System;
using System.Collections.Generic;
using UnityEngine;

public class BattleStateMachine : UpdateStateMachine {
    public BattleStateMachine() {
        _states = new Dictionary<Type, IExitableState>() {
            [typeof(StartBattleState)] = new StartBattleState(),
            [typeof(CameraFocusState)] = new CameraFocusState(),
            [typeof(IdlePlayerMovementState)] = new IdlePlayerMovementState(),
            [typeof(PlayerUnitMovementChoosePathState)] = new PlayerUnitMovementChoosePathState(),
            [typeof(PlayerUnitMovementAnimationState)] = new PlayerUnitMovementAnimationState(),
            [typeof(AIMovementState)] = new AIMovementState(),
            [typeof(AttackSequenceState)] = new AttackSequenceState(),
            [typeof(PlayerMeleeAttackOneToOneState)] = new PlayerMeleeAttackOneToOneState(),
            [typeof(PlayerAttackMeleeRangeState)] = new PlayerAttackMeleeRangeState(),
            [typeof(BattleEndState)] = new BattleEndState(),
        };
    }
}

public class BattleStateMachineData {

}