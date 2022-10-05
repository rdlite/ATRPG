using UnityEngine;

[DefaultExecutionOrder(-100)]
public class SceneReloader : MonoBehaviour
{
    private void Awake()
    {
        if (FindObjectOfType<GamePreloader>() == null)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
    }
}