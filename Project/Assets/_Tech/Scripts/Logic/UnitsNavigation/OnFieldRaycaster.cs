using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class OnFieldRaycaster : MonoBehaviour
{
    [SerializeField] private LayerMask _groundCheck, _raycastRestrictionMask;
    [SerializeField] private float _minDeltaForTouch;
    [SerializeField] private DecalMovementPointer _walkPointerDecalPrefab;

    private List<DecalMovementPointer> _createdDecals;
    private Camera _camera;
    private InputService _inputService;
    private PlayerUnitsGroupContainer _charactersGroupContainer;
    private AStarGrid _globalGrid;
    private Vector2 _start1ButtonPresPosition;

    public void Init(
        Camera camera, AStarGrid globalGrid, PlayerUnitsGroupContainer charactersGroupContainer,
        InputService inputService)
    {
        _inputService = inputService;
        _charactersGroupContainer = charactersGroupContainer;
        _globalGrid = globalGrid;
        _camera = camera;

        _createdDecals = new List<DecalMovementPointer>();
    }

    public void Tick()
    {
        if (Input.GetMouseButtonDown(0) && !_inputService.IsPointerOverUIObject())
        {
            _start1ButtonPresPosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0) && !_inputService.IsPointerOverUIObject())
        {
            if (Vector2.Distance(_start1ButtonPresPosition, Input.mousePosition) < _minDeltaForTouch)
            {
                TrySendCharacterToPoint(Input.mousePosition);
            }
        }
    }

    private void TrySendCharacterToPoint(Vector3 screenPosition)
    {
        RaycastHit groundcastInfo = IsGroundcast(screenPosition);

        if (groundcastInfo.transform != null && _globalGrid.IsSurfaceNodeWalkableBySlope(groundcastInfo.normal))
        {
            Vector3 worldPoint = groundcastInfo.point;
            Vector3 surfaceNormal = groundcastInfo.normal;

            _charactersGroupContainer.SendCharactersToPoint(worldPoint, surfaceNormal, OnPathFound);
        }
    }

    private void OnPathFound(Vector3 worldPoint, Vector3 normal, Transform markPointRequester, bool isErasePreviousPoints)
    {
        if (isErasePreviousPoints)
        {
            DestroyAllPointers();
        }

        CreateMovementPointer(worldPoint, normal, markPointRequester);
    }

    public void ClearWalkPoints()
    {
        DestroyAllPointers();
    }

    private void DestroyAllPointers()
    {
        for (int i = 0; i < _createdDecals.Count; i++)
        {
            if (_createdDecals[i] != null)
            {
                _createdDecals[i].DestroyDecal();
            }
        }

        _createdDecals.Clear();
    }

    private void CreateMovementPointer(Vector3 position, Vector3 normal, Transform pointerDestroyer)
    {
        DecalMovementPointer newDecalPointer = Instantiate(_walkPointerDecalPrefab);
        newDecalPointer.Init(position, normal, pointerDestroyer);
        _createdDecals.Add(newDecalPointer);
    }

    private RaycastHit IsGroundcast(Vector3 screenPosition)
    {
        RaycastHit nonWalkableHitInfo;
        RaycastHit groundHitInfo;
        Physics.Raycast(_camera.ScreenPointToRay(screenPosition), out nonWalkableHitInfo, Mathf.Infinity, _raycastRestrictionMask);
        Physics.Raycast(_camera.ScreenPointToRay(screenPosition), out groundHitInfo, Mathf.Infinity, _groundCheck);

        if (nonWalkableHitInfo.transform == null ||
            Vector3.Distance(groundHitInfo.point, _camera.transform.position) < Vector3.Distance(nonWalkableHitInfo.point, _camera.transform.position))
        {
            return groundHitInfo;
        }

        return new RaycastHit();
    }
}