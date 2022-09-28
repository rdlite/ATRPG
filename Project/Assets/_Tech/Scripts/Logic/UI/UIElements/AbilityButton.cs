using UnityEngine;
using UnityEngine.UI;

public class AbilityButton : MonoBehaviour {
    [field: SerializeField] public EventTriggerButtonMediator EventsMediator { get; private set; }
    [field: SerializeField] public Button Button { get; private set; }

    [SerializeField] private Image _buttonImage;
    [SerializeField] private CanvasGroup _canvasGroup;

    public void SetActiveObject(bool value) {
        gameObject.SetActive(value);
    }

    public void SetImage(Sprite buttonImage) {
        _buttonImage.sprite = buttonImage;
    }

    public void SetActiveView(bool value) {
        _canvasGroup.alpha = value ? 1f : .5f;
        _canvasGroup.interactable = value;
    }

    public void Cleanup() {
        EventsMediator.RemoveAllListeners();
    }
}