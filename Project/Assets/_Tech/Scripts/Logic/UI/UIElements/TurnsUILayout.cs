using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TurnsUILayout : MonoBehaviour{
    [SerializeField] private GameObject _playerTurnIcon, _enemyTurnIcon, _restartRoundIcon;
    [SerializeField] private Transform _parentLayout;

    private Dictionary<IconType, GameObject> _prefabsType_map;
    private List<UnitIcon> _createdIcons;

    public void Init() {
        _createdIcons = new List<UnitIcon>();
        _prefabsType_map = new Dictionary<IconType, GameObject>();

        _prefabsType_map.Add(IconType.Player, _playerTurnIcon);
        _prefabsType_map.Add(IconType.Enemy, _enemyTurnIcon);
        _prefabsType_map.Add(IconType.RestartRound, _restartRoundIcon);
    }

    public void Cleanup() {
        for (int i = 0; i < _createdIcons.Count; i++) {
            Destroy(_createdIcons[i].Icon);
        }
        _createdIcons.Clear();
    }

    public void CreateNewIcon(IconType type, BattleHandler battleHandler, UnitBase unit) {
        UnitIcon newIcon = new UnitIcon(Instantiate(_prefabsType_map[type], _parentLayout), unit);
        newIcon.Icon.transform.localScale = _prefabsType_map[type].transform.localScale;
        if (newIcon.Icon.GetComponent<EventTriggerButtonMediator>() != null && type == IconType.Enemy) {
            newIcon.Icon.GetComponent<EventTriggerButtonMediator>().OnPointerEnter += () => battleHandler.OnTurnButtonEntered(unit);
            newIcon.Icon.GetComponent<EventTriggerButtonMediator>().OnPointerExit += () => battleHandler.OnTurnButtonExit(unit);
        }
        _createdIcons.Add(newIcon);
    }

    public void DestroyFirstIcon() {
        GameObject fistIcon = _createdIcons[0].Icon;
        _createdIcons.RemoveAt(0);
        Destroy(fistIcon);
    }
    
    public void DestroyIconOfUnit(UnitBase unitToDestroyIcon) {
        for (int i = 0; i < _createdIcons.Count; i++) {
            if (_createdIcons[i].ConnectedUnit == unitToDestroyIcon) {
                Destroy(_createdIcons[i].Icon);
                _createdIcons.RemoveAt(i);
                i--;
            }
        }
    }

    public void DestroyIconAt(int id) {
        for (int i = 0; i < _createdIcons.Count; i++) {
            if (i == id) {
                Destroy(_createdIcons[i].Icon);
                _createdIcons.RemoveAt(i);
                break;
            }
        }
    }

    public bool CheckIfNeedNewIcons() {
        return _createdIcons.Count < 12;
    }

    private class UnitIcon {
        public GameObject Icon;
        public UnitBase ConnectedUnit;

        public UnitIcon(GameObject icon, UnitBase connectedUnit) {
            Icon = icon;
            this.ConnectedUnit = connectedUnit;
        }
    }

    public enum IconType {
        Player, Enemy, RestartRound
    }
}