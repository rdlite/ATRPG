using UnityEngine;

public class DamageInfoPanel : MonoBehaviour {
    [SerializeField] private GameObject _skullIcon, _damageIcon;
    [SerializeField] private TMPro.TextMeshProUGUI _damageNumber;

    public void SetActivePanel(bool isDeadlyDamage, int damageAmount) {
        _skullIcon.SetActive(isDeadlyDamage);
        _damageIcon.SetActive(!isDeadlyDamage);
        _damageNumber.text = damageAmount.ToString();
        gameObject.SetActive(true);
    }

    public void DeactivatePanel() {
        gameObject.SetActive(false);
    }
}