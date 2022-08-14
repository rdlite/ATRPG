using UnityEngine;
using UnityEngine.UI;

public class CharacterStatsPanel : MonoBehaviour {
    [SerializeField] private UISlider _healthSlider, _defenseSlider;
    [SerializeField] private Image _icon;

    public void Init() {
        _healthSlider.Init();
        _defenseSlider.Init();
    }

    public void SetData(CharacterWalker character) {
        _healthSlider.UpdateValue(character.GetStatsConfig().HP, character.GetStatsConfig().HP);
        _defenseSlider.UpdateValue(character.GetStatsConfig().Defense, character.GetStatsConfig().Defense);
    }
}