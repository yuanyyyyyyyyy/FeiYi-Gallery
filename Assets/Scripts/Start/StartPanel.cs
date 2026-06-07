using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// 开始场景管理器
/// </summary>
public class StartPanel : MonoBehaviour
{
    private static readonly Color ZhuRed = new Color(0.76f, 0.21f, 0.19f);
    private static readonly Color GoldColor = new Color(0.83f, 0.65f, 0.27f);
    private static readonly Color InkBlack = new Color(0.17f, 0.17f, 0.17f);
    private static readonly Color XuanPaper = new Color(0.96f, 0.90f, 0.78f);
    private static readonly Color JadeGreen = new Color(0.18f, 0.48f, 0.43f);

    private GameObject guidePanel;

    private void Start()
    {
        EnsureSingletons();
        CreateUI();
        StartCoroutine(FadeIn());
    }

    private void EnsureSingletons()
    {
        if (GameManager.Instance == null) new GameObject("[GameManager]").AddComponent<GameManager>();
        if (SceneLoader.Instance == null) new GameObject("[SceneLoader]").AddComponent<SceneLoader>();
    }

    private void CreateUI()
    {
        // Disable any residual Canvas components on this gameObject (from scene file)
        var oldC = GetComponent<Canvas>(); if (oldC != null) oldC.enabled = false;
        var oldCS = GetComponent<CanvasScaler>(); if (oldCS != null) oldCS.enabled = false;
        var oldGR = GetComponent<GraphicRaycaster>(); if (oldGR != null) oldGR.enabled = false;

        // Create Canvas as a ROOT object — ScreenSpaceOverlay Canvas drives its own RectTransform,
        // so child elements directly under Canvas get incorrect offset calculations.
        // Solution: add a full-screen "Root" panel between Canvas and all UI elements
        var canvasObj = new GameObject("Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        if (FindObjectOfType<EventSystem>() == null)
        { var es = new GameObject("EventSystem"); es.AddComponent<EventSystem>(); es.AddComponent<StandaloneInputModule>(); }

        // Full-screen Root panel — all UI goes inside this
        var root = CreateUI("Root", canvas.transform);
        Stretch(root);

        // BG
        var bg = CreateUI("BG", root.transform); Stretch(bg); bg.AddComponent<Image>().color = InkBlack;

        // Content
        var ct = CreateUI("Content", root.transform);
        var cr = ct.GetComponent<RectTransform>();
        cr.anchorMin = cr.anchorMax = new Vector2(0.5f, 0.5f);
        cr.sizeDelta = new Vector2(600, 700);

        // Logo
        var logoBg = CreateUI("LogoBg", ct.transform);
        var lr = logoBg.GetComponent<RectTransform>();
        lr.anchorMin = lr.anchorMax = new Vector2(0.5f, 1f); lr.pivot = new Vector2(0.5f, 1f);
        lr.sizeDelta = new Vector2(120, 120); lr.anchoredPosition = new Vector2(0, -20);
        logoBg.AddComponent<Image>().color = ZhuRed;
        AddLabel("LogoTxt", logoBg.transform, Vector2.zero, new Vector2(100, 100), "非遗", 40, Color.white, true);

        // Title
        AddLabel("Title", ct.transform, new Vector2(0, -170), new Vector2(560, 70), "了不起的非遗", 48, ZhuRed, true);

        // En title
        AddLabel("En", ct.transform, new Vector2(0, -235), new Vector2(560, 28), "Amazing Intangible Cultural Heritage of China", 16, GoldColor, true);

        // Slogan
        AddLabel("Slogan", ct.transform, new Vector2(0, -290), new Vector2(500, 55),
            "传承千年智慧  守护文化根脉\n弘扬中华优秀传统文化 增强文化自信", 18, XuanPaper, true);

        // Start button
        var startBtn = AddBtn("StartBtn", ct.transform, new Vector2(0, -380), new Vector2(280, 60), "开始探索", ZhuRed);
        startBtn.onClick.AddListener(() => SceneLoader.Instance.LoadScene(SceneNames.Main));

        // Guide button
        var guideBtn = AddBtn("GuideBtn", ct.transform, new Vector2(0, -465), new Vector2(200, 45), "使用引导", JadeGreen);
        guideBtn.onClick.AddListener(() => { if (guidePanel != null) guidePanel.SetActive(true); });

        // Welcome
        if (GameManager.Instance != null && GameManager.Instance.isLoggedIn)
            AddLabel("Welcome", ct.transform, new Vector2(0, -530), new Vector2(500, 28),
                $"欢迎回来，{GameManager.Instance.currentUser}！", 18, GoldColor, true);

        // Guide Panel (hidden)
        CreateGuidePanel(root.transform);
    }

    private void CreateGuidePanel(Transform canvasT)
    {
        guidePanel = CreateUI("GuidePanel", canvasT);
        Stretch(guidePanel); guidePanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.85f);
        guidePanel.SetActive(false);
        var overlayBtn = guidePanel.AddComponent<Button>();
        overlayBtn.onClick.AddListener(() => guidePanel.SetActive(false));

        var panel = CreateUI("Panel", guidePanel.transform);
        var pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(500, 450);
        panel.AddComponent<Image>().color = XuanPaper;
        panel.AddComponent<Button>(); // block clicks

        AddLabel("GTitle", panel.transform, new Vector2(0, -25), new Vector2(460, 38), "使用引导", 28, ZhuRed, true);
        AddLabel("GContent", panel.transform, new Vector2(0, -60), new Vector2(440, 300),
            "1. 在主界面选择感兴趣的非遗品类\n\n2. 点击展品卡片进入3D展示场景\n\n3. 拖拽旋转3D模型，滚轮缩放\n\n4. 查看展品的历史、工艺、文化寓意\n\n5. 点击「收藏」将展品加入背包\n\n6. 在背包中管理您收藏的展品", 18, Color.black, false);
        var closeBtn = AddBtn("CloseBtn", panel.transform, new Vector2(0, -395), new Vector2(160, 42), "知道了", ZhuRed);
        closeBtn.onClick.AddListener(() => guidePanel.SetActive(false));
    }

