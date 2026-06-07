using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 展示场景管理器 — 新中式 3D 展示 + 信息面板
/// </summary>
public class ExhibitManager : UIFrame
{
    private static readonly Dictionary<string, Color> ModelColors = new Dictionary<string, Color>
    {
        { "vase", new Color(0.85f, 0.88f, 0.92f) }, { "cup", new Color(0.95f, 0.95f, 0.92f) },
        { "papercut", new Color(0.85f, 0.15f, 0.12f) }, { "scroll", new Color(0.92f, 0.88f, 0.78f) },
        { "bianzhong", new Color(0.65f, 0.50f, 0.20f) }, { "guzheng", new Color(0.45f, 0.25f, 0.12f) },
        { "erhu", new Color(0.55f, 0.30f, 0.15f) }
    };

    private List<ExhibitData> currentExhibits = new List<ExhibitData>();
    private int currentExhibitIndex;
    private GameObject currentModel;
    private Text exhibitNameText;
    private Text exhibitDescText;
    private Text contentText;
    private GameObject detailPanelObj;
    private ExhibitData currentExhibit;
    private int currentTab;
    private GameObject pedestal;

    private void Start()
    {
        EnsureSingletons();
        LoadCategoryData();
        CreateUI();
        ShowCurrentExhibit();
    }

    private void LoadCategoryData()
    {
        string cat = PlayerPrefs.GetString("CurrentCategory", "瓷器");
        currentExhibits = GameManager.Instance.GetExhibitsByCategory(cat);
        currentExhibitIndex = 0;
    }

    private void CreateUI()
    {
        var root = InitCanvas();

        // 淡入
        var cg = root.gameObject.AddComponent<CanvasGroup>();
        StartCoroutine(FadeIn(cg, 0.6f));

        // ── 顶部栏 ──
        var header = AnchorTop("Header", root, 55);
        header.AddComponent<Image>().color = DarkBar;

        // 返回按钮
        var backObj = NewUI("BackBtn", header.transform);
        var br = backObj.GetComponent<RectTransform>();
        br.anchorMin = br.anchorMax = new Vector2(0, 0.5f);
        br.pivot = new Vector2(0, 0.5f);
        br.sizeDelta = new Vector2(100, 38);
        br.anchoredPosition = new Vector2(10, 0);
        backObj.AddComponent<Image>().color = ZhuRed;
        backObj.AddComponent<Button>().onClick.AddListener(() => SceneLoader.Instance.LoadScene(SceneNames.Main));
        var btObj = NewUI("T", backObj.transform); Stretch(btObj);
        var bt = btObj.AddComponent<Text>();
        bt.font = Font(); bt.text = "< 返回"; bt.fontSize = 18; bt.color = Color.white; bt.alignment = TextAnchor.MiddleCenter;

        // 展品名称
        exhibitNameText = AddLabel("Name", header.transform, Vector2.zero, new Vector2(350, 35), "", 22, ZhuRed);

        // 收藏按钮（右上角）
        var collectHeaderBtn = NewUI("CollectBtn", header.transform);
        var chr = collectHeaderBtn.GetComponent<RectTransform>();
        chr.anchorMin = chr.anchorMax = new Vector2(1f, 0.5f);
        chr.pivot = new Vector2(1f, 0.5f);
        chr.sizeDelta = new Vector2(100, 34);
        chr.anchoredPosition = new Vector2(-15, 0);
        collectHeaderBtn.AddComponent<Image>().color = JadeGreen;
        collectHeaderBtn.AddComponent<Button>().onClick.AddListener(OnCollectClicked);
        var cbt = NewUI("T", collectHeaderBtn.transform); Stretch(cbt);
        var cbtxt = cbt.AddComponent<Text>();
        cbtxt.font = Font(); cbtxt.text = "♡ 收藏"; cbtxt.fontSize = 16; cbtxt.color = Color.white; cbtxt.alignment = TextAnchor.MiddleCenter;

        // 简短描述
        exhibitDescText = AddLabel("Desc", root, new Vector2(0, -70), new Vector2(500, 28), "", 15, XuanPaper);

        // ── 底部栏 ──
        var bottomBar = AnchorBottom("Bottom", root, 55);
        bottomBar.AddComponent<Image>().color = DarkBar;

        AddBtnAnchored("PrevBtn", bottomBar.transform, new Vector2(0, 0.5f), new Vector2(120, 38), new Vector2(15, 0), "< 上一个", JadeGreen).onClick.AddListener(ShowPrevious);
        AddBtnAnchored("CollectBtn2", bottomBar.transform, new Vector2(0.5f, 0.5f), new Vector2(130, 38), Vector2.zero, "收藏", ZhuRed).onClick.AddListener(OnCollectClicked);
        AddBtnAnchored("NextBtn", bottomBar.transform, new Vector2(1f, 0.5f), new Vector2(120, 38), new Vector2(-135, 0), "下一个 >", JadeGreen).onClick.AddListener(ShowNext);

        // ── 详情面板（右侧）──
        CreateDetailPanel(root);
    }

