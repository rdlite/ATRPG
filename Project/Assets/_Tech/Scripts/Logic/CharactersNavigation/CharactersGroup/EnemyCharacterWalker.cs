using System.Collections.Generic;

public class EnemyCharacterWalker : CharacterWalker {
    private EnemyContainer _container;

    protected override void LocalInit() {
        _collider.enabled = true;
    }

    public void SetContainer(EnemyContainer container) {
        _container = container;
    }

    public List<EnemyCharacterWalker> GetAllConnectedEnemies() {
        List<EnemyCharacterWalker> result = new List<EnemyCharacterWalker>();

        if (_container == null) {
            result.Add(this);
            return result;
        } else {
            return _container.GetConnectedEnemies();
        }
    }
}