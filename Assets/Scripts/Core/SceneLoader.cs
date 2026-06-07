using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景切换管理器
/// </summary>
public class SceneLoader : MonoBehaviour
{
    private static SceneLoader _instance;
    public static SceneLoader Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("[SceneLoader]");
                _instance = go.AddComponent<SceneLoader>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private bool isTransitioning;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadScene(string sceneName, Action onLoaded = null)
    {
        if (!isTransitioning)
            StartCoroutine(LoadSceneCoroutine(sceneName, onLoaded));
    }

    public void LoadScene(int buildIndex, Action onLoaded = null)
    {
        string sceneName = SceneUtility.GetScenePathByBuildIndex(buildIndex);
        sceneName = System.IO.Path.GetFileNameWithoutExtension(sceneName);
        LoadScene(sceneName, onLoaded);
    }

    private IEnumerator LoadSceneCoroutine(string sceneName, Action onLoaded)
    {
        isTransitioning = true;
        var asyncOp = SceneManager.LoadSceneAsync(sceneName);
        while (asyncOp != null && !asyncOp.isDone)
            yield return null;
        onLoaded?.Invoke();
        isTransitioning = false;
    }
}
