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
    /// 发送对话请求（协程方式）
    /// </summary>
    /// <param name="messages">消息列表</param>
    /// <param name="onComplete">完成回调（成功返回内容，失败返回null）</param>
    public void SendChat(ChatMessage[] messages, Action<string> onComplete)
    {
        if (config == null)
        {
            Debug.LogError("[AIChatClient] 未初始化，请先调用Initialize");
            onComplete?.Invoke(null);
            return;
        }

        StartCoroutine(SendChatCoroutine(messages, onComplete));
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

        StartCoroutine(SendChatCoroutine(testMessages, (response) =>
        {
            if (response != null)
            {
                onResult?.Invoke(true, "连接成功");
            }
            else
            {
                onResult?.Invoke(false, "连接失败，请检查API地址和模型名");
            }
        }));
    }

    private IEnumerator SendChatCoroutine(ChatMessage[] messages, Action<string> onComplete)
    {
        // 构建请求体
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
            request.timeout = config.timeoutSeconds;

            // 设置请求头
            request.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
            if (!string.IsNullOrEmpty(config.apiKey))
            {
                request.SetRequestHeader("Authorization", "Bearer " + config.apiKey);
            }

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
                {
                    errorMsg = "无法连接到API服务器，请确认Ollama或云服务已启动";
                }
                else if (request.responseCode == 401)
                {
                    errorMsg = "API密钥无效";
                }
                else if (request.responseCode == 404)
                {
                    errorMsg = "API地址不正确或模型不存在";
                }

                Debug.LogWarning($"[AIChatClient] 请求失败: {errorMsg} (Code: {request.responseCode})");
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

/// <summary>
/// 对话消息
/// </summary>
[Serializable]
public class ChatMessage
{
    public string role;     // "system" / "user" / "assistant"
    public string content;
}

/// <summary>
/// 对话请求体（OpenAI兼容格式）
/// </summary>
[Serializable]
public class ChatRequest
{
    public string model;
    public ChatMessage[] messages;
    public int max_tokens;
    public float temperature;
}

/// <summary>
/// 对话响应体（OpenAI兼容格式）
/// </summary>
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
