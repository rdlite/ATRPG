using System;
using UnityEngine;

public class UnitSkinContainer : MonoBehaviour {
    public event Action OnWeaponWithdrawEventRaised;
    public event Action OnWeaponShealtEventRaised;
    public event Action UniqueAnimationEvent;

    [field: SerializeField] public Transform WeaponInHandPoint { get; private set; }
    [field: SerializeField] public Transform WeaponIdlePoint { get; private set; }

    public void RaiseWeaponWithdrawEvent() {
        OnWeaponWithdrawEventRaised?.Invoke();
    }

    public void RaiseWeaponShealtEvent() {
        OnWeaponShealtEventRaised?.Invoke();
    }

    public void SignOnAttackAnimation(Action callback) {
        UniqueAnimationEvent += callback;
    }

    public void RaiseOneAnimationAttackCallback() {
        UniqueAnimationEvent?.Invoke();

        Delegate[] dary = UniqueAnimationEvent.GetInvocationList();

        if (dary != null) {
            foreach (Delegate del in dary) {
                UniqueAnimationEvent -= (Action)del;
            }
        }
    }
}