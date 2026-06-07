using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局游戏管理器，管理用户登录状态、展品数据和全局设置
/// </summary>
public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("[GameManager]");
                _instance = go.AddComponent<GameManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [Header("当前用户")]
    public string currentUser;
    public bool isLoggedIn;

    [Header("全局设置")]
    [Range(0f, 1f)]
    public float volume = 1f;
    [Range(0.3f, 1f)]
    public float brightness = 1f;
    public string themeStyle = "default";

    [Header("展品数据")]
    public ExhibitDataList exhibitDataList;
    private Dictionary<string, ExhibitData> exhibitDict;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();
        LoadExhibitData();
    }

    /// <summary>
    /// 加载展品数据
    /// </summary>
    private void LoadExhibitData()
    {
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, "Exhibits.json");
        if (System.IO.File.Exists(path))
        {
            string json = System.IO.File.ReadAllText(path);
            exhibitDataList = JsonUtility.FromJson<ExhibitDataList>(json);
        }
        else
        {
            Debug.LogWarning("Exhibits.json not found, using empty list.");
            exhibitDataList = new ExhibitDataList { exhibits = new List<ExhibitData>() };
        }

        // 构建字典
        exhibitDict = new Dictionary<string, ExhibitData>();
        if (exhibitDataList.exhibits != null)
        {
            foreach (var exhibit in exhibitDataList.exhibits)
            {
                exhibitDict[exhibit.id] = exhibit;
            }
        }
    }

    /// <summary>
    /// 获取展品数据
    /// </summary>
    public ExhibitData GetExhibit(string id)
    {
        if (exhibitDict != null && exhibitDict.ContainsKey(id))
            return exhibitDict[id];
        return null;
    }

    /// <summary>
    /// 获取某个品类的所有展品
    /// </summary>
    public List<ExhibitData> GetExhibitsByCategory(string category)
    {
        var result = new List<ExhibitData>();
        if (exhibitDataList?.exhibits != null)
        {
            foreach (var e in exhibitDataList.exhibits)
            {
                if (e.category == category)
                    result.Add(e);
            }
        }
        return result;
    }

    /// <summary>
    /// 加载用户设置
    /// </summary>
    private void LoadSettings()
    {
        volume = PlayerPrefs.GetFloat("Volume", 1f);
        brightness = PlayerPrefs.GetFloat("Brightness", 1f);
        themeStyle = PlayerPrefs.GetString("ThemeStyle", "default");
    }

    /// <summary>
    /// 保存用户设置
    /// </summary>
    public void SaveSettings()
    {
        PlayerPrefs.SetFloat("Volume", volume);
        PlayerPrefs.SetFloat("Brightness", brightness);
        PlayerPrefs.SetString("ThemeStyle", themeStyle);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    public void Login(string username)
    {
        currentUser = username;
        isLoggedIn = true;
        PlayerPrefs.SetString("LastUser", username);
        PlayerPrefs.SetString("LoginTime", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 用户登出
    /// </summary>
    public void Logout()
    {
        SaveSettings();
        currentUser = "";
        isLoggedIn = false;
    }

    /// <summary>
    /// 简单加密（Base64 + 偏移）
    /// </summary>
    public static string EncryptPassword(string password)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(password);
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)(bytes[i] ^ 0x5A);
        }
        return System.Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// 解密密码
    /// </summary>
    public static string DecryptPassword(string encrypted)
    {
        try
        {
            byte[] bytes = System.Convert.FromBase64String(encrypted);
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(bytes[i] ^ 0x5A);
            }
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return "";
        }
    }
}
