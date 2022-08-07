using UnityEngine;

public class PlayerCharacterWalker : CharacterWalker {
    [SerializeField] private CharactersDetectorTrigger _characterDetectorTrigger;
    [SerializeField] private GameObject _viewFow;
    [SerializeField] private bool _isMainChar;

    protected override void LocalInit() {
        _viewFow.SetActive(true);
        _characterDetectorTrigger.OnCharacterEnterToTrigger += CharacterDetected;
        _characterDetectorTrigger.OnCharacterExitedGromTrigger += CharacterLostInVew;
    }

    public override void Tick() {
        base.Tick();

        if (_isMainChar && Input.GetMouseButtonDown(1)) {
            _agent.StopMovement();
            _fieldRaycaster.ClearWalkPoints();
        }
    }

    private void CharacterDetected(CharacterWalker character) {
        if (character is EnemyCharacterWalker enemy) {
            _abstractEntityMediator.TryEnterToBattleState(enemy);
        }
    }

    public override void AbortMovement() {
        base.AbortMovement();
        _fieldRaycaster.ClearWalkPoints();
    }

    private void CharacterLostInVew(CharacterWalker character) { }
}