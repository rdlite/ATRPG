using UnityEngine;

public class UnitHealth {
    private UnitStatsConfig _defaultStatsCopy;
    private UnitStatsConfig _currentStatsData;
    private DamageReceiver _damageReceiver;

    public UnitHealth(UnitStatsConfig statsData) {
        _currentStatsData = statsData.GetCopyOfData();
        _defaultStatsCopy = statsData.GetCopyOfData();
        _damageReceiver = new DamageReceiver();
        _damageReceiver.ResetData(_currentStatsData);
    }

    public void ResetData() {
        _currentStatsData = _defaultStatsCopy.GetCopyOfData();
        _damageReceiver.ResetData(_currentStatsData);
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
        float currentHealth = _statsConfig.HP;
        currentHealth -= pureDamage;
        return currentHealth <= 0;
    }
}