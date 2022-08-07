using UnityEngine;

public class BattleRaycaster {
    private Camera _camera;
    private LayerMask _charactersLayerMask;

    public BattleRaycaster(LayerMask charactersLayerMask, CameraSimpleFollower cameraFollower) {
        _camera = cameraFollower.GetComponent<Camera>();
        _charactersLayerMask = charactersLayerMask;
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