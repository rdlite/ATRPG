using TMPro;
using UnityEngine;

public class OverUnitWorldDataPanel : MonoBehaviour {
    [SerializeField] private UISlider _healthSlider, _defenseSlider;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private GameObject _damageAmountPanel, _attackIcon, _skullIcon, _imposedIcon;
    [SerializeField] private TextMeshProUGUI _damageAmountText;

    public void Init(UnitBase character, UnitStatsConfig attackerStats, bool imposed) {
        _healthSlider.Init();
        _defenseSlider.Init();

        _damageAmountPanel.SetActive(attackerStats != null);

        float onDefenseDamage = 0f;
        float onHealthDamage = 0f;

        _attackIcon.SetActive(true);
        _skullIcon.SetActive(false);
        _imposedIcon.gameObject.SetActive(imposed);

        if (attackerStats) {
            onDefenseDamage = attackerStats.DefaultAttackDamage;
            if (character.GetUnitHealthContainer().GetCurrentDefense() - onDefenseDamage < 0) {
                onDefenseDamage = character.GetUnitHealthContainer().GetCurrentDefense();
                onHealthDamage = attackerStats.DefaultAttackDamage - onDefenseDamage;
                if (onHealthDamage > character.GetUnitHealthContainer().GetCurrentHealth()) {
                    onHealthDamage = character.GetUnitHealthContainer().GetCurrentHealth();
                    _attackIcon.SetActive(false);
                    _skullIcon.SetActive(true);
                } 
            }

            _damageAmountText.text = attackerStats.DefaultAttackDamage.ToString();
        }

        _healthSlider.UpdateValue(character.GetUnitHealthContainer().GetCurrentHealth(), character.GetUnitHealthContainer().GetMaxHealth(), onHealthDamage);
        _defenseSlider.UpdateValue(character.GetUnitHealthContainer().GetCurrentDefense(), character.GetUnitHealthContainer().GetMaxDefense(), onDefenseDamage);
        _nameText.text = "beaty name";
    }
}