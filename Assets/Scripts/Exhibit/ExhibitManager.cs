using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 展示场景管理器 — 3D模型居中 + 底部抽屉式信息面板
/// </summary>
public class ExhibitManager : UIFrame
{
    // ──────────────────── 配色 ────────────────────
    private static readonly Color XuanPaperBg  = new Color(0.96f, 0.94f, 0.90f);
    private static readonly Color OchreBrown    = new Color(0.55f, 0.35f, 0.17f);
    private static readonly Color ChinaRed      = new Color(0.70f, 0.13f, 0.13f);
    private static readonly Color GoldBtn       = new Color(0.83f, 0.69f, 0.22f);
    private static readonly Color InkTitle      = new Color(0.17f, 0.17f, 0.17f);
    private static readonly Color SummaryGray   = new Color(0.35f, 0.35f, 0.35f);
    private static readonly Color PedestalColor = new Color(0.91f, 0.86f, 0.78f);
    private static readonly Color DrawerBg      = new Color(0.98f, 0.96f, 0.92f);
    private static readonly Color TabActiveBg   = new Color(0.88f, 0.72f, 0.45f);
    private static readonly Color TabInactiveBg = new Color(0.92f, 0.89f, 0.83f);

    private static readonly Dictionary<string, Color> ModelColors = new Dictionary<string, Color>
    {
        { "vase", new Color(0.85f, 0.88f, 0.92f) }, { "cup", new Color(0.95f, 0.95f, 0.92f) },
        { "papercut", new Color(0.85f, 0.15f, 0.12f) }, { "scroll", new Color(0.92f, 0.88f, 0.78f) },
        { "bianzhong", new Color(0.65f, 0.50f, 0.20f) }, { "guzheng", new Color(0.45f, 0.25f, 0.12f) },
        { "erhu", new Color(0.55f, 0.30f, 0.15f) }
    };

    private static readonly string[] TabTitles = { "历史背景", "制作工艺", "文化寓意" };
    private static readonly string[] TabIcons  = { "历", "艺", "寓" };

    private List<ExhibitData> currentExhibits = new List<ExhibitData>();
    private int currentExhibitIndex;
    private GameObject currentModel;
    private Text exhibitNameText;
    private GameObject quoteObj;
    private Text quoteText;
    private GameObject pedestal;
    private ExhibitData currentExhibit;

    // 抽屉相关
    private GameObject drawerPanel;
    private RectTransform drawerRt;
    private GameObject drawerContent;
    private Text drawerContentText;
    private GameObject[] tabBtns = new GameObject[3];
    private Text[] tabTexts = new Text[3];
    private GameObject handleBtn;
    private Text handleText;
    private int activeTab;
    private bool drawerOpen;
    private bool drawerAnimating;

    // 展品缩略图
    private RenderTexture thumbRT;
    private Camera thumbCam;
    private GameObject thumbCamObj;
    private RawImage thumbRawImg;
    private const float DrawerAnimDuration = 0.3f;
    private const float FooterHeight = 55f;

    // 收藏
    private GameObject collectBtn;
    private Text collectBtnText;
    private bool isCollected;

    private void Start()
    {
        EnsureSingletons();
        LoadCategoryData();
        CreateUI();
        ShowCurrentExhibit();
    }

    private void LoadCategoryData()
    {
        string cat = PlayerPrefs.GetString("CurrentCategory", "瓷器");
        currentExhibits = GameManager.Instance.GetExhibitsByCategory(cat);
        currentExhibitIndex = 0;
    }

    // ──────────────────── UI 创建 ────────────────────

    private void CreateUI()
    {
        var root = InitCanvas();

        var canvas = GetComponentInChildren<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 20f;
        }

