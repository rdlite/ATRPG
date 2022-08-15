using System;
using UnityEngine;

public class EventTriggerButtonMediator : MonoBehaviour {
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