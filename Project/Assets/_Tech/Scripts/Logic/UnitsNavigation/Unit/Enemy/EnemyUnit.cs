using System.Collections.Generic;
using UnityEngine;

public class EnemyUnit : UnitBase
{
    private EnemyContainer _container;

    protected override void LocalInit()
    {
        _collider.enabled = true;
    }

    public void SetContainer(EnemyContainer container)
    {
        _container = container;
    }

    public List<EnemyUnit> GetAllConnectedEnemies()
    {
        List<EnemyUnit> result = new List<EnemyUnit>();

        if (_container == null)
        {
            result.Add(this);
            return result;
        }
        else
        {
            return _container.GetConnectedEnemies();
        }
    }

    [ContextMenu("Create decal")]
    public void CreateBloodDecal()
    {
        BloodDecalAppearance bloodDecal = Object.Instantiate(_assetsContainer.BloodDecal);
        bloodDecal.ThrowDecalOnSurface(GetAttackPoint().position, -transform.forward);
    }
}