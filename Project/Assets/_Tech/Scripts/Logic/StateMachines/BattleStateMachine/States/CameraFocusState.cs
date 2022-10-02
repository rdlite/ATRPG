using UnityEngine;

public class CameraFocusState : IPayloadState<(Transform, System.Action)> {
    private CameraSimpleFollower _cameraFollower;

    public CameraFocusState(CameraSimpleFollower cameraFollower) {
        _cameraFollower = cameraFollower;
    }

    public void Enter((Transform, System.Action) data) {
        _cameraFollower.SetTarget(data.Item1);
        data.Item2?.Invoke();
    }

    public void Exit() { }
}