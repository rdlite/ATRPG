using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Characters selection config", menuName = "Configs/Characters/Selection config")]
public class CharactersSelectionConfig : ScriptableObject {
    public SelectionData PlayerSelection {
        get => _selectionDatas[0];
    }
    
    public SelectionData EnemySelection {
        get => _selectionDatas[1];
    }

    [SerializeField] private List<SelectionData> _selectionDatas;
}

[System.Serializable]
public class SelectionData {
    public float Thickness = 1f;
    public Color OutlineColor;
}