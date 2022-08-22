using System.Collections.Generic;

public class EnemyUnit : UnitBase {
    private EnemyContainer _container;

    protected override void LocalInit() {
        _collider.enabled = true;
    }

    public void SetContainer(EnemyContainer container) {
        _container = container;
    }

    public List<EnemyUnit> GetAllConnectedEnemies() {
        List<EnemyUnit> result = new List<EnemyUnit>();

        if (_container == null) {
            result.Add(this);
            return result;
        } else {
            return _container.GetConnectedEnemies();
        }
    }
}