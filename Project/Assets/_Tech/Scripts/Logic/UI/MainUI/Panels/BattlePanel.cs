using System;
using UnityEngine;
using UnityEngine.UI;

public class BattlePanel : UIPanel {
    [SerializeField] private Button _backToCurrentUnitButton, _abortImposionButton;
    [SerializeField] private AbilityButton _waitButton, _walkButton, _attackButton, _abortImposed;
    [SerializeField] private TurnsUILayout _turnsLayoutHandler;
    [SerializeField] private CharacterStatsPanel _characterStatsPanel;

    private bool _isUnitPanelEnabled;

    protected override void LocalInit() {
        _turnsLayoutHandler.Init();
        _waitButton.SetActiveObject(false);
        _walkButton.SetActiveObject(false);
        _attackButton.SetActiveObject(false);
        _abortImposed.SetActiveObject(false);
        _characterStatsPanel.gameObject.SetActive(false);
        _backToCurrentUnitButton.gameObject.SetActive(false);
        _characterStatsPanel.Init();
    }

    public void CleanupAfterBattleEnd() {
        _isUnitPanelEnabled = false;

        _turnsLayoutHandler.Cleanup();

        _characterStatsPanel.gameObject.SetActive(false);
        _waitButton.SetActiveObject(false);
        _walkButton.SetActiveObject(false);
        _attackButton.SetActiveObject(false);

        _walkButton.Cleanup();
        _attackButton.Cleanup();
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

    public void DestroyIconOfUnit(UnitBase unitToDestroyIcon) {
        _turnsLayoutHandler.DestroyIconOfUnit(unitToDestroyIcon);
    }
    
    public void DestroyIconAt(int id) {
        _turnsLayoutHandler.DestroyIconAt(id);
    }

    public void SignOnWaitButton(Action callback) {
        _waitButton.Button.onClick.AddListener(() => callback?.Invoke());
    }
    
    public void SignOnBackButton(Action callback) {
        _backToCurrentUnitButton.onClick.AddListener(() => callback?.Invoke());
    }
    
    public void SignOnAbortImposionButton(Action callback) {
        _abortImposionButton.onClick.AddListener(() => callback?.Invoke());
    }

    public void EnableUnitPanel(
        BattleHandler battleHadler, UnitBase unit, UnitPanelState state,
        BattleTurnsHandler turnHandler, bool isImposed) {
        _isUnitPanelEnabled = true;

        _characterStatsPanel.SetData(unit);
        _characterStatsPanel.gameObject.SetActive(true);
        _waitButton.SetActiveObject(true);
        _abortImposed.SetActiveObject(true && isImposed);
        _walkButton.SetActiveObject(true && !isImposed);
        _attackButton.SetActiveObject(true);

        _waitButton.SetActiveView(state == UnitPanelState.UseTurn);
        _walkButton.SetActiveView(state == UnitPanelState.UseTurn);
        _abortImposed.SetActiveView(state == UnitPanelState.UseTurn);
        _attackButton.SetActiveView(state == UnitPanelState.UseTurn && !turnHandler.IsUnitAttackedDefaultAttack(unit));

        _walkButton.EventsMediator.OnClick += battleHadler.SwitchWalking;
        _attackButton.EventsMediator.OnClick += battleHadler.SwitchAttacking;
        _walkButton.EventsMediator.OnPointerEnter += battleHadler.WalkingPointerEnter;
        _walkButton.EventsMediator.OnPointerExit += battleHadler.WalkingPointerExit;
    }

    public void DisableUnitsPanel(BattleHandler battleHadler) {
        _isUnitPanelEnabled = false;

        _characterStatsPanel.gameObject.SetActive(false);
        _waitButton.SetActiveObject(false);
        _walkButton.SetActiveObject(false);
        _abortImposed.SetActiveObject(false);
        _attackButton.SetActiveObject(false);

        _walkButton.EventsMediator.OnClick -= battleHadler.SwitchWalking;
        _attackButton.EventsMediator.OnClick -= battleHadler.SwitchAttacking;
        _walkButton.EventsMediator.OnPointerEnter -= battleHadler.WalkingPointerEnter;
        _walkButton.EventsMediator.OnPointerExit -= battleHadler.WalkingPointerExit;
    }

    public bool IsUnitPanelAlreadyEnabled() {
        return _isUnitPanelEnabled;
    }

    public void SetActiveBackToUnitButton(bool value) {
        _backToCurrentUnitButton.gameObject.SetActive(value);
    }
}

public enum UnitPanelState {
    UseTurn, ViewTurn, CompletelyDeactivate
}