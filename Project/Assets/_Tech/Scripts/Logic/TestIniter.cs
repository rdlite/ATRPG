using UnityEngine;

[DefaultExecutionOrder(-10)]
public class TestIniter : MonoBehaviour {
    private void Awake() {
        Application.targetFrameRate = 120;
        QualitySettings.SetQualityLevel(3);

        AStarGrid globalGrid = FindObjectOfType<AStarGrid>();
        OnFieldRaycaster fieldRaycaster = FindObjectOfType<OnFieldRaycaster>();
        CameraSimpleFollower cameraFollower = FindObjectOfType<CameraSimpleFollower>();
        Camera mainCamera = Camera.main;
        CharactersGroupContainer charactersGroupContainer = FindObjectOfType<CharactersGroupContainer>();

        charactersGroupContainer.Init(fieldRaycaster, globalGrid);
        cameraFollower.Init(charactersGroupContainer.MainCharacter.transform, globalGrid.LDPoint, globalGrid.RUPoint);
        fieldRaycaster.Init(mainCamera, globalGrid, charactersGroupContainer);
    }
}