        var cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = XuanPaperBg;
        }

        CreateHeader(root);
        CreateQuoteOverlay(root);
        CreateDrawer(root);
        CreateFooter(root);
    }

    // ──────────────────── 顶部导航栏 ────────────────────

    private void CreateHeader(Transform parent)
    {
        var header = AnchorTop("Header", parent, 55);
        var hImg = header.AddComponent<Image>();
        hImg.color = new Color(XuanPaperBg.r, XuanPaperBg.g, XuanPaperBg.b, 0.95f);
        hImg.raycastTarget = false;

        var divLine = NewUI("DivLine", header.transform);
        var dlr = divLine.GetComponent<RectTransform>();
        dlr.anchorMin = new Vector2(0, 0); dlr.anchorMax = new Vector2(1, 0);
        dlr.pivot = new Vector2(0.5f, 0f); dlr.sizeDelta = new Vector2(0, 2);
        var dlImg = divLine.AddComponent<Image>(); dlImg.color = OchreBrown; dlImg.raycastTarget = false;

        var backObj = NewUI("BackBtn", header.transform);
        var br = backObj.GetComponent<RectTransform>();
        br.anchorMin = br.anchorMax = new Vector2(0, 0.5f);
        br.pivot = new Vector2(0, 0.5f);
        br.sizeDelta = new Vector2(100, 36);
        br.anchoredPosition = new Vector2(12, 0);
        backObj.AddComponent<Image>().color = OchreBrown;
        backObj.AddComponent<Button>().onClick.AddListener(() => SceneLoader.Instance.LoadScene(SceneNames.Main));
        var btObj = NewUI("T", backObj.transform); Stretch(btObj);
        var bt = btObj.AddComponent<Text>();
        bt.font = Font(); bt.text = "< 返回"; bt.fontSize = 16; bt.color = Color.white; bt.alignment = TextAnchor.MiddleCenter;

        var nameObj = NewUI("Name", header.transform);
        var nr = nameObj.GetComponent<RectTransform>();
        nr.anchorMin = nr.anchorMax = new Vector2(0.5f, 0.5f);
        nr.pivot = new Vector2(0.5f, 0.5f);
        nr.sizeDelta = new Vector2(400, 30);
        exhibitNameText = nameObj.AddComponent<Text>();
        exhibitNameText.font = Font(); exhibitNameText.fontSize = 20; exhibitNameText.color = InkTitle; exhibitNameText.alignment = TextAnchor.MiddleCenter;

        collectBtn = NewUI("CollectBtn", header.transform);
        var cr = collectBtn.GetComponent<RectTransform>();
        cr.anchorMin = cr.anchorMax = new Vector2(1f, 0.5f);
        cr.pivot = new Vector2(1f, 0.5f);
        cr.sizeDelta = new Vector2(100, 34);
        cr.anchoredPosition = new Vector2(-12, 0);
        collectBtn.AddComponent<Image>().color = OchreBrown;
        collectBtn.AddComponent<Button>().onClick.AddListener(OnCollectClicked);
        var cbtObj = NewUI("T", collectBtn.transform); Stretch(cbtObj);
        collectBtnText = cbtObj.AddComponent<Text>();
        collectBtnText.font = Font(); collectBtnText.fontSize = 16; collectBtnText.color = Color.white; collectBtnText.alignment = TextAnchor.MiddleCenter;
        collectBtnText.text = "♡ 收藏";
    }

    // ──────────────────── 引用语浮层 ────────────────────

    private void CreateQuoteOverlay(Transform parent)
    {
        quoteObj = NewUI("Quote", parent);
        var qr = quoteObj.GetComponent<RectTransform>();
        qr.anchorMin = qr.anchorMax = new Vector2(0.5f, 0.28f);
        qr.pivot = new Vector2(0.5f, 0.5f);
        qr.sizeDelta = new Vector2(500, 50);
        var qImg = quoteObj.AddComponent<Image>(); qImg.color = new Color(0, 0, 0, 0); qImg.raycastTarget = false;

        var qtObj = NewUI("T", quoteObj.transform); Stretch(qtObj);
        quoteText = qtObj.AddComponent<Text>();
        quoteText.font = Font(); quoteText.fontSize = 15; quoteText.color = OchreBrown;
        quoteText.alignment = TextAnchor.MiddleCenter;
        quoteText.lineSpacing = 1.3f;
    }

    // ──────────────────── 底部抽屉面板 ────────────────────

    private void CreateDrawer(Transform parent)
    {
        drawerPanel = NewUI("Drawer", parent);
        drawerRt = drawerPanel.GetComponent<RectTransform>();

        // 锚定屏幕底部，pivot在底边中点
        drawerRt.anchorMin = new Vector2(0f, 0f);
        drawerRt.anchorMax = new Vector2(1f, 0f);
        drawerRt.pivot = new Vector2(0.5f, 0f);

        var bgImg = drawerPanel.AddComponent<Image>();
        bgImg.color = new Color(DrawerBg.r, DrawerBg.g, DrawerBg.b, 0.97f);
        bgImg.raycastTarget = true;

        // 顶部分隔线
        var divLine = NewUI("DivLine", drawerPanel.transform);
        var dlr = divLine.GetComponent<RectTransform>();
        dlr.anchorMin = new Vector2(0, 1); dlr.anchorMax = new Vector2(1, 1);
        dlr.pivot = new Vector2(0.5f, 1f); dlr.sizeDelta = new Vector2(0, 2);
        var dlImg = divLine.AddComponent<Image>(); dlImg.color = OchreBrown; dlImg.raycastTarget = false;

        // 拉手柄按钮 — 在分隔线下方
        CreateHandle(drawerPanel.transform);
        CreateTabBar(drawerPanel.transform);
        CreateDrawerContent(drawerPanel.transform);

        drawerOpen = false;
        SetDrawerSize(false, false);
    }

    private void CreateHandle(Transform parent)
    {
        // 拉手柄：位于抽屉最顶部，醒目的棕色圆角按钮
        handleBtn = NewUI("Handle", parent);
        var hr = handleBtn.GetComponent<RectTransform>();
        hr.anchorMin = hr.anchorMax = new Vector2(0.5f, 1f);
        hr.pivot = new Vector2(0.5f, 1f);
        hr.sizeDelta = new Vector2(160, 32);
        hr.anchoredPosition = new Vector2(0, -4);
        var hBg = handleBtn.AddComponent<Image>();
        hBg.color = OchreBrown;
        hBg.raycastTarget = true;
        handleBtn.AddComponent<Button>().onClick.AddListener(ToggleDrawer);
        var htObj = NewUI("T", handleBtn.transform); Stretch(htObj);
        handleText = htObj.AddComponent<Text>();
        handleText.font = Font(); handleText.fontSize = 14; handleText.color = Color.white;
        handleText.alignment = TextAnchor.MiddleCenter;
        handleText.text = "▲  详情  ▲";
    }

    private void CreateTabBar(Transform parent)
    {
        // Tab栏：在拉手柄下方
        var tabBar = NewUI("TabBar", parent);
        var tbr = tabBar.GetComponent<RectTransform>();
        tbr.anchorMin = new Vector2(0, 1); tbr.anchorMax = new Vector2(1, 1);
        tbr.pivot = new Vector2(0.5f, 1f);
        tbr.sizeDelta = new Vector2(0, 36);
        tbr.anchoredPosition = new Vector2(0, -36);
        var tabBg = tabBar.AddComponent<Image>(); tabBg.color = new Color(0, 0, 0, 0); tabBg.raycastTarget = false;

        var hlg = tabBar.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 6;
        hlg.padding = new RectOffset(10, 10, 3, 3);
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        for (int i = 0; i < 3; i++)
            CreateTabButton(tabBar.transform, i);

        activeTab = 0;
        UpdateTabStyles();
    }

    private void CreateTabButton(Transform parent, int idx)
    {
        var tab = NewUI($"Tab_{idx}", parent);
        var le = tab.AddComponent<LayoutElement>();
        le.preferredHeight = 32;

        var tabImg = tab.AddComponent<Image>();
        tabImg.color = TabInactiveBg;
        tabImg.raycastTarget = true;

        var tabBtn = tab.AddComponent<Button>();
        int capturedIdx = idx;
        tabBtn.onClick.AddListener(() => OnTabClicked(capturedIdx));

        // 纯文字标签，居中
        var tObj = NewUI("Label", tab.transform);
        Stretch(tObj);
        var tt = tObj.AddComponent<Text>();
        tt.font = Font(); tt.text = TabTitles[idx]; tt.fontSize = 14; tt.color = InkTitle;
        tt.alignment = TextAnchor.MiddleCenter;

        tabBtns[idx] = tab;
        tabTexts[idx] = tt;
    }

    private void CreateDrawerContent(Transform parent)
    {
        drawerContent = NewUI("DrawerContent", parent);
        var dcr = drawerContent.GetComponent<RectTransform>();
        dcr.anchorMin = new Vector2(0f, 0f);
        dcr.anchorMax = new Vector2(1f, 1f);
        dcr.pivot = new Vector2(0.5f, 0f);
        dcr.sizeDelta = new Vector2(-12, -78);
        dcr.anchoredPosition = new Vector2(0, 4);

        var dcBg = drawerContent.AddComponent<Image>();
        dcBg.color = new Color(1f, 1f, 1f, 0.65f);
        dcBg.raycastTarget = false;

        // 红色装饰条
        var redBar = NewUI("RedBar", drawerContent.transform);
        var rbr = redBar.GetComponent<RectTransform>();
        rbr.anchorMin = new Vector2(0, 1); rbr.anchorMax = new Vector2(1, 1);
        rbr.pivot = new Vector2(0.5f, 1f);
        rbr.sizeDelta = new Vector2(0, 3);
        var rbImg = redBar.AddComponent<Image>(); rbImg.color = ChinaRed; rbImg.raycastTarget = false;

        // 展品缩略图区域
        var thumbArea = NewUI("ThumbArea", drawerContent.transform);
        var tar = thumbArea.GetComponent<RectTransform>();
        tar.anchorMin = tar.anchorMax = new Vector2(0.5f, 1f);
        tar.pivot = new Vector2(0.5f, 1f);
        tar.sizeDelta = new Vector2(180, 100);
        tar.anchoredPosition = new Vector2(0, -10);

        thumbRT = new RenderTexture(180, 100, 16, RenderTextureFormat.ARGB32);
        thumbRT.name = "ExhibitThumb";
        thumbRawImg = thumbArea.AddComponent<RawImage>();
        thumbRawImg.texture = thumbRT;
        thumbRawImg.raycastTarget = false;
        thumbRawImg.color = new Color(1, 1, 1, 0.9f);

        // 缩略图相机
        thumbCamObj = new GameObject("ThumbCam");
        thumbCamObj.transform.position = new Vector3(0, 1f, 2f);
        thumbCamObj.transform.rotation = Quaternion.Euler(15, 180, 0);
        thumbCam = thumbCamObj.AddComponent<Camera>();
        thumbCam.orthographic = true;
        thumbCam.orthographicSize = 1.2f;
        thumbCam.clearFlags = CameraClearFlags.SolidColor;
        thumbCam.backgroundColor = new Color(0.96f, 0.94f, 0.90f, 0f);
        thumbCam.targetTexture = thumbRT;
        thumbCam.cullingMask = 1 << 0; // Default layer (3D models)
        thumbCamObj.SetActive(false);

        // 正文文字
        var txtObj = NewUI("Text", drawerContent.transform);
        var txtr = txtObj.GetComponent<RectTransform>();
        txtr.anchorMin = new Vector2(0.06f, 0.06f);
        txtr.anchorMax = new Vector2(0.94f, 0.94f);
        txtr.offsetMin = txtr.offsetMax = Vector2.zero;
        txtr.offsetMax = new Vector2(0, -110);
        drawerContentText = txtObj.AddComponent<Text>();
        drawerContentText.font = Font();
        drawerContentText.fontSize = 17;
        drawerContentText.color = InkTitle;
        drawerContentText.alignment = TextAnchor.UpperLeft;
        drawerContentText.lineSpacing = 1.6f;
        drawerContentText.supportRichText = true;

        drawerContent.SetActive(false);
    }

    // ──────────────────── 抽屉控制 ────────────────────

    private void ToggleDrawer()
    {
        if (drawerAnimating) return;
        drawerOpen = !drawerOpen;
        StartCoroutine(AnimateDrawer(drawerOpen));
    }

    private void OnTabClicked(int idx)
    {
        activeTab = idx;
        UpdateTabStyles();
        UpdateDrawerContent();

        if (!drawerOpen)
        {
            drawerOpen = true;
            StartCoroutine(AnimateDrawer(true));
        }
    }

    private System.Collections.IEnumerator AnimateDrawer(bool open)
    {
        drawerAnimating = true;

        float fromH = drawerRt.sizeDelta.y;
        float toH = open ? DrawerOpenHeight() : DrawerCollapsedHeight();
        float fromY = drawerRt.anchoredPosition.y;
        float toY = FooterHeight;

        if (open)
        {
            drawerContent.SetActive(true);
            UpdateDrawerContent();
            RenderThumbnail();
        }

        // 抽屉展开时隐藏引用语，收起时恢复
        if (quoteObj != null)
            quoteObj.SetActive(!open);

        float t = 0;
        while (t < DrawerAnimDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / DrawerAnimDuration);
            float smoothP = Mathf.SmoothStep(0f, 1f, p);
            drawerRt.sizeDelta = new Vector2(drawerRt.sizeDelta.x, Mathf.Lerp(fromH, toH, smoothP));
            drawerRt.anchoredPosition = new Vector2(drawerRt.anchoredPosition.x, Mathf.Lerp(fromY, toY, smoothP));
            yield return null;
        }

        drawerRt.sizeDelta = new Vector2(drawerRt.sizeDelta.x, toH);
        drawerRt.anchoredPosition = new Vector2(drawerRt.anchoredPosition.x, toY);

        if (!open)
            drawerContent.SetActive(false);

        handleText.text = open ? "▼  收起  ▼" : "▲  详情  ▲";

        drawerAnimating = false;
    }

    private float DrawerCollapsedHeight() => 80f;  // handle + tab

    private float DrawerOpenHeight() => Screen.height * 0.3f;  // 更紧凑

    private void SetDrawerSize(bool open, bool animate)
    {
        if (animate)
        {
            StartCoroutine(AnimateDrawer(open));
            return;
        }

        float h = open ? DrawerOpenHeight() : DrawerCollapsedHeight();
        drawerRt.sizeDelta = new Vector2(drawerRt.sizeDelta.x, h);
        drawerRt.anchoredPosition = new Vector2(drawerRt.anchoredPosition.x, FooterHeight);
        drawerContent.SetActive(open);
        if (quoteObj != null) quoteObj.SetActive(!open);
        handleText.text = open ? "▼  收起  ▼" : "▲  详情  ▲";
        if (open) { UpdateDrawerContent(); RenderThumbnail(); }
    }

    private void UpdateTabStyles()
    {
        for (int i = 0; i < 3; i++)
        {
            if (tabBtns[i] == null) continue;
            tabBtns[i].GetComponent<Image>().color = (i == activeTab) ? TabActiveBg : TabInactiveBg;
            if (tabTexts[i] != null)
                tabTexts[i].color = (i == activeTab) ? ChinaRed : InkTitle;
        }
    }

    private void UpdateDrawerContent()
    {
        if (drawerContentText == null || currentExhibit == null) return;

        switch (activeTab)
        {
            case 0: drawerContentText.text = currentExhibit.history; break;
            case 1: drawerContentText.text = currentExhibit.craft; break;
            case 2: drawerContentText.text = currentExhibit.meaning; break;
        }
    }

    // ──────────────────── 底部操作栏 ────────────────────

    private void CreateFooter(Transform parent)
    {
        var footer = AnchorBottom("Footer", parent, 55);
        var fImg = footer.AddComponent<Image>(); fImg.color = new Color(XuanPaperBg.r, XuanPaperBg.g, XuanPaperBg.b, 0.95f); fImg.raycastTarget = false;

        var divLine = NewUI("DivLine", footer.transform);
        var dlr = divLine.GetComponent<RectTransform>();
        dlr.anchorMin = new Vector2(0, 1); dlr.anchorMax = new Vector2(1, 1);
        dlr.pivot = new Vector2(0.5f, 1f); dlr.sizeDelta = new Vector2(0, 2);
        var dlImg = divLine.AddComponent<Image>(); dlImg.color = OchreBrown; dlImg.raycastTarget = false;

        AddBtnAnchored("PrevBtn", footer.transform, new Vector2(0.16f, 0.5f), new Vector2(110, 40), Vector2.zero, "< 上一个", OchreBrown, 14).onClick.AddListener(ShowPrevious);
        AddBtnAnchored("ShareBtn", footer.transform, new Vector2(0.38f, 0.5f), new Vector2(100, 40), Vector2.zero, "分享", new Color(0.18f, 0.48f, 0.43f), 14).onClick.AddListener(OnShareClicked);
        AddBtnAnchored("CollectBtn2", footer.transform, new Vector2(0.62f, 0.5f), new Vector2(100, 40), Vector2.zero, "收藏", OchreBrown, 14).onClick.AddListener(OnCollectClicked);
        AddBtnAnchored("NextBtn", footer.transform, new Vector2(0.84f, 0.5f), new Vector2(110, 40), Vector2.zero, "下一个 >", OchreBrown, 14).onClick.AddListener(ShowNext);
    }

    // ──────────────────── 3D 模型 ────────────────────

    private void ShowCurrentExhibit()
    {
        if (currentExhibits.Count == 0) return;
        var data = currentExhibits[currentExhibitIndex];
        if (currentModel != null) Destroy(currentModel);
        if (pedestal != null) Destroy(pedestal);

        pedestal = new GameObject("Pedestal");
        pedestal.transform.position = new Vector3(0, -0.8f, 5);
        var disk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disk.transform.SetParent(pedestal.transform, false);
        disk.transform.localScale = new Vector3(2.2f, 0.06f, 2.2f);
        disk.GetComponent<Renderer>().material.color = PedestalColor;

        currentModel = CreateModel(data.modelType, data.id);
        currentModel.AddComponent<ModelRotator>();

        exhibitNameText.text = data.category + " - " + data.name;
        quoteText.text = $"\"{data.description}\"";

        currentExhibit = data;

        drawerOpen = false;
        SetDrawerSize(false, false);
        activeTab = 0;
        UpdateTabStyles();

        isCollected = BackpackManager.Instance.IsInBackpack(GameManager.Instance.currentUser, data.id);
        UpdateCollectButton();
    }

    private GameObject CreateModel(string type, string id)
    {
        var p = new GameObject($"Exhibit_{id}");
        p.transform.position = new Vector3(0, 0.5f, 5);
        p.transform.localScale = Vector3.one * 3f;
        Color c = ModelColors.ContainsKey(type) ? ModelColors[type] : Color.gray;

        switch (type)
        {
            case "vase":
                AddPart(PrimitiveType.Cylinder, p, new Vector3(0.6f, 0.8f, 0.6f), new Vector3(0, 0.8f, 0), c);
                AddPart(PrimitiveType.Cylinder, p, new Vector3(0.25f, 0.3f, 0.25f), new Vector3(0, 1.8f, 0), c);
                AddPart(PrimitiveType.Cylinder, p, new Vector3(0.3f, 0.08f, 0.3f), new Vector3(0, 2f, 0), new Color(0.2f, 0.3f, 0.6f));
                AddPart(PrimitiveType.Cylinder, p, new Vector3(0.7f, 0.1f, 0.7f), new Vector3(0, 0.05f, 0), new Color(0.4f, 0.25f, 0.1f));
                AddPart(PrimitiveType.Cylinder, p, new Vector3(0.62f, 0.05f, 0.62f), new Vector3(0, 0.5f, 0), new Color(0.15f, 0.25f, 0.55f));
                break;
            case "cup":
                AddPart(PrimitiveType.Cylinder, p, new Vector3(0.5f, 0.4f, 0.5f), Vector3.zero, c);
                AddPart(PrimitiveType.Sphere, p, new Vector3(0.5f, 0.2f, 0.5f), new Vector3(0, 0.5f, 0), c);
                AddPart(PrimitiveType.Sphere, p, new Vector3(0.12f, 0.12f, 0.12f), new Vector3(0, 0.65f, 0), new Color(0.6f, 0.35f, 0.1f));
                break;
            case "papercut":
                AddPart(PrimitiveType.Quad, p, new Vector3(1.5f, 1.5f, 1f), Vector3.zero, c);
                AddPart(PrimitiveType.Quad, p, new Vector3(1.7f, 1.7f, 0.9f), new Vector3(0, 0, -0.01f), new Color(0.95f, 0.9f, 0.8f));
                break;
            case "scroll":
                AddPart(PrimitiveType.Quad, p, new Vector3(1.2f, 1.8f, 1f), Vector3.zero, c);
                for (int i = 0; i < 2; i++)
                {
                    var rod = AddPart(PrimitiveType.Cylinder, p, new Vector3(1.25f, 0.06f, 0.06f), new Vector3(0, i == 0 ? -0.95f : 0.95f, 0), new Color(0.45f, 0.25f, 0.1f));
                    rod.transform.Rotate(0, 0, 90);
                }
                break;
            case "bianzhong":
                AddPart(PrimitiveType.Cylinder, p, new Vector3(1.5f, 0.06f, 0.06f), new Vector3(0, 1.5f, 0), new Color(0.35f, 0.2f, 0.08f)).transform.Rotate(0, 0, 90);
                for (int i = 0; i < 5; i++) AddPart(PrimitiveType.Cylinder, p, new Vector3((0.3f - i * 0.03f), 0.4f - i * 0.03f, 0.3f - i * 0.03f), new Vector3(-0.5f + i * 0.25f, 1.1f - i * 0.05f, 0), c);
                AddPart(PrimitiveType.Cube, p, new Vector3(1.6f, 0.1f, 0.4f), new Vector3(0, -0.2f, 0), new Color(0.35f, 0.2f, 0.08f));
                break;
            case "guzheng":
                AddPart(PrimitiveType.Cube, p, new Vector3(1.8f, 0.08f, 0.4f), Vector3.zero, c);
                for (int i = 0; i < 8; i++) AddPart(PrimitiveType.Cube, p, new Vector3(1.75f, 0.005f, 0.005f), new Vector3(0, 0.05f, -0.14f + i * 0.04f), Color.white);
                break;
            case "erhu":
                AddPart(PrimitiveType.Cylinder, p, new Vector3(0.04f, 1.2f, 0.04f), new Vector3(0, 0.6f, 0), c);
                var q = AddPart(PrimitiveType.Cylinder, p, new Vector3(0.3f, 0.3f, 0.3f), new Vector3(0, -0.15f, 0), c);
                q.transform.Rotate(90, 0, 0);
                break;
            default:
                AddPart(PrimitiveType.Cube, p, new Vector3(0.8f, 0.8f, 0.8f), Vector3.zero, c);
                break;
        }
        return p;
    }

    private GameObject AddPart(PrimitiveType pt, GameObject parent, Vector3 scale, Vector3 pos, Color color)
    {
        var obj = GameObject.CreatePrimitive(pt);
        obj.transform.SetParent(parent.transform, false);
        obj.transform.localScale = scale;
        obj.transform.localPosition = pos;
        obj.GetComponent<Renderer>().material.color = color;
        return obj;
    }

    // ──────────────────── 导航 ────────────────────

    private void ShowPrevious()
    {
        if (currentExhibits.Count == 0) return;
        currentExhibitIndex = (currentExhibitIndex - 1 + currentExhibits.Count) % currentExhibits.Count;
        ShowCurrentExhibit();
    }

    private void ShowNext()
    {
        if (currentExhibits.Count == 0) return;
        currentExhibitIndex = (currentExhibitIndex + 1) % currentExhibits.Count;
        ShowCurrentExhibit();
    }

    // ──────────────────── 收藏 ────────────────────

    private void OnCollectClicked()
    {
        if (currentExhibits.Count == 0 || currentExhibit == null) return;
        SfxCollect();
        string id = currentExhibit.id;
        if (isCollected)
        {
            BackpackManager.Instance.RemoveFromBackpack(GameManager.Instance.currentUser, id);
            isCollected = false;
            ShowToast($"已取消收藏「{currentExhibit.name}」", SummaryGray);
        }
        else
        {
            BackpackManager.Instance.AddToBackpack(GameManager.Instance.currentUser, id);
            isCollected = true;
            ShowToast($"✅ 已收藏「{currentExhibit.name}」", JadeGreen);
            StartCoroutine(HeartbeatAnimation(collectBtn.transform));
        }
        UpdateCollectButton();
    }

    private void UpdateCollectButton()
    {
        collectBtnText.text = isCollected ? "❤ 已收藏" : "♡ 收藏";
        collectBtn.GetComponent<Image>().color = isCollected ? ChinaRed : OchreBrown;
    }

    // ──────────────────── 分享 ────────────────────

    private void OnShareClicked()
    {
        if (currentExhibit == null) return;
        SfxClick();
        string shareText = $"【了不起的非遗】{currentExhibit.category} · {currentExhibit.name}\n\n{currentExhibit.description}\n\n{currentExhibit.history}\n\n—— 来自「了不起的非遗」交互展示系统";
        GUIUtility.systemCopyBuffer = shareText;
        ShowToast("分享内容已复制到剪贴板", JadeGreen);
    }

    private System.Collections.IEnumerator HeartbeatAnimation(Transform target)
    {
        Vector3 orig = target.localScale;
        float duration = 0.2f;
        float t = 0;
        while (t < duration * 0.5f)
        {
            t += Time.deltaTime;
            target.localScale = Vector3.Lerp(orig, orig * 1.2f, t / (duration * 0.5f));
            yield return null;
        }
        t = 0;
        while (t < duration * 0.5f)
        {
            t += Time.deltaTime;
            target.localScale = Vector3.Lerp(orig * 1.2f, orig, t / (duration * 0.5f));
            yield return null;
        }
        target.localScale = orig;
    }

    // ──────────────────── 缩略图渲染 ────────────────────

    private void RenderThumbnail()
    {
        if (thumbCam == null || currentModel == null) return;
        thumbCamObj.SetActive(true);
        thumbCam.Render();
        thumbCamObj.SetActive(false);
    }

    private void OnDestroy()
    {
        if (thumbRT != null) { thumbRT.Release(); thumbRT = null; }
        if (thumbCamObj != null) Destroy(thumbCamObj);
    }
}
