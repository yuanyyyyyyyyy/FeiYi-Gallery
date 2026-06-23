using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AI对话界面 — 新中式风格聊天面板
/// 挂载到场景中，通过 Open()/Close() 控制显隐
/// </summary>
public class AIChatUI : MonoBehaviour
{
    // 主题色（与UIFrame保持一致）
    private static Color ZhuRed    => GameManager.Instance?.themeStyle == "classic" ? new Color(0.62f,0.14f,0.12f) : GameManager.Instance?.themeStyle == "minimal" ? new Color(0.72f,0.30f,0.28f) : new Color(0.76f,0.21f,0.19f);
    private static Color XuanPaper => GameManager.Instance?.themeStyle == "classic" ? new Color(0.22f,0.17f,0.12f) : GameManager.Instance?.themeStyle == "minimal" ? new Color(0.97f,0.97f,0.97f) : new Color(0.96f,0.90f,0.78f);
    private static Color InkBlack  => GameManager.Instance?.themeStyle == "classic" ? new Color(0.88f,0.82f,0.68f) : GameManager.Instance?.themeStyle == "minimal" ? new Color(0.25f,0.25f,0.28f) : new Color(0.17f,0.17f,0.17f);
    private static Color DarkBar   => GameManager.Instance?.themeStyle == "classic" ? new Color(0.14f,0.10f,0.07f,0.97f) : GameManager.Instance?.themeStyle == "minimal" ? new Color(0.92f,0.92f,0.92f,0.98f) : new Color(0.10f,0.10f,0.10f,0.95f);
    private static Color GoldColor => GameManager.Instance?.themeStyle == "classic" ? new Color(0.90f,0.72f,0.30f) : GameManager.Instance?.themeStyle == "minimal" ? new Color(0.50f,0.50f,0.50f) : new Color(0.83f,0.65f,0.27f);

    // UI引用
    private GameObject overlayObj;
    private GameObject panelObj;
    private ScrollRect scrollRect;
    private Transform contentTransform;
    private InputField inputField;
    private GameObject sendBtn;
    private GameObject quizBtn;
    private GameObject newChatBtn;
    private GameObject historyBtn;
    private Text personaNameText;
    private GameObject thinkingObj;

    // 历史会话面板
    private GameObject historyPanelObj;
    private Transform historyListContent;

    // 角色引用
    private CharacterController2D character;

    // 打字机效果
    private Coroutine typewriterCoroutine;
    private Text currentTypingText;
    private string fullTypingText;
    private bool isTyping = false;
    private string lastGreetingPersonaId = null;

    // 思考状态动画
    private Coroutine thinkingAnimCoroutine;
    private Text thinkingText;
    private GameObject thinkingBarFill;
    private float thinkingStartTime;

    private static Font Fnt => UIFont.Get();

    private void Awake()
    {
        CreateUI();
        if (overlayObj != null)
            overlayObj.SetActive(false);
    }

