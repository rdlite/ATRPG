using UnityEngine;

public class UnitHealth {
    private UnitStatsConfig _defaultStatsCopy;
    private UnitStatsConfig _currentStatsData;
    private DamageReceiver _damageReceiver;
    private float _maxHealth, _maxDefense;

    public UnitHealth(UnitStatsConfig statsData) {
        _currentStatsData = statsData.GetCopyOfData();
        _defaultStatsCopy = statsData.GetCopyOfData();
        _damageReceiver = new DamageReceiver();
        ResetData();
    }

    public void ResetData() {
        _currentStatsData = _defaultStatsCopy.GetCopyOfData();
        _maxHealth = _currentStatsData.HP;
        _maxDefense = _currentStatsData.Defense;
        _damageReceiver.ResetData(_currentStatsData);
    }

    public float GetMaxHealth() {
        return _maxHealth;
    }
    
    public float GetMaxDefense() {
        return _maxDefense;
    }

    public float GetCurrentHealth() {
        return _currentStatsData.HP;
    }

    public float GetCurrentDefense() {
        return _currentStatsData.Defense;
    }

    public bool TakeDamage(float pureDamage) {
        return _damageReceiver.TakeDamage(pureDamage);
    }
}

public class DamageReceiver {
    private UnitStatsConfig _statsConfig;

    public void ResetData(UnitStatsConfig statsConfig) {
        _statsConfig = statsConfig;
    }

    public bool TakeDamage(float pureDamage) {
        _statsConfig.Defense -= pureDamage;

        if (_statsConfig.Defense < 0) {
            _statsConfig.HP -= Mathf.Abs(_statsConfig.Defense);
            _statsConfig.Defense = 0;
        }

        return _statsConfig.HP <= 0;
    }
}