using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISlider : MonoBehaviour {
    [SerializeField] private bool _isSetNumbers, _isDividedNumbers = true;
    [SerializeField] private TextMeshProUGUI _textValues;
    [SerializeField] private RectTransform _sliderRectMask, _underSliderRectMask;
    [SerializeField] private Image _underSliderDamage;
    [SerializeField] private Gradient _underSliderBlinkGradient;

    private float _maxValue;
    private float _currentValue;
    private float _maxWidth;

    public void Init() {
        _maxWidth = _sliderRectMask.rect.width;

        _sliderRectMask.sizeDelta = new Vector2(0f, _sliderRectMask.sizeDelta.y);
        _underSliderRectMask.sizeDelta = new Vector2(0f, _underSliderRectMask.sizeDelta.y);

        _textValues.gameObject.SetActive(_isSetNumbers);
    }

    private void OnEnable() {
        StartCoroutine(UnderSliderBlinking());
    }

    public void UpdateValue(float currentValue, float maxValue, float damageAmount = 0f) {
        _currentValue = currentValue;
        _maxValue = maxValue;

        SetRectMaskValue(_sliderRectMask, Mathf.InverseLerp(0f, maxValue, currentValue - damageAmount));

        if (damageAmount == 0f) {
            SetRectMaskValue(_underSliderRectMask, 0f);
        } else {
            SetRectMaskValue(_underSliderRectMask, Mathf.InverseLerp(0f, maxValue, currentValue));
        }

        if (_isSetNumbers) {
            if (_isDividedNumbers) {
                _textValues.text = $"{Mathf.Round(_currentValue - damageAmount)}/{Mathf.Round(_maxValue)}";
            } else {
                _textValues.text = $"{Mathf.Round(_currentValue - damageAmount)}";
            }
        }
    }

    private void SetRectMaskValue(RectTransform sliderRect, float value) {
        sliderRect.sizeDelta = new Vector2(-_maxWidth * (1f - value), sliderRect.sizeDelta.y);
    }

    private IEnumerator UnderSliderBlinking() {
        while (gameObject != null) {
            float t = 0f;

            while (t <= 1f) {
                t += Time.deltaTime * 2f;

                _underSliderDamage.color = _underSliderBlinkGradient.Evaluate(t);

                yield return null;
            }
        }
    }
}