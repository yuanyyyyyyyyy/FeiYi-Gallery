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

        SetFloat(zaType, za, "m_HScaleMin", 0.05f);
        SetFloat(zaType, za, "m_VScaleMin", 0.05f);
        SetBool(zaType, za, "m_ScaleWithWindow", true);

        var scale = (Vector2)zaType.GetField("m_Scale", flags).GetValue(za);
        var drawArea = (Rect)zaType.GetField("m_DrawArea", flags).GetValue(za);
        var targetSize = (Vector2)gvType.GetProperty("targetRenderSize", flags).GetValue(gv, null);
        if (targetSize.x > 0 && targetSize.y > 0 && drawArea.width > 0 && drawArea.height > 0)
        {
            float fitW = drawArea.width / targetSize.x;
            float fitH = drawArea.height / targetSize.y;
            // 在严格 fit 基础上放大 5%，但不超过高度填满值，避免裁切 header/footer
            float target = Mathf.Min(fitW * 1.05f, fitH);
            if (Mathf.Abs(scale.x - target) > 0.02f)
            {
                zaType.GetField("m_Scale", flags).SetValue(za, new Vector2(target, target));
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
