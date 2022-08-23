using System;
using UnityEngine;
using UnityEngine.UI;

public class BattlePanel : UIPanel {
    [SerializeField] private Button _backToCurrentUnitButton;
    [SerializeField] private AbilityButton _waitButton, _walkButton, _attackButton;
    [SerializeField] private TurnsUILayout _turnsLayoutHandler;
    [SerializeField] private CharacterStatsPanel _characterStatsPanel;

    protected override void LocalInit() {
        _turnsLayoutHandler.Init();
        _waitButton.SetActiveObject(false);
        _walkButton.SetActiveObject(false);
        _attackButton.SetActiveObject(false);
        _characterStatsPanel.gameObject.SetActive(false);
        _backToCurrentUnitButton.gameObject.SetActive(false);
        _characterStatsPanel.Init();
    }

    public void AddTurnIcon(TurnsUILayout.IconType type, BattleHandler battleHadler, UnitBase unit) {
        _turnsLayoutHandler.CreateNewIcon(type, battleHadler, unit);
    }

    public bool CheckIfNeedNewTurnIcons() {
        return _turnsLayoutHandler.CheckIfNeedNewIcons();
    }

    public void DestroyFirstIcon() {
        _turnsLayoutHandler.DestroyFirstIcon();
    }

    public void SignOnWaitButton(Action callback) {
        _waitButton.Button.onClick.AddListener(() => callback?.Invoke());
    }
    
    public void SignOnBackButton(Action callback) {
        _backToCurrentUnitButton.onClick.AddListener(() => callback?.Invoke());
    }

    public void EnableUnitPanel(BattleHandler battleHadler, UnitBase character, UnitPanelState state) {
        _characterStatsPanel.SetData(character);
        _characterStatsPanel.gameObject.SetActive(true);
        _waitButton.SetActiveObject(true);
        _walkButton.SetActiveObject(true);
        _attackButton.SetActiveObject(true);

        _waitButton.SetActiveView(state == UnitPanelState.UseTurn);
        _walkButton.SetActiveView(state == UnitPanelState.UseTurn);
        _attackButton.SetActiveView(state == UnitPanelState.UseTurn);

        _walkButton.EventsMediator.OnClick += battleHadler.SwitchWalking;
        _attackButton.EventsMediator.OnClick += battleHadler.SwitchAttacking;
        _walkButton.EventsMediator.OnPointerEnter += battleHadler.WalkingPointerEnter;
        _walkButton.EventsMediator.OnPointerExit += battleHadler.WalkingPointerExit;
    }

    public void DisableUnitsPanel(BattleHandler battleHadler) {
        _characterStatsPanel.gameObject.SetActive(false);
        _waitButton.SetActiveObject(false);
        _walkButton.SetActiveObject(false);
        _attackButton.SetActiveObject(false);
        _walkButton.EventsMediator.OnClick -= battleHadler.SwitchWalking;
        _attackButton.EventsMediator.OnClick -= battleHadler.SwitchAttacking;
        _walkButton.EventsMediator.OnPointerEnter -= battleHadler.WalkingPointerEnter;
        _walkButton.EventsMediator.OnPointerExit -= battleHadler.WalkingPointerExit;
    }

    public void SetActiveBackToUnitButton(bool value) {
        _backToCurrentUnitButton.gameObject.SetActive(value);
    }
}

public enum UnitPanelState {
    UseTurn, ViewTurn, CompletelyDeactivate
}