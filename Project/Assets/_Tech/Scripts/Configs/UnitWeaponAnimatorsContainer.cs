using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "New animators container", menuName = "Containers/WeaponAnimatorsContainer")]
public class UnitWeaponAnimatorsContainer : ScriptableObject {
    [SerializeField] private List<WeaponDataPair> _weapons_map = new List<WeaponDataPair>();

    public RuntimeAnimatorController GetAnimatorByType(WeaponAnimationLayerType type) {
        return _weapons_map.Single((item) => item.Type == type).Animator;
    }

    public int GetAttackAnimationsAmount(WeaponAnimationLayerType type) {
        return _weapons_map.Single((item) => item.Type == type).AttackAnimationsAmount;
    }

    public int GetImposeAttackAnimationsAmount(WeaponAnimationLayerType type) {
        return _weapons_map.Single((item) => item.Type == type).ImposAnimationsAttackAmount;
    }

    public int GetImposeImpactAnimationsAmount(WeaponAnimationLayerType type) {
        return _weapons_map.Single((item) => item.Type == type).ImposeAnimationsImpactAmount;
    }

    [System.Serializable]
    private class WeaponDataPair {
        public WeaponAnimationLayerType Type;
        public RuntimeAnimatorController Animator;
        public int AttackAnimationsAmount;
        public int ImposAnimationsAttackAmount;
        public int ImposeAnimationsImpactAmount;
    }
}