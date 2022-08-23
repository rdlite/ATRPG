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
        if (TryGetComponent(out CanvasGroup group)) {
            if (group.interactable) {
                OnClick?.Invoke();
            }
        } else {
            OnClick?.Invoke();
        }
    }
}