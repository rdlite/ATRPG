using System.Collections.Generic;
using UnityEngine;

public class OnFieldRaycaster : MonoBehaviour {
    [SerializeField] private LayerMask _groundCheck, _raycastRestrictionMask;
    [SerializeField] private float _minDeltaForTouch;
    [SerializeField] private DecalMovementPointer _walkPointerDecalPrefab;

    private List<DecalMovementPointer> _createdDecals;
    private Camera _camera;
    private AStarGrid _globalGrid;
    private Vector2 _start1ButtonPresPosition;

    public void Init(Camera camera, AStarGrid globalGrid) {
        _globalGrid = globalGrid;
        _camera = camera;

        _createdDecals = new List<DecalMovementPointer>();
    }

    private void Update() {
        if (Input.GetMouseButtonDown(0)) {
            _start1ButtonPresPosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0)) {
            if (Vector2.Distance(_start1ButtonPresPosition, Input.mousePosition) < _minDeltaForTouch) {
                TrySendCharacterToPoint(Input.mousePosition);
            }
        }
    }

    private void TrySendCharacterToPoint(Vector3 screenPosition) {
        RaycastHit groundcastInfo = IsGroundcast(screenPosition);

        if (groundcastInfo.transform != null) {
            Vector3 worldPoint = groundcastInfo.point;
            Vector3 surfaceNormal = groundcastInfo.normal;
            FindObjectOfType<TestCharacterWalker>().GoToPoint(worldPoint, (successful) => 
                OnPathFound(worldPoint, surfaceNormal, FindObjectOfType<TestCharacterWalker>().transform, successful));
        }
    }

    private void OnPathFound(Vector3 worldPoint, Vector3 normal, Transform founder, bool successful) {
        if (successful) {
            DestroyAllPointers();
            CreateMovementPointer(worldPoint, normal, founder);
        }
    }

    private void DestroyAllPointers() {
        for (int i = 0; i < _createdDecals.Count; i++) {
            if (_createdDecals[i] != null) {
                _createdDecals[i].DestroyDecal();
            }
        }

        _createdDecals.Clear();
    }

    private void CreateMovementPointer(Vector3 position, Vector3 normal, Transform pointerDestroyer) {
        DecalMovementPointer newDecalPointer = Instantiate(_walkPointerDecalPrefab);
        newDecalPointer.Init(position, normal, pointerDestroyer);
        _createdDecals.Add(newDecalPointer);
    }

    private RaycastHit IsGroundcast(Vector3 screenPosition) {
        RaycastHit nonWalkableHitInfo;
        RaycastHit groundHitInfo;
        Physics.Raycast(_camera.ScreenPointToRay(screenPosition), out nonWalkableHitInfo, Mathf.Infinity, _raycastRestrictionMask);
        Physics.Raycast(_camera.ScreenPointToRay(screenPosition), out groundHitInfo, Mathf.Infinity, _groundCheck);

        if (nonWalkableHitInfo.transform == null ||
            Vector3.Distance(groundHitInfo.point, _camera.transform.position) < Vector3.Distance(nonWalkableHitInfo.point, _camera.transform.position)) {
            return groundHitInfo;
        }

        return new RaycastHit();
    }
}