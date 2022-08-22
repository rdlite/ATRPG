using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "New weapons container", menuName = "Containers/WeaponPrefabsContainer")]
public class WeaponPrefabsContainer : ScriptableObject {
    [SerializeField] private List<WeaponDataPair> _weapons_map = new List<WeaponDataPair>();

    public GameObject GetWeaponPrefab(WeaponPrefabsType type) {
        return _weapons_map.Single((item) => item.Type == type).Prefab;
    }

    [System.Serializable]
    private class WeaponDataPair {
        public WeaponPrefabsType Type;
        public GameObject Prefab;
    }
}

public enum WeaponPrefabsType {
    None, TestOneHandedSword
}