using System;
using UnityEngine;

public class AbilityButton : MonoBehaviour {
    public event Action OnPointerEnter, OnPointerExit, OnClick;

    public void RaisePointerEnterEvent() {
        OnPointerEnter?.Invoke();
    }

    public void RaisePointerExitEvent() {
        OnPointerExit?.Invoke();
    }

    public void RaiseOnClickEvent() {
        OnClick?.Invoke();
    }
}