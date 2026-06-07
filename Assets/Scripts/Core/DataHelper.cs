using System;
using System.IO;
using UnityEngine;

/// <summary>
/// JSON 文件读写工具类
/// </summary>
public static class DataHelper
{
    /// <summary>
    /// 从 StreamingAssets 读取 JSON 文件并反序列化
    /// </summary>
    public static T LoadFromStreamingAssets<T>(string relativePath)
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, relativePath);
        if (!File.Exists(fullPath))
        {
            Debug.LogWarning($"File not found: {fullPath}");
            return default;
        }
        string json = File.ReadAllText(fullPath);
        return JsonUtility.FromJson<T>(json);
    }

    /// <summary>
    /// 从 persistentDataPath 读取 JSON 文件并反序列化
    /// </summary>
    public static T LoadFromPersistent<T>(string relativePath)
    {
        string fullPath = Path.Combine(Application.persistentDataPath, relativePath);
        if (!File.Exists(fullPath))
        {
            Debug.LogWarning($"File not found: {fullPath}");
            return default;
        }
        string json = File.ReadAllText(fullPath);
        return JsonUtility.FromJson<T>(json);
    }

    /// <summary>
    /// 将对象序列化为 JSON 并保存到 persistentDataPath
    /// </summary>
    public static bool SaveToPersistent<T>(T data, string relativePath)
    {
        try
        {
            string fullPath = Path.Combine(Application.persistentDataPath, relativePath);
            string directory = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(fullPath, json);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save data: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 检查 persistentDataPath 下的文件是否存在
    /// </summary>
    public static bool PersistentFileExists(string relativePath)
    {
        return File.Exists(Path.Combine(Application.persistentDataPath, relativePath));
    }
}
