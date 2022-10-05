using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerUnitsGroupContainer : MonoBehaviour
{
    [field: SerializeField] public UnitBase MainCharacter { get; private set; }

    [SerializeField] private List<UnitBase> _unitsInGroup;

    private InputService _inputService;
    private AStarGrid _grid;
    private OnFieldRaycaster _fieldRaycaster;

    public void Init(
        OnFieldRaycaster fieldRaycaster, AStarGrid grid, SceneAbstractEntitiesMediator abstractEntitiesMediator,
        ConfigsContainer configsContainer, AssetsContainer assetsContainer, CameraSimpleFollower cameraFollower,
        ICoroutineService coroutineService, InputService inputService)
    {
        _inputService = inputService;
        _grid = grid;
        _fieldRaycaster = fieldRaycaster;

        foreach (UnitBase character in _unitsInGroup)
        {
            character.Init(
                fieldRaycaster, abstractEntitiesMediator, configsContainer,
                assetsContainer, cameraFollower, coroutineService);
        }
    }

    public void Tick()
    {
        if (Input.GetMouseButtonDown(1) && !_inputService.IsPointerOverUIObject())
        {
            AbortUnitsMovement();
        }
    }

    private void AbortUnitsMovement()
    {
        _fieldRaycaster.ClearWalkPoints();
        StopUnits();
    }

    public void StopUnits()
    {
        foreach (UnitBase unit in _unitsInGroup)
        {
            unit.AbortMovement();
        }
    }

    public void SendCharactersToPoint(Vector3 worldPoint, Vector3 endPointNormal, Action<Vector3, Vector3, Transform, bool> successfulCallback)
    {
        MainCharacter.GoToPoint(worldPoint, false, false,
            (callbackData) =>
            {
                if (callbackData.IsSuccessful)
                {
                    float circleRange = 4f;
                    float maxPathLength = 8f;

                    successfulCallback?.Invoke(worldPoint, endPointNormal, MainCharacter.transform, true);

                    List<Node> uniqueNodes = _grid.GetUniqueNodesInRange(circleRange, worldPoint - callbackData.EndForwardDirection * 1.5f, worldPoint, _unitsInGroup.Count - 1, maxPathLength);

                    foreach (UnitBase unit in _unitsInGroup)
                    {
                        if (unit != MainCharacter)
                        {
                            unit.GoToPoint(uniqueNodes[0].WorldPosition + Random.insideUnitSphere * .1f, false, false, null);
                            successfulCallback?.Invoke(uniqueNodes[0].WorldPosition, endPointNormal, unit.transform, false);
                            uniqueNodes.RemoveAt(0);
                        }
                    }
                }
            });
    }

    public List<UnitBase> GetUnits()
    {
        return _unitsInGroup;
    }
}