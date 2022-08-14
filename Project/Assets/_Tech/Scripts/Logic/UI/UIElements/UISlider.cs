using TMPro;
using UnityEngine;

public class UISlider : MonoBehaviour {
    [SerializeField] private bool _isSetNumbers, _isDividedNumbers = true;
    [SerializeField] private TextMeshProUGUI _textValues;
    [SerializeField] private RectTransform _sliderRectMask;

    private float _maxValue;
    private float _currentValue;
    private float _maxWidth;

    public void Init() {
        _maxWidth = _sliderRectMask.rect.width;

        _sliderRectMask.sizeDelta = new Vector2(0f, _sliderRectMask.sizeDelta.y);

        _textValues.gameObject.SetActive(_isSetNumbers);
    }

    public void UpdateValue(float currentValue, float maxValue) {
        _currentValue = currentValue;
        _maxValue = maxValue;

        _sliderRectMask.sizeDelta = new Vector2(-_maxWidth * (1f - Mathf.InverseLerp(0f, _maxValue, _currentValue)), _sliderRectMask.sizeDelta.y);

        if (_isSetNumbers) {
            if (_isDividedNumbers) {
                _textValues.text = $"{Mathf.Round(_currentValue)}/{Mathf.Round(_maxValue)}";
            } else {
                _textValues.text = $"{Mathf.Round(_currentValue)}";
            }
        }
    }
}