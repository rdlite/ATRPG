using UnityEngine;

public class UnitWeaponHandler {
    private AssetsContainer _assetsContainer;
    private UnitSkinContainer _skinContainer;
    private GameObject _createdWeapon;

    public void Init(AssetsContainer assetsContainer, UnitSkinContainer skinContainer) {
        _assetsContainer = assetsContainer;
        _skinContainer = skinContainer;
    }

    public void CreateWeapon(WeaponPrefabsType weaponType) {
        _createdWeapon = Object.Instantiate(_assetsContainer.WeaponPrefabsContainer.GetWeaponPrefab(weaponType));
        _createdWeapon.transform.SetParent(_skinContainer.WeaponIdlePoint);
        _createdWeapon.transform.localPosition = Vector3.zero;
        _createdWeapon.transform.localRotation = Quaternion.identity;
    }

    public void SetWeaponInHand() {
        _createdWeapon.transform.SetParent(_skinContainer.WeaponInHandPoint);
        _createdWeapon.transform.localPosition = Vector3.zero;
        _createdWeapon.transform.localRotation = Quaternion.identity;
    }

    public void SetWeaponIdle() {
        _createdWeapon.transform.SetParent(_skinContainer.WeaponIdlePoint);
        _createdWeapon.transform.localPosition = Vector3.zero;
        _createdWeapon.transform.localRotation = Quaternion.identity;
    }

    public void ActivateWeapon() {
        _createdWeapon.SetActive(true);
    }

    public void DeactivateWeapon() {
        _createdWeapon.SetActive(false);
    }
}