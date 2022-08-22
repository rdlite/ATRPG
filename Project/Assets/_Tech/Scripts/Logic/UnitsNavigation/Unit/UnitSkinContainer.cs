using System;
using UnityEngine;

public class UnitSkinContainer : MonoBehaviour {
    public event Action OnWeaponWithdrawEventRaised;

    [field: SerializeField] public Transform WeaponInHandPoint { get; private set; }
    [field: SerializeField] public Transform WeaponIdlePoint { get; private set; }

    public void RaiseWeaponWithdrawEvent() {
        OnWeaponWithdrawEventRaised?.Invoke();
    }
}