using UnityEngine;

public class OnFieldRaycaster : MonoBehaviour {
    [SerializeField] private LayerMask _groundCheck, _raycastRestrictionMask;
    [SerializeField] private float _minDeltaForTouch;

    private Camera _camera;
    private AStarGrid _globalGrid;
    private Vector2 _start1ButtonPresPosition;

    public void Init(Camera camera, AStarGrid globalGrid) {
        _globalGrid = globalGrid;
        _camera = camera;
    }

    private void Update() {
        if (Input.GetMouseButtonDown(1)) {
            _start1ButtonPresPosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(1)) {
            if (Vector2.Distance(_start1ButtonPresPosition, Input.mousePosition) < _minDeltaForTouch) {
                TrySendCharacterToPoint(Input.mousePosition);
            }
        }
    }

    private void TrySendCharacterToPoint(Vector3 screenPosition) {
        Vector3 groundcastPoint = IsGroundcast(screenPosition);

        if (groundcastPoint != Vector3.zero) {
            Vector3 worldPoint = groundcastPoint;
            FindObjectOfType<TestCharacterWalker>().GoToPoint(worldPoint);
        }
    }

    private Vector3 IsGroundcast(Vector3 screenPosition) {
        RaycastHit nonWalkableHitInfo;
        RaycastHit groundHitInfo;
        Physics.Raycast(_camera.ScreenPointToRay(screenPosition), out nonWalkableHitInfo, Mathf.Infinity, _raycastRestrictionMask);
        Physics.Raycast(_camera.ScreenPointToRay(screenPosition), out groundHitInfo, Mathf.Infinity, _groundCheck);

        if (nonWalkableHitInfo.transform == null ||
            Vector3.Distance(groundHitInfo.point, _camera.transform.position) < Vector3.Distance(nonWalkableHitInfo.point, _camera.transform.position)) {
            return groundHitInfo.point;
        }

        return Vector3.zero;
    }
}