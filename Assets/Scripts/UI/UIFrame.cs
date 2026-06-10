using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// UI 共享基类 — 统一 Canvas 架构、颜色常量、UI 辅助方法、动画工具
/// 所有场景 Manager 继承此类即可获得完整的 UI 创建能力
/// </summary>
public abstract class UIFrame : MonoBehaviour
{
    // ──────────────────── 新中式色彩系统（支持主题切换）────────────────────

    private static string _activeTheme;

    private static void EnsureTheme()
    {
        var t = GameManager.Instance != null ? GameManager.Instance.themeStyle : "default";
        if (_activeTheme != t) _activeTheme = t;
    }

    // 主题色板
    private static readonly Color T_Default_ZhuRed    = new Color(0.76f, 0.21f, 0.19f);
    private static readonly Color T_Default_GoldColor = new Color(0.83f, 0.65f, 0.27f);
    private static readonly Color T_Default_InkBlack  = new Color(0.17f, 0.17f, 0.17f);
    private static readonly Color T_Default_XuanPaper = new Color(0.96f, 0.90f, 0.78f);
    private static readonly Color T_Default_JadeGreen= new Color(0.18f, 0.48f, 0.43f);
    private static readonly Color T_Default_DarkBar   = new Color(0.10f, 0.10f, 0.10f, 0.95f);

    private static readonly Color T_Classic_ZhuRed    = new Color(0.62f, 0.14f, 0.12f);  // 暗朱红
    private static readonly Color T_Classic_GoldColor = new Color(0.90f, 0.72f, 0.30f);  // 明金
    private static readonly Color T_Classic_InkBlack  = new Color(0.88f, 0.82f, 0.68f);  // 浅金文字（深底用）
    private static readonly Color T_Classic_XuanPaper = new Color(0.22f, 0.17f, 0.12f);  // 深褐底
    private static readonly Color T_Classic_JadeGreen= new Color(0.28f, 0.58f, 0.52f);  // 翡翠亮
    private static readonly Color T_Classic_DarkBar   = new Color(0.14f, 0.10f, 0.07f, 0.97f);

    private static readonly Color T_Minimal_ZhuRed    = new Color(0.72f, 0.30f, 0.28f);  // 柔红
    private static readonly Color T_Minimal_GoldColor = new Color(0.50f, 0.50f, 0.50f);  // 中灰
    private static readonly Color T_Minimal_InkBlack  = new Color(0.25f, 0.25f, 0.28f);  // 深灰
    private static readonly Color T_Minimal_XuanPaper = new Color(0.97f, 0.97f, 0.97f);  // 浅灰白
    private static readonly Color T_Minimal_JadeGreen= new Color(0.30f, 0.56f, 0.50f);
    private static readonly Color T_Minimal_DarkBar   = new Color(0.92f, 0.92f, 0.92f, 0.98f);

    protected static Color ZhuRed    { get { EnsureTheme(); return _activeTheme == "classic" ? T_Classic_ZhuRed    : _activeTheme == "minimal" ? T_Minimal_ZhuRed    : T_Default_ZhuRed; } }
    protected static Color GoldColor { get { EnsureTheme(); return _activeTheme == "classic" ? T_Classic_GoldColor : _activeTheme == "minimal" ? T_Minimal_GoldColor : T_Default_GoldColor; } }
    protected static Color InkBlack  { get { EnsureTheme(); return _activeTheme == "classic" ? T_Classic_InkBlack  : _activeTheme == "minimal" ? T_Minimal_InkBlack  : T_Default_InkBlack; } }
    protected static Color XuanPaper { get { EnsureTheme(); return _activeTheme == "classic" ? T_Classic_XuanPaper : _activeTheme == "minimal" ? T_Minimal_XuanPaper : T_Default_XuanPaper; } }
    protected static Color JadeGreen { get { EnsureTheme(); return _activeTheme == "classic" ? T_Classic_JadeGreen : _activeTheme == "minimal" ? T_Minimal_JadeGreen : T_Default_JadeGreen; } }
    protected static Color DarkBar   { get { EnsureTheme(); return _activeTheme == "classic" ? T_Classic_DarkBar   : _activeTheme == "minimal" ? T_Minimal_DarkBar   : T_Default_DarkBar; } }

