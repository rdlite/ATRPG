using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DecalMovementPointer : MonoBehaviour {
    [SerializeField] private DecalProjector _decal;
    [SerializeField] private AnimationCurve _appearCurve;

    private const string DECAL_CIRCLE_RADIUS = "_Radius";
    private Transform _destroyer;
    private Material _decalMaterial;
    private float _appearTimer;
    private bool _isDestroying = false;

    public void Init(Vector3 position, Vector3 normal, Transform destroyer) {
        _destroyer = destroyer;

        _decalMaterial = Instantiate(_decal.material);
        _decal.material = _decalMaterial;

        transform.position = position;
        transform.up = Quaternion.Euler(90f, 0f, 0f) * normal;

        _decal.material.SetFloat(DECAL_CIRCLE_RADIUS, 0f);
    }

    private void Update() {
        if (_isDestroying) {
            if (_appearTimer >= 0f) {
                _appearTimer -= Time.unscaledDeltaTime * 5f;

                _decal.material.SetFloat(DECAL_CIRCLE_RADIUS, _appearCurve.Evaluate(_appearTimer));
            } else {
                Destroy(gameObject);
            }
        } else {
            if (_appearTimer <= 1f) {
                _appearTimer += Time.unscaledDeltaTime * 5f;

                _decal.material.SetFloat(DECAL_CIRCLE_RADIUS, _appearCurve.Evaluate(_appearTimer));
            }
        }

        if (!_isDestroying && Vector3.Distance(transform.position.RemoveYCoord(), _destroyer.transform.position.RemoveYCoord()) < .5f) {
            DestroyDecal();
        }
    }

    public void DestroyDecal() {
        _isDestroying = true;
        _appearTimer = 1f;
    }
}