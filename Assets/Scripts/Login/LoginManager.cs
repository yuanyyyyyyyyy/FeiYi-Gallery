using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 登录管理器，运行时自动创建 UI，处理用户注册、登录和信息持久化
/// </summary>
public class LoginManager : MonoBehaviour
{
    public Color errorColor = new Color(0.76f, 0.21f, 0.19f);
    public Color successColor = new Color(0.18f, 0.48f, 0.43f);

    private InputField usernameInput;
    private InputField passwordInput;
    private Text messageText;

    private static readonly Color ZhuRed = new Color(0.76f, 0.21f, 0.19f);
    private static readonly Color GoldColor = new Color(0.83f, 0.65f, 0.27f);
    private static readonly Color InkBlack = new Color(0.17f, 0.17f, 0.17f);
    private static readonly Color XuanPaper = new Color(0.96f, 0.90f, 0.78f);

    private void Start()
    {
        CreateUI();
        string lastUser = PlayerPrefs.GetString("LastUser", "");
        if (!string.IsNullOrEmpty(lastUser)) usernameInput.text = lastUser;
    }

    private void CreateUI()
    {
        if (GameManager.Instance == null) new GameObject("[GameManager]").AddComponent<GameManager>();
        if (SceneLoader.Instance == null) new GameObject("[SceneLoader]").AddComponent<SceneLoader>();

        // Create Canvas as a ROOT object — ScreenSpaceOverlay Canvas drives its own RectTransform,
        // so child elements directly under Canvas get incorrect offset calculations.
        // Solution: add a full-screen "Root" panel between Canvas and all UI elements
        var canvasObj = new GameObject("Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        // Full-screen Root panel — all UI goes inside this
        var root = CreateUI("Root", canvas.transform);
        Stretch(root);

        // Background
        var bg = CreateUI("Background", root.transform);
        Stretch(bg); bg.AddComponent<Image>().color = InkBlack;

        // Panel
        var panel = CreateUI("LoginPanel", root.transform);
        var pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(420, 520);
        panel.AddComponent<Image>().color = XuanPaper;

        // Title
        AddLabel("Title", panel.transform, new Vector2(0, -40), new Vector2(380, 55),
            "了不起的非遗", 32, ZhuRed);

        // Subtitle
        AddLabel("Sub", panel.transform, new Vector2(0, -90), new Vector2(380, 28),
            "弘扬中华优秀传统文化 增强文化自信", 16, GoldColor);

        // Username
        AddLabel("ULbl", panel.transform, new Vector2(0, -130), new Vector2(300, 24),
            "用户名", 16, Color.black);
        usernameInput = AddInputField("UInput", panel.transform, new Vector2(0, -165), new Vector2(300, 40), "请输入用户名");

        // Password
        AddLabel("PLbl", panel.transform, new Vector2(0, -215), new Vector2(300, 24),
            "密码", 16, Color.black);
        passwordInput = AddInputField("PInput", panel.transform, new Vector2(0, -250), new Vector2(300, 40), "请输入密码", true);

        // Login button
        var loginBtn = AddBtn("LoginBtn", panel.transform, new Vector2(0, -315), new Vector2(300, 48), "登  录", ZhuRed);
        loginBtn.onClick.AddListener(OnLoginClicked);

        // Register button
        var regBtn = AddBtn("RegBtn", panel.transform, new Vector2(0, -385), new Vector2(300, 48), "注  册", GoldColor);
        regBtn.onClick.AddListener(OnRegisterClicked);

        // Message
        var msgObj = CreateUI("Msg", panel.transform);
        var mr = msgObj.GetComponent<RectTransform>();
        mr.anchorMin = mr.anchorMax = new Vector2(0.5f, 1f);
        mr.pivot = new Vector2(0.5f, 1f);
        mr.sizeDelta = new Vector2(360, 28);
        mr.anchoredPosition = new Vector2(0, -460);
        messageText = msgObj.AddComponent<Text>();
        messageText.font = UIFont.Get();
        messageText.fontSize = 16;
        messageText.alignment = TextAnchor.MiddleCenter;
    }

    private void OnLoginClicked()
    {
        try
        {
            string u = usernameInput.text.Trim(), p = passwordInput.text;
            if (string.IsNullOrEmpty(u) || string.IsNullOrEmpty(p)) { ShowMsg("请输入用户名和密码", errorColor); return; }
            string stored = PlayerPrefs.GetString($"User_{u}_Password", "");
            if (string.IsNullOrEmpty(stored)) { ShowMsg("用户不存在，请先注册", errorColor); return; }
            if (GameManager.EncryptPassword(p) == stored)
            {
                ShowMsg("登录成功！", successColor);
                GameManager.Instance.Login(u);
                SceneLoader.Instance.LoadScene(SceneNames.Start);
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
            if (string.IsNullOrEmpty(u) || string.IsNullOrEmpty(p)) { ShowMsg("请输入用户名和密码", errorColor); return; }
            if (u.Length < 2) { ShowMsg("用户名至少2个字符", errorColor); return; }
            if (p.Length < 4) { ShowMsg("密码至少4个字符", errorColor); return; }
            if (!string.IsNullOrEmpty(PlayerPrefs.GetString($"User_{u}_Password", ""))) { ShowMsg("用户名已存在", errorColor); return; }
            PlayerPrefs.SetString($"User_{u}_Password", GameManager.EncryptPassword(p));
            PlayerPrefs.SetString($"User_{u}_RegisterTime", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            PlayerPrefs.Save();
            ShowMsg("注册成功！请点击登录", successColor);
        }
        catch (System.Exception e) { Debug.LogError($"Register error: {e.Message}\n{e.StackTrace}"); }
    }

    private void ShowMsg(string t, Color c) { messageText.text = t; messageText.color = c; }

    #region UI Helpers
    private GameObject CreateUI(string name, Transform parent)
    { var o = new GameObject(name); o.transform.SetParent(parent, false); o.AddComponent<RectTransform>(); return o; }

    private void Stretch(GameObject o)
    { var r = o.GetComponent<RectTransform>(); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.sizeDelta = Vector2.zero; }

    private Text AddLabel(string name, Transform parent, Vector2 pos, Vector2 size, string text, int fontSize, Color color)
    {
        var o = CreateUI(name, parent);
        var r = o.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.sizeDelta = size; r.anchoredPosition = pos;
        var t = o.AddComponent<Text>();
        t.font = UIFont.Get();
        t.text = text; t.fontSize = fontSize; t.color = color; t.alignment = TextAnchor.MiddleCenter;
        return t;
    }

    private InputField AddInputField(string name, Transform parent, Vector2 pos, Vector2 size, string placeholder, bool password = false)
    {
        var o = CreateUI(name, parent);
        var r = o.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.sizeDelta = size; r.anchoredPosition = pos;
        o.AddComponent<Image>().color = Color.white;

        // Text (child)
        var tObj = CreateUI("Text", o.transform);
        var tr = tObj.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.sizeDelta = new Vector2(-10, -6);
        var txt = tObj.AddComponent<Text>();
        txt.font = UIFont.Get();
        txt.fontSize = 18; txt.color = Color.black; txt.alignment = TextAnchor.MiddleCenter;

        // Placeholder (child)
        var pObj = CreateUI("Placeholder", o.transform);
        var pr2 = pObj.GetComponent<RectTransform>();
        pr2.anchorMin = Vector2.zero; pr2.anchorMax = Vector2.one; pr2.sizeDelta = new Vector2(-10, -6);
        var pht = pObj.AddComponent<Text>();
        pht.font = UIFont.Get();
        pht.text = placeholder; pht.fontSize = 16; pht.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); pht.alignment = TextAnchor.MiddleCenter;

        var inf = o.AddComponent<InputField>();
        inf.textComponent = txt;
        inf.placeholder = pht;
        if (password) inf.contentType = InputField.ContentType.Password;
        return inf;
    }

    private Button AddBtn(string name, Transform parent, Vector2 pos, Vector2 size, string text, Color bg)
    {
        var o = CreateUI(name, parent);
        var r = o.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.sizeDelta = size; r.anchoredPosition = pos;
        o.AddComponent<Image>().color = bg;
        var btn = o.AddComponent<Button>();
        var tObj = CreateUI("Text", o.transform);
        Stretch(tObj);
        var t = tObj.AddComponent<Text>();
        t.font = UIFont.Get();
        t.text = text; t.fontSize = 22; t.color = Color.white; t.alignment = TextAnchor.MiddleCenter;
        return btn;
    }
    #endregion
}
