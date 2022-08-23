using UnityEngine;
using UnityEngine.Rendering.Universal;

[CreateAssetMenu(fileName = "Assets container", menuName = "Containers/Assets container")]
public class AssetsContainer : ScriptableObject {
    public GameObject BattleOverUnitSelectionPrefab;
    public OverUnitWorldDataPanel BattleOverUnitDataPrefab;
    public DecalProjector UnderUnitDecalPrefab;
    public DecalProjector AttackUnderUnitDecalPrefab;
    public WeaponPrefabsContainer WeaponPrefabsContainer;
}