    private IEnumerator FadeIn()
    {
        // Find our Canvas child to add CanvasGroup for fade effect
        var canvasObj = transform.Find("Canvas");
        if (canvasObj == null) yield break;
        var cg = canvasObj.gameObject.AddComponent<CanvasGroup>();
        if (cg == null) yield break;
        cg.alpha = 0f;
        for (float t = 0; t < 1.5f; t += Time.deltaTime)
        { cg.alpha = t / 1.5f; yield return null; }
        cg.alpha = 1f;
    }

    #region UI Helpers
    private GameObject CreateUI(string name, Transform parent)
    { var o = new GameObject(name); o.transform.SetParent(parent, false); o.AddComponent<RectTransform>(); return o; }
    private void Stretch(GameObject o)
    { var r = o.GetComponent<RectTransform>(); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.sizeDelta = Vector2.zero; }

    private Text AddLabel(string name, Transform parent, Vector2 pos, Vector2 size, string text, int fontSize, Color color, bool center = true)
    {
        var o = CreateUI(name, parent);
        var r = o.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f); r.pivot = new Vector2(0.5f, 1f);
        r.sizeDelta = size; r.anchoredPosition = pos;
        var t = o.AddComponent<Text>();
        t.font = UIFont.Get();
        t.text = text; t.fontSize = fontSize; t.color = color;
        t.alignment = center ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
        return t;
    }

    private Button AddBtn(string name, Transform parent, Vector2 pos, Vector2 size, string text, Color bg)
    {
        var o = CreateUI(name, parent);
        var r = o.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f); r.pivot = new Vector2(0.5f, 1f);
        r.sizeDelta = size; r.anchoredPosition = pos;
        o.AddComponent<Image>().color = bg;
        var btn = o.AddComponent<Button>();
        var tObj = CreateUI("Text", o.transform); Stretch(tObj);
        var t = tObj.AddComponent<Text>();
        t.font = UIFont.Get();
        t.text = text; t.fontSize = 22; t.color = Color.white; t.alignment = TextAnchor.MiddleCenter;
        return btn;
    }
    #endregion
}
