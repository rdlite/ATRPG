using UnityEngine;

public class AttackTransitionState : IPayloadState<(BattleFieldActionAbility, bool)>
{
    private BattleHandler _battleHandler;
    private UpdateStateMachine _battleSM;

    public AttackTransitionState(
        UpdateStateMachine battleSM, BattleHandler battleHandler)
    {
        _battleHandler = battleHandler;
        _battleSM = battleSM;
    }

    public void Enter((BattleFieldActionAbility, bool) data)
    {
        if (data.Item1.Type == AbilityType.MeleeOneToOneAttack)
        {
            _battleSM.Enter<PlayerMeleeAttackOneToOneState, (BattleFieldActionAbility, bool)>(data);
        }
        else if (data.Item1.Type == AbilityType.MeleeRangeAttack)
        {
            _battleSM.Enter<PlayerAttackMeleeRangeState, (BattleFieldActionAbility, bool)>(data);
        }
        else if (data.Item1.Type == AbilityType.AroundAttackWithPush)
        {
            _battleSM.Enter<PlayerAroundAttackWithPush, (BattleFieldActionAbility, bool)>(data);
        }
        else if (data.Item1.Type == AbilityType.ShotAttack)
        {

        }
    }

    public void Exit() { }
}