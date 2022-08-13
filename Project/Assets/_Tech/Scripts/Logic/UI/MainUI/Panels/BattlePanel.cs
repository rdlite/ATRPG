using System;
using UnityEngine;
using UnityEngine.UI;

public class BattlePanel : UIPanel {
    [SerializeField] private AbilityButton _walkButton;
    [SerializeField] private Button _waitButton, _backToCurrentUnitButton;
    [SerializeField] private TurnsUILayout _turnsLayoutHandler;

    protected override void LocalInit() {
        _turnsLayoutHandler.Init();
        _waitButton.gameObject.SetActive(false);
        _walkButton.gameObject.SetActive(false);
        _backToCurrentUnitButton.gameObject.SetActive(false);
    }

    public void AddTurnIcon(TurnsUILayout.IconType type) {
        _turnsLayoutHandler.CreateNewIcon(type);
    }

    public bool CheckIfNeedNewTurnIcons() {
        return _turnsLayoutHandler.CheckIfNeedNewIcons();
    }

    public void DestroyFirstIcon() {
        _turnsLayoutHandler.DestroyFirstIcon();
    }

    public void SignOnWaitButton(Action callback) {
        _waitButton.onClick.AddListener(() => callback?.Invoke());
    }
    
    public void SignOnBackButton(Action callback) {
        _backToCurrentUnitButton.onClick.AddListener(() => callback?.Invoke());
    }

    public void EnableUnitPanel(BattleHandler battleHadler) {
        _walkButton.gameObject.SetActive(true);
        _waitButton.gameObject.SetActive(true);
        _walkButton.OnClick += battleHadler.SwitchWalking;
        _walkButton.OnPointerEnter += battleHadler.WalkingPointerEnter;
        _walkButton.OnPointerExit += battleHadler.WalkingPointerExit;
    }

    public void DisableUnitsPanel(BattleHandler battleHadler) {
        _walkButton.gameObject.SetActive(false);
        _waitButton.gameObject.SetActive(false);
        _walkButton.OnClick -= battleHadler.SwitchWalking;
        _walkButton.OnPointerEnter -= battleHadler.WalkingPointerEnter;
        _walkButton.OnPointerExit -= battleHadler.WalkingPointerExit;
    }

    public void SetActiveBackToUnitButton(bool value) {
        _backToCurrentUnitButton.gameObject.SetActive(value);
    }
}