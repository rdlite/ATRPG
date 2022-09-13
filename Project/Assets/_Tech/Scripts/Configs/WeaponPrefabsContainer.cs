using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "New weapons container", menuName = "Containers/WeaponPrefabsContainer")]
public class WeaponPrefabsContainer : ScriptableObject {
    [SerializeField] private List<WeaponDataPair> _weapons_map = new List<WeaponDataPair>();

    public GameObject GetWeaponPrefab(WeaponPrefabsType type) {
        return _weapons_map.Single((item) => item.Type == type).Prefab;
    }

    public List<BattleFieldActionAbility> GetWeaponAbilities(WeaponPrefabsType type) {
        return _weapons_map.Single((item) => item.Type == type).AbilitiesForThisWeapon;
    }

    public WeaponAnimationLayerType GetWeaponLayerType(WeaponPrefabsType type) {
        if (type == WeaponPrefabsType.None) {
            return WeaponAnimationLayerType.Hands;
        }

        return _weapons_map.Single((item) => item.Type == type).LayerType;
    }

    [System.Serializable]
    private class WeaponDataPair {
        public WeaponPrefabsType Type;
        public WeaponAnimationLayerType LayerType;
        public GameObject Prefab;
        public List<BattleFieldActionAbility> AbilitiesForThisWeapon;
    }
}

public enum WeaponPrefabsType {
    None, TestOneHandedSword, TestTwoHandedAxe
}