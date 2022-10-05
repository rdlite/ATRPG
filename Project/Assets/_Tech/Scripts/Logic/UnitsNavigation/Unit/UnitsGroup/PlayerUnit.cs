using UnityEngine;

public class PlayerUnit : UnitBase
{
    [SerializeField] private UnitsDetectorTrigger _characterDetectorTrigger;
    [SerializeField] private GameObject _viewFow;
    [SerializeField] private bool _isMainChar;

    protected override void LocalInit()
    {
        _viewFow.SetActive(true);
        _characterDetectorTrigger.OnCharacterEnterToTrigger += CharacterDetected;
        _characterDetectorTrigger.OnCharacterExitedGromTrigger += CharacterLostInVew;
    }

    private void CharacterDetected(UnitBase character)
    {
        if (character is EnemyUnit enemy)
        {
            _abstractEntityMediator.TryEnterToBattleState(enemy);
        }
    }

    public override void AbortMovement()
    {
        base.AbortMovement();
        _fieldRaycaster.ClearWalkPoints();
    }

    private void CharacterLostInVew(UnitBase character) { }
}