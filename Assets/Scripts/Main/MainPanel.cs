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

        // 淡入
        var cg = root.gameObject.AddComponent<CanvasGroup>();
        StartCoroutine(FadeIn(cg, 0.6f));

        // 背景
        var bg = NewUI("BG", root);
        Stretch(bg);
        var bgImg = bg.AddComponent<Image>(); bgImg.color = InkBlack; bgImg.raycastTarget = false;

        // ── 顶部标题栏 ──
        var header = AnchorTop("Header", root, 60);
        header.AddComponent<Image>(); header.GetComponent<Image>().color = DarkBar; header.GetComponent<Image>().raycastTarget = false;

        // 左侧印章小图标
        AddSealLogo("MiniLogo", header.transform, new Vector2(-200, 0), 36, "遗", 20);

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

        // ── 品类卡片区域 ──
        var cards = NewUI("Cards", root);
        var cr = cards.GetComponent<RectTransform>();
        cr.anchorMin = new Vector2(0, 0.1f); cr.anchorMax = new Vector2(1, 0.92f);
        cr.offsetMin = cr.offsetMax = Vector2.zero; cr.anchoredPosition = Vector2.zero;
        cr.sizeDelta = Vector2.zero;
        cards.AddComponent<Image>(); cards.GetComponent<Image>().color = InkBlack; cards.GetComponent<Image>().raycastTarget = false;

        for (int i = 0; i < 4; i++)
        {
            int row = i / 2, col = i % 2;
            CreateCategoryCard(cards.transform, i, row, col);
        }

        // ── 底部导航栏 ──
        var nav = AnchorBottom("NavBar", root, 55);
        nav.AddComponent<Image>(); nav.GetComponent<Image>().color = DarkBar; nav.GetComponent<Image>().raycastTarget = false;

        string[] navNames = { "背包", "设置", "帮助", "退出" };
        Color[] navColors = { JadeGreen, GoldColor, ZhuRed, new Color(0.5f, 0.5f, 0.5f) };
        System.Action[] navActions = {
            () => { if (backpackPanel == null) { CreateBackpackPanel(); return; } backpackPanel.SetActive(!backpackPanel.activeSelf); },
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

        // ── 弹窗 ──
        settingsPanel = MakeOverlay(root, "系统设置",
            $"音量：{Mathf.RoundToInt(GameManager.Instance.volume * 100)}%\n" +
            $"亮度：{Mathf.RoundToInt(GameManager.Instance.brightness * 100)}%\n" +
            "主题风格：" + GameManager.Instance.themeStyle + "\n\n" +
            "所有设置已通过 GameManager 保存\n点击「保存设置」按钮应用变更", GoldColor);
        helpPanel = MakeOverlay(root, "帮助",
            "1. 选择品类卡片浏览展品\n2. 拖拽旋转3D模型\n3. 查看展品详细信息\n4. 收藏感兴趣的展品\n5. 在背包中管理收藏", ZhuRed);
    }

    private void CreateCategoryCard(Transform parent, int idx, int row, int col)
    {
        var card = NewUI($"Card_{Categories[idx]}", parent);
        var cr = card.GetComponent<RectTransform>();
        cr.anchorMin = new Vector2(col * 0.5f, 0.5f - row * 0.5f);
        cr.anchorMax = new Vector2((col + 1) * 0.5f, 1f - row * 0.5f);
        cr.offsetMin = new Vector2(10, 10 + row * 10);
        cr.offsetMax = new Vector2(-10 - col * 10, -10 - row * 10);
        card.AddComponent<Image>().color = CategoryColors[idx];

        var btn = card.AddComponent<Button>();
        string cat = Categories[idx];
        btn.onClick.AddListener(() => OnCategoryClicked(cat));

        // 印章风品类图标
        AddSealIcon("Icon", card.transform, new Vector2(0.5f, 0.7f), 35, CategoryIcons[idx], 30);

        // 品类名
        var nameObj = NewUI("Name", card.transform);
        var nr = nameObj.GetComponent<RectTransform>();
        nr.anchorMin = nr.anchorMax = new Vector2(0.5f, 0.3f);
        nr.sizeDelta = new Vector2(200, 35);
        var nm = nameObj.AddComponent<Text>();
        nm.font = Font(); nm.text = Categories[idx]; nm.fontSize = 26; nm.color = Color.white; nm.alignment = TextAnchor.MiddleCenter;

        // 描述
        var descObj = NewUI("Desc", card.transform);
        var dr = descObj.GetComponent<RectTransform>();
        dr.anchorMin = dr.anchorMax = new Vector2(0.5f, 0.15f);
        dr.sizeDelta = new Vector2(250, 25);
        var dt = descObj.AddComponent<Text>();
        dt.font = Font(); dt.text = CategoryDescs[idx]; dt.fontSize = 14; dt.color = new Color(1, 1, 1, 0.8f); dt.alignment = TextAnchor.MiddleCenter;
    }

    private void OnCategoryClicked(string cat)
    {
        PlayerPrefs.SetString("CurrentCategory", cat);
        PlayerPrefs.Save();
        SceneLoader.Instance.LoadScene(SceneNames.Exhibit);
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