    private void CreateDetailPanel(Transform parent)
    {
        detailPanelObj = NewUI("DetailPanel", parent);
        var dpr = detailPanelObj.GetComponent<RectTransform>();
        dpr.anchorMin = new Vector2(1f, 0f); dpr.anchorMax = Vector2.one;
        dpr.pivot = new Vector2(1f, 0.5f);
        dpr.sizeDelta = new Vector2(320, 0);
        detailPanelObj.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f, 0.95f);

        // 顶部朱红装饰线
        var topLine = NewUI("TopLine", detailPanelObj.transform);
        var tlr = topLine.GetComponent<RectTransform>();
        tlr.anchorMin = new Vector2(0, 1); tlr.anchorMax = new Vector2(1, 1);
        tlr.pivot = new Vector2(0.5f, 1f); tlr.sizeDelta = new Vector2(0, 3);
        topLine.AddComponent<Image>().color = ZhuRed;

        // Tabs
        string[] tabNames = { "历史背景", "制作工艺", "文化寓意" };
        for (int i = 0; i < 3; i++)
        {
            var tab = NewUI($"Tab{i}", detailPanelObj.transform);
            var tr = tab.GetComponent<RectTransform>();
            tr.anchorMin = new Vector2(i / 3f, 1f); tr.anchorMax = new Vector2((i + 1) / 3f, 1f);
            tr.pivot = new Vector2(0f, 1f); tr.sizeDelta = new Vector2(0, 38);
            tab.AddComponent<Image>().color = i == 0 ? ZhuRed : new Color(0.25f, 0.25f, 0.25f);
            var tb = tab.AddComponent<Button>(); int idx = i; tb.onClick.AddListener(() => SwitchTab(idx));
            var ttObj = NewUI("T", tab.transform); Stretch(ttObj);
            var tt = ttObj.AddComponent<Text>();
            tt.font = Font(); tt.text = tabNames[i]; tt.fontSize = 15; tt.color = Color.white; tt.alignment = TextAnchor.MiddleCenter;
        }