    /// <summary>
    /// 打开对话面板
    /// </summary>
    public void Open()
    {
        overlayObj.SetActive(true);

        // 切换角色人设
        string category = PlayerPrefs.GetString("CurrentCategory", "");
        AIChatManager.Instance.SwitchPersona(category);

        // 更新角色名显示
        UpdatePersonaDisplay();

        // 角色切换时清空旧对话并显示新问候语
        var persona = AIChatManager.Instance.CurrentPersona;
        if (lastGreetingPersonaId != persona.id)
        {
            ClearChatBubbles();
            AddMessageBubble(persona.greeting, false);
            lastGreetingPersonaId = persona.id;
        }

        // 淡入动画
        var cg = overlayObj.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 0f;
            StartCoroutine(FadeInAnim(cg, 0.3f));
        }
    }

    /// <summary>
    /// 关闭对话面板
    /// </summary>
    public void Close()
    {
        // 停止打字机效果
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            CompleteTyping();
        }

        overlayObj.SetActive(false);
    }

    /// <summary>
    /// 是否已打开
    /// </summary>
    public bool IsOpen => overlayObj != null && overlayObj.activeSelf;

    #region UI Creation

    private void CreateUI()
    {
        // 全屏遮罩
        overlayObj = CreateUIObject("ChatOverlay", transform);
        Stretch(overlayObj);
        var olImg = overlayObj.AddComponent<Image>();
        olImg.color = new Color(0, 0, 0, 0.75f);
        olImg.raycastTarget = true;
        overlayObj.AddComponent<CanvasGroup>();

        // 主面板（居中，560x520）
        panelObj = CreateUIObject("ChatPanel", overlayObj.transform);
        var panelR = panelObj.GetComponent<RectTransform>();
        panelR.anchorMin = panelR.anchorMax = new Vector2(0.5f, 0.5f);
        panelR.sizeDelta = new Vector2(560, 520);
        var panelImg = panelObj.AddComponent<Image>();
        panelImg.color = XuanPaper;
        panelImg.raycastTarget = true;

        // 顶部朱红装饰线
        var topLine = CreateUIObject("TopLine", panelObj.transform);
        var tlr = topLine.GetComponent<RectTransform>();
        tlr.anchorMin = new Vector2(0, 1); tlr.anchorMax = new Vector2(1, 1);
        tlr.pivot = new Vector2(0.5f, 1f);
        tlr.sizeDelta = new Vector2(0, 3);
        topLine.AddComponent<Image>().color = ZhuRed;

        // 标题栏
        CreateHeader();

        // 聊天消息区域
        CreateChatArea();

        // 底部输入区域
        CreateInputArea();

        // 历史会话面板
        CreateHistoryPanel();
    }

    private void CreateHeader()
    {
        // 标题栏背景
        var headerBg = CreateUIObject("HeaderBg", panelObj.transform);
        var hr = headerBg.GetComponent<RectTransform>();
        hr.anchorMin = new Vector2(0, 1); hr.anchorMax = new Vector2(1, 1);
        hr.pivot = new Vector2(0.5f, 1f);
        hr.sizeDelta = new Vector2(0, 48);
        hr.anchoredPosition = new Vector2(0, -3);
        headerBg.AddComponent<Image>().color = DarkBar;

        // 印章图标
        var sealObj = CreateUIObject("Seal", headerBg.transform);
        var sr = sealObj.GetComponent<RectTransform>();
        sr.anchorMin = sr.anchorMax = new Vector2(0, 0.5f);
        sr.pivot = new Vector2(0, 0.5f);
        sr.sizeDelta = new Vector2(32, 32);
        sr.anchoredPosition = new Vector2(15, 0);
        sealObj.AddComponent<Image>().color = ZhuRed;
        var sealText = CreateUIObject("ST", sealObj.transform);
        Stretch(sealText);
        var st = sealText.AddComponent<Text>();
        st.font = Fnt; st.text = "话"; st.fontSize = 18; st.color = Color.white; st.alignment = TextAnchor.MiddleCenter;

        // 角色名
        var nameObj = CreateUIObject("PersonaName", headerBg.transform);
        var nr = nameObj.GetComponent<RectTransform>();
        nr.anchorMin = new Vector2(0, 0); nr.anchorMax = new Vector2(0.7f, 1);
        nr.offsetMin = new Vector2(55, 0); nr.offsetMax = new Vector2(-160, 0);
        personaNameText = nameObj.AddComponent<Text>();
        personaNameText.font = Fnt; personaNameText.text = "守艺人"; personaNameText.fontSize = 20;
        personaNameText.color = GoldColor; personaNameText.alignment = TextAnchor.MiddleLeft;

        // 新建会话按钮
        newChatBtn = CreateUIObject("NewChatBtn", headerBg.transform);
        var nbr = newChatBtn.GetComponent<RectTransform>();
        nbr.anchorMin = nbr.anchorMax = new Vector2(1, 0.5f);
        nbr.pivot = new Vector2(1, 0.5f);
        nbr.sizeDelta = new Vector2(52, 28);
        nbr.anchoredPosition = new Vector2(-98, 0);
        newChatBtn.AddComponent<Image>().color = new Color(0.45f, 0.35f, 0.15f, 0.9f);
        newChatBtn.AddComponent<Button>().onClick.AddListener(OnNewChatClicked);
        var nct = CreateUIObject("NCT", newChatBtn.transform);
        Stretch(nct);
        var ncTxt = nct.AddComponent<Text>();
        ncTxt.font = Fnt; ncTxt.text = "新建"; ncTxt.fontSize = 13; ncTxt.color = Color.white; ncTxt.alignment = TextAnchor.MiddleCenter;

        // 历史会话按钮
        historyBtn = CreateUIObject("HistoryBtn", headerBg.transform);
        var hbr = historyBtn.GetComponent<RectTransform>();
        hbr.anchorMin = hbr.anchorMax = new Vector2(1, 0.5f);
        hbr.pivot = new Vector2(1, 0.5f);
        hbr.sizeDelta = new Vector2(52, 28);
        hbr.anchoredPosition = new Vector2(-40, 0);
        historyBtn.AddComponent<Image>().color = new Color(0.45f, 0.35f, 0.15f, 0.9f);
        historyBtn.AddComponent<Button>().onClick.AddListener(OnHistoryClicked);
        var hbt = CreateUIObject("HBT", historyBtn.transform);
        Stretch(hbt);
        var hbTxt = hbt.AddComponent<Text>();
        hbTxt.font = Fnt; hbTxt.text = "历史"; hbTxt.fontSize = 13; hbTxt.color = Color.white; hbTxt.alignment = TextAnchor.MiddleCenter;

        // 关闭按钮
        var closeBtn = CreateUIObject("CloseBtn", headerBg.transform);
        var cbr = closeBtn.GetComponent<RectTransform>();
        cbr.anchorMin = cbr.anchorMax = new Vector2(1, 0.5f);
        cbr.pivot = new Vector2(1, 0.5f);
        cbr.sizeDelta = new Vector2(28, 28);
        cbr.anchoredPosition = new Vector2(-6, 0);
        closeBtn.AddComponent<Image>().color = ZhuRed;
        closeBtn.AddComponent<Button>().onClick.AddListener(Close);
        var closeTxt = CreateUIObject("CT", closeBtn.transform);
        Stretch(closeTxt);
        var ct = closeTxt.AddComponent<Text>();
        ct.font = Fnt; ct.text = "X"; ct.fontSize = 16; ct.color = Color.white; ct.alignment = TextAnchor.MiddleCenter;
    }

    private void CreateChatArea()
    {
        // 聊天区域（Header下方到InputArea上方）
        var viewport = CreateUIObject("Viewport", panelObj.transform);
        var vpR = viewport.GetComponent<RectTransform>();
        vpR.anchorMin = Vector2.zero; vpR.anchorMax = Vector2.one;
        vpR.offsetMin = new Vector2(12, 60);    // 底部留给输入区
        vpR.offsetMax = new Vector2(-12, -55);   // 顶部留给标题栏
        var vpImg = viewport.AddComponent<Image>();
        vpImg.color = new Color(1, 1, 1, 1);
        vpImg.raycastTarget = true;
        var mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // 内容容器
        var content = CreateUIObject("Content", viewport.transform);
        var cR = content.GetComponent<RectTransform>();
        cR.anchorMin = new Vector2(0, 1); cR.anchorMax = new Vector2(1, 1);
        cR.pivot = new Vector2(0.5f, 1f);
        cR.sizeDelta = new Vector2(0, 0);
        contentTransform = content.transform;

        // VerticalLayoutGroup 自动排列消息气泡
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 10;
        vlg.padding = new RectOffset(8, 8, 8, 8);

        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ScrollRect
        scrollRect = viewport.AddComponent<ScrollRect>();
        scrollRect.content = cR;
        scrollRect.viewport = vpR as RectTransform;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;

        // 思考中提示（带动画状态 + 进度条）
        thinkingObj = CreateUIObject("Thinking", content.transform);
        var thLe = thinkingObj.AddComponent<LayoutElement>();
        thLe.preferredHeight = 52;
        thLe.flexibleWidth = 0;
        var thBubble = CreateUIObject("Bubble", thinkingObj.transform);
        var tbR = thBubble.GetComponent<RectTransform>();
        tbR.anchorMin = Vector2.zero; tbR.anchorMax = Vector2.one;
        tbR.offsetMin = Vector2.zero; tbR.offsetMax = Vector2.zero;
        var thImg = thBubble.AddComponent<Image>();
        thImg.color = new Color(0.96f, 0.90f, 0.78f, 0.9f);
        thImg.raycastTarget = false;

        // 状态文字（左对齐，带计时）
        var thTextObj = CreateUIObject("T", thBubble.transform);
        var thTxtR = thTextObj.GetComponent<RectTransform>();
        thTxtR.anchorMin = new Vector2(0, 0.4f); thTxtR.anchorMax = new Vector2(1, 1f);
        thTxtR.offsetMin = new Vector2(12, 0); thTxtR.offsetMax = new Vector2(-12, -2);
        thinkingText = thTextObj.AddComponent<Text>();
        thinkingText.font = Fnt;
        if (thinkingText.font == null) thinkingText.font = Font.CreateDynamicFontFromOSFont("Arial", 15);
        thinkingText.text = "正在连接AI服务..."; thinkingText.fontSize = 14;
        thinkingText.color = new Color(0.17f, 0.17f, 0.17f, 0.7f);
        thinkingText.alignment = TextAnchor.MiddleLeft;

        // 进度条背景
        var barBg = CreateUIObject("BarBg", thBubble.transform);
        var bbR = barBg.GetComponent<RectTransform>();
        bbR.anchorMin = new Vector2(0, 0.08f); bbR.anchorMax = new Vector2(1, 0.32f);
        bbR.offsetMin = new Vector2(12, 0); bbR.offsetMax = new Vector2(-12, 0);
        barBg.AddComponent<Image>().color = new Color(0.85f, 0.82f, 0.75f, 0.6f);

        // 进度条填充
        thinkingBarFill = CreateUIObject("BarFill", barBg.transform);
        var bfR = thinkingBarFill.GetComponent<RectTransform>();
        bfR.anchorMin = new Vector2(0, 0); bfR.anchorMax = new Vector2(0, 1);
        bfR.pivot = new Vector2(0, 0.5f);
        bfR.offsetMin = bfR.offsetMax = Vector2.zero;
        var bfImg = thinkingBarFill.AddComponent<Image>();
        bfImg.color = ZhuRed;
        bfImg.raycastTarget = false;

        thinkingObj.SetActive(false);
    }

    private void CreateInputArea()
    {
        // 底部输入区域
        var inputArea = CreateUIObject("InputArea", panelObj.transform);
        var iaR = inputArea.GetComponent<RectTransform>();
        iaR.anchorMin = new Vector2(0, 0); iaR.anchorMax = new Vector2(1, 0);
        iaR.pivot = new Vector2(0.5f, 0f);
        iaR.sizeDelta = new Vector2(0, 55);
        var iaImg = inputArea.AddComponent<Image>();
        iaImg.color = DarkBar;

        // 考考我按钮
        quizBtn = CreateUIObject("QuizBtn", inputArea.transform);
        var qr = quizBtn.GetComponent<RectTransform>();
        qr.anchorMin = qr.anchorMax = new Vector2(0, 0.5f);
        qr.pivot = new Vector2(0, 0.5f);
        qr.sizeDelta = new Vector2(70, 36);
        qr.anchoredPosition = new Vector2(10, 0);
        quizBtn.AddComponent<Image>().color = GoldColor;
        quizBtn.AddComponent<Button>().onClick.AddListener(OnQuizClicked);
        var qt = CreateUIObject("QT", quizBtn.transform);
        Stretch(qt);
        var qTxt = qt.AddComponent<Text>();
        qTxt.font = Fnt; qTxt.text = "考考我"; qTxt.fontSize = 14; qTxt.color = Color.white; qTxt.alignment = TextAnchor.MiddleCenter;

        // 输入框
        var inputObj = CreateUIObject("Input", inputArea.transform);
        var ir = inputObj.GetComponent<RectTransform>();
        ir.anchorMin = new Vector2(0, 0.1f); ir.anchorMax = new Vector2(0.8f, 0.9f);
        ir.offsetMin = new Vector2(90, 0); ir.offsetMax = new Vector2(-65, 0);
        inputObj.AddComponent<Image>().color = new Color(XuanPaper.r, XuanPaper.g, XuanPaper.b, 0.3f);

        var inputText = CreateUIObject("IT", inputObj.transform);
        var itr = inputText.GetComponent<RectTransform>();
        itr.anchorMin = Vector2.zero; itr.anchorMax = Vector2.one;
        itr.offsetMin = new Vector2(8, 2); itr.offsetMax = new Vector2(-8, -2);
        var it = inputText.AddComponent<Text>();
        it.font = Fnt; it.fontSize = 16; it.color = InkBlack; it.alignment = TextAnchor.MiddleLeft;

        var placeholder = CreateUIObject("PH", inputObj.transform);
        var phr = placeholder.GetComponent<RectTransform>();
        phr.anchorMin = Vector2.zero; phr.anchorMax = Vector2.one;
        phr.offsetMin = new Vector2(8, 2); phr.offsetMax = new Vector2(-8, -2);
        var pht = placeholder.AddComponent<Text>();
        pht.font = Fnt; pht.text = "向守艺人提问..."; pht.fontSize = 14;
        pht.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); pht.alignment = TextAnchor.MiddleLeft;

        inputField = inputObj.AddComponent<InputField>();
        inputField.textComponent = it;
        inputField.placeholder = pht;
        inputField.onEndEdit.AddListener(OnInputEndEdit);

        // 发送按钮
        sendBtn = CreateUIObject("SendBtn", inputArea.transform);
        var sbr = sendBtn.GetComponent<RectTransform>();
        sbr.anchorMin = sbr.anchorMax = new Vector2(1, 0.5f);
        sbr.pivot = new Vector2(1, 0.5f);
        sbr.sizeDelta = new Vector2(55, 36);
        sbr.anchoredPosition = new Vector2(-8, 0);
        sendBtn.AddComponent<Image>().color = ZhuRed;
        sendBtn.AddComponent<Button>().onClick.AddListener(OnSendClicked);
        var sbt = CreateUIObject("SBT", sendBtn.transform);
        Stretch(sbt);
        var sbTxt = sbt.AddComponent<Text>();
        sbTxt.font = Fnt; sbTxt.text = "发送"; sbTxt.fontSize = 14; sbTxt.color = Color.white; sbTxt.alignment = TextAnchor.MiddleCenter;
    }

    #endregion

    #region Message Bubbles

    /// <summary>
    /// 添加消息气泡
    /// </summary>
    /// <param name="text">消息内容</param>
    /// <param name="isUser">是否为用户消息</param>
    private void AddMessageBubble(string text, bool isUser)
    {
        var bubble = CreateUIObject(isUser ? "UserMsg" : "AIMsg", contentTransform);
        var le = bubble.AddComponent<LayoutElement>();
        le.minWidth = 80;
        le.preferredWidth = 460;
        le.flexibleWidth = 0;

        // 气泡背景
        var bubbleBg = CreateUIObject("Bg", bubble.transform);
        Stretch(bubbleBg);
        var bgImg = bubbleBg.AddComponent<Image>();
        bgImg.raycastTarget = false;

        // 气泡文字
        var textObj = CreateUIObject("T", bubble.transform);
        var tR = textObj.GetComponent<RectTransform>();
        tR.anchorMin = Vector2.zero; tR.anchorMax = Vector2.one;
        tR.offsetMin = new Vector2(12, 8); tR.offsetMax = new Vector2(-12, -8);
        var txt = textObj.AddComponent<Text>();
        txt.font = Fnt; txt.text = text; txt.fontSize = 15; txt.alignment = TextAnchor.UpperLeft;
        txt.lineSpacing = 1.3f;
        txt.supportRichText = false;

        if (isUser)
        {
            // 用户消息：朱红边框 + 白底
            bgImg.color = new Color(1, 1, 1, 0.9f);
            txt.color = InkBlack;
            var outline = bubbleBg.AddComponent<Outline>();
            outline.effectColor = ZhuRed;
            outline.effectDistance = new Vector2(1.5f, 1.5f);
        }
        else
        {
            // AI消息：宣纸色背景 + 墨字
            bgImg.color = new Color(XuanPaper.r * 0.95f, XuanPaper.g * 0.95f, XuanPaper.b * 0.95f, 0.9f);
            txt.color = InkBlack;
        }

        // 计算文字高度并设置气泡高度
        ApplyBubbleHeight(bubble, txt, text);

        // 滚动到底部
        StartCoroutine(ScrollToBottom());
    }

    /// <summary>
    /// 添加AI回复气泡（带打字机效果）
    /// </summary>
    private void AddAIMessageWithTypewriter(string text)
    {
        var bubble = CreateUIObject("AIMsg", contentTransform);
        var le = bubble.AddComponent<LayoutElement>();
        le.minWidth = 80;
        le.preferredWidth = 460;
        le.flexibleWidth = 0;

        // 气泡背景
        var bubbleBg = CreateUIObject("Bg", bubble.transform);
        Stretch(bubbleBg);
        var bgImg = bubbleBg.AddComponent<Image>();
        bgImg.raycastTarget = false;
        bgImg.color = new Color(XuanPaper.r * 0.95f, XuanPaper.g * 0.95f, XuanPaper.b * 0.95f, 0.9f);

        // 气泡文字（初始为空，打字机效果逐字填充）
        var textObj = CreateUIObject("T", bubble.transform);
        var tR = textObj.GetComponent<RectTransform>();
        tR.anchorMin = Vector2.zero; tR.anchorMax = Vector2.one;
        tR.offsetMin = new Vector2(12, 8); tR.offsetMax = new Vector2(-12, -8);
        var txt = textObj.AddComponent<Text>();
        txt.font = Fnt; txt.text = ""; txt.fontSize = 15; txt.color = InkBlack;
        txt.alignment = TextAnchor.UpperLeft;
        txt.lineSpacing = 1.3f;
        txt.supportRichText = false;

        // 计算文字高度并设置气泡高度（使用完整文字计算）
        ApplyBubbleHeight(bubble, txt, text);

        // 启动打字机效果
        currentTypingText = txt;
        fullTypingText = text;
        isTyping = true;
        typewriterCoroutine = StartCoroutine(TypewriterEffect(txt, text));
    }

    /// <summary>
    /// 计算文字高度并设置气泡的preferredHeight
    /// </summary>
    private void ApplyBubbleHeight(GameObject bubble, Text txt, string text)
    {
        // 先强制布局以获取正确宽度
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentTransform as RectTransform);

        // 获取文本可用宽度
        float bubbleWidth = bubble.GetComponent<RectTransform>().rect.width;
        float textWidth = Mathf.Max(100, bubbleWidth - 24); // 减去左右各12的padding

        // 使用Text的cachedTextGenerator计算首选高度
        var settings = txt.GetGenerationSettings(new Vector2(textWidth, 0));
        float prefHeight = txt.cachedTextGenerator.GetPreferredHeight(text, settings);
        float finalHeight = Mathf.Max(36, prefHeight + 16); // 加上上下各8的padding，最小36

        var le = bubble.GetComponent<LayoutElement>();
        if (le != null) le.preferredHeight = finalHeight;

        // 再次强制布局以应用高度
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentTransform as RectTransform);
    }

    /// <summary>
    /// 打字机效果协程
    /// </summary>
    private IEnumerator TypewriterEffect(Text txt, string fullText)
    {
        int charIndex = 0;
        while (charIndex < fullText.Length)
        {
            charIndex++;
            txt.text = fullText.Substring(0, charIndex);
            yield return new WaitForSeconds(0.03f);
        }
        isTyping = false;
        typewriterCoroutine = null;
    }

    /// <summary>
    /// 完成打字机效果（立即显示全部文字）
    /// </summary>
    private void CompleteTyping()
    {
        if (currentTypingText != null && fullTypingText != null)
        {
            currentTypingText.text = fullTypingText;
        }
        isTyping = false;
        typewriterCoroutine = null;
        currentTypingText = null;
    }

    private IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    #endregion

    #region History Panel

    private void CreateHistoryPanel()
    {
        // 半透明遮罩
        historyPanelObj = CreateUIObject("HistoryPanel", overlayObj.transform);
        Stretch(historyPanelObj);
        historyPanelObj.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);
        historyPanelObj.AddComponent<CanvasGroup>();

        // 面板（居中，480x420）
        var panel = CreateUIObject("HistCard", historyPanelObj.transform);
        var pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(480, 420);
        panel.AddComponent<Image>().color = XuanPaper;

        // 顶部装饰线
        var topLine = CreateUIObject("HTopLine", panel.transform);
        var tlr = topLine.GetComponent<RectTransform>();
        tlr.anchorMin = new Vector2(0, 1); tlr.anchorMax = new Vector2(1, 1);
        tlr.pivot = new Vector2(0.5f, 1f);
        tlr.sizeDelta = new Vector2(0, 3);
        topLine.AddComponent<Image>().color = ZhuRed;

        // 标题
        var titleObj = CreateUIObject("HTitle", panel.transform);
        var tr = titleObj.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0, 1); tr.anchorMax = new Vector2(0.8f, 1);
        tr.pivot = new Vector2(0, 1f);
        tr.sizeDelta = new Vector2(0, 40);
        tr.anchoredPosition = new Vector2(15, -8);
        var titleTxt = titleObj.AddComponent<Text>();
        titleTxt.font = Fnt; titleTxt.text = "历史会话"; titleTxt.fontSize = 18; titleTxt.color = ZhuRed; titleTxt.alignment = TextAnchor.MiddleLeft;

        // 关闭按钮
        var closeBtn = CreateUIObject("HClose", panel.transform);
        var cbr = closeBtn.GetComponent<RectTransform>();
        cbr.anchorMin = cbr.anchorMax = new Vector2(1, 1);
        cbr.pivot = new Vector2(1, 1f);
        cbr.sizeDelta = new Vector2(28, 28);
        cbr.anchoredPosition = new Vector2(-6, -6);
        closeBtn.AddComponent<Image>().color = ZhuRed;
        closeBtn.AddComponent<Button>().onClick.AddListener(HideHistoryPanel);
        var cTxt = CreateUIObject("CT", closeBtn.transform);
        Stretch(cTxt);
        var ct = cTxt.AddComponent<Text>();
        ct.font = Fnt; ct.text = "X"; ct.fontSize = 14; ct.color = Color.white; ct.alignment = TextAnchor.MiddleCenter;

        // 滚动列表区域
        var scrollObj = CreateUIObject("HScroll", panel.transform);
        var slR = scrollObj.GetComponent<RectTransform>();
        slR.anchorMin = new Vector2(0.05f, 0.05f);
        slR.anchorMax = new Vector2(0.95f, 0.88f);
        slR.offsetMin = slR.offsetMax = Vector2.zero;
        scrollObj.AddComponent<Image>().color = new Color(1, 1, 1, 0.5f);
        var slMask = scrollObj.AddComponent<Mask>();
        slMask.showMaskGraphic = false;

        var content = CreateUIObject("HListContent", scrollObj.transform);
        var cR = content.GetComponent<RectTransform>();
        cR.anchorMin = new Vector2(0, 1); cR.anchorMax = new Vector2(1, 1);
        cR.pivot = new Vector2(0.5f, 1f);
        cR.sizeDelta = new Vector2(0, 0);
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 6;
        vlg.padding = new RectOffset(4, 4, 4, 4);
        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var sr = scrollObj.AddComponent<ScrollRect>();
        sr.content = cR;
        sr.viewport = slR;
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.inertia = true;

        historyListContent = content.transform;

        // 空提示
        var emptyObj = CreateUIObject("EmptyHint", panel.transform);
        var er = emptyObj.GetComponent<RectTransform>();
        er.anchorMin = Vector2.zero; er.anchorMax = Vector2.one;
        er.offsetMin = er.offsetMax = Vector2.zero;
        var eTxt = emptyObj.AddComponent<Text>();
        eTxt.font = Fnt; eTxt.text = "暂无历史会话"; eTxt.fontSize = 16;
        eTxt.color = new Color(0.5f, 0.5f, 0.5f, 0.6f); eTxt.alignment = TextAnchor.MiddleCenter;
        eTxt.raycastTarget = false;

        historyPanelObj.SetActive(false);
    }

    private void ShowHistoryPanel()
    {
        // 停止打字机
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            CompleteTyping();
        }

        PopulateHistoryList();
        historyPanelObj.SetActive(true);

        var cg = historyPanelObj.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 0f;
            StartCoroutine(FadeInAnim(cg, 0.2f));
        }
    }

    private void HideHistoryPanel()
    {
        historyPanelObj.SetActive(false);
    }

    private void PopulateHistoryList()
    {
        // 清空旧列表
        var toDestroy = new System.Collections.Generic.List<GameObject>();
        foreach (Transform child in historyListContent)
            toDestroy.Add(child.gameObject);
        foreach (var go in toDestroy)
            Destroy(go);

        var sessions = AIChatManager.Instance.GetSavedSessions();
        var emptyHint = historyPanelObj.transform.Find("HistCard/EmptyHint");

        if (sessions == null || sessions.Count == 0)
        {
            if (emptyHint != null) emptyHint.gameObject.SetActive(true);
            return;
        }

        if (emptyHint != null) emptyHint.gameObject.SetActive(false);

        for (int i = 0; i < sessions.Count; i++)
        {
            var session = sessions[i];
            int idx = i;

            // 会话条目
            var item = CreateUIObject($"Session_{i}", historyListContent);
            var le = item.AddComponent<LayoutElement>();
            le.preferredHeight = 64;

            var itemImg = item.AddComponent<Image>();
            itemImg.color = i % 2 == 0 ? new Color(1, 1, 1, 0.6f) : new Color(0.95f, 0.90f, 0.78f, 0.5f);
            itemImg.raycastTarget = true;

            var itemBtn = item.AddComponent<Button>();
            itemBtn.onClick.AddListener(() => OnSessionItemClicked(idx));

            // 角色名 + 时间
            var topRow = CreateUIObject("TopRow", item.transform);
            var trR = topRow.GetComponent<RectTransform>();
            trR.anchorMin = new Vector2(0, 0.5f); trR.anchorMax = new Vector2(0.85f, 1);
            trR.offsetMin = new Vector2(8, 0); trR.offsetMax = Vector2.zero;
            var topTxt = topRow.AddComponent<Text>();
            topTxt.font = Fnt;
            topTxt.text = $"{session.personaName}  ·  {session.timestamp}";
            topTxt.fontSize = 14; topTxt.color = ZhuRed; topTxt.alignment = TextAnchor.MiddleLeft;

            // 预览文字
            var prevRow = CreateUIObject("Preview", item.transform);
            var pvR = prevRow.GetComponent<RectTransform>();
            pvR.anchorMin = new Vector2(0, 0); pvR.anchorMax = new Vector2(0.85f, 0.5f);
            pvR.offsetMin = new Vector2(8, 4); pvR.offsetMax = new Vector2(-8, 0);
            var pvTxt = prevRow.AddComponent<Text>();
            pvTxt.font = Fnt;
            pvTxt.text = string.IsNullOrEmpty(session.preview) ? "(无消息)" : session.preview;
            pvTxt.fontSize = 13; pvTxt.color = InkBlack; pvTxt.alignment = TextAnchor.MiddleLeft;

            // 删除按钮
            var delBtn = CreateUIObject("DelBtn", item.transform);
            var dr = delBtn.GetComponent<RectTransform>();
            dr.anchorMin = dr.anchorMax = new Vector2(1, 0.5f);
            dr.pivot = new Vector2(1, 0.5f);
            dr.sizeDelta = new Vector2(28, 28);
            dr.anchoredPosition = new Vector2(-4, 0);
            delBtn.AddComponent<Image>().color = new Color(0.76f, 0.21f, 0.19f, 0.7f);
            var delBtnComp = delBtn.AddComponent<Button>();
            delBtnComp.onClick.AddListener(() => OnDeleteSessionClicked(idx));
            var delT = CreateUIObject("DT", delBtn.transform);
            Stretch(delT);
            var delTxt = delT.AddComponent<Text>();
            delTxt.font = Fnt; delTxt.text = "删"; delTxt.fontSize = 12; delTxt.color = Color.white; delTxt.alignment = TextAnchor.MiddleCenter;
        }
    }

    private void OnSessionItemClicked(int index)
    {
        var session = AIChatManager.Instance.LoadSession(index);
        if (session == null) return;

        // 清空当前聊天气泡
        ClearChatBubbles();

        // 更新角色显示
        UpdatePersonaDisplay();

        // 重建消息气泡
        foreach (var msg in session.messages)
        {
            AddMessageBubble(msg.content, msg.role == "user");
        }

        // 防止再次显示开场白
        lastGreetingPersonaId = session.personaId;

        HideHistoryPanel();
        AudioManager.Instance?.PlayClick();
        StartCoroutine(ScrollToBottom());
    }

    private void OnDeleteSessionClicked(int index)
    {
        AIChatManager.Instance.DeleteSession(index);
        PopulateHistoryList();
        AudioManager.Instance?.PlayClick();
    }

    #endregion

    #region Button Handlers

    private void OnSendClicked()
    {
        SendUserMessage();
    }

    private void OnInputEndEdit(string text)
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SendUserMessage();
        }
    }

    private void OnNewChatClicked()
    {
        if (isTyping) CompleteTyping();

        // 保存当前会话并清空历史
        AIChatManager.Instance.NewSession();

        // 清空气泡
        ClearChatBubbles();

        // 重置开场白标记，重新显示问候语
        lastGreetingPersonaId = null;

        var persona = AIChatManager.Instance.CurrentPersona;
        AddMessageBubble(persona.greeting, false);
        lastGreetingPersonaId = persona.id;

        AudioManager.Instance?.PlayClick();
        StartCoroutine(ScrollToBottom());
    }

    private void OnHistoryClicked()
    {
        AudioManager.Instance?.PlayClick();
        ShowHistoryPanel();
    }

    private void OnQuizClicked()
    {
        if (AIChatManager.Instance.IsWaitingResponse) return;
        if (isTyping)
        {
            CompleteTyping();
        }

        AddMessageBubble("请考考我！", true);
        AudioManager.Instance?.PlayClick();

        StartThinkingIndicator();

        AIChatManager.Instance.RequestQuiz((response) =>
        {
            StopThinkingIndicator();

            if (response != null)
            {
                AddAIMessageWithTypewriter(response);
            }
            else
            {
                AddMessageBubble("无法连接到AI服务，请在「设置 → AI设置」中检查配置并测试连接。", false);
            }

            SetInputInteractable(true);
            StartCoroutine(ScrollToBottom());
        }, (status) =>
        {
            UpdateThinkingStatus(status);
        });
    }

    private void SendUserMessage()
    {
        if (inputField == null || string.IsNullOrEmpty(inputField.text.Trim())) return;
        if (AIChatManager.Instance.IsWaitingResponse) return;

        if (isTyping)
        {
            CompleteTyping();
        }

        string msg = inputField.text.Trim();
        inputField.text = "";

        AddMessageBubble(msg, true);
        AudioManager.Instance?.PlayClick();

        StartThinkingIndicator();

        AIChatManager.Instance.SendMessage(msg, (response) =>
        {
            StopThinkingIndicator();

            if (response != null)
            {
                AddAIMessageWithTypewriter(response);
            }
            else
            {
                AddMessageBubble("无法连接到AI服务，请在「设置 → AI设置」中检查配置并测试连接。", false);
            }

            SetInputInteractable(true);
            StartCoroutine(ScrollToBottom());
        }, (status) =>
        {
            UpdateThinkingStatus(status);
        });
    }

    private void StartThinkingIndicator()
    {
        thinkingObj.transform.SetAsLastSibling();
        thinkingObj.SetActive(true);
        SetInputInteractable(false);
        thinkingStartTime = Time.time;
        currentStatus = "守艺人思考中";
        if (thinkingAnimCoroutine != null) StopCoroutine(thinkingAnimCoroutine);
        thinkingAnimCoroutine = StartCoroutine(ThinkingAnimRoutine());
    }

    private void StopThinkingIndicator()
    {
        if (thinkingAnimCoroutine != null)
        {
            StopCoroutine(thinkingAnimCoroutine);
            thinkingAnimCoroutine = null;
        }
        thinkingObj.SetActive(false);
    }

    private void UpdateThinkingStatus(string status)
    {
        // status 由 AIChatClient 通过 onStatus 回调传入
        // 在 ThinkingAnimRoutine 中会与计时器组合显示
        if (!string.IsNullOrEmpty(status))
            currentStatus = status;
    }

    private string currentStatus = "正在连接AI服务...";

    private IEnumerator ThinkingAnimRoutine()
    {
        string[] dots = { "·", "··", "···" };
        int dotIdx = 0;
        float barWidth = 0f;
        var barRect = thinkingBarFill?.GetComponent<RectTransform>();
        float parentWidth = 0f;

        // 等一帧让布局计算完成
        yield return null;
        if (barRect?.parent != null)
            parentWidth = ((RectTransform)barRect.parent).rect.width;

        while (true)
        {
            float elapsed = Time.time - thinkingStartTime;
            int seconds = Mathf.FloorToInt(elapsed);
            string timeStr = seconds >= 60 ? $"{seconds / 60}:{seconds % 60:00}" : $"{seconds}s";

            if (thinkingText != null)
            {
                thinkingText.text = $"{currentStatus} {dots[dotIdx]}  ({timeStr})";
            }

            // 进度条：假进度，缓慢爬升到 90%，重试时跳到 50%
            float target = currentStatus.Contains("重试") ? 0.5f : 0.9f;
            barWidth = Mathf.MoveTowards(barWidth, target, Time.deltaTime * 0.4f);
            if (barRect != null && parentWidth > 0)
                barRect.sizeDelta = new Vector2(parentWidth * barWidth, 0);

            dotIdx = (dotIdx + 1) % 3;
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void SetInputInteractable(bool interactable)
    {
        if (inputField != null) inputField.interactable = interactable;
        if (sendBtn != null) sendBtn.GetComponent<Button>().interactable = interactable;
        if (quizBtn != null) quizBtn.GetComponent<Button>().interactable = interactable;
        if (newChatBtn != null) newChatBtn.GetComponent<Button>().interactable = interactable;
        if (historyBtn != null) historyBtn.GetComponent<Button>().interactable = interactable;
    }

    /// <summary>
    /// 清空所有聊天消息气泡（保留 thinkingObj）
    /// </summary>
    private void ClearChatBubbles()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            CompleteTyping();
        }
        var toDestroy = new System.Collections.Generic.List<GameObject>();
        foreach (Transform child in contentTransform)
        {
            if (child == thinkingObj.transform) continue;
            toDestroy.Add(child.gameObject);
        }
        foreach (var go in toDestroy)
            Destroy(go);
    }

    #endregion

    #region Helpers

    private void UpdatePersonaDisplay()
    {
        var persona = AIChatManager.Instance.CurrentPersona;
        if (personaNameText != null)
        {
            personaNameText.text = persona.category != null
                ? $"守艺人 · {persona.name}"
                : persona.name;
        }

        // 更新思考提示文字
        if (thinkingText != null)
        {
            // 仅在非等待状态时更新基础文字
            if (!AIChatManager.Instance.IsWaitingResponse)
                thinkingText.text = $"{persona.name}思考中...";
        }

        // 更新输入框占位文字
        if (inputField != null && inputField.placeholder != null)
        {
            var ph = inputField.placeholder.GetComponent<Text>();
            if (ph != null)
                ph.text = $"向{persona.name}提问...";
        }
    }

    private IEnumerator FadeInAnim(CanvasGroup cg, float duration)
    {
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Clamp01(t / duration);
            yield return null;
        }
        cg.alpha = 1f;
    }

    private GameObject CreateUIObject(string name, Transform parent)
    {
        var o = new GameObject(name);
        o.transform.SetParent(parent, false);
        o.AddComponent<RectTransform>();
        return o;
    }

    private void Stretch(GameObject o)
    {
        var r = o.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.sizeDelta = Vector2.zero;
    }

    #endregion
}
