using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 品类卡片组件
/// </summary>
public class CategoryCard : MonoBehaviour
{
    public string categoryName;
    public string description;
    public Color cardColor;
    public string iconText;
    public event System.Action<string> OnCardClicked;

    private void Start()
    {
        var img = GetComponent<Image>();
        if (img != null) img.color = cardColor;

        var icon = transform.Find("Icon");
        if (icon != null)
        {
            var ic = icon.GetComponent<Text>();
            if (ic != null) ic.text = iconText;
        }

        var nameObj = transform.Find("Name");
        if (nameObj != null)
        {
            var nt = nameObj.GetComponent<Text>();
            if (nt != null) nt.text = categoryName;
        }

        var desc = transform.Find("Desc");
        if (desc != null)
        {
            var dt = desc.GetComponent<Text>();
            if (dt != null) dt.text = description;
        }

        var btn = GetComponent<Button>();
        if (btn == null) btn = gameObject.AddComponent<Button>();
        btn.onClick.AddListener(() => OnCardClicked?.Invoke(categoryName));
    }

    public void Initialize(string name, string desc, Color color, string icon)
    {
        categoryName = name; description = desc; cardColor = color; iconText = icon;
    }
}
