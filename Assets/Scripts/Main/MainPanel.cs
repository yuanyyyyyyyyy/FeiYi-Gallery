using UnityEngine;
using UnityEngine.UI;
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
        var header = AnchorTop("Header", root, 60);
        var hImg = header.AddComponent<Image>(); hImg.color = DarkBar; hImg.raycastTarget = false;

        // 左侧印章小图标
        AddSealLogo("MiniLogo", header.transform, new Vector2(-220, 0), 34, "遗", 18);

        // 标题文字（header 内居中偏左）
        var titleObj = NewUI("Title", header.transform);
        var tr = titleObj.GetComponent<RectTransform>();
        tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0.5f);
        tr.pivot = new Vector2(0.5f, 0.5f);
        tr.sizeDelta = new Vector2(300, 35);
        tr.anchoredPosition = new Vector2(20, 0);
        var tt = titleObj.AddComponent<Text>();
        tt.font = Font(); tt.text = "了不起的非遗"; tt.fontSize = 26; tt.color = ZhuRed; tt.alignment = TextAnchor.MiddleCenter;

        // 右侧用户名
        var userObj = NewUI("User", header.transform);
        var ur = userObj.GetComponent<RectTransform>();
        ur.anchorMin = ur.anchorMax = new Vector2(1f, 0.5f);
        ur.pivot = new Vector2(1f, 0.5f);
        ur.sizeDelta = new Vector2(150, 30);
        ur.anchoredPosition = new Vector2(-15, 0);
        var ut = userObj.AddComponent<Text>();
        ut.font = Font(); ut.text = GameManager.Instance?.currentUser ?? ""; ut.fontSize = 15; ut.color = GoldColor; ut.alignment = TextAnchor.MiddleRight;

        // ── 卷轴 ScrollView 区域 ──
        CreateScrollView(root);

        // ── 底部导航栏 ──
        CreateNavBar(root);

        // ── 弹窗 ──
        settingsPanel = MakeOverlay(root, "系统设置",
            $"音量：{Mathf.RoundToInt(GameManager.Instance.volume * 100)}%\n" +
            $"亮度：{Mathf.RoundToInt(GameManager.Instance.brightness * 100)}%\n" +
            "主题风格：" + GameManager.Instance.themeStyle + "\n\n" +
            "所有设置已通过 GameManager 保存\n点击「保存设置」按钮应用变更", GoldColor);
        helpPanel = MakeOverlay(root, "帮助",
            "1. 选择品类卡片浏览展品\n2. 拖拽旋转3D模型\n3. 查看展品详细信息\n4. 收藏感兴趣的展品\n5. 在背包中管理收藏", ZhuRed);
    }

    // ──────────────────── 卷轴 ScrollView ────────────────────

    private void CreateScrollView(Transform parent)
    {
        // ScrollView 容器 — 占据标题栏和导航栏之间的区域
        var scrollObj = NewUI("ScrollView", parent);
        var sr = scrollObj.GetComponent<RectTransform>();
        sr.anchorMin = new Vector2(0.05f, 0.12f);
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

        // 印章图标（品类字）
        AddSealIcon("Icon", inner.transform, new Vector2(0.5f, 0.72f), 32, CategoryIcons[idx], 28);

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
            () => { if (backpackPanel == null) { CreateBackpackPanel(); return; } backpackPanel.SetActive(!backpackPanel.activeSelf); },
            () => TogglePanel(settingsPanel),
            () => TogglePanel(helpPanel),
            () => { GameManager.Instance.Logout(); SceneLoader.Instance.LoadScene(SceneNames.Login); }
        };

        for (int i = 0; i < 4; i++)
        {
            // 每个竹简片
            var bo = NewUI($"Nav_{navNames[i]}", nav.transform);
            var br = bo.GetComponent<RectTransform>();
            br.anchorMin = new Vector2(i * 0.25f, 0); br.anchorMax = new Vector2((i + 1) * 0.25f, 1);
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

    private void TogglePanel(GameObject p) { if (p != null) p.SetActive(!p.activeSelf); }

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

        if (items.Count == 0)
        {
            var empty = NewUI("Empty", contentArea);
            Stretch(empty);
            var et = empty.AddComponent<Text>();
            et.font = Font(); et.text = "暂无收藏展品\n浏览展品时点击「收藏」即可添加到背包";
            et.fontSize = 16; et.color = new Color(0.5f, 0.5f, 0.5f); et.alignment = TextAnchor.MiddleCenter;
            return;
        }

        float rowH = 0.12f;
        for (int i = 0; i < items.Count; i++)
        {
            string id = items[i];
            var data = GameManager.Instance.GetExhibit(id);
            string label = data != null ? data.name : id;

            var row = NewUI($"Item_{i}", contentArea);
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
        CreateBackpackPanel();
        backpackPanel.SetActive(true);
    }
}
