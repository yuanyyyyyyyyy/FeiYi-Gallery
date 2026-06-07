using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// 展示场景管理器
/// </summary>
public class ExhibitManager : MonoBehaviour
{
    private static readonly Color ZhuRed = new Color(0.76f, 0.21f, 0.19f);
    private static readonly Color GoldColor = new Color(0.83f, 0.65f, 0.27f);
    private static readonly Color InkBlack = new Color(0.17f, 0.17f, 0.17f);
    private static readonly Color XuanPaper = new Color(0.96f, 0.90f, 0.78f);
    private static readonly Color JadeGreen = new Color(0.18f, 0.48f, 0.43f);

    private List<ExhibitData> currentExhibits = new List<ExhibitData>();
    private int currentExhibitIndex;
    private GameObject currentModel;
    private Text exhibitNameText;
    private Text exhibitDescText;
    private Text contentText;
    private GameObject detailPanelObj;
    private ExhibitData currentExhibit;
    private int currentTab;
    private Transform canvasT;

    private static readonly Dictionary<string, Color> ModelColors = new Dictionary<string, Color>
    {
        { "vase", new Color(0.85f, 0.88f, 0.92f) }, { "cup", new Color(0.95f, 0.95f, 0.92f) },
        { "papercut", new Color(0.85f, 0.15f, 0.12f) }, { "scroll", new Color(0.92f, 0.88f, 0.78f) },
        { "bianzhong", new Color(0.65f, 0.50f, 0.20f) }, { "guzheng", new Color(0.45f, 0.25f, 0.12f) },
        { "erhu", new Color(0.55f, 0.30f, 0.15f) }
    };

    private void Start()
    {
        if (GameManager.Instance == null) new GameObject("[GameManager]").AddComponent<GameManager>();
        if (SceneLoader.Instance == null) new GameObject("[SceneLoader]").AddComponent<SceneLoader>();
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
        // Disable any residual Canvas components on this gameObject (from scene file)
        var oldC = GetComponent<Canvas>(); if (oldC != null) oldC.enabled = false;
        var oldCS = GetComponent<CanvasScaler>(); if (oldCS != null) oldCS.enabled = false;
        var oldGR = GetComponent<GraphicRaycaster>(); if (oldGR != null) oldGR.enabled = false;

        // Create Canvas as a ROOT object — ScreenSpaceOverlay Canvas must be root
        var canvasObj = new GameObject("Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        canvasT = canvas.transform;

        if (FindObjectOfType<EventSystem>() == null)
        { var es = new GameObject("EventSystem"); es.AddComponent<EventSystem>(); es.AddComponent<StandaloneInputModule>(); }

        // Full-screen Root panel — all UI goes inside this
        var root = CreateUI("Root", canvas.transform);
        Stretch(root);

        // Header
        var header = CreateUI("Header", root.transform);
        var hr = header.GetComponent<RectTransform>();
        hr.anchorMin = new Vector2(0, 1); hr.anchorMax = Vector2.one;
        hr.pivot = new Vector2(0.5f, 1f); hr.sizeDelta = new Vector2(0, 55); hr.anchoredPosition = Vector2.zero;
        header.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        // Back button
        var backObj = CreateUI("BackBtn", header.transform);
        var br = backObj.GetComponent<RectTransform>();
        br.anchorMin = br.anchorMax = new Vector2(0, 0.5f); br.pivot = new Vector2(0, 0.5f);
        br.sizeDelta = new Vector2(100, 38); br.anchoredPosition = new Vector2(10, 0);
        backObj.AddComponent<Image>().color = ZhuRed;
        backObj.AddComponent<Button>().onClick.AddListener(() => SceneLoader.Instance.LoadScene(SceneNames.Main));
        var btObj = CreateUI("T", backObj.transform); Stretch(btObj);
        var bt = btObj.AddComponent<Text>();
        bt.font = UIFont.Get();
        bt.text = "< 返回"; bt.fontSize = 18; bt.color = Color.white; bt.alignment = TextAnchor.MiddleCenter;

        // Exhibit name
        exhibitNameText = AddLabel("Name", header.transform, Vector2.zero, new Vector2(350, 35), "", 22, ZhuRed);

        // Desc
        exhibitDescText = AddLabel("Desc", root.transform, new Vector2(0, -70), new Vector2(500, 28), "", 15, XuanPaper);

        // Bottom bar
        var bottomBar = CreateUI("Bottom", root.transform);
        var bbr = bottomBar.GetComponent<RectTransform>();
        bbr.anchorMin = new Vector2(0, 0); bbr.anchorMax = new Vector2(1, 0);
        bbr.pivot = new Vector2(0.5f, 0f); bbr.sizeDelta = new Vector2(0, 55); bbr.anchoredPosition = Vector2.zero;
        bottomBar.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        AddSideBtn("PrevBtn", bottomBar.transform, new Vector2(0, 0.5f), new Vector2(120, 38), new Vector2(15, 0), "< 上一个", JadeGreen).onClick.AddListener(ShowPrevious);
        AddSideBtn("CollectBtn", bottomBar.transform, new Vector2(0.5f, 0.5f), new Vector2(130, 38), Vector2.zero, "收藏", ZhuRed).onClick.AddListener(OnCollectClicked);
        AddSideBtn("NextBtn", bottomBar.transform, new Vector2(1f, 0.5f), new Vector2(120, 38), new Vector2(-135, 0), "下一个 >", JadeGreen).onClick.AddListener(ShowNext);

        // Detail panel (right side)
        CreateDetailPanel(root.transform);
    }

    private void CreateDetailPanel(Transform canvasT)
    {
        detailPanelObj = CreateUI("DetailPanel", canvasT);
        var dpr = detailPanelObj.GetComponent<RectTransform>();
        dpr.anchorMin = new Vector2(1f, 0f); dpr.anchorMax = Vector2.one;
        dpr.pivot = new Vector2(1f, 0.5f);
        dpr.sizeDelta = new Vector2(320, 0);
        detailPanelObj.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f, 0.95f);

        // Tabs
        string[] tabNames = { "历史背景", "制作工艺", "文化寓意" };
        for (int i = 0; i < 3; i++)
        {
            var tab = CreateUI($"Tab{i}", detailPanelObj.transform);
            var tr = tab.GetComponent<RectTransform>();
            tr.anchorMin = new Vector2(i / 3f, 1f); tr.anchorMax = new Vector2((i + 1) / 3f, 1f);
            tr.pivot = new Vector2(0f, 1f); tr.sizeDelta = new Vector2(0, 38);
            tab.AddComponent<Image>().color = i == 0 ? ZhuRed : new Color(0.25f, 0.25f, 0.25f);
            var tb = tab.AddComponent<Button>(); int idx = i; tb.onClick.AddListener(() => SwitchTab(idx));
            var ttObj = CreateUI("T", tab.transform); Stretch(ttObj);
            var tt = ttObj.AddComponent<Text>();
            tt.font = UIFont.Get();
            tt.text = tabNames[i]; tt.fontSize = 15; tt.color = Color.white; tt.alignment = TextAnchor.MiddleCenter;
        }

        // Content
        var cObj = CreateUI("Content", detailPanelObj.transform);
        var cr2 = cObj.GetComponent<RectTransform>();
        cr2.anchorMin = Vector2.zero; cr2.anchorMax = new Vector2(1f, 1f);
        cr2.offsetMin = new Vector2(12, 12); cr2.offsetMax = new Vector2(-12, -48);
        contentText = cObj.AddComponent<Text>();
        contentText.font = UIFont.Get();
        contentText.fontSize = 16; contentText.color = XuanPaper;
        contentText.alignment = TextAnchor.UpperLeft;
    }

