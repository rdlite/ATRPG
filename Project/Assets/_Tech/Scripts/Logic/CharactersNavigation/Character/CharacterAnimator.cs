using UnityEngine;

public class CharacterAnimator {
    private int IS_MOVING_HASH = Animator.StringToHash("IsMoving");
    private int MOVEMENT_MAGNITUDE = Animator.StringToHash("MovementMagnitude");

    private Animator _animator;

    public CharacterAnimator(Animator animator) {
        _animator = animator;
    }

    public void SetMovementAnimation(bool value) {
        _animator.SetBool(IS_MOVING_HASH, value);
    }

    public void SetMovementMagnitude(float value) {
        _animator.SetFloat(MOVEMENT_MAGNITUDE, value);
    }
}