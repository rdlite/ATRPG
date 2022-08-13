using System.Collections.Generic;
using UnityEngine;

public class EnemyContainer : MonoBehaviour {
    [SerializeField] private List<EnemyCharacterWalker> _connectedEnemies;

    public void Init() {
        for (int i = 0; i < _connectedEnemies.Count; i++) {
            _connectedEnemies[i].SetContainer(this);
        }
    }

    public List<EnemyCharacterWalker> GetConnectedEnemies() {
        return _connectedEnemies;
    }
}