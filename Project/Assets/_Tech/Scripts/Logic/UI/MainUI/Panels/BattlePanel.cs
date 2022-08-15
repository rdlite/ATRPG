using System;
using UnityEngine;
using UnityEngine.UI;

public class BattlePanel : UIPanel {
    [SerializeField] private EventTriggerButtonMediator _walkButton;
    [SerializeField] private Button _waitButton, _backToCurrentUnitButton;
    [SerializeField] private TurnsUILayout _turnsLayoutHandler;
    [SerializeField] private CharacterStatsPanel _characterStatsPanel;

    protected override void LocalInit() {
        _turnsLayoutHandler.Init();
        _waitButton.gameObject.SetActive(false);
        _walkButton.gameObject.SetActive(false);
        _characterStatsPanel.gameObject.SetActive(false);
        _backToCurrentUnitButton.gameObject.SetActive(false);
        _characterStatsPanel.Init();
    }

    public void AddTurnIcon(TurnsUILayout.IconType type, BattleHandler battleHadler, CharacterWalker unit) {
        _turnsLayoutHandler.CreateNewIcon(type, battleHadler, unit);
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

    public void EnableUnitPanel(BattleHandler battleHadler, CharacterWalker character) {
        _characterStatsPanel.SetData(character);
        _characterStatsPanel.gameObject.SetActive(true);
        _walkButton.gameObject.SetActive(true);
        _waitButton.gameObject.SetActive(true);
        _walkButton.OnClick += battleHadler.SwitchWalking;
        _walkButton.OnPointerEnter += battleHadler.WalkingPointerEnter;
        _walkButton.OnPointerExit += battleHadler.WalkingPointerExit;
    }

    public void DisableUnitsPanel(BattleHandler battleHadler) {
        _characterStatsPanel.gameObject.SetActive(false);
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