using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 自动过滤 Console 中 &lt;Raw Input&gt; 噪音日志
/// Unity 在 Windows 上的已知 bug，不影响构建
/// 通过设置 Console 搜索栏排除词实现，不会清除其他日志
/// </summary>
[InitializeOnLoad]
public static class FilterRawInputNoise
{
    private const string ExcludeText = "-Raw Input";
    private const string PrefKey = "FilterRawInputNoise_Enabled";
    private const string MenuPath = "Tools/Raw Input 噪音过滤";
    private static double _lastApply;

    static FilterRawInputNoise()
    {
        EditorApplication.update += OnUpdate;
        Application.logMessageReceived += OnLogReceived;
    }

    private static bool Enabled => EditorPrefs.GetBool(PrefKey, true);

    [MenuItem(MenuPath)]
    private static void ToggleMenu()
    {
        EditorPrefs.SetBool(PrefKey, !Enabled);
        if (!Enabled)
            ClearFilter();
    }

    [MenuItem(MenuPath, true)]
    private static bool ToggleMenuValidate()
    {
        Menu.SetChecked(MenuPath, Enabled);
        return true;
    }

    private static void OnLogReceived(string condition, string stackTrace, LogType type)
    {
        if (!Enabled || type != LogType.Error) return;
        if (condition.Contains("Raw Input"))
            EditorApplication.delayCall += ApplyFilter;
    }

    private static void OnUpdate()
    {
        if (!Enabled) return;
        if (EditorApplication.timeSinceStartup - _lastApply < 5.0) return;
        _lastApply = EditorApplication.timeSinceStartup;
        ApplyFilter();
    }

    private static void ApplyFilter()
    {
        var console = GetConsoleWindow();
        if (console == null) return;
        var searchText = GetSearchText(console);
        // 仅在搜索栏为空时设置排除词，不覆盖用户自定义搜索
        if (string.IsNullOrEmpty(searchText))
            SetFilter(console, ExcludeText);
    }

    private static void ClearFilter()
    {
        var console = GetConsoleWindow();
        if (console == null) return;
        if (GetSearchText(console) == ExcludeText)
            SetFilter(console, "");
    }

    private static UnityEngine.Object GetConsoleWindow()
    {
        var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.ConsoleWindow");
        if (type == null) return null;
        var windows = Resources.FindObjectsOfTypeAll(type);
        return windows.Length > 0 ? windows[0] : null;
    }

    private static string GetSearchText(UnityEngine.Object console)
    {
        var type = console.GetType();
        var field = type.GetField("m_SearchText", BindingFlags.NonPublic | BindingFlags.Instance);
        return field?.GetValue(console) as string;
    }

    private static void SetFilter(UnityEngine.Object console, string text)
    {
        var type = console.GetType();
        var method = type.GetMethod("SetFilter", BindingFlags.NonPublic | BindingFlags.Instance);
        if (method == null) return;
        method.Invoke(console, new object[] { text });
        (console as EditorWindow)?.Repaint();
    }
}
