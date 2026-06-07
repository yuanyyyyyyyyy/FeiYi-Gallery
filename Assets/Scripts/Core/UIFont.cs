using UnityEngine;

/// <summary>
/// 全局 UI 字体工具类
/// </summary>
public static class UIFont
{
    private static Font _cached;
    private static bool _init;

    public static Font Get()
    {
        if (_init && _cached != null) return _cached;
        _init = true;
        _cached = GetBuiltinFontSafe("LegacyRuntime.ttf");
        return _cached != null ? _cached : (new Font());
    }

    private static Font GetBuiltinFontSafe(string name)
    {
        try { return Resources.GetBuiltinResource<Font>(name); }
        catch (System.Exception e) { Debug.LogWarning($"UIFont: failed to load {name}: {e.Message}"); return null; }
    }
}