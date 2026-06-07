using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// 主场景管理器
/// </summary>
public class MainPanel : MonoBehaviour
{
    private static readonly Color ZhuRed = new Color(0.76f, 0.21f, 0.19f);
    private static readonly Color GoldColor = new Color(0.83f, 0.65f, 0.27f);
    private static readonly Color InkBlack = new Color(0.17f, 0.17f, 0.17f);
    private static readonly Color XuanPaper = new Color(0.96f, 0.90f, 0.78f);
    private static readonly Color JadeGreen = new Color(0.18f, 0.48f, 0.43f);

    private static readonly string[] Categories = { "瓷器", "剪纸", "书法", "民族乐器" };
    private static readonly string[] CategoryDescs = { "千年窑火 瓷韵流芳", "纸艺生花 巧夺天工", "笔墨丹青 翰墨飘香", "丝竹管弦 余音绕梁" };
    private static readonly Color[] CategoryColors = {
        new Color(0.26f, 0.47f, 0.72f), new Color(0.80f, 0.20f, 0.18f),
        new Color(0.35f, 0.35f, 0.38f), new Color(0.72f, 0.53f, 0.19f)
    };
    private static readonly string[] CategoryIcons = { "瓷", "剪", "书", "乐" };

    private GameObject backpackPanel, settingsPanel, helpPanel;
    private Transform canvasT;

    private void Start()
    {
        if (GameManager.Instance == null) new GameObject("[GameManager]").AddComponent<GameManager>();
        if (SceneLoader.Instance == null) new GameObject("[SceneLoader]").AddComponent<SceneLoader>();
        CreateUI();
        Create3DCharacter();
    }

    private void CreateUI()
    {
        // Disable any residual Canvas components on this gameObject (from scene file)
        var oldC = GetComponent<Canvas>(); if (oldC != null) oldC.enabled = false;
        var oldCS = GetComponent<CanvasScaler>(); if (oldCS != null) oldCS.enabled = false;
        var oldGR = GetComponent<GraphicRaycaster>(); if (oldGR != null) oldGR.enabled = false;

        // Create Canvas as a ROOT object — ScreenSpaceOverlay Canvas must be root
        // to ensure its RectTransform matches the screen, not influenced by parent's residual components
        var canvasObj = new GameObject("Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        canvasT = canvas.transform;

        if (FindObjectOfType<EventSystem>() == null)
        { var es = new GameObject("EventSystem"); es.AddComponent<EventSystem>(); es.AddComponent<StandaloneInputModule>(); }

        // Full-screen Root panel — all UI goes inside this
        var root = NewUI("Root", canvas.transform);
        Stretch(root);

        // BG
        var bg = NewUI("BG", root.transform); Stretch(bg); bg.AddComponent<Image>().color = InkBlack;

        // Header - anchored to top with 60px height
        var header = NewUI("Header", root.transform);
        var hr = header.GetComponent<RectTransform>();
        hr.anchorMin = new Vector2(0, 1); hr.anchorMax = new Vector2(1, 1);
        hr.pivot = new Vector2(0.5f, 1f); hr.sizeDelta = new Vector2(0, 60); hr.anchoredPosition = Vector2.zero;
        header.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f, 0.95f);
        
        // Title - centered in header, positioned near top
        var titleText = NewUI("Title", header.transform);
        var trt = titleText.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.5f, 0.5f); trt.anchorMax = new Vector2(0.5f, 0.5f);
        trt.sizeDelta = new Vector2(400, 35); trt.anchoredPosition = Vector2.zero;
        var tt = titleText.AddComponent<Text>();
        tt.font = Font(); tt.text = "了不起的非遗"; tt.fontSize = 28; tt.color = ZhuRed; tt.alignment = TextAnchor.MiddleCenter;

        // Cards area - between header and nav bar
        var cards = NewUI("Cards", root.transform);
        var cr = cards.GetComponent<RectTransform>();
        cr.anchorMin = new Vector2(0, 0.1f); cr.anchorMax = new Vector2(1, 0.92f);
        cr.offsetMin = Vector2.zero; cr.offsetMax = Vector2.zero; cr.anchoredPosition = Vector2.zero;
        cards.AddComponent<Image>().color = InkBlack;

