using UnityEngine;

public class BattleRaycaster
{
    private Camera _camera;
    private LayerMask _unitsLayerMask;
    private LayerMask _groundLayerMask;

    public BattleRaycaster(LayerMask unitsLayerMask, CameraSimpleFollower cameraFollower, LayerMask groundLayerMask)
    {
        _camera = cameraFollower.GetComponent<Camera>();
        _unitsLayerMask = unitsLayerMask;
        _groundLayerMask = groundLayerMask;
    }

    public Vector3 GetRaycastPoint()
    {
        RaycastHit hitInfo;
        Physics.Raycast(_camera.ScreenPointToRay(Input.mousePosition), out hitInfo, Mathf.Infinity, _groundLayerMask);
        if (hitInfo.transform != null)
        {
            return hitInfo.point;
        }
        return Vector3.zero;
    }

    public UnitBase GetCurrentMouseOverSelectionUnit()
    {
        RaycastHit hitInfo;
        Physics.Raycast(_camera.ScreenPointToRay(Input.mousePosition), out hitInfo, Mathf.Infinity, _unitsLayerMask);
        UnitBase unit = null;
        if (hitInfo.transform != null)
        {
            unit = hitInfo.transform.GetComponent<UnitBase>();
        }
        return unit;
    }
}