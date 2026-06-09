using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 事件展示场景 — 历史故事叙事展示
/// 品类选择 → 事件列表 → 事件详情
/// </summary>
public class EventManager : UIFrame
{
    // ──────────────────── 配色 ────────────────────
    private static readonly Color CardBg = new Color(0.98f, 0.96f, 0.92f);
    private static readonly Color EraColor = new Color(0.60f, 0.50f, 0.35f);

    // 品类配置
    private static readonly string[] Categories = { "瓷器", "剪纸", "书法", "民族乐器" };
    private static readonly string[] CategoryDescs = { "千年窑火 瓷韵流芳", "纸艺生花 巧夺天工", "笔墨丹青 翰墨飘香", "丝竹管弦 余音绕梁" };
    private static readonly Color[] CategoryColors = {
        new Color(0.26f, 0.47f, 0.72f), new Color(0.80f, 0.20f, 0.18f),
        new Color(0.35f, 0.35f, 0.38f), new Color(0.72f, 0.53f, 0.19f)
    };
    private static readonly string[] CategoryIcons = { "瓷", "剪", "书", "乐" };

    // 视图状态
    private enum ViewState { CategorySelect, EventList, EventDetail }
    private ViewState currentState = ViewState.CategorySelect;

    // 数据
    private List<EventItem> currentEvents;
    private int eventIndex;
    private string currentCategory;

    // 视图
    private GameObject categoryView, eventListView, eventDetailView;
    private CanvasGroup categoryCG, eventListCG, eventDetailCG;

    // 事件列表UI
    private Transform eventListContent;

    // 事件详情UI
    private Text detailTitleText;
    private Text detailEraText;
    private Text detailStoryText;
    private Text detailIndexText;
    private GameObject detailIconObj;

    private void Start()
    {
        EnsureSingletons();
        CreateUI();
    }

    private void CreateUI()
    {
        var root = InitCanvas();

        // 背景
        var bg = NewUI("BG", root);
        Stretch(bg);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = XuanPaper;
        bgImg.raycastTarget = false;
        AddInkWashCorners(root);

        // 顶部导航栏
        CreateHeader(root);

        // 三个视图容器
        CreateCategoryView(root);
        CreateEventListView(root);
        CreateEventDetailView(root);

        ShowCategoryView();
    }

    // ──────────────────── 顶部导航栏 ────────────────────

