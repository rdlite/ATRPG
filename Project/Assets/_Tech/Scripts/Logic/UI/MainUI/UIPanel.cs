using UnityEngine;

public abstract class UIPanel : MonoBehaviour {
    protected UIRoot _ui;
    protected UpdateStateMachine _globalStateMachine;

    public void Init(
        UpdateStateMachine globalStateMachine, UIRoot ui) {
        _globalStateMachine = globalStateMachine;
        _ui = ui;
        LocalInit();
    }

    protected abstract void LocalInit();

    public virtual void Enable() {
        gameObject.SetActive(true);
    }

    public virtual void Enable<TPayload>(TPayload payload) {
        gameObject.SetActive(true);
    }

    public virtual void Disable() {
        gameObject.SetActive(false);
    }
}

public class PayloadData {
    public bool IsShowAnimation;

    public PayloadData(bool isShowAnimation) {
        IsShowAnimation = isShowAnimation;
    }
}