    // ──────────────────── 运行时引用 ────────────────────
    protected Transform canvasT;   // Canvas transform
    protected Transform rootT;     // Root (全屏) panel transform

    // ──────────────────── Canvas 初始化 ────────────────────

    /// <summary>
    /// 创建标准 Canvas 架构：Canvas(根对象) → Root(全屏面板)
    /// 返回 Root 的 Transform，所有 UI 元素应放在 Root 下
    /// </summary>
    protected Transform InitCanvas()
    {
        // 禁用 [App] 上残留的 Canvas 组件（来自场景文件）
        DisableResidualCanvas();

        // Canvas 作为根对象 — ScreenSpaceOverlay 驱动自身 RectTransform
        var canvasObj = new GameObject("Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        canvasT = canvas.transform;

        // EventSystem
        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            var eventSys = es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
            // 降低拖拽阈值，让短距离拖拽更容易被识别
            eventSys.pixelDragThreshold = 5;
        }

        // 全屏 Root 面板 — 所有 UI 元素的父级
        var root = NewUI("Root", canvasT);
        Stretch(root);
        rootT = root.transform;

        return rootT;
    }

    /// <summary>
    /// 禁用 [App] 上残留的 Canvas 相关组件，避免干扰
    /// </summary>
    protected void DisableResidualCanvas()
    {
        var c = GetComponent<Canvas>(); if (c != null) c.enabled = false;
        var cs = GetComponent<CanvasScaler>(); if (cs != null) cs.enabled = false;
        var gr = GetComponent<GraphicRaycaster>(); if (gr != null) gr.enabled = false;
    }

    // ──────────────────── 单例保障 ────────────────────

    protected void EnsureSingletons()
    {
        if (GameManager.Instance == null) new GameObject("[GameManager]").AddComponent<GameManager>();
        if (SceneLoader.Instance == null) new GameObject("[SceneLoader]").AddComponent<SceneLoader>();
        if (AudioManager.Instance == null) new GameObject("[AudioManager]").AddComponent<AudioManager>();
    }

    // ──────────────────── UI 基础方法 ────────────────────

    protected static Font Font() => UIFont.Get();

    protected GameObject NewUI(string name, Transform parent)
    {
        var o = new GameObject(name);
        o.transform.SetParent(parent, false);
        o.AddComponent<RectTransform>();
        return o;
    }

    protected void Stretch(GameObject o)
    {
        var r = o.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.sizeDelta = Vector2.zero;
    }

    // ──────────────────── 布局：锚定到顶部 ────────────────────

    protected GameObject AnchorTop(string name, Transform parent, float height)
    {
        var o = NewUI(name, parent);
        var r = o.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(1, 1);
        r.pivot = new Vector2(0.5f, 1f);
        r.sizeDelta = new Vector2(0, height);
        r.anchoredPosition = Vector2.zero;
        return o;
    }

    // ──────────────────── 布局：锚定到底部 ────────────────────

    protected GameObject AnchorBottom(string name, Transform parent, float height)
    {
        var o = NewUI(name, parent);
        var r = o.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(1, 0);
        r.pivot = new Vector2(0.5f, 0f);
        r.sizeDelta = new Vector2(0, height);
        r.anchoredPosition = Vector2.zero;
        return o;
    }

    // ──────────────────── 文字标签 ────────────────────

    protected Text AddLabel(string name, Transform parent, Vector2 pos, Vector2 size, string text, int fontSize, Color color, TextAnchor alignment = TextAnchor.MiddleCenter)
    {
        var o = NewUI(name, parent);
        var r = o.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.sizeDelta = size;
        r.anchoredPosition = pos;
        var t = o.AddComponent<Text>();
        t.font = Font(); t.text = text; t.fontSize = fontSize; t.color = color; t.alignment = alignment;
        return t;
    }

    /// <summary>
    /// 居中定位的标签（相对于父级中心）
    /// </summary>
    protected Text AddLabelCenter(string name, Transform parent, Vector2 pos, Vector2 size, string text, int fontSize, Color color)
    {
        var o = NewUI(name, parent);
        var r = o.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.sizeDelta = size;
        r.anchoredPosition = pos;
        var t = o.AddComponent<Text>();
        t.font = Font(); t.text = text; t.fontSize = fontSize; t.color = color; t.alignment = TextAnchor.MiddleCenter;
        return t;
    }

