using TMPro;
using UnityEngine;

public class OverUnitWorldDataPanel : MonoBehaviour {
    [SerializeField] private GameObject _fullInfo, _smallInfo;
    [SerializeField] private UISlider _healthSlider_FULL, _defenseSlider_FULL, _healthSlider_SMALL, _defenseSlider_SMALL;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private GameObject _imposedIcon;
    [SerializeField] private DamageInfoPanel[] _damagePanels;

    private UnitBase _currentUnit;

    public void Init(UnitBase unit, UnitHealth unitHealth) {
        _currentUnit = unit;

        _healthSlider_FULL.Init();
        _defenseSlider_FULL.Init();
        _healthSlider_SMALL.Init();
        _defenseSlider_SMALL.Init();

        for (int i = 0; i < _damagePanels.Length; i++) {
            _damagePanels[i].DeactivatePanel();
        }

        _imposedIcon.gameObject.SetActive(false);

        UpdateSliderValues();

        unitHealth.OnUnitHealthChanged += () => UpdateSliderValues(0f, 0f);

        _nameText.text = "beaty name";
    }

    public void UpdateData(UnitStatsConfig attackerStats, bool imposed) {
        bool isDeadlyDamage = false;
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
                    isDeadlyDamage = true;
                }
            }
            
            for (int i = 0; i < _damagePanels.Length; i++) {
                if (attackerStats != null) {
                    _damagePanels[i].SetActivePanel(isDeadlyDamage, (int)attackerStats.DefaultAttackDamage);
                } else {
                    _damagePanels[i].DeactivatePanel();
                }
            }
        }

        UpdateSliderValues(onHealthDamage, onDefenseDamage);
    }

    public void UpdateSliderValues(float onHealthDamage = 0f, float onDefenseDamage = 0f) {
        _healthSlider_FULL.UpdateValue(_currentUnit.GetUnitHealthContainer().GetCurrentHealth(), _currentUnit.GetUnitHealthContainer().GetMaxHealth(), onHealthDamage);
        _defenseSlider_FULL.UpdateValue(_currentUnit.GetUnitHealthContainer().GetCurrentDefense(), _currentUnit.GetUnitHealthContainer().GetMaxDefense(), onDefenseDamage);
        _healthSlider_SMALL.UpdateValue(_currentUnit.GetUnitHealthContainer().GetCurrentHealth(), _currentUnit.GetUnitHealthContainer().GetMaxHealth(), 0f);
        _defenseSlider_SMALL.UpdateValue(_currentUnit.GetUnitHealthContainer().GetCurrentDefense(), _currentUnit.GetUnitHealthContainer().GetMaxDefense(), 0f);
    }

    public void SetActivePanel(PanelActivationType activationType, bool isShowData) {
        _smallInfo.SetActive(activationType == PanelActivationType.Small);
        _fullInfo.SetActive(activationType == PanelActivationType.Full);
        if (!isShowData) {
            for (int i = 0; i < _damagePanels.Length; i++) {
                _damagePanels[i].DeactivatePanel();
            }
        }
    }

    public enum PanelActivationType {
        None, Full, Small
    }
}