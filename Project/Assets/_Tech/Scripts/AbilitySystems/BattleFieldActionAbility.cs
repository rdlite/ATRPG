using UnityEngine;

[CreateAssetMenu(fileName = "New ability", menuName = "Configs/Abilities/New ability")]
public class BattleFieldActionAbility : ScriptableObject {
    public AbilityType Type;
    public Sprite ButtonImage;
}