using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 开始场景管理器 — 新中式全屏竖排构图
/// </summary>
public class StartPanel : UIFrame
{
    private GameObject guidePanel;
    private IEnumerator pulseCoroutine;

    private void Start()
    {
        EnsureSingletons();
        CreateUI();
    }

    private void CreateUI()
    {
        var root = InitCanvas();

        // 淡入动画
        var cg = root.gameObject.AddComponent<CanvasGroup>();
        StartCoroutine(FadeIn(cg, 1.2f));

        // 背景
        var bg = NewUI("BG", root);
        Stretch(bg);
        bg.AddComponent<Image>().color = InkBlack;

        // 印章 Logo（大）
        AddSealLogo("Logo", root, new Vector2(0, -80), 120, "非遗", 50);

        // 标题
        AddLabel("Title", root, new Vector2(0, -220), new Vector2(560, 70),
            "了不起的非遗", 48, ZhuRed);

        // 英文副标题
        AddLabel("En", root, new Vector2(0, -280), new Vector2(560, 26),
            "Amazing Intangible Cultural Heritage of China", 14, GoldColor);

        // 分隔线装饰
        AddDivider("Div", root, new Vector2(0, -320), 460, "传承千年智慧 守护文化根脉",
            new Color(0.83f, 0.65f, 0.27f, 0.4f), XuanPaper, 16);

        // 开始按钮
        var startBtn = AddBtn("StartBtn", root, new Vector2(0, -400), new Vector2(300, 60), "开 始 探 索", ZhuRed);
        startBtn.onClick.AddListener(() => SceneLoader.Instance.LoadScene(SceneNames.Main));
        // 脉冲动画
        pulseCoroutine = Pulse(startBtn.transform, 0.97f, 1.03f, 2f);
        StartCoroutine(pulseCoroutine);

        // 引导按钮
        var guideBtn = AddBtn("GuideBtn", root, new Vector2(0, -480), new Vector2(200, 44), "使用引导", JadeGreen);
        guideBtn.onClick.AddListener(() => { if (guidePanel != null) guidePanel.SetActive(true); });

        // 欢迎文字
        if (GameManager.Instance != null && GameManager.Instance.isLoggedIn)
        {
            AddLabel("Welcome", root, new Vector2(0, -540), new Vector2(500, 26),
                $"欢迎回来，{GameManager.Instance.currentUser}！", 18, GoldColor);
        }

        // 引导弹窗
        CreateGuidePanel(root);
    }

    private void CreateGuidePanel(Transform parent)
    {
        guidePanel = MakeOverlay(parent, "使用引导",
            "1. 在主界面选择感兴趣的非遗品类\n\n" +
            "2. 点击展品卡片进入3D展示场景\n\n" +
            "3. 拖拽旋转3D模型，滚轮缩放\n\n" +
            "4. 查看展品的历史、工艺、文化寓意\n\n" +
            "5. 点击「收藏」将展品加入背包\n\n" +
            "6. 在背包中管理您收藏的展品", ZhuRed);
    }
}
