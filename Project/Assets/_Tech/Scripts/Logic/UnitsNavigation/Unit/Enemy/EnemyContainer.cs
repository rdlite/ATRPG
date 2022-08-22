using System.Collections.Generic;
using UnityEngine;

public class EnemyContainer : MonoBehaviour {
    [SerializeField] private List<EnemyUnit> _connectedEnemies;

    public void Init() {
        for (int i = 0; i < _connectedEnemies.Count; i++) {
            _connectedEnemies[i].SetContainer(this);
        }
    }

    public List<EnemyUnit> GetConnectedEnemies() {
        return _connectedEnemies;
    }
}