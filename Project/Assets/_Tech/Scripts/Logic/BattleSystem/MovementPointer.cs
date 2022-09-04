using UnityEngine;

public class MovementPointer : MonoBehaviour {
    [SerializeField] private Transform[] _corners;
    [SerializeField] private Transform[] _startCornersPoses, _endCornersPoses;
    [SerializeField] private AnimationCurve _movementCurve;
    [SerializeField] private float _animtaionDuration = 1f;

    private float t;

    private void Update() {
        t += Time.deltaTime / _animtaionDuration;

        for (int i = 0; i < _corners.Length; i++) {
            _corners[i].transform.localPosition = Vector3.Lerp(_startCornersPoses[i].localPosition, _endCornersPoses[i].localPosition, _movementCurve.Evaluate(t));
        }

        if (t >= 1f) {
            t = 0f;
        }
    }
}