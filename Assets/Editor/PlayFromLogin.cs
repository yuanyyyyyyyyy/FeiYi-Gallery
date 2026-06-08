using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 点击 Play 时自动打开 LoginScene（Build Index 0），确保总是从登录页开始运行
/// </summary>
[InitializeOnLoad]
public static class PlayFromLogin
{
    static PlayFromLogin()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            // 打开 Build Index 0 的场景（LoginScene）
            var scenes = EditorBuildSettings.scenes;
            if (scenes != null && scenes.Length > 0 && scenes[0].enabled)
            {
                var path = scenes[0].path;
                if (!string.IsNullOrEmpty(path))
                {
                    EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                    EditorSceneManager.OpenScene(path);
                }
            }
        }
    }
}
