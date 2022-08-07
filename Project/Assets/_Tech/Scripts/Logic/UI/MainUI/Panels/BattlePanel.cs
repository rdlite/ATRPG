using UnityEngine;
using UnityEngine.UI;

public class BattlePanel : UIPanel {
    [SerializeField] private GameObject _walkButton;

    protected override void LocalInit() {

    }

    public void EnableUnitPanel(BattleHandler battleHadler) {
        _walkButton.SetActive(true);
        _walkButton.GetComponent<Button>().onClick.RemoveAllListeners();
        _walkButton.GetComponent<Button>().onClick.AddListener(() => battleHadler.SwitchWalkableViewForCurrentUnit());
    }

    public void DisableUnitsPanel() {
        _walkButton.SetActive(false);
    }
}