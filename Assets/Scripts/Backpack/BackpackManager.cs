using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// 背包管理器，管理用户收藏的展品，支持 JSON 持久化存储
/// </summary>
public class BackpackManager
{
    private const string BACKPACK_KEY = "Backpack_";
    private static BackpackManager _instance;
    public static BackpackManager Instance
    {
        get
        {
            if (_instance == null) _instance = new BackpackManager();
            return _instance;
        }
    }

    /// <summary>
    /// 获取当前用户的背包列表
    /// </summary>
    public List<string> GetBackpackItems(string username)
    {
        string key = BACKPACK_KEY + username;
        string data = PlayerPrefs.GetString(key, "");
        if (string.IsNullOrEmpty(data)) return new List<string>();
        return new List<string>(data.Split(','));
    }

    /// <summary>
    /// 添加展品到背包
    /// </summary>
    public bool AddToBackpack(string username, string exhibitId)
    {
        var items = GetBackpackItems(username);
        if (items.Contains(exhibitId)) return false;
        items.Add(exhibitId);
        SaveBackpack(username, items);

        // 同步到 JSON 文件
        SaveBackpackToJson(username, items);
        return true;
    }

    /// <summary>
    /// 从背包移除展品
    /// </summary>
    public bool RemoveFromBackpack(string username, string exhibitId)
    {
        var items = GetBackpackItems(username);
        if (!items.Contains(exhibitId)) return false;
        items.Remove(exhibitId);
        SaveBackpack(username, items);

        SaveBackpackToJson(username, items);
        return true;
    }

    /// <summary>
    /// 检查展品是否在背包中
    /// </summary>
    public bool IsInBackpack(string username, string exhibitId)
    {
        return GetBackpackItems(username).Contains(exhibitId);
    }

    /// <summary>
    /// 按品类筛选背包中的展品
    /// </summary>
    public List<ExhibitData> GetBackpackByCategory(string username, string category)
    {
        var items = GetBackpackItems(username);
        var result = new List<ExhibitData>();
        foreach (var id in items)
        {
            var data = GameManager.Instance.GetExhibit(id);
            if (data != null && (string.IsNullOrEmpty(category) || data.category == category))
            {
                result.Add(data);
            }
        }
        return result;
    }

    private void SaveBackpack(string username, List<string> items)
    {
        string key = BACKPACK_KEY + username;
        PlayerPrefs.SetString(key, string.Join(",", items));
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 将背包数据保存到 JSON 文件（满足课程 JSON 存储要求）
    /// </summary>
    private void SaveBackpackToJson(string username, List<string> items)
    {
        try
        {
            var backpackData = new BackpackJsonData
            {
                username = username,
                items = items,
                updateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            string json = JsonUtility.ToJson(backpackData, true);
            string dir = Path.Combine(Application.persistentDataPath, "Backpack");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, $"{username}_backpack.json");
            File.WriteAllText(path, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save backpack JSON: {e.Message}");
        }
    }

    [System.Serializable]
    private class BackpackJsonData
    {
        public string username;
        public List<string> items;
        public string updateTime;
    }
}
