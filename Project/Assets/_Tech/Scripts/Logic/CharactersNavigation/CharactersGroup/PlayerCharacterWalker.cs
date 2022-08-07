using UnityEngine;

public class PlayerCharacterWalker : CharacterWalker {
    [SerializeField] private GameObject _viewFow;
    [SerializeField] private bool _isMainChar;

    protected override void LocalInit() {
        _viewFow.SetActive(true);
    }

    protected override void Update() {
        base.Update();

        if (_isMainChar && Input.GetMouseButtonDown(1)) {
            _agent.StopMovement();
            _fieldRaycaster.ClearWalkPoints();
        }
    }
}
