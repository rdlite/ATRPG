using UnityEngine;

public class BattleRaycaster {
    private Camera _camera;
    private LayerMask _charactersLayerMask;
    private LayerMask _groundLayerMask;

    public BattleRaycaster(LayerMask charactersLayerMask, CameraSimpleFollower cameraFollower, LayerMask groundLayerMask) {
        _camera = cameraFollower.GetComponent<Camera>();
        _charactersLayerMask = charactersLayerMask;
        _groundLayerMask = groundLayerMask;
    }

    public Vector3 GetRaycastPoint() {
        RaycastHit hitInfo;
        Physics.Raycast(_camera.ScreenPointToRay(Input.mousePosition), out hitInfo, Mathf.Infinity, _groundLayerMask);
        if (hitInfo.transform != null) {
            return hitInfo.point;
        }
        return Vector3.zero;
    }

    public CharacterWalker GetCurrentMouseOverSelectionUnit() {
        RaycastHit hitInfo;
        Physics.Raycast(_camera.ScreenPointToRay(Input.mousePosition), out hitInfo, Mathf.Infinity, _charactersLayerMask);
        CharacterWalker character = null;
        if (hitInfo.transform != null) {
            character = hitInfo.transform.GetComponent<CharacterWalker>();
        }
        return character;
    }
}