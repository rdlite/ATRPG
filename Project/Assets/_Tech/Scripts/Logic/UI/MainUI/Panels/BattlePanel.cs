using UnityEngine;
using UnityEngine.UI;

public class BattlePanel : UIPanel {
    [SerializeField] private AbilityButton _walkButton;

    protected override void LocalInit() {

    }

    public void EnableUnitPanel(BattleHandler battleHadler) {
        _walkButton.gameObject.SetActive(true);
        _walkButton.OnClick += battleHadler.SwitchWalking;
        _walkButton.OnPointerEnter += battleHadler.WalkingPointerEnter;
        _walkButton.OnPointerExit += battleHadler.WalkingPointerExit;
    }

    public void DisableUnitsPanel(BattleHandler battleHadler) {
        _walkButton.gameObject.SetActive(false);
        _walkButton.OnClick -= battleHadler.SwitchWalking;
        _walkButton.OnPointerEnter -= battleHadler.WalkingPointerEnter;
        _walkButton.OnPointerExit -= battleHadler.WalkingPointerExit;
    }
}