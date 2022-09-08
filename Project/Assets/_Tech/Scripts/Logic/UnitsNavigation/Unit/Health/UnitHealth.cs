using System;
using UnityEngine;

public class UnitHealth {
    public event Action OnUnitHealthChanged;

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
        bool isDead = _damageReceiver.TakeDamage(pureDamage);
        OnUnitHealthChanged?.Invoke();

        return isDead;
    }

    public (bool, float) GedModifiedDamageAmount(float notModifiedDamage) {
        float modifiedDamage = notModifiedDamage;
        return new(_damageReceiver.IsCanKill(modifiedDamage), modifiedDamage);
    }

    public float GetHealthCompleteness() {
        float maxHealthPoints = _maxDefense + _maxHealth;
        float currentValues = _currentStatsData.HP + _currentStatsData.Defense;

        return currentValues / maxHealthPoints;
    }
}

public class DamageReceiver {
    private UnitStatsConfig _statsConfig;

    public void ResetData(UnitStatsConfig statsConfig) {
        _statsConfig = statsConfig;
    }

    public bool TakeDamage(float pureDamage) {
        _statsConfig.Defense -= pureDamage;

        if (_statsConfig.Defense < 0f) {
            _statsConfig.HP -= Mathf.Abs(_statsConfig.Defense);
            if (_statsConfig.HP < 0f) {
                _statsConfig.HP = 0f;
            }

            _statsConfig.Defense = 0f;
        }

        return _statsConfig.HP <= 0f;
    }

    public bool IsCanKill(float pureDamage) {
        float currentDefense = _statsConfig.Defense - pureDamage;

        if (_statsConfig.Defense < 0f) {
            if ((_statsConfig.HP - Mathf.Abs(currentDefense)) < 0f) {
                return true;
            }
        }

        return false;
    }
}