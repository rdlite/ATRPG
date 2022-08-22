using UnityEngine;

[CreateAssetMenu(fileName = "New character config", menuName = "Configs/Characters/Stats config")]
public class UnitStatsConfig : ScriptableObject {
    public float MovementSpeed = 7f;
    [Header("Battle field stats")]
    public float MovementLength = 5f;
    public float HP = 15f;
    public float Defense = 10f;
    public float DefaultAttackDamage = 5;

    public UnitStatsConfig GetCopyOfData() {
        UnitStatsConfig unitStatsConfig = (UnitStatsConfig)CreateInstance(typeof(UnitStatsConfig));
        unitStatsConfig.MovementSpeed = MovementSpeed;
        unitStatsConfig.MovementLength = MovementLength;
        unitStatsConfig.HP = HP;
        unitStatsConfig.Defense = Defense;
        unitStatsConfig.DefaultAttackDamage = DefaultAttackDamage;
        return unitStatsConfig;
    }
}