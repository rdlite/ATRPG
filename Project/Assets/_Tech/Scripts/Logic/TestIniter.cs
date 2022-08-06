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

    private int currentFPS = 120;

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Space)) {
            currentFPS += 10;
            if (currentFPS > 120) {
                currentFPS = 10;
            }
            Application.targetFrameRate = currentFPS; 
            print(currentFPS);
        }
    }
}