        // Content
        var cObj = NewUI("Content", detailPanelObj.transform);
        var cr = cObj.GetComponent<RectTransform>();
        cr.anchorMin = Vector2.zero; cr.anchorMax = new Vector2(1f, 1f);
        cr.offsetMin = new Vector2(12, 12); cr.offsetMax = new Vector2(-12, -48);
        contentText = cObj.AddComponent<Text>();
        contentText.font = Font(); contentText.fontSize = 16; contentText.color = XuanPaper;
        contentText.alignment = TextAnchor.UpperLeft;
    }

    private void SwitchTab(int idx)
    {
        currentTab = idx;
        if (currentExhibit == null) return;
        for (int i = 0; i < 3; i++)
        {
            var tab = detailPanelObj.transform.Find($"Tab{i}");
            if (tab != null) tab.GetComponent<Image>().color = i == idx ? ZhuRed : new Color(0.25f, 0.25f, 0.25f);
        }
        switch (idx)
        {
            case 0: contentText.text = currentExhibit.history; break;
            case 1: contentText.text = currentExhibit.craft; break;
            case 2: contentText.text = currentExhibit.meaning; break;
        }
    }

    #region 3D Models

    private void ShowCurrentExhibit()
    {
        if (currentExhibits.Count == 0) return;
        var data = currentExhibits[currentExhibitIndex];
        if (currentModel != null) Destroy(currentModel);
        if (pedestal != null) Destroy(pedestal);

        // 展台底座
        pedestal = new GameObject("Pedestal");
        pedestal.transform.position = new Vector3(0, -0.05f, 5);
        var disk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disk.transform.SetParent(pedestal.transform, false);
        disk.transform.localScale = new Vector3(1.8f, 0.05f, 1.8f);
        disk.GetComponent<Renderer>().material.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        currentModel = CreateModel(data.modelType, data.id);
        currentModel.AddComponent<ModelRotator>();
        exhibitNameText.text = data.name;
        exhibitDescText.text = data.description;
        currentExhibit = data;
        SwitchTab(0);
    }

    private GameObject CreateModel(string type, string id)
    {
        var p = new GameObject($"Exhibit_{id}");
        p.transform.position = new Vector3(0, 0.5f, 5);
        p.transform.localScale = Vector3.one * 1.5f;
        Color c = ModelColors.ContainsKey(type) ? ModelColors[type] : Color.gray;

        switch (type)
        {
            case "vase":
                AddPart(PrimitiveType.Cylinder, p, new Vector3(0.6f, 0.8f, 0.6f), new Vector3(0, 0.8f, 0), c);
                AddPart(PrimitiveType.Cylinder, p, new Vector3(0.25f, 0.3f, 0.25f), new Vector3(0, 1.8f, 0), c);
                AddPart(PrimitiveType.Cylinder, p, new Vector3(0.3f, 0.08f, 0.3f), new Vector3(0, 2f, 0), new Color(0.2f, 0.3f, 0.6f));
                AddPart(PrimitiveType.Cylinder, p, new Vector3(0.7f, 0.1f, 0.7f), new Vector3(0, 0.05f, 0), new Color(0.4f, 0.25f, 0.1f));
                AddPart(PrimitiveType.Cylinder, p, new Vector3(0.62f, 0.05f, 0.62f), new Vector3(0, 0.5f, 0), new Color(0.15f, 0.25f, 0.55f));
                break;
            case "cup":
                AddPart(PrimitiveType.Cylinder, p, new Vector3(0.5f, 0.4f, 0.5f), Vector3.zero, c);
                AddPart(PrimitiveType.Sphere, p, new Vector3(0.5f, 0.2f, 0.5f), new Vector3(0, 0.5f, 0), c);
                AddPart(PrimitiveType.Sphere, p, new Vector3(0.12f, 0.12f, 0.12f), new Vector3(0, 0.65f, 0), new Color(0.6f, 0.35f, 0.1f));
                break;
            case "papercut":
                AddPart(PrimitiveType.Quad, p, new Vector3(1.5f, 1.5f, 1f), Vector3.zero, c);
                AddPart(PrimitiveType.Quad, p, new Vector3(1.7f, 1.7f, 0.9f), new Vector3(0, 0, -0.01f), new Color(0.95f, 0.9f, 0.8f));
                break;
            case "scroll":
                AddPart(PrimitiveType.Quad, p, new Vector3(1.2f, 1.8f, 1f), Vector3.zero, c);
                for (int i = 0; i < 2; i++)
                {
                    var rod = AddPart(PrimitiveType.Cylinder, p, new Vector3(1.25f, 0.06f, 0.06f), new Vector3(0, i == 0 ? -0.95f : 0.95f, 0), new Color(0.45f, 0.25f, 0.1f));
                    rod.transform.Rotate(0, 0, 90);
                }
                break;
            case "bianzhong":
                AddPart(PrimitiveType.Cylinder, p, new Vector3(1.5f, 0.06f, 0.06f), new Vector3(0, 1.5f, 0), new Color(0.35f, 0.2f, 0.08f)).transform.Rotate(0, 0, 90);
                for (int i = 0; i < 5; i++) AddPart(PrimitiveType.Cylinder, p, new Vector3((0.3f - i * 0.03f), 0.4f - i * 0.03f, 0.3f - i * 0.03f), new Vector3(-0.5f + i * 0.25f, 1.1f - i * 0.05f, 0), c);
                AddPart(PrimitiveType.Cube, p, new Vector3(1.6f, 0.1f, 0.4f), new Vector3(0, -0.2f, 0), new Color(0.35f, 0.2f, 0.08f));
                break;
            case "guzheng":
                AddPart(PrimitiveType.Cube, p, new Vector3(1.8f, 0.08f, 0.4f), Vector3.zero, c);
                for (int i = 0; i < 8; i++) AddPart(PrimitiveType.Cube, p, new Vector3(1.75f, 0.005f, 0.005f), new Vector3(0, 0.05f, -0.14f + i * 0.04f), Color.white);
                break;
            case "erhu":
                AddPart(PrimitiveType.Cylinder, p, new Vector3(0.04f, 1.2f, 0.04f), new Vector3(0, 0.6f, 0), c);
                var q = AddPart(PrimitiveType.Cylinder, p, new Vector3(0.3f, 0.3f, 0.3f), new Vector3(0, -0.15f, 0), c);
                q.transform.Rotate(90, 0, 0);
                break;
            default:
                AddPart(PrimitiveType.Cube, p, new Vector3(0.8f, 0.8f, 0.8f), Vector3.zero, c);
                break;
        }
        return p;
    }

    private GameObject AddPart(PrimitiveType pt, GameObject parent, Vector3 scale, Vector3 pos, Color color)
    {
        var obj = GameObject.CreatePrimitive(pt);
        obj.transform.SetParent(parent.transform, false);
        obj.transform.localScale = scale;
        obj.transform.localPosition = pos;
        obj.GetComponent<Renderer>().material.color = color;
        return obj;
    }

    #endregion

    private void ShowPrevious()
    { if (currentExhibits.Count == 0) return; currentExhibitIndex = (currentExhibitIndex - 1 + currentExhibits.Count) % currentExhibits.Count; ShowCurrentExhibit(); }

    private void ShowNext()
    { if (currentExhibits.Count == 0) return; currentExhibitIndex = (currentExhibitIndex + 1) % currentExhibits.Count; ShowCurrentExhibit(); }

    private void OnCollectClicked()
    {
        if (currentExhibits.Count == 0) return;
        var d = currentExhibits[currentExhibitIndex];
        bool added = BackpackManager.Instance.AddToBackpack(GameManager.Instance.currentUser, d.id);
        if (added) ShowToast($"✅ 已收藏「{d.name}」", JadeGreen);
        else ShowToast($"⚠ 「{d.name}」已在背包中", ZhuRed);
    }
}
