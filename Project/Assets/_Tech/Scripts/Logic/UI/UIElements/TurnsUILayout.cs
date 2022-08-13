using System.Collections.Generic;
using UnityEngine;

public class TurnsUILayout : MonoBehaviour{
    [SerializeField] private GameObject _playerTurnIcon, _enemyTurnIcon, _restartRoundIcon;
    [SerializeField] private Transform _parentLayout;

    private Dictionary<IconType, GameObject> _prefabsType_map;
    private List<GameObject> _createdIcons;

    public void Init() {
        _createdIcons = new List<GameObject>();
        _prefabsType_map = new Dictionary<IconType, GameObject>();

        _prefabsType_map.Add(IconType.Player, _playerTurnIcon);
        _prefabsType_map.Add(IconType.Enemy, _enemyTurnIcon);
        _prefabsType_map.Add(IconType.RestartRound, _restartRoundIcon);
    }

    public void CreateNewIcon(IconType type) {
        GameObject newIcon = Instantiate(_prefabsType_map[type], _parentLayout);
        newIcon.transform.localScale = _prefabsType_map[type].transform.localScale;
        _createdIcons.Add(newIcon);
    }

    public void DestroyFirstIcon() {
        GameObject fistIcon = _createdIcons[0];
        _createdIcons.RemoveAt(0);
        Destroy(fistIcon);
    }

    public bool CheckIfNeedNewIcons() {
        return _createdIcons.Count < 12;
    }

    public enum IconType {
        Player, Enemy, RestartRound
    }
}