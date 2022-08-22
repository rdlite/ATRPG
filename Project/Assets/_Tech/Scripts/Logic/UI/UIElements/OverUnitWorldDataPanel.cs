using TMPro;
using UnityEngine;

public class OverUnitWorldDataPanel : MonoBehaviour {
    [SerializeField] private UISlider _healthSlider, _defenseSlider;
    [SerializeField] private TextMeshProUGUI _nameText;

    public void Init(UnitBase character) {
        _healthSlider.Init();
        _defenseSlider.Init();

        _healthSlider.UpdateValue(character.GetUnitHealthContainer().GetCurrentHealth(), character.GetUnitHealthContainer().GetCurrentHealth());
        _defenseSlider.UpdateValue(character.GetUnitHealthContainer().GetCurrentDefense(), character.GetUnitHealthContainer().GetCurrentDefense());
        _nameText.text = "beaty name";
    }
}