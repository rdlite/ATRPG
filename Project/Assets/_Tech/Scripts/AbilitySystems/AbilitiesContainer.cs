using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "New ability container", menuName = "Containers/Ability container")]
public class AbilitiesContainer : ScriptableObject {
    [SerializeField] private List<BattleFieldActionAbility> _allAbilities;

    public BattleFieldActionAbility GetAbility(AbilityType type) {
        return _allAbilities.Single(ability => ability.Type == type);
    }
}

public enum AbilityType {
    None, Walk, MeleeOneToOneAttack, MeleeRangeAttack, ShotAttack
}