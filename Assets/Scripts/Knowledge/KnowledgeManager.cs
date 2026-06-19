using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 知识探索场景 — 知识可视化展示 + 趣味互动问答
/// 三种视图状态：品类选择 → 知识卡片浏览 → 知识问答
/// </summary>
public class KnowledgeManager : UIFrame
{
    // ──────────────────── 配色 ────────────────────
    private static readonly Color CardBg       = new Color(0.98f, 0.96f, 0.92f);
    private static readonly Color TabActiveBg  = new Color(0.88f, 0.72f, 0.45f);
    private static readonly Color TabInactiveBg= new Color(0.92f, 0.89f, 0.83f);
    private static readonly Color CorrectGreen  = new Color(0.18f, 0.60f, 0.34f);
    private static readonly Color WrongRed      = new Color(0.80f, 0.18f, 0.18f);

    // 品类配置（与 MainPanel 一致）
    private static readonly string[] Categories = { "瓷器", "剪纸", "书法", "民族乐器", "刺绣", "茶艺", "皮影戏", "扎染蜡染" };
    private static readonly string[] CategoryDescs = { "千年窑火 瓷韵流芳", "纸艺生花 巧夺天工", "笔墨丹青 翰墨飘香", "丝竹管弦 余音绕梁", "千针万线 锦上添花", "茶香千年 壶中天地", "光影交错 戏说千古", "蓝白相映 染就乾坤" };
    private static readonly Color[] CategoryColors = {
        new Color(0.26f, 0.47f, 0.72f), new Color(0.80f, 0.20f, 0.18f),
        new Color(0.35f, 0.35f, 0.38f), new Color(0.72f, 0.53f, 0.19f),
        new Color(0.18f, 0.42f, 0.32f), new Color(0.45f, 0.35f, 0.15f),
        new Color(0.35f, 0.20f, 0.40f), new Color(0.15f, 0.25f, 0.55f)
    };
    private static readonly string[] CategoryIcons = { "瓷", "剪", "书", "乐", "绣", "茶", "影", "染" };

    // 视图状态
    private enum ViewState { CategorySelect, KnowledgeBrowse, Quiz }
    private ViewState currentState = ViewState.CategorySelect;

    // 知识浏览
    private List<KnowledgeItem> currentKnowledge;
    private int knowledgeIndex;
    private string currentCategory;
    private GameObject categoryView, knowledgeView, quizView;
    private CanvasGroup categoryCG, knowledgeCG, quizCG;

    // 知识卡片UI引用
    private Text knowledgeTitleText;
    private Text knowledgeContentText;
    private Text knowledgeIndexText;
    private GameObject knowledgeIconObj;

    // 问答
    private List<QuizQuestion> currentQuiz;
    private int quizIndex;
    private int correctCount;
    private bool answered;
    private Text quizQuestionText;
    private Text quizProgressText;
    private GameObject[] optionBtns = new GameObject[4];
    private Text[] optionTexts = new Text[4];
    private Text feedbackText;
    private GameObject feedbackPanel;
    private int[] userAnswers;           // 每题用户选择，-1=未答
    private bool[] questionAnswered;     // 每题是否已确认
    private Button prevBtn, nextBtn, submitBtn;

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
        CreateKnowledgeView(root);
        CreateQuizView(root);

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

        // 印章小图标（组内偏左）
        AddSealLogo("MiniLogo", titleGroup.transform, new Vector2(-95, 0), 34, "学", 18);

