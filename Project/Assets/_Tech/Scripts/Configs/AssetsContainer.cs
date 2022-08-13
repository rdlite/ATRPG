using UnityEngine;
using UnityEngine.Rendering.Universal;

[CreateAssetMenu(fileName = "Assets container", menuName = "Containers/Assets container")]
public class AssetsContainer : ScriptableObject {
    public GameObject BattleOverCharacterSelectionPrefab;
    public DecalProjector UnderCharacterDecalPrefab;
}