using System.Collections;
using TMPro;
using UnityEngine;

public class DynamicObjectsPanel : MonoBehaviour {
    [SerializeField] private AnimationCurve _damageNumberAlphaCurve;
    [SerializeField] private AnimationCurve _damageNumberScaleCurve;
    [SerializeField] private Color _defaultDamageNumbersColor, _criticalDamageNumbersColor;
    [SerializeField] private float _damageNumberDuration = 3f;
    [SerializeField] private float _damageNumberUpMovementSpeed = .5f;

    private ICoroutineService _coroutineService;
    private AssetsContainer _assetsContainer;

    public void Init(AssetsContainer assetsContainer, ICoroutineService coroutineService) {
        _coroutineService = coroutineService;
        _assetsContainer = assetsContainer;
    }

    public void SpawnDamageNumber(int amount, bool isCritical, Vector3 worldPos, Camera relativeCamera) {
        _coroutineService.StartCoroutine(DamageNumberMovement(amount, isCritical, worldPos, relativeCamera));
    }

    private IEnumerator DamageNumberMovement(int amount, bool isCritical, Vector3 worldPos, Camera relativeCamera) {
        GameObject newNumber = Instantiate(_assetsContainer.DamageNumber);
        newNumber.transform.SetParent(transform);
        TextMeshProUGUI textGUI = newNumber.GetComponent<TextMeshProUGUI>();
        Color numberColor = isCritical ? _criticalDamageNumbersColor : _defaultDamageNumbersColor;

        Vector3 smoothUpMovementSum = Vector3.zero;

        textGUI.text = amount.ToString();

        float t = 0f;

        while (t <= 1f) {
            t += Time.deltaTime / _damageNumberDuration;

            newNumber.transform.position = relativeCamera.WorldToScreenPoint(worldPos) + smoothUpMovementSum;
            smoothUpMovementSum += Vector3.up * (_damageNumberUpMovementSpeed / Screen.height) * Mathf.Clamp01(2f - _damageNumberScaleCurve.Evaluate(t));
            newNumber.transform.localScale = _assetsContainer.DamageNumber.transform.localScale * _damageNumberScaleCurve.Evaluate(t);
            textGUI.color = Color.Lerp(new Color(numberColor.r, numberColor.g, numberColor.b, 0f), numberColor, _damageNumberAlphaCurve.Evaluate(t));

            yield return null;
        }

        Destroy(newNumber);
    }
}