        // 标题文字（组内偏右）
        var titleObj = NewUI("Title", titleGroup.transform);
        var tr = titleObj.GetComponent<RectTransform>();
        tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0.5f);
        tr.pivot = new Vector2(0.5f, 0.5f);
        tr.sizeDelta = new Vector2(220, 35);
        tr.anchoredPosition = new Vector2(18, 0);
        var tt = titleObj.AddComponent<Text>();
        tt.font = Font(); tt.text = "知识探索"; tt.fontSize = 26; tt.color = ZhuRed; tt.alignment = TextAnchor.MiddleCenter;
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
        AddDivider("Tip", categoryView.transform, new Vector2(0, -85), 550, "选择品类，开启知识之旅", GoldColor, InkBlack, 15);

        // 品类卡片（4列2行网格）
        for (int i = 0; i < Categories.Length; i++)
        {
            int row = i / 4;
            int col = i % 4;
            CreateCategoryCard(categoryView.transform, i, row, col);
        }

        // 底部激励语
        var tipObj = NewUI("TipBottom", categoryView.transform);
        var tipR = tipObj.GetComponent<RectTransform>();
        tipR.anchorMin = tipR.anchorMax = new Vector2(0.5f, 0.08f);
        tipR.pivot = new Vector2(0.5f, 0.5f);
        tipR.sizeDelta = new Vector2(400, 30);
        var tipT = tipObj.AddComponent<Text>();
        tipT.font = Font(); tipT.text = "学而不厌，诲人不倦 — 孔子"; tipT.fontSize = 14;
        tipT.color = new Color(0.5f, 0.5f, 0.5f); tipT.alignment = TextAnchor.MiddleCenter;
    }

    private void CreateCategoryCard(Transform parent, int idx, int row, int col)
    {
        float cardW = 180, cardH = 130;
        float gapX = 14, gapY = 14;
        // 4列2行网格布局
        float x = (col - 1.5f) * (cardW + gapX);
        float y = (0.5f - row) * (cardH + gapY) - 30;

        var card = NewUI($"Card_{Categories[idx]}", parent);
        var cr = card.GetComponent<RectTransform>();
        cr.anchorMin = cr.anchorMax = new Vector2(0.5f, 0.5f);
        cr.pivot = new Vector2(0.5f, 0.5f);
        cr.sizeDelta = new Vector2(cardW, cardH);
        cr.anchoredPosition = new Vector2(x, y);

        // 外框
        var outImg = card.AddComponent<Image>();
        outImg.color = new Color(0.82f, 0.80f, 0.72f);
        outImg.raycastTarget = true;

        // 内层
        var inner = NewUI("Inner", card.transform);
        var ir = inner.GetComponent<RectTransform>();
        ir.anchorMin = new Vector2(0.04f, 0.04f);
        ir.anchorMax = new Vector2(0.96f, 0.96f);
        ir.offsetMin = ir.offsetMax = Vector2.zero;
        var inImg = inner.AddComponent<Image>();
        inImg.color = XuanPaper;
        inImg.raycastTarget = false;

        // 印章图标
        AddSealIcon("Icon", inner.transform, new Vector2(0.5f, 0.72f), 24, CategoryIcons[idx], 22);

        // 品类名
        var nameObj = NewUI("Name", inner.transform);
        var nr = nameObj.GetComponent<RectTransform>();
        nr.anchorMin = nr.anchorMax = new Vector2(0.5f, 0.42f);
        nr.sizeDelta = new Vector2(180, 28);
        var nm = nameObj.AddComponent<Text>();
        nm.font = Font(); nm.text = Categories[idx]; nm.fontSize = 22; nm.color = CategoryColors[idx]; nm.alignment = TextAnchor.MiddleCenter;

        // 描述
        var descObj = NewUI("Desc", inner.transform);
        var dr = descObj.GetComponent<RectTransform>();
        dr.anchorMin = dr.anchorMax = new Vector2(0.5f, 0.22f);
        dr.sizeDelta = new Vector2(220, 22);
        var dt = descObj.AddComponent<Text>();
        dt.font = Font(); dt.text = CategoryDescs[idx]; dt.fontSize = 12; dt.color = new Color(0.45f, 0.45f, 0.45f); dt.alignment = TextAnchor.MiddleCenter;

        // 按钮
        var btn = card.AddComponent<Button>();
        int capturedIdx = idx;
        btn.onClick.AddListener(() => OnCategorySelected(capturedIdx, card.transform));
    }

    // ──────────────────── 知识浏览视图 ────────────────────

    private void CreateKnowledgeView(Transform parent)
    {
        knowledgeView = NewUI("KnowledgeView", parent);
        Stretch(knowledgeView);
        knowledgeView.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        knowledgeView.GetComponent<Image>().raycastTarget = false;
        knowledgeCG = knowledgeView.AddComponent<CanvasGroup>();
        var catHeader = NewUI("CatHeader", knowledgeView.transform);
        var chr = catHeader.GetComponent<RectTransform>();
        chr.anchorMin = chr.anchorMax = new Vector2(0.5f, 1f);
        chr.pivot = new Vector2(0.5f, 1f);
        chr.sizeDelta = new Vector2(300, 50);
        chr.anchoredPosition = new Vector2(0, -65);
        var catText = catHeader.AddComponent<Text>();
        catText.font = Font(); catText.fontSize = 22; catText.color = ZhuRed; catText.alignment = TextAnchor.MiddleCenter;

        // 知识卡片容器
        var cardContainer = NewUI("CardContainer", knowledgeView.transform);
        var ccr = cardContainer.GetComponent<RectTransform>();
        ccr.anchorMin = new Vector2(0.08f, 0.18f);
        ccr.anchorMax = new Vector2(0.92f, 0.76f);
        ccr.offsetMin = ccr.offsetMax = Vector2.zero;
        var ccImg = cardContainer.AddComponent<Image>();
        ccImg.color = new Color(0, 0, 0, 0);
        ccImg.raycastTarget = false;

        // 知识卡片
        var card = NewUI("KnowledgeCard", cardContainer.transform);
        Stretch(card);
        var cardImg = card.AddComponent<Image>();
        cardImg.color = CardBg;
        cardImg.raycastTarget = false;
        AddBorderLines(card);

        // 印章图标
        knowledgeIconObj = AddSealIcon("KIcon", card.transform, new Vector2(0.5f, 0.88f), 28, "知", 24);

        // 标题
        var titleObj = NewUI("KTitle", card.transform);
        var ktr = titleObj.GetComponent<RectTransform>();
        ktr.anchorMin = new Vector2(0.08f, 0.68f);
        ktr.anchorMax = new Vector2(0.92f, 0.78f);
        ktr.offsetMin = ktr.offsetMax = Vector2.zero;
        knowledgeTitleText = titleObj.AddComponent<Text>();
        knowledgeTitleText.font = Font(); knowledgeTitleText.fontSize = 20; knowledgeTitleText.color = InkBlack; knowledgeTitleText.alignment = TextAnchor.MiddleCenter;

        // 分隔线
        AddDivider("KDiv", card.transform, new Vector2(0, -10), 350, "◆", GoldColor, GoldColor, 12);

        // 正文内容
        var contentObj = NewUI("KContent", card.transform);
        var kcr = contentObj.GetComponent<RectTransform>();
        kcr.anchorMin = new Vector2(0.06f, 0.05f);
        kcr.anchorMax = new Vector2(0.94f, 0.65f);
        kcr.offsetMin = kcr.offsetMax = Vector2.zero;
        knowledgeContentText = contentObj.AddComponent<Text>();
        knowledgeContentText.font = Font(); knowledgeContentText.fontSize = 16; knowledgeContentText.color = InkBlack;
        knowledgeContentText.alignment = TextAnchor.UpperLeft;
        knowledgeContentText.lineSpacing = 1.6f;

        // 翻页索引
        knowledgeIndexText = AddLabelCenter("KIndex", card.transform, new Vector2(0, -20), new Vector2(200, 25), "", 14, new Color(0.5f, 0.5f, 0.5f));

        // 左右翻页按钮
        AddBtnAnchored("PrevBtn", knowledgeView.transform, new Vector2(0.12f, 0.12f), new Vector2(100, 38), Vector2.zero, "< 上一条", OchreBrown(), 14).onClick.AddListener(ShowPrevKnowledge);
        AddBtnAnchored("NextBtn", knowledgeView.transform, new Vector2(0.38f, 0.12f), new Vector2(100, 38), Vector2.zero, "下一条 >", OchreBrown(), 14).onClick.AddListener(ShowNextKnowledge);

        // 开始问答按钮
        AddBtnAnchored("QuizBtn", knowledgeView.transform, new Vector2(0.72f, 0.12f), new Vector2(160, 38), Vector2.zero, "开始知识问答", JadeGreen, 15).onClick.AddListener(StartQuiz);
    }

    private Color OchreBrown() => new Color(0.55f, 0.35f, 0.17f);

    // ──────────────────── 问答视图 ────────────────────

    private void CreateQuizView(Transform parent)
    {
        quizView = NewUI("QuizView", parent);
        Stretch(quizView);
        quizView.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        quizView.GetComponent<Image>().raycastTarget = false;
        quizCG = quizView.AddComponent<CanvasGroup>();

        // 品类标题
        var catObj = NewUI("QCatTitle", quizView.transform);
        var qcr = catObj.GetComponent<RectTransform>();
        qcr.anchorMin = qcr.anchorMax = new Vector2(0.5f, 1f);
        qcr.pivot = new Vector2(0.5f, 1f);
        qcr.sizeDelta = new Vector2(300, 35);
        qcr.anchoredPosition = new Vector2(0, -70);
        var qct = catObj.AddComponent<Text>();
        qct.font = Font(); qct.fontSize = 20; qct.color = ZhuRed; qct.alignment = TextAnchor.MiddleCenter;

        // 进度
        quizProgressText = AddLabelCenter("QProgress", quizView.transform, new Vector2(0, 20), new Vector2(200, 25), "", 15, GoldColor);

        // 题目
        var qObj = NewUI("QQuestion", quizView.transform);
        var qr = qObj.GetComponent<RectTransform>();
        qr.anchorMin = new Vector2(0.08f, 0.58f);
        qr.anchorMax = new Vector2(0.92f, 0.78f);
        qr.offsetMin = qr.offsetMax = Vector2.zero;
        quizQuestionText = qObj.AddComponent<Text>();
        quizQuestionText.font = Font(); quizQuestionText.fontSize = 19; quizQuestionText.color = InkBlack;
        quizQuestionText.alignment = TextAnchor.MiddleCenter;
        quizQuestionText.lineSpacing = 1.4f;

        // 4个选项按钮
        for (int i = 0; i < 4; i++)
        {
            var opt = NewUI($"Opt_{i}", quizView.transform);
            var or_ = opt.GetComponent<RectTransform>();
            or_.anchorMin = or_.anchorMax = new Vector2(0.5f, 0.5f);
            or_.pivot = new Vector2(0.5f, 0.5f);
            or_.sizeDelta = new Vector2(500, 42);
            or_.anchoredPosition = new Vector2(0, 60 - i * 52);

            opt.AddComponent<Image>().color = CardBg;
            var optBtn = opt.AddComponent<Button>();
            int capturedI = i;
            optBtn.onClick.AddListener(() => OnOptionSelected(capturedI));

            // 选项标签
            var labelObj = NewUI("Label", opt.transform);
            var lr = labelObj.GetComponent<RectTransform>();
            lr.anchorMin = new Vector2(0, 0);
            lr.anchorMax = new Vector2(0.08f, 1f);
            lr.offsetMin = lr.offsetMax = Vector2.zero;
            var lt = labelObj.AddComponent<Text>();
            lt.font = Font(); lt.text = $"{(char)('A' + i)}"; lt.fontSize = 17; lt.color = ZhuRed; lt.alignment = TextAnchor.MiddleCenter;

            // 选项内容
            var valObj = NewUI("Value", opt.transform);
            var vr = valObj.GetComponent<RectTransform>();
            vr.anchorMin = new Vector2(0.08f, 0);
            vr.anchorMax = new Vector2(0.96f, 1f);
            vr.offsetMin = vr.offsetMax = Vector2.zero;
            var vt = valObj.AddComponent<Text>();
            vt.font = Font(); vt.fontSize = 16; vt.color = InkBlack; vt.alignment = TextAnchor.MiddleLeft;

            optionBtns[i] = opt;
            optionTexts[i] = vt;
        }

        // 反馈区域
        feedbackPanel = NewUI("Feedback", quizView.transform);
        var fpr = feedbackPanel.GetComponent<RectTransform>();
        fpr.anchorMin = new Vector2(0.1f, 0.10f);
        fpr.anchorMax = new Vector2(0.9f, 0.28f);
        fpr.offsetMin = fpr.offsetMax = Vector2.zero;
        feedbackPanel.AddComponent<Image>().color = CardBg;
        feedbackPanel.GetComponent<Image>().raycastTarget = false;
        AddBorderLines(feedbackPanel);

        // Text 必须在单独的子对象上（不能与 Image 共存），留出边距避免被边框线遮挡
        var fbTextObj = NewUI("FBText", feedbackPanel.transform);
        var fbtr = fbTextObj.GetComponent<RectTransform>();
        fbtr.anchorMin = new Vector2(0.06f, 0.04f);
        fbtr.anchorMax = new Vector2(0.94f, 0.86f);
        fbtr.offsetMin = fbtr.offsetMax = Vector2.zero;
        feedbackText = fbTextObj.AddComponent<Text>();
        feedbackText.font = Font(); feedbackText.fontSize = 15; feedbackText.color = InkBlack;
        feedbackText.alignment = TextAnchor.UpperLeft;
        feedbackText.lineSpacing = 1.5f;

        // 底部导航按钮：上一题 / 下一题 / 提交
        prevBtn = AddBtnAnchored("PrevQ", quizView.transform, new Vector2(0.15f, 0.05f), new Vector2(90, 28), Vector2.zero, "< 上一题", OchreBrown(), 13);
        prevBtn.onClick.AddListener(OnPrevQuestion);

        nextBtn = AddBtnAnchored("NextQ", quizView.transform, new Vector2(0.5f, 0.05f), new Vector2(90, 28), Vector2.zero, "下一题 >", OchreBrown(), 13);
        nextBtn.onClick.AddListener(OnNextQuestion);

        submitBtn = AddBtnAnchored("SubmitQ", quizView.transform, new Vector2(0.85f, 0.05f), new Vector2(90, 28), Vector2.zero, "提交", JadeGreen, 13);
        submitBtn.onClick.AddListener(OnSubmitQuiz);
    }

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
        SetViewVisible(knowledgeCG, false);
        SetViewVisible(quizCG, false);
    }

    private void ShowKnowledgeView()
    {
        currentState = ViewState.KnowledgeBrowse;
        SetViewVisible(categoryCG, false);
        SetViewVisible(knowledgeCG, true);
        SetViewVisible(quizCG, false);
        UpdateKnowledgeCard();
    }

    private void ShowQuizView()
    {
        currentState = ViewState.Quiz;
        SetViewVisible(categoryCG, false);
        SetViewVisible(knowledgeCG, false);
        SetViewVisible(quizCG, true);
    }

    // ──────────────────── 品类选择 ────────────────────

    private void OnCategorySelected(int idx, Transform cardTransform)
    {
        StartCoroutine(CardClickFeedback(cardTransform));
        currentCategory = Categories[idx];
        currentKnowledge = GameManager.Instance.GetKnowledgeByCategory(currentCategory);
        knowledgeIndex = 0;

        // 更新品类标题
        var catHeader = knowledgeView.transform.Find("CatHeader");
        if (catHeader != null) catHeader.GetComponent<Text>().text = $"{currentCategory} · 知识长廊";

        // 更新印章图标颜色
        if (knowledgeIconObj != null)
        {
            knowledgeIconObj.GetComponent<Image>().color = CategoryColors[idx];
        }

        ShowKnowledgeView();
    }

    // ──────────────────── 知识卡片浏览 ────────────────────

    private void UpdateKnowledgeCard()
    {
        if (currentKnowledge == null || currentKnowledge.Count == 0) return;
        var item = currentKnowledge[knowledgeIndex];

        knowledgeTitleText.text = item.title;
        knowledgeContentText.text = item.content;
        knowledgeIndexText.text = $"{knowledgeIndex + 1} / {currentKnowledge.Count}";

        // 更新印章文字
        var iconText = knowledgeIconObj.transform.Find("T");
        if (iconText != null) iconText.GetComponent<Text>().text = item.iconText;
    }

    private void ShowPrevKnowledge()
    {
        if (currentKnowledge == null || currentKnowledge.Count == 0) return;
        SfxFlip();
        knowledgeIndex = (knowledgeIndex - 1 + currentKnowledge.Count) % currentKnowledge.Count;
        UpdateKnowledgeCard();
    }

    private void ShowNextKnowledge()
    {
        if (currentKnowledge == null || currentKnowledge.Count == 0) return;
        SfxFlip();
        knowledgeIndex = (knowledgeIndex + 1) % currentKnowledge.Count;
        UpdateKnowledgeCard();
    }

    // ──────────────────── 问答系统 ────────────────────

    private void StartQuiz()
    {
        currentQuiz = GameManager.Instance.GetQuizByCategory(currentCategory);
        if (currentQuiz == null || currentQuiz.Count == 0)
        {
            ShowToast("暂无该品类问答数据", ZhuRed);
            return;
        }

        quizIndex = 0;
        correctCount = 0;
        answered = false;
        userAnswers = new int[currentQuiz.Count];
        questionAnswered = new bool[currentQuiz.Count];
        for (int i = 0; i < currentQuiz.Count; i++)
        {
            userAnswers[i] = -1;
            questionAnswered[i] = false;
        }

        // 更新品类标题
        var catObj = quizView.transform.Find("QCatTitle");
        if (catObj != null) catObj.GetComponent<Text>().text = $"{currentCategory} · 知识问答";

        // 显示导航按钮
        if (prevBtn != null) prevBtn.gameObject.SetActive(true);
        if (nextBtn != null) nextBtn.gameObject.SetActive(true);
        if (submitBtn != null) submitBtn.gameObject.SetActive(true);

        ShowQuizView();
        ShowCurrentQuestion();
    }

    private void ShowCurrentQuestion()
    {
        if (currentQuiz == null || quizIndex >= currentQuiz.Count)
        {
            ShowQuizResult();
            return;
        }

        var q = currentQuiz[quizIndex];
        bool wasAnswered = questionAnswered[quizIndex];
        answered = wasAnswered;

        quizProgressText.text = $"第 {quizIndex + 1} / {currentQuiz.Count} 题";
        quizQuestionText.text = q.question;

        // 恢复或重置选项
        for (int i = 0; i < 4; i++)
        {
            if (i < q.options.Length)
            {
                optionBtns[i].SetActive(true);
                optionTexts[i].text = q.options[i];
                optionBtns[i].GetComponent<Image>().color = CardBg;
                optionBtns[i].GetComponent<Button>().interactable = !wasAnswered;
            }
            else
            {
                optionBtns[i].SetActive(false);
            }
        }

        // 恢复已答题目的状态
        if (wasAnswered)
        {
            int sel = userAnswers[quizIndex];
            bool isCorrect = (sel == q.correctIndex);

            // 高亮用户选择和正确答案
            if (sel >= 0 && sel < 4)
                optionBtns[sel].GetComponent<Image>().color = isCorrect ? CorrectGreen : WrongRed;
            optionBtns[q.correctIndex].GetComponent<Image>().color = CorrectGreen;

            // 恢复反馈文字
            if (isCorrect)
            {
                feedbackText.text = $"✅ 回答正确！\n{q.explanation}";
                feedbackText.color = CorrectGreen;
            }
            else
            {
                feedbackText.text = $"❌ 回答错误\n正确答案：{q.options[q.correctIndex]}\n{q.explanation}";
                feedbackText.color = WrongRed;
            }
        }
        else
        {
            feedbackText.text = "请选择答案";
            feedbackText.color = new Color(0.5f, 0.5f, 0.5f);
        }

        // 更新导航按钮
        UpdateNavButtons();
    }

    private void UpdateNavButtons()
    {
        if (prevBtn != null) prevBtn.interactable = (quizIndex > 0);
        if (nextBtn != null) nextBtn.interactable = (quizIndex < currentQuiz.Count - 1);
        // 提交按钮始终可用（允许未答完全部就提交）
        if (submitBtn != null) submitBtn.interactable = true;
    }

    private void OnOptionSelected(int idx)
    {
        if (answered || currentQuiz == null || quizIndex >= currentQuiz.Count) return;
        answered = true;

        var q = currentQuiz[quizIndex];
        bool isCorrect = (idx == q.correctIndex);

        // 记录答案
        userAnswers[quizIndex] = idx;
        questionAnswered[quizIndex] = true;
        if (isCorrect) correctCount++;

        // 高亮选项
        optionBtns[idx].GetComponent<Image>().color = isCorrect ? CorrectGreen : WrongRed;
        optionBtns[q.correctIndex].GetComponent<Image>().color = CorrectGreen;

        // 显示反馈
        if (isCorrect)
        {
            feedbackText.text = $"✅ 回答正确！\n{q.explanation}";
            feedbackText.color = CorrectGreen;
        }
        else
        {
            feedbackText.text = $"❌ 回答错误\n正确答案：{q.options[q.correctIndex]}\n{q.explanation}";
            feedbackText.color = WrongRed;
        }

        // 禁用当前题选项
        for (int i = 0; i < 4; i++)
            optionBtns[i].GetComponent<Button>().interactable = false;
    }

    private void OnPrevQuestion()
    {
        if (currentQuiz == null || quizIndex <= 0) return;
        quizIndex--;
        ShowCurrentQuestion();
    }

    private void OnNextQuestion()
    {
        if (currentQuiz == null || quizIndex >= currentQuiz.Count - 1) return;
        quizIndex++;
        ShowCurrentQuestion();
    }

    private void OnSubmitQuiz()
    {
        ShowQuizResult();
    }

    private void ShowQuizResult()
    {
        int total = currentQuiz.Count;
        float pct = total > 0 ? (float)correctCount / total * 100f : 0f;

        string grade;
        Color gradeColor;
        if (pct >= 100) { grade = "🏆 学识渊博！大师风范！"; gradeColor = GoldColor; }
        else if (pct >= 66) { grade = "📚 学有所成！继续加油！"; gradeColor = JadeGreen; }
        else { grade = "💪 再接再厉，温故知新！"; gradeColor = ZhuRed; }

        quizQuestionText.text = $"答题完成！\n\n正确：{correctCount} / {total}\n正确率：{pct:F0}%\n\n{grade}";
        quizQuestionText.color = gradeColor;
        quizProgressText.text = "问答结束";

        feedbackText.text = "点击返回，继续探索更多知识";
        feedbackText.color = new Color(0.5f, 0.5f, 0.5f);

        for (int i = 0; i < 4; i++)
            optionBtns[i].SetActive(false);

        // 隐藏导航按钮
        if (prevBtn != null) prevBtn.gameObject.SetActive(false);
        if (nextBtn != null) nextBtn.gameObject.SetActive(false);
        if (submitBtn != null) submitBtn.gameObject.SetActive(false);
    }

    // ──────────────────── 导航 ────────────────────

    private void OnBackClicked()
    {
        switch (currentState)
        {
            case ViewState.CategorySelect:
                SceneLoader.Instance.LoadScene(SceneNames.Main);
                break;
            case ViewState.KnowledgeBrowse:
                ShowCategoryView();
                break;
            case ViewState.Quiz:
                ShowKnowledgeView();
                break;
        }
    }

    // ──────────────────── 卡片点击反馈 ────────────────────

    private System.Collections.IEnumerator CardClickFeedback(Transform card)
    {
        Vector3 orig = card.localScale;
        Vector3 target = orig * 1.05f;
        float duration = 0.12f;

        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            card.localScale = Vector3.Lerp(orig, target, Mathf.Clamp01(t / duration));
            yield return null;
        }
        t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            card.localScale = Vector3.Lerp(target, orig, Mathf.Clamp01(t / duration));
            yield return null;
        }
        card.localScale = orig;
    }
}
