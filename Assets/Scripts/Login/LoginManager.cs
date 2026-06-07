using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 登录管理器 — 新中式风格，运行时创建 UI
/// </summary>
public class LoginManager : UIFrame
{
    public Color errorColor = new Color(0.76f, 0.21f, 0.19f);
    public Color successColor = new Color(0.18f, 0.48f, 0.43f);

    private InputField usernameInput;
    private InputField passwordInput;
    private Text messageText;
    private CanvasGroup rootCanvasGroup;

    private void Start()
    {
        EnsureSingletons();
        CreateUI();
        string lastUser = PlayerPrefs.GetString("LastUser", "");
        if (!string.IsNullOrEmpty(lastUser)) usernameInput.text = lastUser;
    }

    private void CreateUI()
    {
        var root = InitCanvas();

        // 淡入动画
        rootCanvasGroup = root.gameObject.AddComponent<CanvasGroup>();
        StartCoroutine(FadeIn(rootCanvasGroup, 0.8f));

        // 背景
        var bg = NewUI("BG", root);
        Stretch(bg);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = InkBlack;
        bgImg.raycastTarget = false;

        // 居中面板
        var panel = NewUI("Panel", root);
        var pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(440, 560);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = XuanPaper;
        panelImg.raycastTarget = false;

        // 中式边框装饰线
        AddBorderLines(panel, 3f);

        // 印章 Logo
        AddSealLogo("Logo", panel.transform, new Vector2(0, -35), 90, "非遗", 36);

        // 标题
        AddLabel("Title", panel.transform, new Vector2(0, -140), new Vector2(400, 50),
            "了不起的非遗", 34, ZhuRed);

        // 副标题
        AddLabel("Sub", panel.transform, new Vector2(0, -185), new Vector2(400, 26),
            "弘扬中华优秀传统文化 增强文化自信", 15, GoldColor);

        // 分隔线
        AddDivider("Div", panel.transform, new Vector2(0, -215), 320, "— 登录 —", new Color(0.76f, 0.21f, 0.19f, 0.3f), GoldColor, 14);

        // 用户名
        AddLabel("ULbl", panel.transform, new Vector2(0, -245), new Vector2(300, 22),
            "用户名", 15, InkBlack);
        usernameInput = AddInputField("UInput", panel.transform, new Vector2(0, -275), new Vector2(320, 40), "请输入用户名");

        // 密码
        AddLabel("PLbl", panel.transform, new Vector2(0, -330), new Vector2(300, 22),
            "密码", 15, InkBlack);
        passwordInput = AddInputField("PInput", panel.transform, new Vector2(0, -360), new Vector2(320, 40), "请输入密码", true);

        // 登录按钮
        var loginBtn = AddBtn("LoginBtn", panel.transform, new Vector2(0, -420), new Vector2(320, 48), "登  录", ZhuRed);

        // 注册按钮
        var regBtn = AddBtn("RegBtn", panel.transform, new Vector2(0, -485), new Vector2(320, 48), "注  册", GoldColor);

        // 提示信息
        var msgObj = NewUI("Msg", panel.transform);
        var mr = msgObj.GetComponent<RectTransform>();
        mr.anchorMin = mr.anchorMax = new Vector2(0.5f, 1f);
        mr.pivot = new Vector2(0.5f, 1f);
        mr.sizeDelta = new Vector2(360, 26);
        mr.anchoredPosition = new Vector2(0, -525);
        messageText = msgObj.AddComponent<Text>();
        messageText.font = Font();
        messageText.fontSize = 15;
        messageText.alignment = TextAnchor.MiddleCenter;
        messageText.color = InkBlack;

        // 绑定事件
        loginBtn.onClick.AddListener(OnLoginClicked);
        regBtn.onClick.AddListener(OnRegisterClicked);
    }

    private void OnLoginClicked()
    {
        try
        {
            string u = usernameInput.text.Trim(), p = passwordInput.text;
            if (string.IsNullOrEmpty(u) || string.IsNullOrEmpty(p))
            { ShowMsg("请输入用户名和密码", errorColor); return; }
            string stored = PlayerPrefs.GetString($"User_{u}_Password", "");
            if (string.IsNullOrEmpty(stored))
            { ShowMsg("用户不存在，请先注册", errorColor); return; }
            if (GameManager.EncryptPassword(p) == stored)
            {
                ShowMsg("登录成功！", successColor);
                GameManager.Instance.Login(u);
                StartCoroutine(FadeOut(rootCanvasGroup, 0.6f, () =>
                    SceneLoader.Instance.LoadScene(SceneNames.Start)));
            }
            else ShowMsg("密码错误", errorColor);
        }
        catch (System.Exception e) { Debug.LogError($"Login error: {e.Message}\n{e.StackTrace}"); }
    }

    private void OnRegisterClicked()
    {
        try
        {
            string u = usernameInput.text.Trim(), p = passwordInput.text;
            if (string.IsNullOrEmpty(u) || string.IsNullOrEmpty(p))
            { ShowMsg("请输入用户名和密码", errorColor); return; }
            if (u.Length < 2) { ShowMsg("用户名至少2个字符", errorColor); return; }
            if (p.Length < 4) { ShowMsg("密码至少4个字符", errorColor); return; }
            if (!string.IsNullOrEmpty(PlayerPrefs.GetString($"User_{u}_Password", "")))
            { ShowMsg("用户名已存在", errorColor); return; }
            PlayerPrefs.SetString($"User_{u}_Password", GameManager.EncryptPassword(p));
            PlayerPrefs.SetString($"User_{u}_RegisterTime", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            PlayerPrefs.Save();
            ShowMsg("注册成功！请点击登录", successColor);
        }
        catch (System.Exception e) { Debug.LogError($"Register error: {e.Message}\n{e.StackTrace}"); }
    }

    private void ShowMsg(string t, Color c) { messageText.text = t; messageText.color = c; }
}