    // ──────────────────── 按钮 ────────────────────

    protected Button AddBtn(string name, Transform parent, Vector2 pos, Vector2 size, string text, Color bg)
    {
        var o = NewUI(name, parent);
        var r = o.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.sizeDelta = size;
        r.anchoredPosition = pos;
        o.AddComponent<Image>().color = bg;
        var btn = o.AddComponent<Button>();
        var tObj = NewUI("T", o.transform);
        Stretch(tObj);
        var t = tObj.AddComponent<Text>();
        t.font = Font(); t.text = text; t.fontSize = 22; t.color = Color.white; t.alignment = TextAnchor.MiddleCenter;
        return btn;
    }

    /// <summary>
    /// 锚定到底部的按钮（用于 Bottom Bar 内）
    /// </summary>
    protected Button AddBtnAnchored(string name, Transform parent, Vector2 anchor, Vector2 size, Vector2 offset, string text, Color bg, int fontSize = 16)
    {
        var o = NewUI(name, parent);
        var r = o.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = anchor;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.sizeDelta = size;
        r.anchoredPosition = offset;
        o.AddComponent<Image>().color = bg;
        var btn = o.AddComponent<Button>();
        var tObj = NewUI("T", o.transform);
        Stretch(tObj);
        var t = tObj.AddComponent<Text>();
        t.font = Font(); t.text = text; t.fontSize = fontSize; t.color = Color.white; t.alignment = TextAnchor.MiddleCenter;
        return btn;
    }

    // ──────────────────── 印章 Logo ────────────────────

    /// <summary>
    /// 创建中式印章 Logo — 朱红方块 + 白色文字
    /// </summary>
    protected GameObject AddSealLogo(string name, Transform parent, Vector2 pos, float size, string text, int fontSize = 40)
    {
        var o = NewUI(name, parent);
        var r = o.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.sizeDelta = new Vector2(size, size);
        r.anchoredPosition = pos;
        var sealLogoImg = o.AddComponent<Image>(); sealLogoImg.color = ZhuRed; sealLogoImg.raycastTarget = false;
        var txt = NewUI("T", o.transform);
        Stretch(txt);
        var t = txt.AddComponent<Text>();
        t.font = Font(); t.text = text; t.fontSize = fontSize; t.color = Color.white; t.alignment = TextAnchor.MiddleCenter;
        return o;
    }

    // ──────────────────── 分隔线装饰 ────────────────────

    /// <summary>
    /// 创建中式分隔线 ──── 文字 ────
    /// </summary>
    protected GameObject AddDivider(string name, Transform parent, Vector2 pos, float width, string text, Color lineColor, Color textColor, int fontSize = 16)
    {
        var o = NewUI(name, parent);
        var r = o.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.sizeDelta = new Vector2(width, 30);
        r.anchoredPosition = pos;

        // 左线
        var leftLine = NewUI("LL", o.transform);
        var llr = leftLine.GetComponent<RectTransform>();
        llr.anchorMin = new Vector2(0, 0.3f); llr.anchorMax = new Vector2(0.35f, 0.7f);
        llr.offsetMin = llr.offsetMax = Vector2.zero; llr.sizeDelta = Vector2.zero;
        var llImg = leftLine.AddComponent<Image>(); llImg.color = lineColor; llImg.raycastTarget = false;

        // 中间文字
        var tObj = NewUI("T", o.transform);
        var tr = tObj.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0.35f, 0f); tr.anchorMax = new Vector2(0.65f, 1f);
        tr.offsetMin = tr.offsetMax = Vector2.zero; tr.sizeDelta = Vector2.zero;
        var t = tObj.AddComponent<Text>();
        t.font = Font(); t.text = text; t.fontSize = fontSize; t.color = textColor; t.alignment = TextAnchor.MiddleCenter;

        // 右线
        var rightLine = NewUI("RL", o.transform);
        var rlr = rightLine.GetComponent<RectTransform>();
        rlr.anchorMin = new Vector2(0.65f, 0.3f); rlr.anchorMax = new Vector2(1f, 0.7f);
        rlr.offsetMin = rlr.offsetMax = Vector2.zero; rlr.sizeDelta = Vector2.zero;
        var rlImg = rightLine.AddComponent<Image>(); rlImg.color = lineColor; rlImg.raycastTarget = false;

