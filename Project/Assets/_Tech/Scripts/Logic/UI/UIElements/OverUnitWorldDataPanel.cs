using TMPro;
using UnityEngine;

public class OverUnitWorldDataPanel : MonoBehaviour {
    [SerializeField] private UISlider _healthSlider, _defenseSlider;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private GameObject _damageAmountPanel, _attackIcon, _skullIcon, _imposedIcon;
    [SerializeField] private TextMeshProUGUI _damageAmountText;

    private UnitBase _currentUnit;

    public void Init(UnitBase unit) {
        _currentUnit = unit;

        _healthSlider.Init();
        _defenseSlider.Init();

        _damageAmountPanel.SetActive(false);

        _attackIcon.SetActive(false);
        _skullIcon.SetActive(false);
        _imposedIcon.gameObject.SetActive(false);

        _healthSlider.UpdateValue(unit.GetUnitHealthContainer().GetCurrentHealth(), unit.GetUnitHealthContainer().GetMaxHealth(), 0f);
        _defenseSlider.UpdateValue(unit.GetUnitHealthContainer().GetCurrentDefense(), unit.GetUnitHealthContainer().GetMaxDefense(), 0f);
        _nameText.text = "beaty name";
    }

    public void UpdateData(UnitStatsConfig attackerStats, bool imposed) {
        _damageAmountPanel.SetActive(attackerStats != null);

        _attackIcon.SetActive(true);
        _skullIcon.SetActive(false);
        _imposedIcon.gameObject.SetActive(imposed);

        float onDefenseDamage = 0f;
        float onHealthDamage = 0f;

        if (attackerStats) {
            onDefenseDamage = attackerStats.DefaultAttackDamage;
            if (_currentUnit.GetUnitHealthContainer().GetCurrentDefense() - onDefenseDamage < 0) {
                onDefenseDamage = _currentUnit.GetUnitHealthContainer().GetCurrentDefense();
                onHealthDamage = attackerStats.DefaultAttackDamage - onDefenseDamage;
                if (onHealthDamage > _currentUnit.GetUnitHealthContainer().GetCurrentHealth()) {
                    onHealthDamage = _currentUnit.GetUnitHealthContainer().GetCurrentHealth();
                    _attackIcon.SetActive(false);
                    _skullIcon.SetActive(true);
                }
            }

            _damageAmountText.text = attackerStats.DefaultAttackDamage.ToString();
        }

        _healthSlider.UpdateValue(_currentUnit.GetUnitHealthContainer().GetCurrentHealth(), _currentUnit.GetUnitHealthContainer().GetMaxHealth(), onHealthDamage);
        _defenseSlider.UpdateValue(_currentUnit.GetUnitHealthContainer().GetCurrentDefense(), _currentUnit.GetUnitHealthContainer().GetMaxDefense(), onDefenseDamage);
    }
}