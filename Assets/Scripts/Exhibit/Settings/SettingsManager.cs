using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 设置管理器，管理音量、亮度、个性化设置
/// </summary>
public class SettingsManager : MonoBehaviour
{
    private static readonly Color ZhuRed = new Color(0.76f, 0.21f, 0.19f);
    private static readonly Color GoldColor = new Color(0.83f, 0.65f, 0.27f);
    private static readonly Color XuanPaper = new Color(0.96f, 0.90f, 0.78f);

    public void CreateSettingsUI(Transform parent)
    {
        var panel = CreateUIObject("SettingsContent", parent);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(380, 320);
        panelRect.anchoredPosition = Vector2.zero;

        AddText("SettingsTitle", panel.transform,
            new Vector2(0, -20), new Vector2(340, 35), "系统设置", 26, ZhuRed, new Vector2(0.5f, 1f));

        // 音量
        AddText("VolumeLabel", panel.transform,
            new Vector2(0, -70), new Vector2(340, 25), "音量", 16, Color.black, new Vector2(0.5f, 1f));

        var volumeSlider = CreateSlider("VolumeSlider", panel.transform,
            new Vector2(0, -100), new Vector2(300, 30), 0f, 1f, GameManager.Instance.volume);
        volumeSlider.onValueChanged.AddListener(v =>
        {
            GameManager.Instance.volume = v;
            AudioListener.volume = v;
        });

        // 亮度
        AddText("BrightnessLabel", panel.transform,
            new Vector2(0, -140), new Vector2(340, 25), "亮度", 16, Color.black, new Vector2(0.5f, 1f));

        var brightnessSlider = CreateSlider("BrightnessSlider", panel.transform,
            new Vector2(0, -170), new Vector2(300, 30), 0.3f, 1f, GameManager.Instance.brightness);
        brightnessSlider.onValueChanged.AddListener(v =>
        {
            GameManager.Instance.brightness = v;
            // 简单地通过 Render Settings 控制环境光亮度
            RenderSettings.ambientIntensity = v;
        });

        // 主题风格
        AddText("ThemeLabel", panel.transform,
            new Vector2(0, -210), new Vector2(340, 25), "主题风格", 16, Color.black, new Vector2(0.5f, 1f));

        string[] themes = { "默认", "古典", "简约" };
        for (int i = 0; i < themes.Length; i++)
        {
            var btn = CreateSideButton($"ThemeBtn_{i}", panel.transform,
                new Vector2(-130 + i * 130, -245), new Vector2(110, 35), themes[i],
                GameManager.Instance.themeStyle == themes[i].ToLower() ? ZhuRed : new Color(0.4f, 0.4f, 0.4f));
            int idx = i;
            btn.onClick.AddListener(() =>
            {
                GameManager.Instance.themeStyle = themes[idx].ToLower();
                GameManager.Instance.SaveSettings();
            });
        }

        // 保存按钮
        var saveBtn = CreateSideButton("SaveBtn", panel.transform,
            new Vector2(0, -295), new Vector2(160, 40), "保存设置", ZhuRed);
        saveBtn.onClick.AddListener(() =>
        {
            GameManager.Instance.SaveSettings();
            Debug.Log("Settings saved!");
        });
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
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        return tmp;
    }

    private Slider CreateSlider(string name, Transform parent, Vector2 pos, Vector2 size,
        float min, float max, float value)
    {
        var obj = CreateUIObject(name, parent);
        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        var bgObj = CreateUIObject("Background", obj.transform);
        SetStretch(bgObj);
        bgObj.AddComponent<UnityEngine.UI.Image>().color = new Color(0.3f, 0.3f, 0.3f);

        var fillObj = CreateUIObject("Fill", obj.transform);
        var fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2((value - min) / (max - min), 1f);
        fillRect.sizeDelta = Vector2.zero;
        fillObj.AddComponent<UnityEngine.UI.Image>().color = ZhuRed;

        var handleObj = CreateUIObject("Handle", obj.transform);
        var handleRect = handleObj.GetComponent<RectTransform>();
        handleRect.anchorMin = handleRect.anchorMax = new Vector2((value - min) / (max - min), 0.5f);
        handleRect.sizeDelta = new Vector2(20, 30);
        handleObj.AddComponent<UnityEngine.UI.Image>().color = Color.white;

        var slider = obj.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = value;
        slider.fillRect = fillObj.GetComponent<RectTransform>();
        slider.handleRect = handleObj.GetComponent<RectTransform>();

        return slider;
    }

    private Button CreateSideButton(string name, Transform parent, Vector2 pos, Vector2 size, string text, Color bgColor)
    {
        var obj = CreateUIObject(name, parent);
        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        obj.AddComponent<UnityEngine.UI.Image>().color = bgColor;
        var btn = obj.AddComponent<Button>();
        var tObj = CreateUIObject("Text", obj.transform);
        SetStretch(tObj);
        var tmp = tObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = 16; tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white;
        return btn;
    }

    #endregion
}
