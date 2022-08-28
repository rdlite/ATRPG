using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitAnimator {
    private int IS_MOVING_HASH = Animator.StringToHash("IsMoving");
    private int IS_DEAD_HASH = Animator.StringToHash("IsDead");
    private int MOVEMENT_MAGNITUDE_HASH = Animator.StringToHash("MovementMagnitude");
    private int IMPART_TRIIGER_HASH = Animator.StringToHash("ImpactTrigger");
    private int ATTACK_TRIGGER_HASH = Animator.StringToHash("AttackTrigger");
    private int WITHDRAWWEAPON_TRIGGER_HASH = Animator.StringToHash("WithdrawWeaponTrigger");
    private int SHEALTWEAPON_TRIGGER_HASH = Animator.StringToHash("ShealtWeaponTrigger");
    private int ANIMATON_SPEED_RANDOM_HASH = Animator.StringToHash("AnimationSpeedRandom");

    private Dictionary<WeaponAnimationLayerType, int> _weaponLayers_map = new Dictionary<WeaponAnimationLayerType, int>();
    private ICoroutineService _coroutineService;
    private Animator _animator;
    private UnitWeaponHandler _weaponHandler;
    private UnitSkinContainer _skinContainer;
    private WeaponAnimationLayerType _currentWeapon = WeaponAnimationLayerType.OneHandSword;
    private Coroutine _smoothLayerChangeRoutine;
    private float _layerChangeDuration = .3f;

    public UnitAnimator(
        Animator animator, ICoroutineService coroutineService, UnitWeaponHandler weaponHandler, 
        UnitSkinContainer skinContainer) {
        _coroutineService = coroutineService;
        _animator = animator;
        _weaponHandler = weaponHandler;
        _skinContainer = skinContainer;

        _weaponLayers_map.Add(WeaponAnimationLayerType.Hands, 0);
        _weaponLayers_map.Add(WeaponAnimationLayerType.OneHandSword, 1);

        _skinContainer.OnWeaponWithdrawEventRaised += WithdrawWeaponToHand;
        _skinContainer.OnWeaponShealtEventRaised += ShealthWeaponToIdle;
    }

    public void SetMovementAnimation(bool value) {
        _animator.SetBool(IS_MOVING_HASH, value);
    }

    public void SetMovementMagnitude(float value) {
        _animator.SetFloat(MOVEMENT_MAGNITUDE_HASH, value);
    }

    public void WithdrawWeapon() {
        for (int i = 0; i < _animator.layerCount; i++) {
            _animator.SetLayerWeight(i, 0f);
        }

        _animator.SetLayerWeight(0, 1f);

        if (_smoothLayerChangeRoutine != null) {
            _coroutineService.StopCoroutine(_smoothLayerChangeRoutine);
        }

        _smoothLayerChangeRoutine = _coroutineService.StartCoroutine(SetSmoothLayerWeight(_weaponLayers_map[_currentWeapon], 0f, 1f));
        _animator.SetFloat(ANIMATON_SPEED_RANDOM_HASH, Random.Range(.7f, 1.3f));
        _animator.SetTrigger(WITHDRAWWEAPON_TRIGGER_HASH);
    }

    public void ShealtWeapon() {
        for (int i = 0; i < _animator.layerCount; i++) {
            _animator.SetLayerWeight(i, 0f);
        }

        _animator.SetLayerWeight(0, 1f);

        if (_smoothLayerChangeRoutine != null) {
            _coroutineService.StopCoroutine(_smoothLayerChangeRoutine);
        }

        _smoothLayerChangeRoutine = _coroutineService.StartCoroutine(SetSmoothLayerWeight(_weaponLayers_map[_currentWeapon], 1f, 0f));
        _animator.SetFloat(ANIMATON_SPEED_RANDOM_HASH, Random.Range(.7f, 1.3f));
        _animator.SetTrigger(SHEALTWEAPON_TRIGGER_HASH);
    }

    public void PlayAttackAnimation() {
        _animator.SetTrigger(ATTACK_TRIGGER_HASH);
    }

    public void PlayImpactFromSwordAnimation() {
        _animator.SetTrigger(IMPART_TRIIGER_HASH);
    }

    public void PlayDeadAnimation() {
        _animator.SetBool(IS_DEAD_HASH, true);
    }

    private void WithdrawWeaponToHand() {
        _weaponHandler.SetWeaponInHand();
    }

    private void ShealthWeaponToIdle() {
        _weaponHandler.SetWeaponIdle();
    }

    public void SetActiveWeapon(bool value) {
        if (!value) {
            _weaponHandler.DeactivateWeapon();
        }
    }

    private IEnumerator SetSmoothLayerWeight(int layerID, float startValue, float endValue) {
        float t = 0f;

        while (t <= 1f) {
            t += Time.deltaTime / _layerChangeDuration;
            _animator.SetLayerWeight(layerID, Mathf.Lerp(startValue, endValue, t));
            yield return null;
        }
    }
}

public enum WeaponAnimationLayerType {
    Hands, OneHandSword
}