        // 2x2 grid of category cards
        for (int i = 0; i < 4; i++)
        {
            int row = i / 2, col = i % 2;
            CreateCategoryCard(cards.transform, i, row, col);
        }

        // Bottom nav bar - anchored to bottom with 55px height
        var nav = NewUI("NavBar", root.transform);
        var nr = nav.GetComponent<RectTransform>();
        nr.anchorMin = new Vector2(0, 0); nr.anchorMax = new Vector2(1, 0);
        nr.pivot = new Vector2(0.5f, 0f); nr.sizeDelta = new Vector2(0, 55); nr.anchoredPosition = Vector2.zero;
        nav.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        string[] navNames = { "背包", "设置", "帮助", "退出" };
        Color[] navColors = { JadeGreen, GoldColor, ZhuRed, new Color(0.5f, 0.5f, 0.5f) };
        System.Action[] navActions = {
            () => {
                if (backpackPanel == null) CreateBackpackPanel(canvasT);
                backpackPanel.SetActive(!backpackPanel.activeSelf);
            },
            () => TogglePanel(settingsPanel),
            () => TogglePanel(helpPanel),
            () => { GameManager.Instance.Logout(); SceneLoader.Instance.LoadScene(SceneNames.Login); }
        };
        for (int i = 0; i < 4; i++)
        {
            var bo = NewUI($"Nav_{navNames[i]}", nav.transform);
            var br = bo.GetComponent<RectTransform>();
            br.anchorMin = new Vector2(i * 0.25f, 0); br.anchorMax = new Vector2((i + 1) * 0.25f, 1);
            br.sizeDelta = Vector2.zero;
            bo.AddComponent<Image>().color = navColors[i];
            var nb = bo.AddComponent<Button>(); int idx = i; nb.onClick.AddListener(() => navActions[idx]());
            var to = NewUI("T", bo.transform); Stretch(to);
            var nt = to.AddComponent<Text>();
            nt.font = Font(); nt.text = navNames[i]; nt.fontSize = 18; nt.color = Color.white; nt.alignment = TextAnchor.MiddleCenter;
        }

