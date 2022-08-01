using UnityEngine;

[DefaultExecutionOrder(-10)]
public class TestIniter : MonoBehaviour {
    private void Awake() {
        AStarGrid globalGrid = FindObjectOfType<AStarGrid>();
        OnFieldRaycaster fieldRaycaster = FindObjectOfType<OnFieldRaycaster>();
        CameraSimpleFollower cameraFollower = FindObjectOfType<CameraSimpleFollower>();
        Camera mainCamera = Camera.main;
        TestCharacterWalker testCharacterWalker = FindObjectOfType<TestCharacterWalker>();

        testCharacterWalker.Init();
        cameraFollower.Init(testCharacterWalker.transform);
        fieldRaycaster.Init(mainCamera, globalGrid);
    }
}