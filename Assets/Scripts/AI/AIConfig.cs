using UnityEngine;

/// <summary>
/// AI对话配置，支持OpenAI兼容API（Ollama/云端大模型）
/// </summary>
[System.Serializable]
public class AIConfig
{
    [Header("API设置")]
    [Tooltip("API地址（OpenAI兼容格式）")]
    public string apiUrl = "http://localhost:11434/v1/chat/completions";

    [Tooltip("模型名称")]
    public string modelName = "qwen2.5:3b";

    [Tooltip("API密钥（Ollama本地可留空）")]
    public string apiKey = "";

    [Header("生成参数")]
    [Tooltip("最大生成长度")]
    public int maxTokens = 512;

    [Tooltip("温度（越高越随机）")]
    [Range(0f, 1f)]
    public float temperature = 0.7f;

    [Tooltip("请求超时（秒）")]
    public int timeoutSeconds = 30;

    [Header("对话设置")]
    [Tooltip("上下文保留轮数")]
    public int maxContextTurns = 10;

    // PlayerPrefs 键名
    private const string PREF_API_URL = "AI_ApiUrl";
    private const string PREF_MODEL_NAME = "AI_ModelName";
    private const string PREF_API_KEY = "AI_ApiKey";
    private const string PREF_MAX_TOKENS = "AI_MaxTokens";
    private const string PREF_TEMPERATURE = "AI_Temperature";
    private const string PREF_TIMEOUT = "AI_Timeout";

    /// <summary>
    /// 从PlayerPrefs加载配置
    /// </summary>
    public static AIConfig Load()
    {
        var config = new AIConfig();
        if (PlayerPrefs.HasKey(PREF_API_URL))
            config.apiUrl = PlayerPrefs.GetString(PREF_API_URL);
        if (PlayerPrefs.HasKey(PREF_MODEL_NAME))
            config.modelName = PlayerPrefs.GetString(PREF_MODEL_NAME);
        if (PlayerPrefs.HasKey(PREF_API_KEY))
            config.apiKey = PlayerPrefs.GetString(PREF_API_KEY);
        if (PlayerPrefs.HasKey(PREF_MAX_TOKENS))
            config.maxTokens = PlayerPrefs.GetInt(PREF_MAX_TOKENS, 512);
        if (PlayerPrefs.HasKey(PREF_TEMPERATURE))
            config.temperature = PlayerPrefs.GetFloat(PREF_TEMPERATURE, 0.7f);
        if (PlayerPrefs.HasKey(PREF_TIMEOUT))
            config.timeoutSeconds = PlayerPrefs.GetInt(PREF_TIMEOUT, 30);
        return config;
    }

    /// <summary>
    /// 保存配置到PlayerPrefs
    /// </summary>
    public void Save()
    {
        PlayerPrefs.SetString(PREF_API_URL, apiUrl);
        PlayerPrefs.SetString(PREF_MODEL_NAME, modelName);
        PlayerPrefs.SetString(PREF_API_KEY, apiKey);
        PlayerPrefs.SetInt(PREF_MAX_TOKENS, maxTokens);
        PlayerPrefs.SetFloat(PREF_TEMPERATURE, temperature);
        PlayerPrefs.SetInt(PREF_TIMEOUT, timeoutSeconds);
        PlayerPrefs.Save();
    }
}