        // Overlay panels (lazy init for backpack)
        settingsPanel = MakeOverlay(canvasT, "系统设置",
            $"音量：{Mathf.RoundToInt(GameManager.Instance.volume * 100)}%\n" +
            $"亮度：{Mathf.RoundToInt(GameManager.Instance.brightness * 100)}%\n" +
            "主题风格：" + GameManager.Instance.themeStyle + "\n\n" +
            "所有设置已通过 GameManager 保存\n点击「保存设置」按钮应用变更",
            GoldColor);
        helpPanel = MakeOverlay(canvasT, "帮助", "1. 选择品类卡片浏览展品\n2. 拖拽旋转3D模型\n3. 查看展品详细信息\n4. 收藏感兴趣的展品\n5. 在背包中管理收藏", ZhuRed);
    }

    private void CreateCategoryCard(Transform parent, int idx, int row, int col)
    {
        // Each card takes half the width and half the height of the cards area
        var card = NewUI($"Card_{Categories[idx]}", parent);
        var cr = card.GetComponent<RectTransform>();
        cr.anchorMin = new Vector2(col * 0.5f, 0.5f - row * 0.5f);
        cr.anchorMax = new Vector2((col + 1) * 0.5f, 1f - row * 0.5f);
        cr.offsetMin = new Vector2(8, 8 + row * 8);
        cr.offsetMax = new Vector2(-8 - col * 8, -8 - row * 8);
        card.AddComponent<Image>().color = CategoryColors[idx];

        var btn = card.AddComponent<Button>();
        string cat = Categories[idx];
        btn.onClick.AddListener(() => OnCategoryClicked(cat));

        // Icon circle (parent Image + child Text)
        var iconBg = NewUI("IconBg", card.transform);
        var irBg = iconBg.GetComponent<RectTransform>();
        irBg.anchorMin = irBg.anchorMax = new Vector2(0.5f, 0.7f);
        irBg.sizeDelta = new Vector2(70, 70);
        iconBg.AddComponent<Image>().color = new Color(1, 1, 1, 0.25f);
        var iconTxt = NewUI("IconTxt", iconBg.transform);
        Stretch(iconTxt);
        var it = iconTxt.AddComponent<Text>();
        it.font = Font(); it.text = CategoryIcons[idx]; it.fontSize = 36; it.color = Color.white; it.alignment = TextAnchor.MiddleCenter;

        // Name
        var nameObj = NewUI("Name", card.transform);
        var nr = nameObj.GetComponent<RectTransform>();
        nr.anchorMin = nr.anchorMax = new Vector2(0.5f, 0.3f);
        nr.sizeDelta = new Vector2(200, 35);
        var nm = nameObj.AddComponent<Text>();
        nm.font = Font(); nm.text = Categories[idx]; nm.fontSize = 26; nm.color = Color.white; nm.alignment = TextAnchor.MiddleCenter;

        // Description
        var descObj = NewUI("Desc", card.transform);
        var dr = descObj.GetComponent<RectTransform>();
        dr.anchorMin = dr.anchorMax = new Vector2(0.5f, 0.15f);
        dr.sizeDelta = new Vector2(250, 25);
        var dt = descObj.AddComponent<Text>();
        dt.font = Font(); dt.text = CategoryDescs[idx]; dt.fontSize = 14; dt.color = new Color(1, 1, 1, 0.8f); dt.alignment = TextAnchor.MiddleCenter;
    }

    private void Create3DCharacter()
    {
        var charObj = new GameObject("Character");
        charObj.transform.position = new Vector3(0, -1, 0);

        var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.transform.SetParent(charObj.transform, false);
        body.transform.localScale = new Vector3(0.4f, 0.5f, 0.4f);
        body.transform.localPosition = new Vector3(0, 0.8f, 0);
        body.GetComponent<Renderer>().material.color = ZhuRed;

        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.transform.SetParent(charObj.transform, false);
        head.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
        head.transform.localPosition = new Vector3(0, 1.55f, 0);
        head.GetComponent<Renderer>().material.color = XuanPaper;

        var ll = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ll.transform.SetParent(charObj.transform, false);
        ll.transform.localScale = new Vector3(0.12f, 0.35f, 0.12f);
        ll.transform.localPosition = new Vector3(-0.12f, 0.05f, 0);
        ll.GetComponent<Renderer>().material.color = InkBlack;

        var rl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rl.transform.SetParent(charObj.transform, false);
        rl.transform.localScale = new Vector3(0.12f, 0.35f, 0.12f);
        rl.transform.localPosition = new Vector3(0.12f, 0.05f, 0);
        rl.GetComponent<Renderer>().material.color = InkBlack;

        charObj.AddComponent<CharacterController2D>();
    }

    private void OnCategoryClicked(string cat)
    {
        PlayerPrefs.SetString("CurrentCategory", cat);
        PlayerPrefs.Save();
        SceneLoader.Instance.LoadScene(SceneNames.Exhibit);
    }

    private GameObject MakeOverlay(Transform canvasT, string title, string content, Color accent)
    {
        var overlay = NewUI("Overlay", canvasT); Stretch(overlay);
        overlay.AddComponent<Image>().color = new Color(0, 0, 0, 0.8f);
        overlay.SetActive(false);
        overlay.AddComponent<Button>().onClick.AddListener(() => overlay.SetActive(false));

        var panel = NewUI("Panel", overlay.transform);
        var pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(420, 360);
        panel.AddComponent<Image>().color = XuanPaper;
        panel.AddComponent<Button>(); // block click-through

        var tObj = NewUI("T", panel.transform);
        var tr = tObj.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0, 1); tr.anchorMax = new Vector2(1, 1);
        tr.pivot = new Vector2(0.5f, 1f); tr.sizeDelta = new Vector2(0, 40);
        var tl = tObj.AddComponent<Text>();
        tl.font = Font(); tl.text = title; tl.fontSize = 24; tl.color = accent; tl.alignment = TextAnchor.MiddleCenter;

        var cObj = NewUI("C", panel.transform);
        var cr = cObj.GetComponent<RectTransform>();
        cr.anchorMin = Vector2.zero; cr.anchorMax = new Vector2(1, 1);
        cr.offsetMin = new Vector2(15, 15); cr.offsetMax = new Vector2(-15, -50);
        var cl = cObj.AddComponent<Text>();
        cl.font = Font(); cl.text = content; cl.fontSize = 17; cl.color = Color.black; cl.alignment = TextAnchor.UpperLeft;

        var xObj = NewUI("X", panel.transform);
        var xr = xObj.GetComponent<RectTransform>();
        xr.anchorMin = xr.anchorMax = new Vector2(1, 1); xr.pivot = new Vector2(1, 1);
        xr.sizeDelta = new Vector2(36, 36); xr.anchoredPosition = new Vector2(-5, -5);
        xObj.AddComponent<Image>().color = ZhuRed;
        xObj.AddComponent<Button>().onClick.AddListener(() => overlay.SetActive(false));
        var xTxtObj = NewUI("XTxt", xObj.transform); Stretch(xTxtObj);
        var xt = xTxtObj.AddComponent<Text>();
        xt.font = Font(); xt.text = "X"; xt.fontSize = 20; xt.color = Color.white; xt.alignment = TextAnchor.MiddleCenter;

        return overlay;
    }

    private void TogglePanel(GameObject p) { if (p != null) p.SetActive(!p.activeSelf); }

    private void CreateBackpackPanel(Transform canvasT)
    {
        var items = BackpackManager.Instance.GetBackpackItems(GameManager.Instance.currentUser);
        // Destroy old panel if exists
        if (backpackPanel != null) Destroy(backpackPanel);

        // Create overlay
        backpackPanel = NewUI("Overlay_Backpack", canvasT); Stretch(backpackPanel);
        backpackPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.8f);
        backpackPanel.AddComponent<Button>().onClick.AddListener(() => backpackPanel.SetActive(false));
        backpackPanel.SetActive(true);

        var panel = NewUI("Panel", backpackPanel.transform);
        var pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(420, 360);
        panel.AddComponent<Image>().color = XuanPaper;

        // Title
        var tObj = NewUI("T", panel.transform);
        var tr = tObj.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0, 1); tr.anchorMax = new Vector2(1, 1);
        tr.pivot = new Vector2(0.5f, 1f); tr.sizeDelta = new Vector2(0, 40);
        var tl = tObj.AddComponent<Text>();
        tl.font = Font(); tl.text = "我的背包"; tl.fontSize = 24; tl.color = JadeGreen; tl.alignment = TextAnchor.MiddleCenter;

        // Close button
        var xObj = NewUI("X", panel.transform);
        var xr = xObj.GetComponent<RectTransform>();
        xr.anchorMin = xr.anchorMax = new Vector2(1, 1); xr.pivot = new Vector2(1, 1);
        xr.sizeDelta = new Vector2(36, 36); xr.anchoredPosition = new Vector2(-5, -5);
        xObj.AddComponent<Image>().color = ZhuRed;
        xObj.AddComponent<Button>().onClick.AddListener(() => backpackPanel.SetActive(false));
        var xTxtObj = NewUI("XTxt", xObj.transform); Stretch(xTxtObj);
        var xt = xTxtObj.AddComponent<Text>();
        xt.font = Font(); xt.text = "X"; xt.fontSize = 20; xt.color = Color.white; xt.alignment = TextAnchor.MiddleCenter;

        // Content area
        var contentObj = NewUI("Content", panel.transform);
        var cr = contentObj.GetComponent<RectTransform>();
        cr.anchorMin = Vector2.zero; cr.anchorMax = new Vector2(1, 1);
        cr.offsetMin = new Vector2(15, 15); cr.offsetMax = new Vector2(-15, -50);

        if (items.Count == 0)
        {
            var cObj = NewUI("C", contentObj.transform);
            var cr2 = cObj.GetComponent<RectTransform>();
            cr2.anchorMin = Vector2.zero; cr2.anchorMax = new Vector2(1, 1);
            cr2.sizeDelta = Vector2.zero;
            var cl = cObj.AddComponent<Text>();
            cl.font = Font(); cl.text = "暂无收藏展品\n浏览展品时点击「收藏」即可添加到背包"; cl.fontSize = 17; cl.color = Color.black; cl.alignment = TextAnchor.MiddleCenter;
        }
        else
        {
            // Item list with delete buttons
            float yOffset = 0;
            foreach (var id in items)
            {
                var data = GameManager.Instance.GetExhibit(id);
                string label = data != null ? data.name : id;

                var row = NewUI($"Row_{id}", contentObj.transform);
                var rowr = row.GetComponent<RectTransform>();
                rowr.anchorMin = new Vector2(0, 1); rowr.anchorMax = new Vector2(1, 1);
                rowr.pivot = new Vector2(0, 1); rowr.sizeDelta = new Vector2(0, 30);
                rowr.anchoredPosition = new Vector2(0, yOffset);

                var itemTxt = NewUI("Txt", row.transform);
                var itr = itemTxt.GetComponent<RectTransform>();
                itr.anchorMin = Vector2.zero; itr.anchorMax = new Vector2(0.7f, 1f);
                itr.offsetMin = new Vector2(10, 3); itr.offsetMax = new Vector2(0, -3);
                var itl = itemTxt.AddComponent<Text>();
                itl.font = Font(); itl.text = $"• {label}"; itl.fontSize = 16; itl.color = Color.black; itl.alignment = TextAnchor.MiddleLeft;

                var delBtn = NewUI("DelBtn", row.transform);
                var dbr = delBtn.GetComponent<RectTransform>();
                dbr.anchorMin = new Vector2(0.72f, 0.25f); dbr.anchorMax = new Vector2(0.98f, 0.75f);
                dbr.offsetMin = Vector2.zero; dbr.offsetMax = Vector2.zero;
                delBtn.AddComponent<Image>().color = ZhuRed;
                var btnComp = delBtn.AddComponent<Button>();
                string capturedId = id;
                btnComp.onClick.AddListener(() => {
                    BackpackManager.Instance.RemoveFromBackpack(GameManager.Instance.currentUser, capturedId);
                    if (backpackPanel != null) backpackPanel.SetActive(false);
                    CreateBackpackPanel(canvasT);
                });
                var delTxtObj = NewUI("T", delBtn.transform);
                var dtr = delTxtObj.GetComponent<RectTransform>();
                dtr.anchorMin = Vector2.zero; dtr.anchorMax = Vector2.one;
                dtr.offsetMin = Vector2.zero; dtr.offsetMax = Vector2.zero;
                var delTxt = delTxtObj.AddComponent<Text>();
                delTxt.font = Font(); delTxt.text = "删除"; delTxt.fontSize = 14; delTxt.color = Color.white; delTxt.alignment = TextAnchor.MiddleCenter;

                yOffset -= 32;
            }
        }
    }

    #region UI Helpers
    private static Font Font() => UIFont.Get();
    private GameObject NewUI(string name, Transform parent)
    { var o = new GameObject(name); o.transform.SetParent(parent, false); o.AddComponent<RectTransform>(); return o; }
    private void Stretch(GameObject o)
    { var r = o.GetComponent<RectTransform>(); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.sizeDelta = Vector2.zero; r.anchoredPosition = Vector2.zero; }
    private void CenterStretch(GameObject o, float left, float right, float top, float bottom)
    { var r = o.GetComponent<RectTransform>(); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(left, bottom); r.offsetMax = new Vector2(-right, -top); r.anchoredPosition = Vector2.zero; }
    #endregion
}
