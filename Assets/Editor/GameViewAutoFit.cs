using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 自动修正 Game View 缩放：强制 ScaleWithWindow + 降低最小缩放阈值
/// 解决 Game View 放大时顶部/底部被裁切的问题
/// </summary>
[InitializeOnLoad]
public static class GameViewAutoFit
{
    static GameViewAutoFit()
    {
        EditorApplication.update += Apply;
        EditorApplication.playModeStateChanged += _ => Apply();
    }

    private static int _cooldown;

    private static void Apply()
    {
        if (_cooldown-- > 0) return;
        _cooldown = 120;

        var gvType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        if (gvType == null) return;
        var gv = EditorWindow.GetWindow(gvType, false, null, false);
        if (gv == null) return;

        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var zaField = gvType.GetField("m_ZoomArea", flags);
        if (zaField == null) return;
        var za = zaField.GetValue(gv);
        if (za == null) return;
        var zaType = za.GetType();

        // 允许缩放范围 0.3x ~ 1.2x，用户可自由滑动
        SetFloat(zaType, za, "m_HScaleMin", 0.3f);
        SetFloat(zaType, za, "m_VScaleMin", 0.3f);
        SetFloat(zaType, za, "m_HScaleMax", 1.2f);
        SetFloat(zaType, za, "m_VScaleMax", 1.2f);
        SetBool(zaType, za, "m_ScaleWithWindow", true);

        // 仅当缩放超过 1.2x 时才拉回（防止旧的高缩放状态卡住）
        var scale = (Vector2)zaType.GetField("m_Scale", flags).GetValue(za);
        if (scale.x > 1.2f)
        {
            var drawArea = (Rect)zaType.GetField("m_DrawArea", flags).GetValue(za);
            var targetSize = (Vector2)gvType.GetProperty("targetRenderSize", flags).GetValue(gv, null);
            if (targetSize.x > 0 && targetSize.y > 0 && drawArea.width > 0 && drawArea.height > 0)
            {
                float fit = Mathf.Min(drawArea.width / targetSize.x, drawArea.height / targetSize.y);
                zaType.GetField("m_Scale", flags).SetValue(za, new Vector2(fit, fit));
            }
        }
    }

    private static void SetFloat(System.Type t, object obj, string name, float val)
    {
        var f = t.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        if (f != null) f.SetValue(obj, val);
    }

    private static void SetBool(System.Type t, object obj, string name, bool val)
    {
        var f = t.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        if (f != null) f.SetValue(obj, val);
    }
}
