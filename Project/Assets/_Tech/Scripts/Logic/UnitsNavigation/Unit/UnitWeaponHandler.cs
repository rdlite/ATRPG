using UnityEngine;

public class UnitWeaponHandler
{
    private AssetsContainer _assetsContainer;
    private UnitSkinContainer _skinContainer;
    private GameObject _createdWeapon;

    private WeaponPrefabsType _currentWeaponType;
    private int _characterLayer;

    public void Init(
        AssetsContainer assetsContainer, UnitSkinContainer skinContainer, int layer)
    {
        _assetsContainer = assetsContainer;
        _skinContainer = skinContainer;

        _characterLayer = layer;
    }

    public void CreateWeapon(WeaponPrefabsType weaponType)
    {
        if (weaponType != WeaponPrefabsType.None)
        {
            _createdWeapon = Object.Instantiate(_assetsContainer.WeaponPrefabsContainer.GetWeaponPrefab(weaponType));
            _currentWeaponType = weaponType;
            _createdWeapon.transform.SetParent(_skinContainer.GetWeaponIdlePointByType(CurrentWeaponLayerType()));
            _createdWeapon.transform.localPosition = Vector3.zero;
            _createdWeapon.transform.localRotation = Quaternion.identity;
            _createdWeapon.gameObject.layer = _characterLayer;
            foreach (Transform weaponChild in _createdWeapon.transform)
            {
                weaponChild.gameObject.layer = _characterLayer;
            }
        }
    }

    public WeaponPrefabsType GetCurrentWeaponType()
    {
        return _currentWeaponType;
    }

    public void SetWeaponInHand()
    {
        if (_createdWeapon != null)
        {
            _createdWeapon.transform.SetParent(_skinContainer.GetWeaponAttackPointByType(CurrentWeaponLayerType()));
            _createdWeapon.transform.localPosition = Vector3.zero;
            _createdWeapon.transform.localRotation = Quaternion.identity;
        }
    }

    public void SetWeaponIdle()
    {
        if (_createdWeapon != null)
        {
            _createdWeapon.transform.SetParent(_skinContainer.GetWeaponIdlePointByType(CurrentWeaponLayerType()));
            _createdWeapon.transform.localPosition = Vector3.zero;
            _createdWeapon.transform.localRotation = Quaternion.identity;
        }
    }

    public void ActivateWeapon()
    {
        _createdWeapon?.SetActive(true);
    }

    public void DeactivateWeapon()
    {
        _createdWeapon?.SetActive(false);
    }

    private WeaponAnimationLayerType CurrentWeaponLayerType() => _assetsContainer.WeaponPrefabsContainer.GetWeaponLayerType(_currentWeaponType);
}