public class EnemyCharacterWalker : CharacterWalker {
    protected override void LocalInit() {
        _collider.enabled = true;
    }
}