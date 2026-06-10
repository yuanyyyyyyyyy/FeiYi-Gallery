using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 主场景管理器 — 新中式品类网格
/// </summary>
public class MainPanel : UIFrame
{
    private static readonly string[] Categories = { "瓷器", "剪纸", "书法", "民族乐器" };
    private static readonly string[] CategoryDescs = { "千年窑火 瓷韵流芳", "纸艺生花 巧夺天工", "笔墨丹青 翰墨飘香", "丝竹管弦 余音绕梁" };
    private static readonly Color[] CategoryColors = {
        new Color(0.26f, 0.47f, 0.72f), new Color(0.80f, 0.20f, 0.18f),
        new Color(0.35f, 0.35f, 0.38f), new Color(0.72f, 0.53f, 0.19f)
    };
    private static readonly string[] CategoryIcons = { "瓷", "剪", "书", "乐" };

    private GameObject backpackPanel, settingsPanel, helpPanel;
    private Transform canvasTRef;
    private CharacterController2D character;
    private InputField backpackSearchInput;
    private int backpackCategoryFilter = -1; // -1=全部

    private void Start()
    {
        EnsureSingletons();
        CreateUI();
    }

    private void CreateUI()
    {
        var root = InitCanvas();
        canvasTRef = canvasT;

        // ── 背景：宣纸底纹 + 水墨晕染 ──
        var bg = NewUI("BG", root);
        Stretch(bg);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.96f, 0.90f, 0.78f);  // 宣纸色
        bgImg.raycastTarget = false;

        // 水墨晕染四角
        AddInkWashCorners(root);

        // ── 顶部标题栏 ──
        CreateHeader(root);

        // ── 卷轴 ScrollView 区域 ──
        CreateScrollView(root);

        // ── 3D 角色展示区 ──
        CreateCharacterStage(root);

        // ── 功能入口：知识探索 + 历史故事 ──
        CreateFeatureEntry(root);

        // ── 底部导航栏（4项：背包/设置/帮助/退出）──
        CreateNavBar(root);

        // ── 弹窗 ──
        settingsPanel = CreateSettingsPanel(root);

