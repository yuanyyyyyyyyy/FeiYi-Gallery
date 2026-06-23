using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AI对话管理器，管理上下文、角色切换、prompt构建、知识注入
/// 单例模式，挂载到GameManager所在GameObject上
/// </summary>
public class AIChatManager : MonoBehaviour
{
    private static AIChatManager _instance;
    public static AIChatManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("[AIChatManager]");
                _instance = go.AddComponent<AIChatManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private AIChatClient chatClient;
    private AIConfig config;
    private AIPersona currentPersona;
    private List<ChatMessage> chatHistory = new List<ChatMessage>();
    private bool isWaitingResponse = false;

    private List<ChatSession> savedSessions = new List<ChatSession>();
    private const int MaxSavedSessions = 20;
    private static readonly string SessionsFilePath =
        System.IO.Path.Combine(Application.persistentDataPath, "chat_sessions.json");

    /// <summary>
    /// 当前角色人设
    /// </summary>
    public AIPersona CurrentPersona => currentPersona ?? AIPersona.GetGuardian();

    /// <summary>
    /// 是否正在等待AI回复
    /// </summary>
    public bool IsWaitingResponse => isWaitingResponse;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        Initialize();
    }

    /// <summary>
    /// 初始化：加载配置、创建客户端
    /// </summary>
    public void Initialize()
    {
        config = AIConfig.Load();
        chatClient = gameObject.AddComponent<AIChatClient>();
        chatClient.Initialize(config);

        // 默认使用守艺人
        currentPersona = AIPersona.GetGuardian();

        // 加载历史会话
        LoadSessionsFromDisk();
    }

    /// <summary>
    /// 切换角色人设（根据品类）
    /// </summary>
    public void SwitchPersona(string category)
    {
        var newPersona = AIPersona.GetByCategory(category);
        if (newPersona.id != currentPersona?.id)
        {
            currentPersona = newPersona;
            // 切换角色时清空对话历史
            chatHistory.Clear();
        }
    }

    /// <summary>
    /// 切换到指定人设
    /// </summary>
    public void SwitchToPersona(string personaId)
    {
        var personas = AIPersona.GetAllPersonas();
        foreach (var p in personas)
        {
            if (p.id == personaId)
            {
                currentPersona = p;
                chatHistory.Clear();
                return;
            }
        }
    }

    /// <summary>
    /// 发送用户消息
    /// </summary>
    /// <param name="userMessage">用户输入</param>
    /// <param name="onComplete">AI回复回调</param>
    public void SendMessage(string userMessage, System.Action<string> onComplete)
    {
        SendMessage(userMessage, onComplete, null);
    }

    /// <summary>
    /// 发送用户消息（带状态回调）
    /// </summary>
    public void SendMessage(string userMessage, System.Action<string> onComplete, System.Action<string> onStatus)
    {
        if (isWaitingResponse) return;

        chatHistory.Add(new ChatMessage { role = "user", content = userMessage });

        var allMessages = BuildMessages();

        isWaitingResponse = true;
        chatClient.SendChat(allMessages, (response) =>
        {
            isWaitingResponse = false;

            if (response != null)
            {
                chatHistory.Add(new ChatMessage { role = "assistant", content = response });
            }

            onComplete?.Invoke(response);
        }, onStatus);
    }

    /// <summary>
    /// 请求出题（考考我）
    /// </summary>
    /// <param name="onComplete">AI回复回调</param>
    public void RequestQuiz(System.Action<string> onComplete)
    {
        RequestQuiz(onComplete, null);
    }

    /// <summary>
    /// 请求出题（考考我，带状态回调）
    /// </summary>
    public void RequestQuiz(System.Action<string> onComplete, System.Action<string> onStatus)
    {
        string category = currentPersona.category;
        var quizList = GameManager.Instance.GetQuizByCategory(category ?? "瓷器");

        // 构建出题指令
        string quizContext = "";
        if (quizList != null && quizList.Count > 0)
        {
            // 随机选一道题，将题目信息作为提示
            var quiz = quizList[Random.Range(0, quizList.Count)];
            quizContext = $"\n\n请基于以下题目出题（不要直接给答案，只出题）：\n" +
                         $"问题：{quiz.question}\n" +
                         $"选项：A.{quiz.options[0]} B.{quiz.options[1]} C.{quiz.options[2]} D.{quiz.options[3]}\n" +
                         $"正确答案：{quiz.options[quiz.correctIndex]}\n" +
                         $"解析：{quiz.explanation}\n" +
                         $"请用你的身份口吻重新包装这道题，让它更有趣，但保持选项内容不变。";
        }
        else
        {
            quizContext = "\n\n请出一道关于非遗文化的选择题，4个选项，标明正确答案。";
        }

        string quizMessage = "请考考我吧！" + quizContext;
        SendMessage(quizMessage, onComplete, onStatus);
    }

    /// <summary>
    /// 清空对话历史
    /// </summary>
    public void ClearHistory()
    {
        chatHistory.Clear();
    }

    // ──────────────────── 会话管理 ────────────────────

    /// <summary>
    /// 新建会话：保存当前会话后清空历史
    /// </summary>
    public void NewSession()
    {
        SaveCurrentSession();
        chatHistory.Clear();
    }

    /// <summary>
    /// 保存当前会话到历史列表
    /// </summary>
    public void SaveCurrentSession()
    {
        if (chatHistory.Count == 0) return;

        var persona = CurrentPersona;
        string firstUserMsg = null;
        foreach (var msg in chatHistory)
        {
            if (msg.role == "user") { firstUserMsg = msg.content; break; }
        }

        var session = new ChatSession
        {
            personaId = persona.id,
            personaName = persona.name,
            category = persona.category,
            timestamp = System.DateTime.Now.ToString("MM-dd HH:mm"),
            preview = firstUserMsg ?? "",
            messages = new List<ChatMessage>(chatHistory)
        };

        savedSessions.Insert(0, session);
        if (savedSessions.Count > MaxSavedSessions)
            savedSessions.RemoveRange(MaxSavedSessions, savedSessions.Count - MaxSavedSessions);

        SaveSessionsToDisk();
    }

    /// <summary>
    /// 获取所有已保存的会话
    /// </summary>
    public List<ChatSession> GetSavedSessions()
    {
        return savedSessions;
    }

    /// <summary>
    /// 加载指定会话到当前上下文
    /// </summary>
    public ChatSession LoadSession(int index)
    {
        if (index < 0 || index >= savedSessions.Count) return null;

        var session = savedSessions[index];

        // 切换到会话的角色
        SwitchToPersona(session.personaId);

        // 加载消息
        chatHistory.Clear();
        chatHistory.AddRange(session.messages);

        return session;
    }

    /// <summary>
    /// 删除指定会话
    /// </summary>
    public void DeleteSession(int index)
    {
        if (index < 0 || index >= savedSessions.Count) return;
        savedSessions.RemoveAt(index);
        SaveSessionsToDisk();
    }

    private void SaveSessionsToDisk()
    {
        try
        {
            var wrapper = new ChatSessionList { sessions = savedSessions };
            string json = JsonUtility.ToJson(wrapper, true);
            System.IO.File.WriteAllText(SessionsFilePath, json);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[AIChatManager] 保存会话失败: {e.Message}");
        }
    }

    private void LoadSessionsFromDisk()
    {
        try
        {
            if (System.IO.File.Exists(SessionsFilePath))
            {
                string json = System.IO.File.ReadAllText(SessionsFilePath);
                var wrapper = JsonUtility.FromJson<ChatSessionList>(json);
                if (wrapper?.sessions != null)
                    savedSessions = wrapper.sessions;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[AIChatManager] 加载会话失败: {e.Message}");
        }
    }

    /// <summary>
    /// 重新加载配置
    /// </summary>
    public void ReloadConfig()
    {
        config = AIConfig.Load();
        chatClient.Initialize(config);
    }

    /// <summary>
    /// 获取AI配置
    /// </summary>
    public AIConfig GetConfig()
    {
        return config;
    }

    /// <summary>
    /// 测试API连接
    /// </summary>
    public void TestConnection(System.Action<bool, string> onResult)
    {
        chatClient.TestConnection(onResult);
    }

    /// <summary>
    /// 构建完整消息列表（system prompt + 上下文历史）
    /// </summary>
    private ChatMessage[] BuildMessages()
    {
        var messages = new List<ChatMessage>();

        // System prompt
        string systemPrompt = BuildSystemPrompt();
        messages.Add(new ChatMessage { role = "system", content = systemPrompt });

        // 保留最近N轮对话
        int maxTurns = config?.maxContextTurns ?? 10;
        int startIndex = Mathf.Max(0, chatHistory.Count - maxTurns * 2);
        for (int i = startIndex; i < chatHistory.Count; i++)
        {
            messages.Add(chatHistory[i]);
        }

        return messages.ToArray();
    }

    /// <summary>
    /// 构建系统提示词
    /// </summary>
    private string BuildSystemPrompt()
    {
        var persona = CurrentPersona;

        string prompt = $"你是「{persona.name}」。{persona.description}\n\n" +
                        $"说话风格：{persona.speakingStyle}\n\n" +
                        $"规则：\n" +
                        $"1. 用「{persona.name}」的身份口吻回答问题\n" +
                        $"2. 回答要融入历史文化典故，让知识变得生动有趣\n" +
                        $"3. 如果用户问与非遗无关的问题，礼貌地引导回非遗话题\n" +
                        $"4. 回答简洁，不超过200字\n" +
                        $"5. 出题时保持选项内容准确，但可以用角色口吻包装\n" +
                        $"6. 回答用户答题时，先判断对错，再给出解析";

        // 注入品类相关知识
        string knowledgeContext = BuildKnowledgeContext();
        if (!string.IsNullOrEmpty(knowledgeContext))
        {
            prompt += $"\n\n你了解以下知识：\n{knowledgeContext}";
        }

        return prompt;
    }

    /// <summary>
    /// 构建品类知识上下文（从GameManager加载的数据中提取）
    /// </summary>
    private string BuildKnowledgeContext()
    {
        var sb = new System.Text.StringBuilder();
        string category = currentPersona.category;

        if (!string.IsNullOrEmpty(category))
        {
            // 注入展品知识
            var exhibits = GameManager.Instance.GetExhibitsByCategory(category);
            if (exhibits != null && exhibits.Count > 0)
            {
                sb.AppendLine($"【{category}展品】");
                foreach (var e in exhibits)
                {
                    sb.AppendLine($"- {e.name}：{e.description}");
                    if (!string.IsNullOrEmpty(e.history))
                        sb.AppendLine($"  历史背景：{e.history}");
                    if (!string.IsNullOrEmpty(e.craft))
                        sb.AppendLine($"  制作工艺：{e.craft}");
                    if (!string.IsNullOrEmpty(e.meaning))
                        sb.AppendLine($"  文化寓意：{e.meaning}");
                }
            }

            // 注入文化知识
            var knowledge = GameManager.Instance.GetKnowledgeByCategory(category);
            if (knowledge != null && knowledge.Count > 0)
            {
                sb.AppendLine($"\n【{category}文化知识】");
                foreach (var k in knowledge)
                {
                    sb.AppendLine($"- {k.title}：{k.content}");
                }
            }

            // 注入历史事件
            var events = GameManager.Instance.GetEventsByCategory(category);
            if (events != null && events.Count > 0)
            {
                sb.AppendLine($"\n【{category}历史故事】");
                foreach (var ev in events)
                {
                    sb.AppendLine($"- {ev.title}（{ev.era}）：{ev.description}");
                }
            }
        }
        else
        {
            // 守艺人通用模式，注入所有品类的概要
            sb.AppendLine("【非遗四大品类概览】");
            string[] categories = { "瓷器", "剪纸", "书法", "民族乐器", "刺绣", "茶艺", "皮影戏", "扎染蜡染" };
            foreach (var cat in categories)
            {
                var exhibits = GameManager.Instance.GetExhibitsByCategory(cat);
                if (exhibits != null && exhibits.Count > 0)
                {
                    sb.AppendLine($"\n{cat}：");
                    foreach (var e in exhibits)
                    {
                        sb.AppendLine($"  - {e.name}：{e.description}");
                    }
                }
            }
        }

        return sb.ToString();
    }
}

/// <summary>
/// 单个聊天会话记录
/// </summary>
[System.Serializable]
public class ChatSession
{
    public string personaId;
    public string personaName;
    public string category;
    public string timestamp;
    public string preview;
    public List<ChatMessage> messages = new List<ChatMessage>();
}

/// <summary>
/// 会话列表包装类（用于JSON序列化）
/// </summary>
[System.Serializable]
public class ChatSessionList
{
    public List<ChatSession> sessions = new List<ChatSession>();
}
