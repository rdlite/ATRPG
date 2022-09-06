using UnityEngine;
using UnityEngine.Rendering.Universal;

[CreateAssetMenu(fileName = "Assets container", menuName = "Containers/Assets container")]
public class AssetsContainer : ScriptableObject {
    [Header("Battle")]
    public GameObject BattleOverUnitSelectionPrefab;
    public OverUnitWorldDataPanel BattleOverUnitDataPrefab;
    public DecalProjector UnderUnitDecalPrefab;
    public DecalProjector AttackUnderUnitDecalPrefab;
    public WeaponPrefabsContainer WeaponPrefabsContainer;
    public GameObject DamageNumber;
    public MovementPointer MovementPointer;
    [Header("FX")]
    public ParticleSystem BloodImpact;
    public BloodDecalAppearance BloodDecal;
    public StunEffect StunEffect;
    [Header("Containers")]
    public UnitWeaponAnimatorsContainer AnimatorsContainer;
}