        // 帮助弹窗 — 从 HelpManager 读取引导步骤和FAQ
        var guideSteps = HelpManager.GetGuideSteps();
        var faqItems = HelpManager.GetFAQ();
        string helpContent = "【使用引导】\n\n" + string.Join("\n\n", guideSteps)
            + "\n\n━━━━━━━━━━━━━━━━\n\n【常见问题】\n\n" + string.Join("\n\n", faqItems);
        helpPanel = MakeOverlay(root, "帮助", helpContent, ZhuRed);
    }

    // ──────────────────── 顶部标题栏 ────────────────────

    private void CreateHeader(Transform root)
    {
        var header = AnchorTop("Header", root, 60);
        var hImg = header.AddComponent<Image>(); hImg.color = DarkBar; hImg.raycastTarget = false;

        // 标题组：印章+文字，整体居中
        var titleGroup = NewUI("TitleGroup", header.transform);
        var tgr = titleGroup.GetComponent<RectTransform>();
        tgr.anchorMin = tgr.anchorMax = new Vector2(0.5f, 0.5f);
        tgr.pivot = new Vector2(0.5f, 0.5f);
        tgr.sizeDelta = new Vector2(300, 40);
        tgr.anchoredPosition = Vector2.zero;

        // 左侧印章小图标（组内偏左）
        AddSealLogo("MiniLogo", titleGroup.transform, new Vector2(-110, 0), 34, "遗", 18);

        // 标题文字（组内偏右）
        var titleObj = NewUI("Title", titleGroup.transform);
        var tr = titleObj.GetComponent<RectTransform>();
        tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0.5f);
        tr.pivot = new Vector2(0.5f, 0.5f);
        tr.sizeDelta = new Vector2(260, 35);
        tr.anchoredPosition = new Vector2(20, 0);
        var tt = titleObj.AddComponent<Text>();
        tt.font = Font(); tt.text = "了不起的非遗"; tt.fontSize = 26; tt.color = ZhuRed; tt.alignment = TextAnchor.MiddleCenter;

        // 右侧头像 + 用户名
        var userGroup = NewUI("UserGroup", header.transform);
        var ugr = userGroup.GetComponent<RectTransform>();
        ugr.anchorMin = ugr.anchorMax = new Vector2(1f, 0.5f);
        ugr.pivot = new Vector2(1f, 0.5f);
        ugr.sizeDelta = new Vector2(180, 40);
        ugr.anchoredPosition = new Vector2(-15, 0);

        // 头像印章
        int avatarIdx = PlayerPrefs.GetInt($"User_{GameManager.Instance?.currentUser}_Avatar", 0);
        var avatarObj = AddSealIcon("Avatar", userGroup.transform, new Vector2(0.82f, 0.5f), 14, AvatarChars[Mathf.Clamp(avatarIdx, 0, AvatarChars.Length - 1)], 14);
        avatarObj.GetComponent<Image>().color = AvatarColors[Mathf.Clamp(avatarIdx, 0, AvatarColors.Length - 1)];

        // 用户名
        var userObj = NewUI("User", userGroup.transform);
        var ur = userObj.GetComponent<RectTransform>();
        ur.anchorMin = Vector2.zero; ur.anchorMax = new Vector2(0.78f, 1f);
        ur.offsetMin = ur.offsetMax = Vector2.zero;
        var ut = userObj.AddComponent<Text>();
        ut.font = Font(); ut.text = GameManager.Instance?.currentUser ?? ""; ut.fontSize = 15; ut.color = GoldColor; ut.alignment = TextAnchor.MiddleRight;
    }

    // ──────────────────── 功能入口区 ────────────────────

    private void CreateFeatureEntry(Transform parent)
    {
        // 功能入口容器 — 在 ScrollView 和 NavBar 之间
        var entryBar = NewUI("FeatureEntry", parent);
        var er = entryBar.GetComponent<RectTransform>();
        er.anchorMin = new Vector2(0.15f, 0.14f);
        er.anchorMax = new Vector2(0.85f, 0.24f);
        er.offsetMin = er.offsetMax = Vector2.zero;
        var eImg = entryBar.AddComponent<Image>();
        eImg.color = new Color(0, 0, 0, 0);
        eImg.raycastTarget = false;

        // 左侧：知识探索 小标签按钮
        var knowBtn = NewUI("KnowBtn", entryBar.transform);
        var kr = knowBtn.GetComponent<RectTransform>();
        kr.anchorMin = new Vector2(0, 0.1f);
        kr.anchorMax = new Vector2(0.47f, 0.9f);
        kr.offsetMin = kr.offsetMax = Vector2.zero;
        knowBtn.AddComponent<Image>().color = new Color(0.96f, 0.92f, 0.88f);
        var kBtn = knowBtn.AddComponent<Button>();
        kBtn.onClick.AddListener(() => { SfxClick(); SceneLoader.Instance.LoadScene(SceneNames.Knowledge); });
        // 印章小图标
        AddSealIcon("KIcon", knowBtn.transform, new Vector2(0.18f, 0.5f), 16, "知", 13);
        // 文字
        var kLabel = NewUI("KLabel", knowBtn.transform);
        var klR = kLabel.GetComponent<RectTransform>();
        klR.anchorMin = new Vector2(0.32f, 0);
        klR.anchorMax = new Vector2(0.95f, 1f);
        klR.offsetMin = klR.offsetMax = Vector2.zero;
        var kTxt = kLabel.AddComponent<Text>();
        kTxt.font = Font(); kTxt.text = "知识探索"; kTxt.fontSize = 15; kTxt.color = ZhuRed; kTxt.alignment = TextAnchor.MiddleLeft;

        // 右侧：历史故事 小标签按钮
        var evtBtn = NewUI("EvtBtn", entryBar.transform);
        var evr = evtBtn.GetComponent<RectTransform>();
        evr.anchorMin = new Vector2(0.53f, 0.1f);
        evr.anchorMax = new Vector2(1f, 0.9f);
        evr.offsetMin = evr.offsetMax = Vector2.zero;
        evtBtn.AddComponent<Image>().color = new Color(0.96f, 0.92f, 0.88f);
        var eBtn = evtBtn.AddComponent<Button>();
        eBtn.onClick.AddListener(() => { SfxClick(); SceneLoader.Instance.LoadScene(SceneNames.Event); });
        // 印章小图标
        AddSealIcon("EIcon", evtBtn.transform, new Vector2(0.18f, 0.5f), 16, "史", 13);
        // 文字
        var eLabel = NewUI("ELabel", evtBtn.transform);
        var elR = eLabel.GetComponent<RectTransform>();
        elR.anchorMin = new Vector2(0.32f, 0);
        elR.anchorMax = new Vector2(0.95f, 1f);
        elR.offsetMin = elR.offsetMax = Vector2.zero;
        var eTxt = eLabel.AddComponent<Text>();
        eTxt.font = Font(); eTxt.text = "历史故事"; eTxt.fontSize = 15; eTxt.color = new Color(0.55f, 0.38f, 0.18f); eTxt.alignment = TextAnchor.MiddleLeft;
    }

    // ──────────────────── 卷轴 ScrollView ────────────────────

    private void CreateScrollView(Transform parent)
    {
        // ScrollView 容器 — 占据标题栏和导航栏之间的区域
        var scrollObj = NewUI("ScrollView", parent);
        var sr = scrollObj.GetComponent<RectTransform>();
        sr.anchorMin = new Vector2(0.05f, 0.26f);
        sr.anchorMax = new Vector2(0.95f, 0.88f);
        sr.sizeDelta = Vector2.zero;
        var sImg = scrollObj.AddComponent<Image>(); sImg.color = new Color(0, 0, 0, 0); sImg.raycastTarget = true;
        var scrollRect = scrollObj.AddComponent<ScrollRect>();

// Viewport — 需要 Image 组件让 Mask 裁剪正常工作
        var viewport = NewUI("Viewport", scrollObj.transform);
        Stretch(viewport);
        var vpImg = viewport.AddComponent<Image>();
        vpImg.color = new Color(1, 1, 1, 1);  // 白色底（为 Mask 提供裁剪区域）
        vpImg.raycastTarget = false;
        var mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // Content
        var content = NewUI("Content", viewport.transform);
        var cr = content.GetComponent<RectTransform>();
        cr.anchorMin = new Vector2(0, 0.5f);
        cr.anchorMax = new Vector2(0, 0.5f);
        cr.pivot = new Vector2(0, 0.5f);
        cr.sizeDelta = new Vector2(0, 380);  // 高度
        var cImg = content.AddComponent<Image>(); cImg.color = new Color(0, 0, 0, 0); cImg.raycastTarget = false;
        // 水平排列
        var hlg = content.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 18;
        hlg.padding = new RectOffset(25, 25, 0, 0);
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        // 自动计算宽度
        var csf = content.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = content.GetComponent<RectTransform>();
        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.horizontal = true;
        scrollRect.vertical = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.1f;

        // ── 创建 4 张卡片 ──
        for (int i = 0; i < 4; i++)
        {
            CreateScrollCard(content.transform, i);
        }
    }

    // ──────────────────── 3D 角色展示区 ────────────────────

    private RenderTexture charRT;
    private Camera charCam;
    private GameObject charStageRoot;
    private float charWalkTimer;
    private float nextWalkDelay = 3f;

    private void CreateCharacterStage(Transform parent)
    {
        // UI 区域：在 ScrollView 下方、FeatureEntry 上方
        var stageUI = NewUI("CharStage", parent);
        var sr = stageUI.GetComponent<RectTransform>();
        sr.anchorMin = new Vector2(0.02f, 0.27f);
        sr.anchorMax = new Vector2(0.98f, 0.42f);
        sr.offsetMin = sr.offsetMax = Vector2.zero;

        // 3D 场景根对象（先创建以确定 layer）
        int charLayer = 8;
        charStageRoot = new GameObject("[CharStage]");
        charStageRoot.layer = charLayer;

        // 宣纸色地面
        var ground = GameObject.CreatePrimitive(PrimitiveType.Quad);
        ground.name = "Ground";
        ground.transform.SetParent(charStageRoot.transform);
        ground.transform.position = new Vector3(0, 0, 0);
        ground.transform.rotation = Quaternion.Euler(90, 0, 0);
        ground.transform.localScale = new Vector3(14f, 2f, 1f);
        var gMat = new Material(Shader.Find("Unlit/Color"));
        gMat.color = new Color(0.93f, 0.89f, 0.82f);
        ground.GetComponent<Renderer>().material = gMat;
        ground.layer = charLayer;
        Object.Destroy(ground.GetComponent<Collider>());

        // 创建3D角色
        Create3DCharacter(charStageRoot.transform, charLayer);

        // 计算UI区域实际像素尺寸，匹配RenderTexture宽高比
        // 先用合理尺寸创建RT，Start后用实际尺寸更新
        charRT = new RenderTexture(960, 160, 24, RenderTextureFormat.ARGB32);
        charRT.name = "CharRT";

        // 专用正交相机 — 角色总高约1.8（脚y=0 头顶y≈1.75），相机对准0.9
        var camObj = new GameObject("CharCam");
        camObj.transform.SetParent(charStageRoot.transform);
        camObj.layer = charLayer;
        charCam = camObj.AddComponent<Camera>();
        charCam.orthographic = true;
        // orthoSize=半高，角色高1.8，留边距→1.1（可见范围y: -0.2到2.0）
        charCam.orthographicSize = 1.1f;
        charCam.clearFlags = CameraClearFlags.SolidColor;
        charCam.backgroundColor = new Color(0.96f, 0.90f, 0.78f, 0f);
        charCam.targetTexture = charRT;
        charCam.cullingMask = 1 << charLayer;
        camObj.transform.position = new Vector3(0, 0.9f, -6f);
        camObj.transform.rotation = Quaternion.identity;

        // RawImage 显示3D渲染结果
        var rawImg = stageUI.AddComponent<RawImage>();
        rawImg.texture = charRT;
        rawImg.raycastTarget = false;
        rawImg.color = Color.white;

        // 启动自动走动协程
        StartCoroutine(AutoWalkCoroutine());
        // 延迟一帧后根据实际UI尺寸调整RenderTexture
        StartCoroutine(AdjustRTSize());
    }

    private IEnumerator AdjustRTSize()
    {
        yield return null; // 等一帧让Layout计算完成
        var stageUI = GameObject.Find("CharStage");
        if (stageUI != null)
        {
            var rt = stageUI.GetComponent<RectTransform>();
            float w = rt.rect.width;
            float h = rt.rect.height;
            if (w > 0 && h > 0 && charRT != null)
            {
                int rtW = Mathf.ClosestPowerOfTwo((int)w);
                int rtH = Mathf.ClosestPowerOfTwo((int)h);
                // 确保最小尺寸
                rtW = Mathf.Max(rtW, 128);
                rtH = Mathf.Max(rtH, 64);
                charRT.Release();
                charRT.width = rtW;
                charRT.height = rtH;
                charRT.Create();
                // 调整相机orthoSize匹配RT比例
                float aspect = (float)rtW / rtH;
                charCam.orthographicSize = 1.1f; // 高度方向半高
                // 调整相机宽高比不会裁剪角色（orthoSize控制垂直，宽度自动）
            }
        }
    }

    private void Create3DCharacter(Transform parent, int layer)
    {
        var charObj = new GameObject("Character");
        charObj.transform.SetParent(parent);
        charObj.transform.position = new Vector3(0, 0, 0);
        charObj.layer = layer;

        // 材质
        var bodyMat = new Material(Shader.Find("Unlit/Color")) { color = ZhuRed };
        var headMat = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.82f, 0.62f, 0.44f) };  // 暖肤色
        var legMat = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.55f, 0.35f, 0.17f) };
        var armMat = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.65f, 0.45f, 0.25f) };

        // 身体（圆柱）— 朱红色小人
        var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "Body";
        body.transform.SetParent(charObj.transform);
        body.transform.localPosition = new Vector3(0, 0.8f, 0);
        body.transform.localScale = new Vector3(0.4f, 0.45f, 0.3f);
        body.GetComponent<Renderer>().material = bodyMat;
        Object.Destroy(body.GetComponent<Collider>());
        body.layer = layer;

        // 头（球）— 暖肤色
        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(charObj.transform);
        head.transform.localPosition = new Vector3(0, 1.6f, 0);
        head.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
        head.GetComponent<Renderer>().material = headMat;
        Object.Destroy(head.GetComponent<Collider>());
        head.layer = layer;

        // 左腿
        var leftLeg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        leftLeg.name = "LeftLeg";
        leftLeg.transform.SetParent(charObj.transform);
        leftLeg.transform.localPosition = new Vector3(-0.12f, 0.2f, 0);
        leftLeg.transform.localScale = new Vector3(0.12f, 0.22f, 0.12f);
        leftLeg.GetComponent<Renderer>().material = legMat;
        Object.Destroy(leftLeg.GetComponent<Collider>());
        leftLeg.layer = layer;

        // 右腿
        var rightLeg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rightLeg.name = "RightLeg";
        rightLeg.transform.SetParent(charObj.transform);
        rightLeg.transform.localPosition = new Vector3(0.12f, 0.2f, 0);
        rightLeg.transform.localScale = new Vector3(0.12f, 0.22f, 0.12f);
        rightLeg.GetComponent<Renderer>().material = legMat;
        Object.Destroy(rightLeg.GetComponent<Collider>());
        rightLeg.layer = layer;

        // 左臂
        var leftArm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        leftArm.name = "LeftArm";
        leftArm.transform.SetParent(charObj.transform);
        leftArm.transform.localPosition = new Vector3(-0.35f, 0.85f, 0);
        leftArm.transform.localScale = new Vector3(0.08f, 0.3f, 0.08f);
        leftArm.GetComponent<Renderer>().material = armMat;
        Object.Destroy(leftArm.GetComponent<Collider>());
        leftArm.layer = layer;

        // 右臂
        var rightArm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rightArm.name = "RightArm";
        rightArm.transform.SetParent(charObj.transform);
        rightArm.transform.localPosition = new Vector3(0.35f, 0.85f, 0);
        rightArm.transform.localScale = new Vector3(0.08f, 0.3f, 0.08f);
        rightArm.GetComponent<Renderer>().material = armMat;
        Object.Destroy(rightArm.GetComponent<Collider>());
        rightArm.layer = layer;

        // 附加角色控制器
        character = charObj.AddComponent<CharacterController2D>();
    }

    private IEnumerator AutoWalkCoroutine()
    {
        yield return new WaitForSeconds(2f); // 初始等待
        while (charStageRoot != null && character != null)
        {
            if (character.GetCurrentState() == CharacterState.Idle)
            {
                charWalkTimer += Time.deltaTime;
                if (charWalkTimer >= nextWalkDelay)
                {
                    // 随机走动到 [-3, 3] 范围
                    float targetX = Random.Range(-3f, 3f);
                    character.WalkTo(new Vector3(targetX, 0, 0));
                    charWalkTimer = 0f;
                    nextWalkDelay = Random.Range(2f, 5f);
                }
            }
            else
            {
                charWalkTimer = 0f;
            }
            yield return null;
        }
    }

    private void CreateScrollCard(Transform parent, int idx)
    {
        string cat = Categories[idx];

        // 外层：绫布边框（淡灰装裱边框）
        var outer = NewUI($"CardOuter_{cat}", parent);
        var le = outer.AddComponent<LayoutElement>();
        le.preferredWidth = 220;
        le.preferredHeight = 320;
        var or = outer.GetComponent<RectTransform>();
        // 淡灰边框色 — raycastTarget 必须为 true，Button 才能接收点击
        var outImg = outer.AddComponent<Image>();
        outImg.color = new Color(0.82f, 0.80f, 0.72f);  // 绫布色
        outImg.raycastTarget = true;

        // 内层：宣纸白底（margin 效果）
        var inner = NewUI("CardInner", outer.transform);
        var ir = inner.GetComponent<RectTransform>();
        ir.anchorMin = new Vector2(0.04f, 0.04f);
        ir.anchorMax = new Vector2(0.96f, 0.96f);
        ir.offsetMin = ir.offsetMax = Vector2.zero;
        var inImg = inner.AddComponent<Image>();
        inImg.color = XuanPaper;
        inImg.raycastTarget = false;

        // 印章图标（品类字）— 使用品类对应颜色
        var sealObj = AddSealIcon("Icon", inner.transform, new Vector2(0.5f, 0.72f), 32, CategoryIcons[idx], 28);
        sealObj.GetComponent<Image>().color = CategoryColors[idx];

        // 品类名
        var nameObj = NewUI("Name", inner.transform);
        var nr = nameObj.GetComponent<RectTransform>();
        nr.anchorMin = nr.anchorMax = new Vector2(0.5f, 0.42f);
        nr.sizeDelta = new Vector2(180, 32);
        var nm = nameObj.AddComponent<Text>();
        nm.font = Font(); nm.text = cat; nm.fontSize = 24; nm.color = CategoryColors[idx]; nm.alignment = TextAnchor.MiddleCenter;

        // 描述
        var descObj = NewUI("Desc", inner.transform);
        var dr = descObj.GetComponent<RectTransform>();
        dr.anchorMin = dr.anchorMax = new Vector2(0.5f, 0.25f);
        dr.sizeDelta = new Vector2(190, 22);
        var dt = descObj.AddComponent<Text>();
        dt.font = Font(); dt.text = CategoryDescs[idx]; dt.fontSize = 14; dt.color = new Color(0.35f, 0.35f, 0.35f); dt.alignment = TextAnchor.MiddleCenter;

        // 按钮（整张卡片可点击）
        var btn = outer.AddComponent<Button>();
        btn.onClick.AddListener(() => OnCardClicked(cat, outer.transform));
    }

    private void OnCardClicked(string category, Transform cardTransform)
    {
        SfxClick();
        // 角色互动
        if (character != null) character.Interact();
        // 点击反馈：放大弹回
        StartCoroutine(CardClickFeedback(cardTransform));
        // 跳转
        PlayerPrefs.SetString("CurrentCategory", category);
        PlayerPrefs.Save();
        SceneLoader.Instance.LoadScene(SceneNames.Exhibit);
    }

    private System.Collections.IEnumerator CardClickFeedback(Transform card)
    {
        Vector3 orig = card.localScale;
        Vector3 target = orig * 1.05f;
        float duration = 0.12f;

        // 放大
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            card.localScale = Vector3.Lerp(orig, target, Mathf.Clamp01(t / duration));
            yield return null;
        }
        // 弹回
        t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            card.localScale = Vector3.Lerp(target, orig, Mathf.Clamp01(t / duration));
            yield return null;
        }
        card.localScale = orig;
    }

    // ──────────────────── 底部导航栏 ────────────────────

    // 竹简色系
    private static readonly Color BambooDark  = new Color(0.35f, 0.25f, 0.15f);   // 深竹棕
    private static readonly Color BambooLight = new Color(0.55f, 0.42f, 0.28f);   // 浅竹棕
    private static readonly Color BambooGloss = new Color(0.68f, 0.55f, 0.38f);   // 竹光面
    private static readonly Color BambooTwine = new Color(0.45f, 0.32f, 0.18f);   // 麻绳色

    private void CreateNavBar(Transform parent)
    {
        // 竹简底栏 — 整体深棕底
        var nav = AnchorBottom("NavBar", parent, 52);
        var nImg = nav.AddComponent<Image>(); nImg.color = BambooDark; nImg.raycastTarget = false;

        // 上下朱红细线（竹简端头装饰）
        AddBambooEdgeLines(nav);

        string[] navNames = { "背包", "设置", "帮助", "退出" };
        Color[] navAccents = { JadeGreen, GoldColor, ZhuRed, new Color(0.5f, 0.5f, 0.5f) };
        System.Action[] navActions = {
            () => { SfxClick(); if (backpackPanel == null) { CreateBackpackPanel(); return; } backpackPanel.SetActive(!backpackPanel.activeSelf); },
            () => { SfxClick(); TogglePanel(settingsPanel); },
            () => { SfxClick(); TogglePanel(helpPanel); },
            () => { SfxClick(); GameManager.Instance.Logout(); SceneLoader.Instance.LoadScene(SceneNames.Login); }
        };

        for (int i = 0; i < 4; i++)
        {
            // 每个竹简片
            var bo = NewUI($"Nav_{navNames[i]}", nav.transform);
            var br = bo.GetComponent<RectTransform>();
            br.anchorMin = new Vector2(i / 4f, 0); br.anchorMax = new Vector2((i + 1) / 4f, 1);
            br.sizeDelta = Vector2.zero;

            // 竹片底色：中间亮两侧暗（模拟竹片弧面反光）
            var bImg = bo.AddComponent<Image>();
            bImg.color = BambooGloss; bImg.raycastTarget = true;

            // 竹片左侧深色竖边（模拟竹节缝隙）
            if (i > 0)
            {
                var divider = NewUI($"Divider_{i}", nav.transform);
                var dr = divider.GetComponent<RectTransform>();
                dr.anchorMin = new Vector2(i * 0.25f, 0.1f);
                dr.anchorMax = new Vector2(i * 0.25f, 0.9f);
                dr.sizeDelta = new Vector2(2, 0);
                var dImg = divider.AddComponent<Image>();
                dImg.color = BambooTwine; dImg.raycastTarget = false;
            }

            var nb = bo.AddComponent<Button>(); int idx = i; nb.onClick.AddListener(() => navActions[idx]());

            // 顶部小圆点指示器（各按钮的主题色）
            var dot = NewUI("Dot", bo.transform);
            var dotR = dot.GetComponent<RectTransform>();
            dotR.anchorMin = dotR.anchorMax = new Vector2(0.5f, 1f);
            dotR.pivot = new Vector2(0.5f, 1f);
            dotR.sizeDelta = new Vector2(6, 6);
            dotR.anchoredPosition = new Vector2(0, -4);
            var dotImg = dot.AddComponent<Image>(); dotImg.color = navAccents[i]; dotImg.raycastTarget = false;

            // 文字
            var to = NewUI("T", bo.transform); Stretch(to);
            var nt = to.AddComponent<Text>();
            nt.font = Font(); nt.text = navNames[i]; nt.fontSize = 16; nt.color = InkBlack; nt.alignment = TextAnchor.MiddleCenter;
        }
    }

    /// <summary>
    /// 竹简端头朱红装饰线
    /// </summary>
    private void AddBambooEdgeLines(GameObject nav)
    {
        // 上线
        var top = NewUI("TopEdge", nav.transform);
        var tr = top.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0, 1); tr.anchorMax = new Vector2(1, 1);
        tr.pivot = new Vector2(0.5f, 1f);
        tr.sizeDelta = new Vector2(0, 2);
        var tImg = top.AddComponent<Image>(); tImg.color = ZhuRed; tImg.raycastTarget = false;

        // 下线
        var bot = NewUI("BotEdge", nav.transform);
        var br = bot.GetComponent<RectTransform>();
        br.anchorMin = new Vector2(0, 0); br.anchorMax = new Vector2(1, 0);
        br.pivot = new Vector2(0.5f, 0f);
        br.sizeDelta = new Vector2(0, 2);
        var bImg = bot.AddComponent<Image>(); bImg.color = ZhuRed; bImg.raycastTarget = false;
    }

    // ──────────────────── 设置面板 ────────────────────

    private static readonly Color[] AvatarColors = {
        new Color(0.76f, 0.21f, 0.19f),   // 朱红
        new Color(0.17f, 0.17f, 0.17f),   // 墨色
        new Color(0.26f, 0.47f, 0.72f),   // 青蓝
        new Color(0.72f, 0.53f, 0.19f),   // 琥珀
        new Color(0.18f, 0.48f, 0.43f),   // 翡翠
        new Color(0.83f, 0.65f, 0.27f),   // 金色
    };
    private static readonly string[] AvatarChars = { "遗", "韵", "雅", "风", "翠", "锦" };

    private InputField oldPwdInput, newPwdInput, confirmPwdInput;

    private GameObject CreateSettingsPanel(Transform parent)
    {
        var overlay = NewUI("SettingsOverlay", parent);
        Stretch(overlay);
        var overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = new Color(0, 0, 0, 0.85f);
        // 遮罩层不拦截点击，只有 X 按钮可关闭
        overlayImg.raycastTarget = false;
        overlay.SetActive(false);

        // 面板 — Image 必须拦截 raycast，否则点击会穿透到遮罩层
        var panel = NewUI("Panel", overlay.transform);
        var pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(440, 700);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = XuanPaper;
        panelImg.raycastTarget = true;

        // 顶部朱红装饰线
        var topLine = NewUI("TopLine", panel.transform);
        var tlr = topLine.GetComponent<RectTransform>();
        tlr.anchorMin = new Vector2(0, 1); tlr.anchorMax = new Vector2(1, 1);
        tlr.pivot = new Vector2(0.5f, 1f); tlr.sizeDelta = new Vector2(0, 3);
        topLine.AddComponent<Image>().color = ZhuRed;

        // 标题
        var titleObj = NewUI("Title", panel.transform);
        var tr = titleObj.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0, 1); tr.anchorMax = new Vector2(1, 1);
        tr.pivot = new Vector2(0.5f, 1f); tr.sizeDelta = new Vector2(0, 45);
        var tt = titleObj.AddComponent<Text>();
        tt.font = Font(); tt.text = "系统设置"; tt.fontSize = 24; tt.color = ZhuRed; tt.alignment = TextAnchor.MiddleCenter;

        // 关闭按钮 — 右上角标题行内
        var xObj = NewUI("X", panel.transform);
        var xr = xObj.GetComponent<RectTransform>();
        xr.anchorMin = xr.anchorMax = new Vector2(1, 1);
        xr.pivot = new Vector2(1, 1);
        xr.sizeDelta = new Vector2(44, 44);
        xr.anchoredPosition = new Vector2(-6, -28);
        xObj.AddComponent<Image>().color = ZhuRed;
        xObj.AddComponent<Button>().onClick.AddListener(() => overlay.SetActive(false));
        var xTxt = NewUI("XT", xObj.transform); Stretch(xTxt);
        var xt = xTxt.AddComponent<Text>();
        xt.font = Font(); xt.text = "✕"; xt.fontSize = 26; xt.color = Color.white; xt.alignment = TextAnchor.MiddleCenter;

        // ── 内容区（可滚动） ──
        var content = NewUI("Content", panel.transform);
        var cr = content.GetComponent<RectTransform>();
        cr.anchorMin = Vector2.zero; cr.anchorMax = new Vector2(1, 1);
        cr.offsetMin = new Vector2(20, 20); cr.offsetMax = new Vector2(-20, -55);

        float y = -15f;

        // ── 音量滑块 ──
        y = AddSettingSectionTitle(content.transform, "音量调节", y);
        y = AddVolumeSlider(content.transform, y);

        y += 20f;

        // ── 主题切换 ──
        y = AddSettingSectionTitle(content.transform, "主题风格", y);
        y = AddThemeSelector(content.transform, y);

        y += 20f;

        // ── 头像编辑 ──
        y = AddSettingSectionTitle(content.transform, "头像选择", y);
        y = AddAvatarSelector(content.transform, y);

        y += 20f;

        // ── 修改密码 ──
        y = AddSettingSectionTitle(content.transform, "修改密码", y);
        y = AddPasswordChangeSection(content.transform, y);

        return overlay;
    }

    private float AddSettingSectionTitle(Transform parent, string text, float y)
    {
        var lbl = NewUI("SecLbl", parent);
        var r = lbl.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(1, 1);
        r.pivot = new Vector2(0, 1f); r.sizeDelta = new Vector2(0, 28);
        r.anchoredPosition = new Vector2(0, y);
        var t = lbl.AddComponent<Text>();
        t.font = Font(); t.text = "— " + text + " —"; t.fontSize = 16; t.color = GoldColor; t.alignment = TextAnchor.MiddleCenter;
        return y - 36f;
    }

    private float AddVolumeSlider(Transform parent, float y)
    {
        // 滑块行容器
        var row = NewUI("VolRow", parent);
        var rr = row.GetComponent<RectTransform>();
        rr.anchorMin = new Vector2(0, 1); rr.anchorMax = new Vector2(1, 1);
        rr.pivot = new Vector2(0, 1f); rr.sizeDelta = new Vector2(0, 40);
        rr.anchoredPosition = new Vector2(0, y);

        // 左标签
        var lblObj = NewUI("Lbl", row.transform);
        var lr = lblObj.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero; lr.anchorMax = new Vector2(0.15f, 1f);
        lr.offsetMin = lr.offsetMax = Vector2.zero;
        var lt = lblObj.AddComponent<Text>();
        lt.font = Font(); lt.text = "音量"; lt.fontSize = 15; lt.color = InkBlack; lt.alignment = TextAnchor.MiddleRight;

        // 滑块
        var sliderObj = NewUI("Slider", row.transform);
        var sr = sliderObj.GetComponent<RectTransform>();
        sr.anchorMin = new Vector2(0.18f, 0.15f); sr.anchorMax = new Vector2(0.75f, 0.85f);
        sr.offsetMin = sr.offsetMax = Vector2.zero;

        // Slider 背景
        var bgObj = NewUI("BG", sliderObj.transform);
        Stretch(bgObj);
        bgObj.AddComponent<Image>().color = new Color(0.85f, 0.82f, 0.75f);

        // Fill 区域
        var fillArea = NewUI("FillArea", sliderObj.transform);
        var far = fillArea.GetComponent<RectTransform>();
        far.anchorMin = Vector2.zero; far.anchorMax = Vector2.one;
        far.offsetMin = far.offsetMax = Vector2.zero;
        var fill = NewUI("Fill", fillArea.transform);
        Stretch(fill);
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = ZhuRed;

        // Handle
        var handleArea = NewUI("HandleArea", sliderObj.transform);
        Stretch(handleArea);
        var handle = NewUI("Handle", handleArea.transform);
        var hr = handle.GetComponent<RectTransform>();
        hr.anchorMin = hr.anchorMax = new Vector2(0.5f, 0.5f);
        hr.sizeDelta = new Vector2(24, 24);
        var handleImg = handle.AddComponent<Image>();
        handleImg.color = ZhuRed;

        var slider = sliderObj.AddComponent<Slider>();
        slider.targetGraphic = handleImg;
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = handle.GetComponent<RectTransform>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = GameManager.Instance.volume;

        // 百分比文字
        var pctObj = NewUI("Pct", row.transform);
        var pctR = pctObj.GetComponent<RectTransform>();
        pctR.anchorMin = new Vector2(0.78f, 0); pctR.anchorMax = new Vector2(1f, 1f);
        pctR.offsetMin = pctR.offsetMax = Vector2.zero;
        var pctTxt = pctObj.AddComponent<Text>();
        pctTxt.font = Font(); pctTxt.fontSize = 15; pctTxt.color = InkBlack; pctTxt.alignment = TextAnchor.MiddleCenter;

        void OnVolChanged(float v)
        {
            GameManager.Instance.volume = v;
            pctTxt.text = Mathf.RoundToInt(v * 100) + "%";
            if (AudioManager.Instance != null) AudioManager.Instance.UpdateVolume();
        }
        slider.onValueChanged.AddListener(OnVolChanged);
        OnVolChanged(slider.value);

        return y - 50f;
    }

    private float AddAvatarSelector(Transform parent, float y)
    {
        var row = NewUI("AvatarRow", parent);
        var rr = row.GetComponent<RectTransform>();
        rr.anchorMin = new Vector2(0, 1); rr.anchorMax = new Vector2(1, 1);
        rr.pivot = new Vector2(0, 1f); rr.sizeDelta = new Vector2(0, 72);
        rr.anchoredPosition = new Vector2(0, y);

        int currentAvatar = PlayerPrefs.GetInt($"User_{GameManager.Instance.currentUser}_Avatar", 0);

        for (int i = 0; i < AvatarColors.Length; i++)
        {
            var avatar = NewUI($"Av{i}", row.transform);
            var ar = avatar.GetComponent<RectTransform>();
            float cellW = 1f / AvatarColors.Length;
            ar.anchorMin = new Vector2(i * cellW + 0.02f, 0.08f);
            ar.anchorMax = new Vector2((i + 1) * cellW - 0.02f, 0.92f);
            ar.offsetMin = ar.offsetMax = Vector2.zero;

            avatar.AddComponent<Image>().color = AvatarColors[i];
            var avBtn = avatar.AddComponent<Button>();
            int idx = i;
            avBtn.onClick.AddListener(() => OnAvatarSelected(idx));

            var aTxt = NewUI("T", avatar.transform); Stretch(aTxt);
            var at = aTxt.AddComponent<Text>();
            at.font = Font(); at.text = AvatarChars[i]; at.fontSize = 20; at.color = Color.white; at.alignment = TextAnchor.MiddleCenter;

            // 当前选中项加金色边框
            if (i == currentAvatar)
            {
                var border = NewUI("Border", avatar.transform);
                Stretch(border);
                var bImg = border.AddComponent<Image>();
                bImg.color = GoldColor; bImg.raycastTarget = false;
                var bRect = border.GetComponent<RectTransform>();
                bRect.anchorMin = Vector2.zero; bRect.anchorMax = Vector2.one;
                bRect.offsetMin = new Vector2(-3, -3); bRect.offsetMax = new Vector2(3, 3);
                border.transform.SetAsFirstSibling();
            }
        }

        return y - 84f;
    }

    private static readonly string[] ThemeNames = { "default", "classic", "minimal" };
    private static readonly string[] ThemeLabels = { "默认", "古典", "简约" };
    private static readonly Color[] ThemePreviewColors = {
        new Color(0.96f, 0.90f, 0.78f),  // 宣纸白
        new Color(0.22f, 0.17f, 0.12f),  // 深褐
        new Color(0.97f, 0.97f, 0.97f),  // 浅灰
    };

    private float AddThemeSelector(Transform parent, float y)
    {
        var row = NewUI("ThemeRow", parent);
        var rr = row.GetComponent<RectTransform>();
        rr.anchorMin = new Vector2(0, 1); rr.anchorMax = new Vector2(1, 1);
        rr.pivot = new Vector2(0, 1f); rr.sizeDelta = new Vector2(0, 56);
        rr.anchoredPosition = new Vector2(0, y);

        string current = GameManager.Instance.themeStyle;

        for (int i = 0; i < ThemeNames.Length; i++)
        {
            var btn = NewUI($"Theme_{ThemeNames[i]}", row.transform);
            var br = btn.GetComponent<RectTransform>();
            float cellW = 1f / ThemeNames.Length;
            br.anchorMin = new Vector2(i * cellW + 0.03f, 0.08f);
            br.anchorMax = new Vector2((i + 1) * cellW - 0.03f, 0.92f);
            br.offsetMin = br.offsetMax = Vector2.zero;

            btn.AddComponent<Image>().color = ThemePreviewColors[i];
            var button = btn.AddComponent<Button>();
            int idx = i;
            button.onClick.AddListener(() => OnThemeSelected(idx));

            var tObj = NewUI("T", btn.transform); Stretch(tObj);
            var t = tObj.AddComponent<Text>();
            t.font = Font(); t.text = ThemeLabels[i]; t.fontSize = 16;
            t.color = i == 1 ? new Color(0.90f, 0.72f, 0.30f) : InkBlack;
            t.alignment = TextAnchor.MiddleCenter;

            // 选中高亮边框
            if (ThemeNames[i] == current)
            {
                var border = NewUI("Border", btn.transform);
                Stretch(border);
                var bImg = border.AddComponent<Image>();
                bImg.color = ZhuRed; bImg.raycastTarget = false;
                var bRect = border.GetComponent<RectTransform>();
                bRect.anchorMin = Vector2.zero; bRect.anchorMax = Vector2.one;
                bRect.offsetMin = new Vector2(-3, -3); bRect.offsetMax = new Vector2(3, 3);
                border.transform.SetAsFirstSibling();
            }
        }

        return y - 68f;
    }

    private void OnThemeSelected(int idx)
    {
        SfxClick();
        GameManager.Instance.themeStyle = ThemeNames[idx];
        GameManager.Instance.SaveSettings();

        // 刷新选中高亮
        RefreshThemeHighlight(idx);

        ShowToast("主题已切换，返回主页后生效", JadeGreen);
    }

    private void RefreshThemeHighlight(int selectedIdx)
    {
        if (settingsPanel == null) return;
        var themeRow = settingsPanel.transform.Find("Panel/Content/ThemeRow");
        if (themeRow == null) return;

        for (int i = 0; i < ThemeNames.Length; i++)
        {
            var btn = themeRow.Find($"Theme_{ThemeNames[i]}");
            if (btn == null) continue;
            var oldBorder = btn.Find("Border");
            if (oldBorder != null) Destroy(oldBorder.gameObject);
            if (i == selectedIdx)
            {
                var border = NewUI("Border", btn);
                Stretch(border);
                var bImg = border.AddComponent<Image>();
                bImg.color = ZhuRed; bImg.raycastTarget = false;
                var bRect = border.GetComponent<RectTransform>();
                bRect.anchorMin = Vector2.zero; bRect.anchorMax = Vector2.one;
                bRect.offsetMin = new Vector2(-3, -3); bRect.offsetMax = new Vector2(3, 3);
                border.transform.SetAsFirstSibling();
            }
        }
    }

    private void OnAvatarSelected(int idx)
    {
        SfxClick();
        PlayerPrefs.SetInt($"User_{GameManager.Instance.currentUser}_Avatar", idx);
        PlayerPrefs.Save();

        // 刷新头像选中高亮
        RefreshAvatarHighlight(idx);

        // 更新 header 头像
        RefreshHeaderAvatar(idx);

        ShowToast("头像已更新", JadeGreen);
    }

    private void RefreshAvatarHighlight(int selectedIdx)
    {
        if (settingsPanel == null) return;
        var avatarRow = settingsPanel.transform.Find("Panel/Content/AvatarRow");
        if (avatarRow == null) return;

        for (int i = 0; i < AvatarColors.Length; i++)
        {
            var av = avatarRow.Find($"Av{i}");
            if (av == null) continue;
            // 移除旧边框
            var oldBorder = av.Find("Border");
            if (oldBorder != null) Destroy(oldBorder.gameObject);
            // 选中项加金色边框
            if (i == selectedIdx)
            {
                var border = NewUI("Border", av);
                Stretch(border);
                var bImg = border.AddComponent<Image>();
                bImg.color = GoldColor; bImg.raycastTarget = false;
                var bRect = border.GetComponent<RectTransform>();
                bRect.anchorMin = Vector2.zero; bRect.anchorMax = Vector2.one;
                bRect.offsetMin = new Vector2(-3, -3); bRect.offsetMax = new Vector2(3, 3);
                border.transform.SetAsFirstSibling();
            }
        }
    }

    private void RefreshHeaderAvatar(int idx)
    {
        var headerAvatar = GameObject.Find("Avatar");
        if (headerAvatar != null)
        {
            headerAvatar.GetComponent<Image>().color = AvatarColors[Mathf.Clamp(idx, 0, AvatarColors.Length - 1)];
            var t = headerAvatar.transform.Find("T")?.GetComponent<Text>();
            if (t != null) t.text = AvatarChars[Mathf.Clamp(idx, 0, AvatarChars.Length - 1)];
        }
    }

    private float AddPasswordChangeSection(Transform parent, float y)
    {
        // 旧密码
        y = AddPwdField("OldPwd", parent, y, "旧密码", ref oldPwdInput, "请输入旧密码", true);
        y -= 6f;

        // 新密码
        y = AddPwdField("NewPwd", parent, y, "新密码", ref newPwdInput, "请输入新密码（至少4位）", true);
        y -= 6f;

        // 确认密码
        y = AddPwdField("ConfirmPwd", parent, y, "确认新密码", ref confirmPwdInput, "请再次输入新密码", true);
        y -= 16f;

        // 确认修改按钮
        var btnObj = NewUI("ChangePwdBtn", parent);
        var br = btnObj.GetComponent<RectTransform>();
        br.anchorMin = br.anchorMax = new Vector2(0.5f, 1f);
        br.pivot = new Vector2(0.5f, 1f);
        br.sizeDelta = new Vector2(200, 44);
        br.anchoredPosition = new Vector2(0, y);
        btnObj.AddComponent<Image>().color = ZhuRed;
        btnObj.AddComponent<Button>().onClick.AddListener(OnChangePassword);
        var bTxt = NewUI("T", btnObj.transform); Stretch(bTxt);
        var bt = bTxt.AddComponent<Text>();
        bt.font = Font(); bt.text = "确认修改"; bt.fontSize = 18; bt.color = Color.white; bt.alignment = TextAnchor.MiddleCenter;

        return y - 54f;
    }

    private float AddPwdField(string name, Transform parent, float y, string label, ref InputField inputRef, string placeholder, bool isPassword)
    {
        // 标签
        var lbl = NewUI(name + "Lbl", parent);
        var lr = lbl.GetComponent<RectTransform>();
        lr.anchorMin = new Vector2(0, 1); lr.anchorMax = new Vector2(1, 1);
        lr.pivot = new Vector2(0, 1f); lr.sizeDelta = new Vector2(0, 22);
        lr.anchoredPosition = new Vector2(0, y);
        var lt = lbl.AddComponent<Text>();
        lt.font = Font(); lt.text = label; lt.fontSize = 14; lt.color = InkBlack; lt.alignment = TextAnchor.MiddleLeft;
        y -= 26f;

        // 输入框（手工定位，避免 AddInputField 的 anchor 冲突）
        var field = NewUI(name, parent);
        var fr = field.GetComponent<RectTransform>();
        fr.anchorMin = new Vector2(0, 1); fr.anchorMax = new Vector2(1, 1);
        fr.pivot = new Vector2(0, 1f); fr.sizeDelta = new Vector2(0, 38);
        fr.anchoredPosition = new Vector2(0, y);
        field.AddComponent<Image>().color = new Color(XuanPaper.r, XuanPaper.g, XuanPaper.b, 0.5f);

        // 底部朱红线
        var underline = NewUI("Line", field.transform);
        var ulr = underline.GetComponent<RectTransform>();
        ulr.anchorMin = new Vector2(0, 0); ulr.anchorMax = new Vector2(1, 0);
        ulr.pivot = new Vector2(0.5f, 0f);
        ulr.sizeDelta = new Vector2(0, 2);
        var ulImg = underline.AddComponent<Image>(); ulImg.color = ZhuRed; ulImg.raycastTarget = false;

        // 文本
        var tObj = NewUI("Text", field.transform);
        var tr = tObj.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(12, 6); tr.offsetMax = new Vector2(-12, -4);
        var txt = tObj.AddComponent<Text>();
        txt.font = Font(); txt.fontSize = 16; txt.color = InkBlack; txt.alignment = TextAnchor.MiddleLeft;

        // 占位符
        var pObj = NewUI("Placeholder", field.transform);
        var pr2 = pObj.GetComponent<RectTransform>();
        pr2.anchorMin = Vector2.zero; pr2.anchorMax = Vector2.one;
        pr2.offsetMin = new Vector2(12, 6); pr2.offsetMax = new Vector2(-12, -4);
        var pht = pObj.AddComponent<Text>();
        pht.font = Font(); pht.text = placeholder; pht.fontSize = 14;
        pht.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); pht.alignment = TextAnchor.MiddleLeft;

        var inf = field.AddComponent<InputField>();
        inf.textComponent = txt;
        inf.placeholder = pht;
        if (isPassword) inf.contentType = InputField.ContentType.Password;

        inputRef = inf;
        return y - 44f;
    }

    private void OnChangePassword()
    {
        string oldPwd = oldPwdInput.text;
        string newPwd = newPwdInput.text;
        string confirmPwd = confirmPwdInput.text;

        // 全部为空则跳过
        if (string.IsNullOrEmpty(oldPwd) && string.IsNullOrEmpty(newPwd) && string.IsNullOrEmpty(confirmPwd))
            return;

        if (string.IsNullOrEmpty(oldPwd))
        { SfxToast(); ShowToast("请输入旧密码", ZhuRed); return; }

        string storedPwd = PlayerPrefs.GetString($"User_{GameManager.Instance.currentUser}_Password", "");
        if (GameManager.EncryptPassword(oldPwd) != storedPwd)
        { SfxToast(); ShowToast("旧密码错误", ZhuRed); return; }

        if (string.IsNullOrEmpty(newPwd) || string.IsNullOrEmpty(confirmPwd))
        { SfxToast(); ShowToast("请输入新密码", ZhuRed); return; }

        if (newPwd.Length < 4)
        { SfxToast(); ShowToast("新密码至少4位", ZhuRed); return; }

        if (newPwd != confirmPwd)
        { SfxToast(); ShowToast("两次密码不一致", ZhuRed); return; }

        PlayerPrefs.SetString($"User_{GameManager.Instance.currentUser}_Password", GameManager.EncryptPassword(newPwd));
        PlayerPrefs.Save();
        oldPwdInput.text = "";
        newPwdInput.text = "";
        confirmPwdInput.text = "";
        ShowToast("密码修改成功", JadeGreen);
    }

    private void TogglePanel(GameObject p) { if (p != null) p.SetActive(!p.activeSelf); }

    private void OnDestroy()
    {
        if (charRT != null) { charRT.Release(); charRT = null; }
        if (charStageRoot != null) Destroy(charStageRoot);
    }

    private void CreateBackpackPanel()
    {
        var items = BackpackManager.Instance.GetBackpackItems(GameManager.Instance.currentUser);
        if (backpackPanel != null) Destroy(backpackPanel);

        backpackPanel = MakeOverlay(rootT, "我的背包", "", JadeGreen);
        backpackPanel.SetActive(true);

        var panel = backpackPanel.transform.Find("Panel");
        var contentArea = panel.Find("C");
        if (contentArea == null) return;

        Object.Destroy(contentArea.GetComponent<Text>());

        // ── 搜索框 ──
        var searchRow = NewUI("SearchRow", contentArea);
        var srR = searchRow.GetComponent<RectTransform>();
        srR.anchorMin = new Vector2(0, 1); srR.anchorMax = new Vector2(1, 1);
        srR.pivot = new Vector2(0, 1f);
        srR.sizeDelta = new Vector2(0, 36);
        srR.anchoredPosition = Vector2.zero;

        var searchBg = searchRow.AddComponent<Image>();
        searchBg.color = new Color(XuanPaper.r, XuanPaper.g, XuanPaper.b, 0.8f);
        searchBg.raycastTarget = true;

        var searchInputObj = NewUI("SearchInput", searchRow.transform);
        var siR = searchInputObj.GetComponent<RectTransform>();
        siR.anchorMin = Vector2.zero; siR.anchorMax = Vector2.one;
        siR.offsetMin = new Vector2(8, 4); siR.offsetMax = new Vector2(-8, -4);

        var searchTxt = NewUI("Text", searchInputObj.transform);
        var stR = searchTxt.GetComponent<RectTransform>();
        stR.anchorMin = Vector2.zero; stR.anchorMax = Vector2.one;
        stR.offsetMin = new Vector2(8, 2); stR.offsetMax = new Vector2(-4, -2);
        var sTxt = searchTxt.AddComponent<Text>();
        sTxt.font = Font(); sTxt.fontSize = 16; sTxt.color = InkBlack; sTxt.alignment = TextAnchor.MiddleLeft;

        var searchPh = NewUI("Placeholder", searchInputObj.transform);
        var spR = searchPh.GetComponent<RectTransform>();
        spR.anchorMin = Vector2.zero; spR.anchorMax = Vector2.one;
        spR.offsetMin = new Vector2(8, 2); spR.offsetMax = new Vector2(-4, -2);
        var sPh = searchPh.AddComponent<Text>();
        sPh.font = Font(); sPh.text = "🔍 搜索展品..."; sPh.fontSize = 14;
        sPh.color = new Color(0.5f, 0.5f, 0.5f, 0.6f); sPh.alignment = TextAnchor.MiddleLeft;

        backpackSearchInput = searchInputObj.AddComponent<InputField>();
        backpackSearchInput.textComponent = sTxt;
        backpackSearchInput.placeholder = sPh;
        backpackSearchInput.onValueChanged.AddListener(_ => RefreshBackpackList());

        // ── 品类筛选按钮行 ──
        var filterRow = NewUI("FilterRow", contentArea);
        var frR = filterRow.GetComponent<RectTransform>();
        frR.anchorMin = new Vector2(0, 1); frR.anchorMax = new Vector2(1, 1);
        frR.pivot = new Vector2(0, 1f);
        frR.sizeDelta = new Vector2(0, 30);
        frR.anchoredPosition = new Vector2(0, -38);

        var flHlg = filterRow.AddComponent<HorizontalLayoutGroup>();
        flHlg.childAlignment = TextAnchor.MiddleCenter;
        flHlg.spacing = 4;
        flHlg.padding = new RectOffset(4, 4, 2, 2);
        flHlg.childForceExpandWidth = true;
        flHlg.childForceExpandHeight = false;

        string[] filterNames = { "全部", "瓷器", "剪纸", "书法", "乐器" };
        for (int i = 0; i < filterNames.Length; i++)
        {
            var fBtn = NewUI($"Filter_{i}", filterRow.transform);
            var le = fBtn.AddComponent<LayoutElement>();
            le.preferredHeight = 26;
            var fImg = fBtn.AddComponent<Image>();
            fImg.color = (i == 0) ? ZhuRed : new Color(0.85f, 0.82f, 0.75f);
            var fb = fBtn.AddComponent<Button>();
            int idx = i - 1; // -1=全部
            fb.onClick.AddListener(() => { backpackCategoryFilter = idx; RefreshBackpackList(); });
            var fTxtObj = NewUI("T", fBtn.transform); Stretch(fTxtObj);
            var ft = fTxtObj.AddComponent<Text>();
            ft.font = Font(); ft.text = filterNames[i]; ft.fontSize = 13;
            ft.color = (i == 0) ? Color.white : InkBlack; ft.alignment = TextAnchor.MiddleCenter;
        }

        // ── 列表容器 ──
        var listArea = NewUI("ListArea", contentArea);
        var laR = listArea.GetComponent<RectTransform>();
        laR.anchorMin = Vector2.zero; laR.anchorMax = new Vector2(1, 1);
        laR.offsetMin = Vector2.zero; laR.offsetMax = new Vector2(0, -68);

        // 初始渲染列表
        backpackCategoryFilter = -1;
        if (backpackSearchInput != null) backpackSearchInput.text = "";
        RefreshBackpackList();
    }

    private void RefreshBackpackList()
    {
        if (backpackPanel == null) return;
        var contentArea = backpackPanel.transform.Find("Panel/C");
        if (contentArea == null) return;
        var listArea = contentArea.Find("ListArea");
        if (listArea == null) return;

        // 清空旧列表
        for (int c = listArea.childCount - 1; c >= 0; c--)
            Destroy(listArea.GetChild(c).gameObject);

        var allItems = BackpackManager.Instance.GetBackpackItems(GameManager.Instance.currentUser);
        string search = backpackSearchInput != null ? backpackSearchInput.text.Trim() : "";

        // 过滤
        var filtered = new List<string>();
        foreach (var id in allItems)
        {
            var data = GameManager.Instance.GetExhibit(id);
            if (data == null) continue;

            // 品类筛选
            if (backpackCategoryFilter >= 0)
            {
                string targetCat = Categories[backpackCategoryFilter];
                if (data.category != targetCat) continue;
            }

            // 搜索筛选
            if (!string.IsNullOrEmpty(search))
            {
                if (data.name.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) < 0 &&
                    data.category.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
            }

            filtered.Add(id);
        }

        // 更新筛选按钮样式
        var filterRow = contentArea.Find("FilterRow");
        if (filterRow != null)
        {
            for (int i = 0; i < filterRow.childCount; i++)
            {
                var fBtn = filterRow.GetChild(i);
                var img = fBtn.GetComponent<Image>();
                var txt = fBtn.Find("T")?.GetComponent<Text>();
                bool selected = (i - 1) == backpackCategoryFilter;
                if (img != null) img.color = selected ? ZhuRed : new Color(0.85f, 0.82f, 0.75f);
                if (txt != null) txt.color = selected ? Color.white : InkBlack;
            }
        }

        if (filtered.Count == 0)
        {
            var empty = NewUI("Empty", listArea);
            Stretch(empty);
            var et = empty.AddComponent<Text>();
            et.font = Font(); et.text = string.IsNullOrEmpty(search) && backpackCategoryFilter < 0
                ? "暂无收藏展品\n浏览展品时点击「收藏」即可添加到背包"
                : "没有匹配的展品";
            et.fontSize = 16; et.color = new Color(0.5f, 0.5f, 0.5f); et.alignment = TextAnchor.MiddleCenter;
            return;
        }

        float rowH = 0.12f;
        for (int i = 0; i < filtered.Count; i++)
        {
            string id = filtered[i];
            var data = GameManager.Instance.GetExhibit(id);
            string label = data != null ? $"{data.category} · {data.name}" : id;

            var row = NewUI($"Item_{i}", listArea);
            var rr = row.GetComponent<RectTransform>();
            rr.anchorMin = new Vector2(0, 1f - (i + 1) * rowH);
            rr.anchorMax = new Vector2(1f, 1f - i * rowH);
            rr.offsetMin = rr.offsetMax = Vector2.zero;
            var ri = row.AddComponent<Image>(); ri.color = i % 2 == 0 ? new Color(0.95f, 0.93f, 0.88f) : XuanPaper; ri.raycastTarget = false;

            var nameObj = NewUI("Name", row.transform);
            var nr = nameObj.GetComponent<RectTransform>();
            nr.anchorMin = Vector2.zero; nr.anchorMax = new Vector2(0.7f, 1f);
            nr.offsetMin = new Vector2(10, 0); nr.offsetMax = Vector2.zero;
            var nt = nameObj.AddComponent<Text>();
            nt.font = Font(); nt.text = $"• {label}"; nt.fontSize = 16; nt.color = InkBlack; nt.alignment = TextAnchor.MiddleLeft;

            var del = NewUI("Del", row.transform);
            var dr = del.GetComponent<RectTransform>();
            dr.anchorMin = new Vector2(0.75f, 0.1f); dr.anchorMax = new Vector2(0.95f, 0.9f);
            dr.offsetMin = dr.offsetMax = Vector2.zero;
            del.AddComponent<Image>().color = ZhuRed;
            var db = del.AddComponent<Button>();
            string eid = id;
            db.onClick.AddListener(() => OnDeleteFromBackpack(eid));
            var dt = NewUI("DT", del.transform); Stretch(dt);
            var dtt = dt.AddComponent<Text>();
            dtt.font = Font(); dtt.text = "删除"; dtt.fontSize = 14; dtt.color = Color.white; dtt.alignment = TextAnchor.MiddleCenter;
        }
    }

    private void OnDeleteFromBackpack(string exhibitId)
    {
        BackpackManager.Instance.RemoveFromBackpack(GameManager.Instance.currentUser, exhibitId);
        RefreshBackpackList();
    }
}
