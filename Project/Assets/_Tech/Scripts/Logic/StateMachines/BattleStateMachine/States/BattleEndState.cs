using UnityEngine;

public class BattleEndState : IPayloadState<bool>
{
    private BattleGridGenerator _gridGenerator;
    private BattleGridData _battleGridData;
    private BattleTurnsHandler _turnsHandler;
    private BattleHandler _battleHandler;

    public BattleEndState(
        BattleHandler battleHandler, BattleTurnsHandler turnsHandler, BattleGridData battleGridData,
        BattleGridGenerator gridGenerator)
    {
        _gridGenerator = gridGenerator;
        _battleGridData = battleGridData;
        _turnsHandler = turnsHandler;
        _battleHandler = battleHandler;
    }

    public void Enter(bool isPlayerWon)
    {
        Object.Destroy(_battleHandler.AttackRangeDecalProjector.gameObject);

        _battleHandler.IsBattleEnded = true;

        _battleHandler.DestroyAllBloodDecals();
        _battleHandler.DestroyAllStunEffects();

        _battleHandler.DeselectAllUnits();
        _battleHandler.CompletelyDeactivateOverUnitsData();

        _turnsHandler.Cleanup();

        if (isPlayerWon)
        {
            for (int i = 0; i < _battleGridData.Units.Count; i++)
            {
                _battleHandler.DestroyUnderUnitWalkDecal(i);
                _battleHandler.DestroyUnderUnitAttackDecal(i);

                if (!_battleGridData.Units[i].IsDeadOnBattleField)
                {
                    if (_battleGridData.Units[i] is PlayerUnit)
                    {
                        _battleGridData.Units[i].HealAfterBattle();
                    }

                    _battleGridData.Units[i].ShealtWeapon();
                }
                else
                {
                    if (_battleGridData.Units[i] is EnemyUnit)
                    {
                        _battleGridData.Units[i].DeactivateWeapon();
                    }
                    else
                    {
                        _battleGridData.Units[i].Revive();
                    }
                }
            }

            for (int x = 0; x < _battleGridData.Width; x++)
            {
                for (int y = 0; y < _battleGridData.Height; y++)
                {
                    _battleGridData.NodesGrid[x, y].SetPlacedByUnit(false);
                }
            }

            _gridGenerator.StopBattle();
        }
    }

    public void Exit() { }
}