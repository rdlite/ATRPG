using System;
using System.Collections.Generic;
using UnityEngine;

public class UIRoot : MonoBehaviour {
    [SerializeField] private List<PanelData> _uiPanels;

    private Dictionary<Type, PanelData> _panelsMap = new Dictionary<Type, PanelData>();
    private UIPanelType _previousPanelType, _currentPanelType;

    public void Init(
        UpdateStateMachine globalStateMachine) {
        for (int i = 0; i < _uiPanels.Count; i++) {
            _uiPanels[i].Panel.Init(
                globalStateMachine, this);

            _panelsMap.Add(_uiPanels[i].Panel.GetType(), _uiPanels[i]);
        }

        EnablePanel(UIPanelType.None);
    }

    public void EnablePanel(UIPanelType type) {
        for (int i = 0; i < _uiPanels.Count; i++) {
            if (_uiPanels[i].Type == type) {
                _uiPanels[i].Panel.Enable();
            } else {
                _uiPanels[i].Panel.Disable();
            }
        }

        _previousPanelType = _currentPanelType;
        _currentPanelType = type;
    }

    public void EnablePanel<TPayload>(UIPanelType type, TPayload payload) {
        for (int i = 0; i < _uiPanels.Count; i++) {
            if (_uiPanels[i].Type == type) {
                _uiPanels[i].Panel.Enable(payload);
            } else {
                _uiPanels[i].Panel.Disable();
            }
        }

        _previousPanelType = _currentPanelType;
        _currentPanelType = type;
    }

    public void EnablePreviousPanel() {
        EnablePanel(_previousPanelType);
    }

    public void EnablePreviousPanel<TPayload>(TPayload payload) {
        EnablePanel(_previousPanelType, payload);
    }

    public T GetPanel<T>() where T : UIPanel {
        return (T)_panelsMap[typeof(T)].Panel;
    }

    [Serializable]
    private class PanelData {
        public UIPanelType Type;
        public UIPanel Panel;
    }

    public enum UIPanelType {
        None, WorldWalking, BattleState
    }
}