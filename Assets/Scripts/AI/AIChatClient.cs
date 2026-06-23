using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// AI对话HTTP客户端，支持OpenAI兼容API（Ollama/云端大模型）
/// </summary>
public class AIChatClient : MonoBehaviour
{
    private AIConfig config;

    /// <summary>
    /// 初始化客户端
    /// </summary>
    public void Initialize(AIConfig aiConfig)
    {
        config = aiConfig;
    }

    /// <summary>
    /// 发送对话请求（协程方式，首次失败自动重试一次）
    /// </summary>
    public void SendChat(ChatMessage[] messages, Action<string> onComplete)
    {
        SendChat(messages, onComplete, null);
    }

    /// <summary>
    /// 发送对话请求（带状态回调）
    /// </summary>
    public void SendChat(ChatMessage[] messages, Action<string> onComplete, Action<string> onStatus)
    {
        if (config == null)
        {
            Debug.LogError("[AIChatClient] 未初始化，请先调用Initialize");
            onComplete?.Invoke(null);
            return;
        }

        StartCoroutine(SendChatWithRetry(messages, onComplete, onStatus));
    }

    /// <summary>
    /// 测试API连接
    /// </summary>
    public void TestConnection(Action<bool, string> onResult)
    {
        var testMessages = new ChatMessage[]
        {
            new ChatMessage { role = "user", content = "你好" }
        };

        StartCoroutine(SendChatWithRetry(testMessages, (response) =>
        {
            if (response != null)
                onResult?.Invoke(true, "连接成功");
            else
                onResult?.Invoke(false, "连接失败，请检查API地址和模型名");
        }, null));
    }

    private IEnumerator SendChatWithRetry(ChatMessage[] messages, Action<string> onComplete, Action<string> onStatus)
    {
        string result = null;
        bool success = false;

        // 第一次尝试
        yield return StartCoroutine(SendChatOnce(messages, config.timeoutSeconds, (r) => { result = r; success = r != null; }));
        if (success) { onComplete?.Invoke(result); yield break; }

        // 首次失败 → 自动重试（超时翻倍，等待模型加载）
        Debug.Log("[AIChatClient] 首次请求失败，等待3秒后重试（模型可能正在加载）...");
        onStatus?.Invoke("AI模型加载中，正在重试...");
        yield return new WaitForSeconds(3f);

        int retryTimeout = Mathf.Max(config.timeoutSeconds * 2, 60);
        yield return StartCoroutine(SendChatOnce(messages, retryTimeout, (r) => { result = r; success = r != null; }));

        if (success)
            onStatus?.Invoke("收到回复，正在生成...");
        else
            onStatus?.Invoke("连接失败");

        onComplete?.Invoke(result);
    }

    private IEnumerator SendChatOnce(ChatMessage[] messages, int timeout, Action<string> onComplete)
    {
        var requestBody = new ChatRequest
        {
            model = config.modelName,
            messages = messages,
            max_tokens = config.maxTokens,
            temperature = config.temperature
        };

        string jsonBody = JsonUtility.ToJson(requestBody);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest request = new UnityWebRequest(config.apiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = timeout;

            if (!string.IsNullOrEmpty(config.apiKey))
            {
                request.SetRequestHeader("Authorization", "Bearer " + config.apiKey);
            }
            request.SetRequestHeader("Content-Type", "application/json; charset=utf-8");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                string content = ParseResponse(responseText);
                onComplete?.Invoke(content);
            }
            else
            {
                string errorMsg = request.error ?? "未知错误";
                if (request.responseCode == 0)
                    errorMsg = "无法连接到API服务器";
                else if (request.responseCode == 401)
                    errorMsg = "API密钥无效";
                else if (request.responseCode == 404)
                    errorMsg = "API地址不正确或模型不存在";

                Debug.LogWarning($"[AIChatClient] 请求失败: {errorMsg} (Code: {request.responseCode}, Timeout: {timeout}s)");
                onComplete?.Invoke(null);
            }
        }
    }

    /// <summary>
    /// 解析OpenAI兼容格式的响应
    /// </summary>
    private string ParseResponse(string responseText)
    {
        try
        {
            var response = JsonUtility.FromJson<ChatResponse>(responseText);
            if (response?.choices != null && response.choices.Length > 0)
            {
                return response.choices[0].message?.content;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[AIChatClient] 解析响应失败: {e.Message}\n原始响应: {responseText}");
        }
        return null;
    }
}

#region 数据结构

[Serializable]
public class ChatMessage
{
    public string role;
    public string content;
}

[Serializable]
public class ChatRequest
{
    public string model;
    public ChatMessage[] messages;
    public int max_tokens;
    public float temperature;
}

[Serializable]
public class ChatResponse
{
    public ChatChoice[] choices;
}

[Serializable]
public class ChatChoice
{
    public int index;
    public ChatMessage message;
    public string finish_reason;
}

#endregion
