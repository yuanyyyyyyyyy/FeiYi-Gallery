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

    [Header("知识数据")]
    public KnowledgeDataList knowledgeDataList;
    private Dictionary<string, KnowledgeItem> knowledgeDict;

    [Header("问答数据")]
    public QuizDataList quizDataList;
    private Dictionary<string, QuizQuestion> quizDict;

    [Header("事件数据")]
    public EventDataList eventDataList;
    private Dictionary<string, EventItem> eventDict;

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
        LoadKnowledgeData();
        LoadQuizData();
        LoadEventData();
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
    /// 加载知识数据
    /// </summary>
    private void LoadKnowledgeData()
    {
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, "Knowledge.json");
        if (System.IO.File.Exists(path))
        {
            string json = System.IO.File.ReadAllText(path);
            knowledgeDataList = JsonUtility.FromJson<KnowledgeDataList>(json);
        }
        else
        {
            Debug.LogWarning("Knowledge.json not found, using empty list.");
            knowledgeDataList = new KnowledgeDataList { knowledge = new List<KnowledgeItem>() };
        }

        knowledgeDict = new Dictionary<string, KnowledgeItem>();
        if (knowledgeDataList.knowledge != null)
        {
            foreach (var item in knowledgeDataList.knowledge)
                knowledgeDict[item.id] = item;
        }
    }

    /// <summary>
    /// 加载问答数据
    /// </summary>
    private void LoadQuizData()
    {
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, "Quiz.json");
        if (System.IO.File.Exists(path))
        {
            string json = System.IO.File.ReadAllText(path);
            quizDataList = JsonUtility.FromJson<QuizDataList>(json);
        }
        else
        {
            Debug.LogWarning("Quiz.json not found, using empty list.");
            quizDataList = new QuizDataList { quizzes = new List<QuizQuestion>() };
        }

        quizDict = new Dictionary<string, QuizQuestion>();
        if (quizDataList.quizzes != null)
        {
            foreach (var q in quizDataList.quizzes)
                quizDict[q.id] = q;
        }
    }

    /// <summary>
    /// 获取某个品类的所有知识卡片
    /// </summary>
    public List<KnowledgeItem> GetKnowledgeByCategory(string category)
    {
        var result = new List<KnowledgeItem>();
        if (knowledgeDataList?.knowledge != null)
        {
            foreach (var k in knowledgeDataList.knowledge)
            {
                if (k.category == category)
                    result.Add(k);
            }
        }
        return result;
    }

    /// <summary>
    /// 获取某个品类的所有问答题目
    /// </summary>
    public List<QuizQuestion> GetQuizByCategory(string category)
    {
        var result = new List<QuizQuestion>();
        if (quizDataList?.quizzes != null)
        {
            foreach (var q in quizDataList.quizzes)
            {
                if (q.category == category)
                    result.Add(q);
            }
        }
        return result;
    }

    /// <summary>
    /// 加载事件数据
    /// </summary>
    private void LoadEventData()
    {
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, "Events.json");
        if (System.IO.File.Exists(path))
        {
            string json = System.IO.File.ReadAllText(path);
            eventDataList = JsonUtility.FromJson<EventDataList>(json);
        }
        else
        {
            Debug.LogWarning("Events.json not found, using empty list.");
            eventDataList = new EventDataList { events = new List<EventItem>() };
        }

        eventDict = new Dictionary<string, EventItem>();
        if (eventDataList.events != null)
        {
            foreach (var e in eventDataList.events)
                eventDict[e.id] = e;
        }
    }

    /// <summary>
    /// 获取某个品类的所有事件
    /// </summary>
    public List<EventItem> GetEventsByCategory(string category)
    {
        var result = new List<EventItem>();
        if (eventDataList?.events != null)
        {
            foreach (var e in eventDataList.events)
            {
                if (e.category == category)
                    result.Add(e);
            }
        }
        return result;
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
