using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 展品详情面板，显示展品的历史背景、制作工艺、文化寓意
/// </summary>
public class DetailPanel : MonoBehaviour
{
    private static readonly Color ZhuRed = new Color(0.76f, 0.21f, 0.19f);
    private static readonly Color GoldColor = new Color(0.83f, 0.65f, 0.27f);
    private static readonly Color XuanPaper = new Color(0.96f, 0.90f, 0.78f);

    private ExhibitData currentExhibit;
    private int currentTab; // 0=历史, 1=工艺, 2=寓意
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI contentText;
    private TextMeshProUGUI[] tabTexts;
    private GameObject panelGameObject;

    public void Initialize(Transform parent)
    {
        // 面板背景
        var panelObj = CreateUIObject("DetailPanel", parent);
        var panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = Vector2.one;
        panelRect.pivot = new Vector2(1f, 0.5f);
        panelRect.sizeDelta = new Vector2(350, 0);
        panelRect.anchoredPosition = Vector2.zero;
        panelObj.AddComponent<UnityEngine.UI.Image>().color = new Color(0.12f, 0.12f, 0.12f, 0.95f);

        // 标题
        titleText = AddText("ExhibitTitle", panelObj.transform,
            new Vector2(0, -20), new Vector2(310, 40), "", 24, ZhuRed, new Vector2(0.5f, 1f));

        // Tab 按钮
        string[] tabNames = { "历史背景", "制作工艺", "文化寓意" };
        tabTexts = new TextMeshProUGUI[3];
        for (int i = 0; i < 3; i++)
        {
            var tabObj = CreateUIObject($"Tab_{i}", panelObj.transform);
            var tabRect = tabObj.GetComponent<RectTransform>();
            tabRect.anchorMin = new Vector2(i / 3f, 1f);
            tabRect.anchorMax = new Vector2((i + 1) / 3f, 1f);
            tabRect.pivot = new Vector2(0f, 1f);
            tabRect.sizeDelta = new Vector2(0, 40);
            tabRect.anchoredPosition = Vector2.zero;
            var tabImg = tabObj.AddComponent<UnityEngine.UI.Image>();
            tabImg.color = i == 0 ? ZhuRed : new Color(0.2f, 0.2f, 0.2f);
            var tabBtn = tabObj.AddComponent<Button>();
            int idx = i;
            tabBtn.onClick.AddListener(() => SwitchTab(idx));
            var tabTextObj = CreateUIObject("Text", tabObj.transform);
            SetStretch(tabTextObj);
            tabTexts[i] = tabTextObj.AddComponent<TextMeshProUGUI>();
            tabTexts[i].text = tabNames[i];
            tabTexts[i].fontSize = 16;
            tabTexts[i].alignment = TextAlignmentOptions.Center;
            tabTexts[i].color = Color.white;
        }

        // 内容区
        contentText = AddText("ContentText", panelObj.transform,
            new Vector2(0, -100), new Vector2(320, 400), "", 16, XuanPaper, new Vector2(0.5f, 1f));
        var ctRect = contentText.GetComponent<RectTransform>();
        ctRect.anchorMin = new Vector2(0f, 0f);
        ctRect.anchorMax = new Vector2(1f, 1f);
        ctRect.offsetMin = new Vector2(15, 15);
        ctRect.offsetMax = new Vector2(-15, -70);

        panelGameObject = panelObj;
    }

    public void ShowExhibit(ExhibitData data)
    {
        currentExhibit = data;
        titleText.text = data.name;
        SwitchTab(0);
        panelGameObject.SetActive(true);
    }

    private void SwitchTab(int index)
    {
        currentTab = index;
        if (currentExhibit == null) return;

        // 更新 Tab 样式
        for (int i = 0; i < 3; i++)
        {
            var tabImg = tabTexts[i].transform.parent.GetComponent<UnityEngine.UI.Image>();
            tabImg.color = i == index ? ZhuRed : new Color(0.2f, 0.2f, 0.2f);
        }

        // 更新内容
        switch (index)
        {
            case 0: contentText.text = currentExhibit.history; break;
            case 1: contentText.text = currentExhibit.craft; break;
            case 2: contentText.text = currentExhibit.meaning; break;
        }
    }

    #region UI Helpers
    private GameObject CreateUIObject(string name, Transform parent)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }

    private void SetStretch(GameObject obj)
    {
        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
    }

    private TextMeshProUGUI AddText(string name, Transform parent, Vector2 pos, Vector2 size,
        string text, float fontSize, Color color, Vector2 anchor)
    {
        var obj = CreateUIObject(name, parent);
        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.color = color;
        return tmp;
    }
    #endregion
}