    private void SwitchTab(int idx)
    {
        currentTab = idx;
        if (currentExhibit == null) return;
        // Update tab colors
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

    private void ShowToast(string msg, Color color)
    {
        var toastObj = CreateUI("Toast", canvasT);
        var tr = toastObj.GetComponent<RectTransform>();
        tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0f);
        tr.pivot = new Vector2(0.5f, 0f);
        tr.sizeDelta = new Vector2(300, 40);
        tr.anchoredPosition = new Vector2(0, 10);
        toastObj.AddComponent<Image>().color = new Color(0, 0, 0, 0.8f);
        var toastTxt = CreateUI("T", toastObj.transform); Stretch(toastTxt);
        var t = toastTxt.AddComponent<Text>();
        t.font = UIFont.Get();
        t.text = msg; t.fontSize = 18; t.color = color; t.alignment = TextAnchor.MiddleCenter;
        Destroy(toastObj, 2.5f);
    }

    #region UI Helpers
    private GameObject CreateUI(string name, Transform parent)
    { var o = new GameObject(name); o.transform.SetParent(parent, false); o.AddComponent<RectTransform>(); return o; }
    private void Stretch(GameObject o)
    { var r = o.GetComponent<RectTransform>(); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.sizeDelta = Vector2.zero; }

    private Text AddLabel(string name, Transform parent, Vector2 pos, Vector2 size, string text, int fontSize, Color color)
    {
        var o = CreateUI(name, parent);
        var r = o.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f); r.pivot = new Vector2(0.5f, 1f);
        r.sizeDelta = size; r.anchoredPosition = pos;
        var t = o.AddComponent<Text>();
        t.font = UIFont.Get();
        t.text = text; t.fontSize = fontSize; t.color = color; t.alignment = TextAnchor.MiddleCenter;
        return t;
    }

    private Button AddSideBtn(string name, Transform parent, Vector2 anchor, Vector2 size, Vector2 offset, string text, Color bg)
    {
        var o = CreateUI(name, parent);
        var r = o.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = anchor; r.pivot = new Vector2(0.5f, 0.5f);
        r.sizeDelta = size; r.anchoredPosition = offset;
        o.AddComponent<Image>().color = bg;
        var btn = o.AddComponent<Button>();
        var to = CreateUI("T", o.transform); Stretch(to);
        var t = to.AddComponent<Text>();
        t.font = UIFont.Get();
        t.text = text; t.fontSize = 16; t.color = Color.white; t.alignment = TextAnchor.MiddleCenter;
        return btn;
    }
    #endregion
}
