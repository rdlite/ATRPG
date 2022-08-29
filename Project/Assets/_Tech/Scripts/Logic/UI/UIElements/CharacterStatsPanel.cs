using UnityEngine;
using UnityEngine.UI;

public class CharacterStatsPanel : MonoBehaviour {
    [SerializeField] private UISlider _healthSlider, _defenseSlider;
    [SerializeField] private Image _icon;

    public void Init() {
        _healthSlider.Init();
        _defenseSlider.Init();
    }

    public void SetData(UnitBase character) {
        _healthSlider.UpdateValue(character.GetUnitHealthContainer().GetCurrentHealth(), character.GetUnitHealthContainer().GetMaxHealth());
        _defenseSlider.UpdateValue(character.GetUnitHealthContainer().GetCurrentDefense(), character.GetUnitHealthContainer().GetMaxDefense());
    }
}