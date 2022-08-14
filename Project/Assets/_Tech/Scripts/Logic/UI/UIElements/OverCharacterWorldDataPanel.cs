using TMPro;
using UnityEngine;

public class OverCharacterWorldDataPanel : MonoBehaviour {
    [SerializeField] private UISlider _healthSlider, _defenseSlider;
    [SerializeField] private TextMeshProUGUI _nameText;

    public void Init(CharacterWalker character) {
        _healthSlider.Init();
        _defenseSlider.Init();

        _healthSlider.UpdateValue(character.GetStatsConfig().HP, character.GetStatsConfig().HP);
        _defenseSlider.UpdateValue(character.GetStatsConfig().Defense, character.GetStatsConfig().Defense);
        _nameText.text = "beaty name";
    }
}