    private void CreateHeader(Transform parent)
    {
        var header = AnchorTop("Header", parent, 55);
        var hImg = header.AddComponent<Image>();
        hImg.color = DarkBar;
        hImg.raycastTarget = false;

        // 返回按钮
        var backObj = NewUI("BackBtn", header.transform);
        var br = backObj.GetComponent<RectTransform>();
        br.anchorMin = br.anchorMax = new Vector2(0, 0.5f);
        br.pivot = new Vector2(0, 0.5f);
        br.sizeDelta = new Vector2(100, 36);
        br.anchoredPosition = new Vector2(12, 0);
        backObj.AddComponent<Image>().color = ZhuRed;
        backObj.AddComponent<Button>().onClick.AddListener(OnBackClicked);
        var btObj = NewUI("T", backObj.transform); Stretch(btObj);
        var bt = btObj.AddComponent<Text>();
        bt.font = Font(); bt.text = "< 返回"; bt.fontSize = 16; bt.color = Color.white; bt.alignment = TextAnchor.MiddleCenter;

        // 标题组：印章+文字，整体居中
        var titleGroup = NewUI("TitleGroup", header.transform);
        var tgr = titleGroup.GetComponent<RectTransform>();
        tgr.anchorMin = tgr.anchorMax = new Vector2(0.5f, 0.5f);
        tgr.pivot = new Vector2(0.5f, 0.5f);
        tgr.sizeDelta = new Vector2(260, 40);
        tgr.anchoredPosition = Vector2.zero;

        AddSealLogo("MiniLogo", titleGroup.transform, new Vector2(-95, 0), 34, "史", 18);

        var titleObj = NewUI("Title", titleGroup.transform);
        var tr = titleObj.GetComponent<RectTransform>();
        tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0.5f);
        tr.pivot = new Vector2(0.5f, 0.5f);
        tr.sizeDelta = new Vector2(220, 35);
        tr.anchoredPosition = new Vector2(18, 0);
        var tt = titleObj.AddComponent<Text>();
        tt.font = Font(); tt.text = "历史故事"; tt.fontSize = 26; tt.color = ZhuRed; tt.alignment = TextAnchor.MiddleCenter;
    }

    // ──────────────────── 品类选择视图 ────────────────────

    private void CreateCategoryView(Transform parent)
    {
        categoryView = NewUI("CategoryView", parent);
        Stretch(categoryView);
        categoryView.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        categoryView.GetComponent<Image>().raycastTarget = false;
        categoryCG = categoryView.AddComponent<CanvasGroup>();

        // 顶部提示语
        AddDivider("Tip", categoryView.transform, new Vector2(0, -85), 550, "选择品类，探寻历史故事", GoldColor, InkBlack, 15);

        // 4张品类卡片（2x2网格）
        for (int i = 0; i < 4; i++)
        {
            int row = i / 2;
            int col = i % 2;
            CreateCategoryCard(categoryView.transform, i, row, col);
        }

        // 底部激励语
        var tipObj = NewUI("TipBottom", categoryView.transform);
        var tipR = tipObj.GetComponent<RectTransform>();
        tipR.anchorMin = tipR.anchorMax = new Vector2(0.5f, 0.08f);
        tipR.pivot = new Vector2(0.5f, 0.5f);
        tipR.sizeDelta = new Vector2(400, 30);
        var tipT = tipObj.AddComponent<Text>();
        tipT.font = Font(); tipT.text = "以史为鉴，可以知兴替 — 司马光"; tipT.fontSize = 14;
        tipT.color = new Color(0.5f, 0.5f, 0.5f); tipT.alignment = TextAnchor.MiddleCenter;
    }

    private void CreateCategoryCard(Transform parent, int idx, int row, int col)
    {
        float cardW = 220, cardH = 150;
        float gapX = 16, gapY = 16;
        float x = (2 * col - 1) * (cardW / 2f + gapX / 2f);
        float y = (1 - 2 * row) * (cardH / 2f + gapY / 2f) - 40;

        var card = NewUI($"Card_{Categories[idx]}", parent);
        var cr = card.GetComponent<RectTransform>();
        cr.anchorMin = cr.anchorMax = new Vector2(0.5f, 0.5f);
        cr.pivot = new Vector2(0.5f, 0.5f);
        cr.sizeDelta = new Vector2(cardW, cardH);
        cr.anchoredPosition = new Vector2(x, y);

        var outImg = card.AddComponent<Image>();
        outImg.color = new Color(0.82f, 0.80f, 0.72f);
        outImg.raycastTarget = true;

        var inner = NewUI("Inner", card.transform);
        var ir = inner.GetComponent<RectTransform>();
        ir.anchorMin = new Vector2(0.04f, 0.04f);
        ir.anchorMax = new Vector2(0.96f, 0.96f);
        ir.offsetMin = ir.offsetMax = Vector2.zero;
        var inImg = inner.AddComponent<Image>();
        inImg.color = XuanPaper;
        inImg.raycastTarget = false;

        AddSealIcon("Icon", inner.transform, new Vector2(0.5f, 0.72f), 24, CategoryIcons[idx], 22);

        var nameObj = NewUI("Name", inner.transform);
        var nr = nameObj.GetComponent<RectTransform>();
        nr.anchorMin = nr.anchorMax = new Vector2(0.5f, 0.42f);
        nr.sizeDelta = new Vector2(180, 28);
        var nm = nameObj.AddComponent<Text>();
        nm.font = Font(); nm.text = Categories[idx]; nm.fontSize = 22; nm.color = CategoryColors[idx]; nm.alignment = TextAnchor.MiddleCenter;

        var descObj = NewUI("Desc", inner.transform);
        var dr = descObj.GetComponent<RectTransform>();
        dr.anchorMin = dr.anchorMax = new Vector2(0.5f, 0.22f);
        dr.sizeDelta = new Vector2(220, 22);
        var dt = descObj.AddComponent<Text>();
        dt.font = Font(); dt.text = CategoryDescs[idx]; dt.fontSize = 12; dt.color = new Color(0.45f, 0.45f, 0.45f); dt.alignment = TextAnchor.MiddleCenter;

        var btn = card.AddComponent<Button>();
        int capturedIdx = idx;
        btn.onClick.AddListener(() => OnCategorySelected(capturedIdx));
    }

    // ──────────────────── 事件列表视图 ────────────────────

    private void CreateEventListView(Transform parent)
    {
        eventListView = NewUI("EventListView", parent);
        Stretch(eventListView);
        eventListView.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        eventListView.GetComponent<Image>().raycastTarget = false;
        eventListCG = eventListView.AddComponent<CanvasGroup>();

        // 品类标题
        var catHeader = NewUI("CatHeader", eventListView.transform);
        var chr = catHeader.GetComponent<RectTransform>();
        chr.anchorMin = chr.anchorMax = new Vector2(0.5f, 1f);
        chr.pivot = new Vector2(0.5f, 1f);
        chr.sizeDelta = new Vector2(300, 50);
        chr.anchoredPosition = new Vector2(0, -65);
        var catText = catHeader.AddComponent<Text>();
        catText.font = Font(); catText.fontSize = 22; catText.color = ZhuRed; catText.alignment = TextAnchor.MiddleCenter;

        // 事件列表容器
        var listContainer = NewUI("ListContainer", eventListView.transform);
        var lcr = listContainer.GetComponent<RectTransform>();
        lcr.anchorMin = new Vector2(0.08f, 0.15f);
        lcr.anchorMax = new Vector2(0.92f, 0.82f);
        lcr.offsetMin = lcr.offsetMax = Vector2.zero;
        var lcImg = listContainer.AddComponent<Image>();
        lcImg.color = new Color(0, 0, 0, 0);
        lcImg.raycastTarget = false;

        eventListContent = listContainer.transform;
    }

    private void RefreshEventList()
    {
        // 清空旧内容
        for (int i = eventListContent.childCount - 1; i >= 0; i--)
            Destroy(eventListContent.GetChild(i).gameObject);

        if (currentEvents == null || currentEvents.Count == 0) return;

        float cardH = 80;
        float gapY = 8;
        float totalH = currentEvents.Count * cardH + (currentEvents.Count - 1) * gapY;

        for (int i = 0; i < currentEvents.Count; i++)
        {
            var item = currentEvents[i];
            var card = NewUI($"Event_{i}", eventListContent);
            var cr = card.GetComponent<RectTransform>();
            cr.anchorMin = cr.anchorMax = new Vector2(0.5f, 1f);
            cr.pivot = new Vector2(0.5f, 1f);
            cr.sizeDelta = new Vector2(460, cardH);
            cr.anchoredPosition = new Vector2(0, -(i * (cardH + gapY)));

            var cImg = card.AddComponent<Image>();
            cImg.color = i % 2 == 0 ? CardBg : new Color(0.96f, 0.94f, 0.88f);
            cImg.raycastTarget = true;

            // 序号印章
            AddSealIcon("NumIcon", card.transform, new Vector2(0.08f, 0.5f), 20, $"{i + 1}", 16);

            // 标题
            var titleObj = NewUI("ETitle", card.transform);
            var tr = titleObj.GetComponent<RectTransform>();
            tr.anchorMin = new Vector2(0.18f, 0.55f);
            tr.anchorMax = new Vector2(0.85f, 0.95f);
            tr.offsetMin = tr.offsetMax = Vector2.zero;
            var tt = titleObj.AddComponent<Text>();
            tt.font = Font(); tt.text = item.title; tt.fontSize = 17; tt.color = InkBlack; tt.alignment = TextAnchor.MiddleLeft;

            // 时代
            var eraObj = NewUI("EEra", card.transform);
            var er = eraObj.GetComponent<RectTransform>();
            er.anchorMin = new Vector2(0.18f, 0.05f);
            er.anchorMax = new Vector2(0.85f, 0.45f);
            er.offsetMin = er.offsetMax = Vector2.zero;
            var et = eraObj.AddComponent<Text>();
            et.font = Font(); et.text = item.era; et.fontSize = 13; et.color = EraColor; et.alignment = TextAnchor.MiddleLeft;

            // 箭头
            var arrowObj = NewUI("Arrow", card.transform);
            var arr = arrowObj.GetComponent<RectTransform>();
            arr.anchorMin = arr.anchorMax = new Vector2(0.94f, 0.5f);
            arr.pivot = new Vector2(0.5f, 0.5f);
            arr.sizeDelta = new Vector2(20, 20);
            var at = arrowObj.AddComponent<Text>();
            at.font = Font(); at.text = ">"; at.fontSize = 18; at.color = GoldColor; at.alignment = TextAnchor.MiddleCenter;

            // 点击进入详情
            var btn = card.AddComponent<Button>();
            int capturedI = i;
            btn.onClick.AddListener(() => OnEventSelected(capturedI));
        }
    }

    // ──────────────────── 事件详情视图 ────────────────────

    private void CreateEventDetailView(Transform parent)
    {
        eventDetailView = NewUI("EventDetailView", parent);
        Stretch(eventDetailView);
        eventDetailView.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        eventDetailView.GetComponent<Image>().raycastTarget = false;
        eventDetailCG = eventDetailView.AddComponent<CanvasGroup>();

        // 品类标题
        var catObj = NewUI("DCatTitle", eventDetailView.transform);
        var dcr = catObj.GetComponent<RectTransform>();
        dcr.anchorMin = dcr.anchorMax = new Vector2(0.5f, 1f);
        dcr.pivot = new Vector2(0.5f, 1f);
        dcr.sizeDelta = new Vector2(300, 35);
        dcr.anchoredPosition = new Vector2(0, -65);
        var dct = catObj.AddComponent<Text>();
        dct.font = Font(); dct.fontSize = 20; dct.color = ZhuRed; dct.alignment = TextAnchor.MiddleCenter;

        // 详情卡片容器
        var cardContainer = NewUI("DetailCard", eventDetailView.transform);
        var ccr = cardContainer.GetComponent<RectTransform>();
        ccr.anchorMin = new Vector2(0.08f, 0.15f);
        ccr.anchorMax = new Vector2(0.92f, 0.82f);
        ccr.offsetMin = ccr.offsetMax = Vector2.zero;
        var ccImg = cardContainer.AddComponent<Image>();
        ccImg.color = CardBg;
        ccImg.raycastTarget = false;
        AddBorderLines(cardContainer);

        // 印章图标
        detailIconObj = AddSealIcon("DIcon", cardContainer.transform, new Vector2(0.5f, 0.92f), 28, "史", 24);

        // 标题
        var titleObj = NewUI("DTitle", cardContainer.transform);
        var dtr = titleObj.GetComponent<RectTransform>();
        dtr.anchorMin = new Vector2(0.08f, 0.78f);
        dtr.anchorMax = new Vector2(0.92f, 0.88f);
        dtr.offsetMin = dtr.offsetMax = Vector2.zero;
        detailTitleText = titleObj.AddComponent<Text>();
        detailTitleText.font = Font(); detailTitleText.fontSize = 20; detailTitleText.color = InkBlack; detailTitleText.alignment = TextAnchor.MiddleCenter;

        // 时代标签
        var eraObj = NewUI("DEra", cardContainer.transform);
        var der = eraObj.GetComponent<RectTransform>();
        der.anchorMin = new Vector2(0.08f, 0.72f);
        der.anchorMax = new Vector2(0.92f, 0.78f);
        der.offsetMin = der.offsetMax = Vector2.zero;
        detailEraText = eraObj.AddComponent<Text>();
        detailEraText.font = Font(); detailEraText.fontSize = 14; detailEraText.color = EraColor; detailEraText.alignment = TextAnchor.MiddleCenter;

        // 分隔线
        AddDivider("DDiv", cardContainer.transform, new Vector2(0, 20), 350, "◆", GoldColor, GoldColor, 12);

        // 叙事正文
        var storyObj = NewUI("DStory", cardContainer.transform);
        var dsr = storyObj.GetComponent<RectTransform>();
        dsr.anchorMin = new Vector2(0.06f, 0.05f);
        dsr.anchorMax = new Vector2(0.94f, 0.68f);
        dsr.offsetMin = dsr.offsetMax = Vector2.zero;
        detailStoryText = storyObj.AddComponent<Text>();
        detailStoryText.font = Font(); detailStoryText.fontSize = 16; detailStoryText.color = InkBlack;
        detailStoryText.alignment = TextAnchor.UpperLeft;
        detailStoryText.lineSpacing = 1.6f;

        // 翻页索引
        detailIndexText = AddLabelCenter("DIndex", cardContainer.transform, new Vector2(0, -20), new Vector2(200, 25), "", 14, new Color(0.5f, 0.5f, 0.5f));

        // 左右翻页按钮
        AddBtnAnchored("PrevBtn", eventDetailView.transform, new Vector2(0.12f, 0.08f), new Vector2(100, 38), Vector2.zero, "< 上一条", OchreBrown(), 14).onClick.AddListener(ShowPrevEvent);
        AddBtnAnchored("NextBtn", eventDetailView.transform, new Vector2(0.38f, 0.08f), new Vector2(100, 38), Vector2.zero, "下一条 >", OchreBrown(), 14).onClick.AddListener(ShowNextEvent);

        // 返回列表按钮
        AddBtnAnchored("BackListBtn", eventDetailView.transform, new Vector2(0.72f, 0.08f), new Vector2(120, 38), Vector2.zero, "返回列表", JadeGreen, 14).onClick.AddListener(ShowEventListView);
    }

    private Color OchreBrown() => new Color(0.55f, 0.35f, 0.17f);

    // ──────────────────── 视图切换 ────────────────────

    private void SetViewVisible(CanvasGroup cg, bool visible)
    {
        if (cg == null) return;
        cg.alpha = visible ? 1f : 0f;
        cg.blocksRaycasts = visible;
        cg.interactable = visible;
    }

    private void ShowCategoryView()
    {
        currentState = ViewState.CategorySelect;
        SetViewVisible(categoryCG, true);
        SetViewVisible(eventListCG, false);
        SetViewVisible(eventDetailCG, false);
    }

    private void ShowEventListView()
    {
        currentState = ViewState.EventList;
        SetViewVisible(categoryCG, false);
        SetViewVisible(eventListCG, true);
        SetViewVisible(eventDetailCG, false);

        // 更新品类标题
        var catHeader = eventListView.transform.Find("CatHeader");
        if (catHeader != null) catHeader.GetComponent<Text>().text = $"{currentCategory} · 历史故事";

        RefreshEventList();
    }

    private void ShowEventDetailView()
    {
        currentState = ViewState.EventDetail;
        SetViewVisible(categoryCG, false);
        SetViewVisible(eventListCG, false);
        SetViewVisible(eventDetailCG, true);

        // 更新品类标题
        var catObj = eventDetailView.transform.Find("DCatTitle");
        if (catObj != null) catObj.GetComponent<Text>().text = $"{currentCategory} · 故事详情";

        UpdateDetailCard();
    }

    // ──────────────────── 品类选择 ────────────────────

    private void OnCategorySelected(int idx)
    {
        currentCategory = Categories[idx];
        currentEvents = GameManager.Instance.GetEventsByCategory(currentCategory);
        eventIndex = 0;

        // 更新详情页印章颜色
        if (detailIconObj != null)
            detailIconObj.GetComponent<Image>().color = CategoryColors[idx];

        ShowEventListView();
    }

    // ──────────────────── 事件选择 ────────────────────

    private void OnEventSelected(int idx)
    {
        eventIndex = idx;
        ShowEventDetailView();
    }

    // ──────────────────── 事件详情 ────────────────────

    private void UpdateDetailCard()
    {
        if (currentEvents == null || currentEvents.Count == 0) return;
        var item = currentEvents[eventIndex];

        detailTitleText.text = item.title;
        detailEraText.text = item.era;
        detailStoryText.text = item.storyText;
        detailIndexText.text = $"{eventIndex + 1} / {currentEvents.Count}";

        // 更新印章文字
        var iconText = detailIconObj.transform.Find("T");
        if (iconText != null) iconText.GetComponent<Text>().text = item.title.Length > 0 ? item.title[0].ToString() : "史";
    }

    private void ShowPrevEvent()
    {
        if (currentEvents == null || currentEvents.Count == 0) return;
        eventIndex = (eventIndex - 1 + currentEvents.Count) % currentEvents.Count;
        UpdateDetailCard();
    }

    private void ShowNextEvent()
    {
        if (currentEvents == null || currentEvents.Count == 0) return;
        eventIndex = (eventIndex + 1) % currentEvents.Count;
        UpdateDetailCard();
    }

    // ──────────────────── 导航 ────────────────────

    private void OnBackClicked()
    {
        switch (currentState)
        {
            case ViewState.CategorySelect:
                SceneLoader.Instance.LoadScene(SceneNames.Main);
                break;
            case ViewState.EventList:
                ShowCategoryView();
                break;
            case ViewState.EventDetail:
                ShowEventListView();
                break;
        }
    }
}
