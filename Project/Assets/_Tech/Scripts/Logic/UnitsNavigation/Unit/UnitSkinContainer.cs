using System;
using System.Collections.Generic;
using UnityEngine;

public class UnitSkinContainer : MonoBehaviour
{
    public event Action OnWeaponWithdrawEventRaised;
    public event Action OnWeaponShealtEventRaised;
    public event Action UniqueAnimationEvent;

    [SerializeField] private List<WeaponPoint> _weaponPoints = new List<WeaponPoint>();

    public void RaiseWeaponWithdrawEvent()
    {
        OnWeaponWithdrawEventRaised?.Invoke();
    }

    public void RaiseWeaponShealtEvent()
    {
        OnWeaponShealtEventRaised?.Invoke();
    }

    public void SignOnAttackAnimation(Action callback)
    {
        UniqueAnimationEvent += callback;
    }

    public void RaiseOneAnimationAttackCallback()
    {
        UniqueAnimationEvent?.Invoke();

        if (UniqueAnimationEvent != null)
        {
            Delegate[] dary = UniqueAnimationEvent.GetInvocationList();

            if (dary != null)
            {
                foreach (Delegate del in dary)
                {
                    UniqueAnimationEvent -= (Action)del;
                }
            }
        }
    }

    public Transform GetWeaponAttackPointByType(WeaponAnimationLayerType layerType)
    {
        for (int i = 0; i < _weaponPoints.Count; i++)
        {
            if (_weaponPoints[i].Type == layerType)
            {
                return _weaponPoints[i].AttackPoint;
            }
        }

        return null;
    }

    public Transform GetWeaponIdlePointByType(WeaponAnimationLayerType layerType)
    {
        for (int i = 0; i < _weaponPoints.Count; i++)
        {
            if (_weaponPoints[i].Type == layerType)
            {
                return _weaponPoints[i].IdlePoint;
            }
        }

        return null;
    }

    [System.Serializable]
    private class WeaponPoint
    {
        public WeaponAnimationLayerType Type;
        public Transform AttackPoint;
        public Transform IdlePoint;
    }
}