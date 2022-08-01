using UnityEngine;

public class CameraSimpleFollower : MonoBehaviour {
    [SerializeField] private Vector3 _offset;
    [SerializeField] private float _movementLerpSpeed = 10f;

    private Transform _target;

    public void Init(Transform target) {
        _target = target;
    }

    private void LateUpdate() {
        transform.position = Vector3.Lerp(transform.position, _target.position + _offset, _movementLerpSpeed * Time.deltaTime);
    }
}