        return o;
    }

    // ──────────────────── 输入框 ────────────────────

    protected InputField AddInputField(string name, Transform parent, Vector2 pos, Vector2 size, string placeholder, bool password = false)
    {
        var o = NewUI(name, parent);
        var r = o.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.sizeDelta = size;
        r.anchoredPosition = pos;

        // 背景：宣纸白底 + 底部朱红线
        o.AddComponent<Image>().color = new Color(XuanPaper.r, XuanPaper.g, XuanPaper.b, 0.5f);
        var underline = NewUI("Line", o.transform);
        var ulr = underline.GetComponent<RectTransform>();
        ulr.anchorMin = new Vector2(0, 0); ulr.anchorMax = new Vector2(1, 0);
        ulr.pivot = new Vector2(0.5f, 0f);
        ulr.sizeDelta = new Vector2(0, 2);
        var ulImg = underline.AddComponent<Image>(); ulImg.color = ZhuRed; ulImg.raycastTarget = false;

        // 文本
        var tObj = NewUI("Text", o.transform);
        var tr = tObj.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(10, 6); tr.offsetMax = new Vector2(-10, -4);
        var txt = tObj.AddComponent<Text>();
        txt.font = Font(); txt.fontSize = 18; txt.color = InkBlack; txt.alignment = TextAnchor.MiddleCenter;

        // 占位符
        var pObj = NewUI("Placeholder", o.transform);
        var pr = pObj.GetComponent<RectTransform>();
        pr.anchorMin = Vector2.zero; pr.anchorMax = Vector2.one;
        pr.offsetMin = new Vector2(10, 6); pr.offsetMax = new Vector2(-10, -4);
        var pht = pObj.AddComponent<Text>();
        pht.font = Font(); pht.text = placeholder; pht.fontSize = 16;
        pht.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); pht.alignment = TextAnchor.MiddleCenter;

        var inf = o.AddComponent<InputField>();
        inf.textComponent = txt;
        inf.placeholder = pht;
        if (password) inf.contentType = InputField.ContentType.Password;
        return inf;
    }

    // ──────────────────── 弹窗遮罩 ────────────────────

    protected GameObject MakeOverlay(Transform parent, string title, string content, Color accent)
    {
        var overlay = NewUI("Overlay", parent);
        Stretch(overlay);
        var olImg = overlay.AddComponent<Image>();
        olImg.color = new Color(0, 0, 0, 0.85f);
        olImg.raycastTarget = false; // 遮罩不拦截点击，只有 X 按钮关闭
        overlay.SetActive(false);

        var panel = NewUI("Panel", overlay.transform);
        var pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(440, 380);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = XuanPaper;
        panelImg.raycastTarget = true;

        // 顶部朱红装饰线
        var topLine = NewUI("TopLine", panel.transform);
        var tlr = topLine.GetComponent<RectTransform>();
        tlr.anchorMin = new Vector2(0, 1); tlr.anchorMax = new Vector2(1, 1);
        tlr.pivot = new Vector2(0.5f, 1f);
        tlr.sizeDelta = new Vector2(0, 3);
        var tlImg = topLine.AddComponent<Image>(); tlImg.color = ZhuRed; tlImg.raycastTarget = false;

        // 标题
        var tObj = NewUI("T", panel.transform);
        var tr = tObj.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0, 1); tr.anchorMax = new Vector2(1, 1);
        tr.pivot = new Vector2(0.5f, 1f); tr.sizeDelta = new Vector2(0, 45);
        var tl = tObj.AddComponent<Text>();
        tl.font = Font(); tl.text = title; tl.fontSize = 24; tl.color = accent; tl.alignment = TextAnchor.MiddleCenter;

        // 内容（ScrollRect 可滚动）
        var viewport = NewUI("Viewport", panel.transform);
        var vpR = viewport.GetComponent<RectTransform>();
        vpR.anchorMin = Vector2.zero; vpR.anchorMax = new Vector2(1, 1);
        vpR.offsetMin = new Vector2(20, 20); vpR.offsetMax = new Vector2(-20, -55);
        var vpImg = viewport.AddComponent<Image>();
        vpImg.color = new Color(1, 1, 1, 1);
        vpImg.raycastTarget = true;
        var mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        var contentObj = NewUI("C", viewport.transform);
        var cR = contentObj.GetComponent<RectTransform>();
        cR.anchorMin = new Vector2(0, 1); cR.anchorMax = new Vector2(1, 1);
        cR.pivot = new Vector2(0.5f, 1f);
        cR.sizeDelta = new Vector2(0, 0);
        var cl = contentObj.AddComponent<Text>();
        cl.font = Font(); cl.text = content; cl.fontSize = 17; cl.color = InkBlack; cl.alignment = TextAnchor.UpperLeft;
        cl.lineSpacing = 1.4f;

        var csf = contentObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scrollRect = viewport.AddComponent<ScrollRect>();
        scrollRect.content = cR;
        scrollRect.viewport = vpR;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.1f;

        // 关闭按钮
        var xObj = NewUI("X", panel.transform);
        var xr = xObj.GetComponent<RectTransform>();
        xr.anchorMin = xr.anchorMax = new Vector2(1, 1);
        xr.pivot = new Vector2(1, 1);
        xr.sizeDelta = new Vector2(36, 36);
        xr.anchoredPosition = new Vector2(-8, -8);
        xObj.AddComponent<Image>().color = ZhuRed;
        xObj.AddComponent<Button>().onClick.AddListener(() => overlay.SetActive(false));
        var xTxt = NewUI("XT", xObj.transform);
        Stretch(xTxt);
        var xt = xTxt.AddComponent<Text>();
        xt.font = Font(); xt.text = "X"; xt.fontSize = 20; xt.color = Color.white; xt.alignment = TextAnchor.MiddleCenter;

        // 点击遮罩关闭
        overlay.AddComponent<Button>().onClick.AddListener(() => overlay.SetActive(false));

        return overlay;
    }

    // ──────────────────── 动画工具 ────────────────────

    /// <summary>
    /// 淡入动画（CanvasGroup alpha 0→1）
    /// </summary>
    protected IEnumerator FadeIn(CanvasGroup cg, float duration = 0.8f)
    {
        if (cg == null) yield break;
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Clamp01(t / duration);
            yield return null;
        }
        cg.alpha = 1f;
        cg.blocksRaycasts = true;
    }

    /// <summary>
    /// 淡出动画（CanvasGroup alpha 1→0）
    /// </summary>
    protected IEnumerator FadeOut(CanvasGroup cg, float duration = 0.5f, System.Action onComplete = null)
    {
        if (cg == null) { onComplete?.Invoke(); yield break; }
        cg.blocksRaycasts = false;
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            cg.alpha = 1f - Mathf.Clamp01(t / duration);
            yield return null;
        }
        cg.alpha = 0f;
        onComplete?.Invoke();
    }

    /// <summary>
    /// 脉冲呼吸动画（轻微缩放循环）
    /// </summary>
    protected IEnumerator Pulse(Transform target, float minScale = 0.97f, float maxScale = 1.03f, float speed = 2f)
    {
        if (target == null) yield break;
        while (target != null)
        {
            float s = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(Time.time * speed) + 1f) * 0.5f);
            target.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
    }

    /// <summary>
    /// Toast 提示（底部弹出，自动淡出消失）
    /// </summary>
    protected void ShowToast(string msg, Color color, float duration = 2.5f)
    {
        SfxToast();
        var toastObj = NewUI("Toast", rootT);
        var tr = toastObj.GetComponent<RectTransform>();
        tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0f);
        tr.pivot = new Vector2(0.5f, 0f);
        tr.sizeDelta = new Vector2(320, 44);
        tr.anchoredPosition = new Vector2(0, 80);
        toastObj.AddComponent<Image>().color = new Color(0, 0, 0, 0.85f);

        var tObj = NewUI("T", toastObj.transform);
        Stretch(tObj);
        var t = tObj.AddComponent<Text>();
        t.font = Font(); t.text = msg; t.fontSize = 18; t.color = color; t.alignment = TextAnchor.MiddleCenter;

        // 添加 CanvasGroup 用于淡出
        var cg = toastObj.AddComponent<CanvasGroup>();
        StartCoroutine(ToastFade(cg, toastObj, duration));
    }

    private IEnumerator ToastFade(CanvasGroup cg, GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay - 0.5f);
        if (cg == null) yield break;
        float t = 0;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            if (cg != null) cg.alpha = 1f - t / 0.5f;
            yield return null;
        }
        if (obj != null) Destroy(obj);
    }

    // ──────────────────── 中式边框装饰 ────────────────────

    /// <summary>
    /// 在面板上下添加朱红装饰线
    /// </summary>
    protected void AddBorderLines(GameObject panel, float lineWidth = 3f)
    {
        // 顶部线
        var top = NewUI("TopLine", panel.transform);
        var tr = top.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0.05f, 1); tr.anchorMax = new Vector2(0.95f, 1);
        tr.pivot = new Vector2(0.5f, 1f);
        tr.sizeDelta = new Vector2(0, lineWidth);
        tr.anchoredPosition = new Vector2(0, -8);
        var topImg = top.AddComponent<Image>(); topImg.color = ZhuRed; topImg.raycastTarget = false;

        // 底部线
        var bot = NewUI("BotLine", panel.transform);
        var br = bot.GetComponent<RectTransform>();
        br.anchorMin = new Vector2(0.05f, 0); br.anchorMax = new Vector2(0.95f, 0);
        br.pivot = new Vector2(0.5f, 0f);
        br.sizeDelta = new Vector2(0, lineWidth);
        br.anchoredPosition = new Vector2(0, 8);
        var botImg = bot.AddComponent<Image>(); botImg.color = ZhuRed; botImg.raycastTarget = false;
    }

    // ──────────────────── 圆形印章图标（品类卡片用） ────────────────────

    protected GameObject AddSealIcon(string name, Transform parent, Vector2 center, float radius, string text, int fontSize = 36)
    {
        var o = NewUI(name, parent);
        var r = o.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = center;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(radius * 2, radius * 2);
        var sealIconImg = o.AddComponent<Image>(); sealIconImg.color = ZhuRed; sealIconImg.raycastTarget = false;
        var tObj = NewUI("T", o.transform);
        Stretch(tObj);
        var t = tObj.AddComponent<Text>();
        t.font = Font(); t.text = text; t.fontSize = fontSize; t.color = Color.white; t.alignment = TextAnchor.MiddleCenter;
        return o;
    }

    // ──────────────────── 水墨晕染边缘 ────────────────────

    /// <summary>
    /// 在画轴四角添加水墨晕染效果 — 半透明墨色渐变角
    /// </summary>
    protected void AddInkWashCorners(Transform parent, float cornerSize = 120f)
    {
        Color washColor = new Color(0.15f, 0.15f, 0.15f, 0.3f);

        // 左上角
        AddWashCorner("Wash_TL", parent, new Vector2(0, 1), new Vector2(0, 1), cornerSize, washColor);
        // 右上角
        AddWashCorner("Wash_TR", parent, new Vector2(1, 1), new Vector2(1, 1), cornerSize, washColor);
        // 左下角
        AddWashCorner("Wash_BL", parent, new Vector2(0, 0), new Vector2(0, 0), cornerSize, washColor);
        // 右下角
        AddWashCorner("Wash_BR", parent, new Vector2(1, 0), new Vector2(1, 0), cornerSize, washColor);
    }

    private void AddWashCorner(string name, Transform parent, Vector2 anchor, Vector2 pivot, float size, Color color)
    {
        var o = NewUI(name, parent);
        var r = o.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = anchor;
        r.pivot = pivot;
        r.sizeDelta = new Vector2(size, size);
        var img = o.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        // 用简单的半透明纯色模拟水墨晕染（后续可替换为渐变纹理）
    }

    // ──────────────────── 音效快捷方法 ────────────────────

    protected static void SfxClick()   => AudioManager.Instance?.PlayClick();
    protected static void SfxFlip()    => AudioManager.Instance?.PlayFlip();
    protected static void SfxCollect()=> AudioManager.Instance?.PlayCollect();
    protected static void SfxToast()  => AudioManager.Instance?.PlayToast();
}
