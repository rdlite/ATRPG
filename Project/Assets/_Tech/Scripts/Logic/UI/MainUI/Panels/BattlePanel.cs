using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattlePanel : UIPanel {
    [SerializeField] private Button _backToCurrentUnitButton;
    [SerializeField] private AbilityButton _waitButton, _abilityButtonPrefab;
    [SerializeField] private Transform _buttonsLayout;
    [SerializeField] private TurnsUILayout _turnsLayoutHandler;
    [SerializeField] private CharacterStatsPanel _characterStatsPanel;

    private List<AbilityButton> _createdAbilityButtons;
    private bool _isUnitPanelEnabled;

    protected override void LocalInit() {
        _createdAbilityButtons = new List<AbilityButton>();

        _turnsLayoutHandler.Init();
        _waitButton.SetActiveObject(false);
        _characterStatsPanel.gameObject.SetActive(false);
        _backToCurrentUnitButton.gameObject.SetActive(false);
        _characterStatsPanel.Init();
    }

    public void CleanupAfterBattleEnd() {
        _isUnitPanelEnabled = false;

        _turnsLayoutHandler.Cleanup();

        _characterStatsPanel.gameObject.SetActive(false);
        _waitButton.SetActiveObject(false);

        _waitButton.Button.onClick.RemoveAllListeners();
        _backToCurrentUnitButton.onClick.RemoveAllListeners();
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
    
    public void EnableUnitPanel(
        BattleHandler battleHadler, UnitBase unit, UnitPanelState state,
        BattleTurnsHandler turnHandler, bool isImposed) {
        _isUnitPanelEnabled = true;

        _characterStatsPanel.SetData(unit);
        _characterStatsPanel.gameObject.SetActive(true);
        _waitButton.SetActiveObject(true);

        _waitButton.SetActiveView(state == UnitPanelState.UseTurn);

        foreach (var unitAbility in unit.GetUnitAbilitites()) {
            AbilityButton createdAbilityButton = CreateButton(unitAbility);

            if (unitAbility.Type == AbilityType.Walk) {
                createdAbilityButton.SetImage(isImposed ? unitAbility.AdditionalButtonImage : unitAbility.ButtonImage);
                createdAbilityButton.SetActiveObject(true);
                createdAbilityButton.SetActiveView(state == UnitPanelState.UseTurn);
            } else {
                createdAbilityButton.SetImage(unitAbility.ButtonImage);
                createdAbilityButton.SetActiveObject(true);
                createdAbilityButton.SetActiveView(state == UnitPanelState.UseTurn && !turnHandler.IsUnitUsedAbility(unit, unitAbility));
            }

            if (unitAbility.IsHavePointerButtonEvents) {
                createdAbilityButton.EventsMediator.OnPointerEnter += () => battleHadler.AbilityButtonPointerEnter(unitAbility, isImposed);
                createdAbilityButton.EventsMediator.OnPointerExit += () => battleHadler.AbilityButtonPointerExit(unitAbility, isImposed);
            }

            createdAbilityButton.EventsMediator.OnClick += () => battleHadler.AbilityButtonPressed(unitAbility, isImposed);

            _createdAbilityButtons.Add(createdAbilityButton);
        }
    }

    private AbilityButton CreateButton(BattleFieldActionAbility abilityData) {
        AbilityButton newAbilityButton = Instantiate(_abilityButtonPrefab);
        newAbilityButton.transform.SetParent(_buttonsLayout);
        newAbilityButton.transform.localScale = _abilityButtonPrefab.transform.localScale;
        newAbilityButton.transform.localRotation = _abilityButtonPrefab.transform.localRotation;

        return newAbilityButton;
    }

    public void DisableUnitsPanel(BattleHandler battleHadler) {
        _isUnitPanelEnabled = false;

        _characterStatsPanel.gameObject.SetActive(false);
        _waitButton.SetActiveObject(false);

        foreach (var abilityButton in _createdAbilityButtons) {
            Destroy(abilityButton.gameObject);
        }

        _createdAbilityButtons.Clear();
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