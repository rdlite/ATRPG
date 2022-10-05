using System;
using System.Collections;
using UnityEngine;

public class LevelsLoadingService : IService
{
    private ICoroutineService _coroutineSystem;

    public LevelsLoadingService(ICoroutineService coroutineSystem)
    {
        _coroutineSystem = coroutineSystem;
    }

    public void LoadSceneAsync(string sceneName, Action callback)
    {
        _coroutineSystem.StartCoroutine(LoadLevelAsync(sceneName, callback));
    }

    public int GetNextLevelID()
    {
        int nextLevelID = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;

        nextLevelID++;

        if (nextLevelID >= UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings)
        {
            nextLevelID = 1;
        }

        return nextLevelID;
    }

    public void LoadPreloader()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Preloader");
    }

    private IEnumerator LoadLevelAsync(string sceneName, Action callback)
    {
        AsyncOperation asyncOperation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
        asyncOperation.completed += (asyncOp) => callback.Invoke();

        while (asyncOperation.progress <= .99f)
        {
            yield return null;
        }
    }
}