using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 跨场景角色管理器 — 猫咪图片头像 + 浮动呼吸动画 + AI对话
/// DontDestroyOnLoad 单例，在所有场景中显示可点击的猫咪头像
/// 点击头像打开AI对话面板
/// </summary>
public class CharacterManager : MonoBehaviour
{
    private static CharacterManager _instance;
    public static CharacterManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("[CharacterManager]");
                _instance = go.AddComponent<CharacterManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // 角色头像相关
    private Image avatarImg;
    private RectTransform avatarImgRect;
    private Image shadowImg;
    private RectTransform shadowRect;
    private Vector2 savedAvatarPos;

    // 浮动动画
    private const float FloatSpeed = 1.8f;
    private const float FloatAmplitude = 4f;
    private const float BreathSpeed = 1.5f;
    private const float BreathScale = 0.03f;

    // 品类跟踪
    private string lastCategory = "";

    // UI相关
    private Canvas overlayCanvas;
    private RectTransform avatarRect;
    private GameObject avatarButton;
    private AIChatUI chatUI;
    private GameObject personaLabel;

    // 拖动相关
    private bool isDragging;
    private bool hasDragged;
    private Vector2 dragStartPos;
    private const float DragThreshold = 8f;

    // 主题色
    private static Color ZhuRed => GameManager.Instance?.themeStyle == "classic" ? new Color(0.62f,0.14f,0.12f) : GameManager.Instance?.themeStyle == "minimal" ? new Color(0.72f,0.30f,0.28f) : new Color(0.76f,0.21f,0.19f);
    private static Color XuanPaper => GameManager.Instance?.themeStyle == "classic" ? new Color(0.22f,0.17f,0.12f) : GameManager.Instance?.themeStyle == "minimal" ? new Color(0.97f,0.97f,0.97f) : new Color(0.96f,0.90f,0.78f);
    private static Color InkBlack => GameManager.Instance?.themeStyle == "classic" ? new Color(0.88f,0.82f,0.68f) : GameManager.Instance?.themeStyle == "minimal" ? new Color(0.25f,0.25f,0.28f) : new Color(0.17f,0.17f,0.17f);
    private static Color GoldColor => GameManager.Instance?.themeStyle == "classic" ? new Color(0.90f,0.72f,0.30f) : GameManager.Instance?.themeStyle == "minimal" ? new Color(0.50f,0.50f,0.50f) : new Color(0.83f,0.65f,0.27f);

    private static Font Fnt => UIFont.Get();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // 延迟一帧创建，确保 GameManager 等单例先初始化完成
        StartCoroutine(DeferredInit());
    }

    private IEnumerator DeferredInit()
    {
        yield return null; // 等一帧
        LoadCatSprite();
        CreateOverlayCanvas();
        EnsureAIChatManager();
        // personaLabel 刚创建，但 Update 可能已在首帧设置过 lastCategory 并切换了 persona
        // 此时需要重新同步标签
        lastCategory = null; // 强制下次 Update 重新检测并更新标签
        UpdatePersonaFromCategory();
    }

    private void Update()
    {
        UpdateFloatAnimation();
        UpdatePersonaFromCategory();
    }

    private void UpdatePersonaFromCategory()
    {
        string currentCategory = PlayerPrefs.GetString("CurrentCategory", "");
        if (currentCategory != lastCategory)
        {
            lastCategory = currentCategory;
            if (AIChatManager.Instance != null)
                AIChatManager.Instance.SwitchPersona(currentCategory);
            UpdatePersonaLabel();
        }
    }

    private void UpdateFloatAnimation()
    {
        if (avatarImgRect == null || shadowRect == null) return;

        // 拖动时不浮动
        if (isDragging)
        {
            savedAvatarPos = avatarRect.anchoredPosition;
            return;
        }

        float t = Time.time;
        float floatY = Mathf.Sin(t * FloatSpeed) * FloatAmplitude;
        float breath = 1f + Mathf.Sin(t * BreathSpeed) * BreathScale;

        // 猫咪图片上下浮动 + 呼吸缩放
        if (avatarImgRect != null)
        {
            avatarImgRect.anchoredPosition = new Vector2(0, 10f + floatY);
            avatarImgRect.localScale = new Vector3(breath, breath, 1f);
        }

        // 阴影：猫升高时阴影变小变淡，猫降低时阴影变大变浓
        if (shadowRect != null)
        {
            float normalizedFloat = floatY / FloatAmplitude; // -1 ~ 1
            float shadowScale = Mathf.Lerp(1f, 0.75f, (normalizedFloat + 1f) * 0.5f);
            float shadowAlpha = Mathf.Lerp(0.35f, 0.12f, (normalizedFloat + 1f) * 0.5f);
            // 闪烁
            float flicker = 1f + Mathf.Sin(t * 7f) * 0.05f;
            shadowRect.localScale = new Vector3(shadowScale * flicker, shadowScale * 0.6f, 1f);

            var sc = shadowImg.color;
            sc.a = shadowAlpha;
            shadowImg.color = sc;
        }
    }

    private void LateUpdate()
    {
        // 当对话打开时的处理（保留接口，不再操作3D角色）
    }

    /// <summary>
    /// 打开AI对话
    /// </summary>
    public void OpenChat()
    {
        if (chatUI == null) return;
        chatUI.Open();
        UpdatePersonaLabel();
    }

    /// <summary>
    /// 关闭AI对话
    /// </summary>
    public void CloseChat()
    {
        if (chatUI != null) chatUI.Close();
    }

    /// <summary>
    /// 对话是否打开
    /// </summary>
    public bool IsChatOpen => chatUI != null && chatUI.IsOpen;

    /// <summary>
    /// 切换对话显隐
    /// </summary>
    public void ToggleChat()
    {
        if (IsChatOpen) CloseChat();
        else OpenChat();
    }

    #region Cat Sprite Loading

    private void LoadCatSprite()
    {
        var sprite = Resources.Load<Sprite>("CatAvatar");
        if (sprite == null)
            Debug.LogWarning("[CharacterManager] CatAvatar sprite not found in Resources!");
    }

    private static Sprite CreateCircleSprite()
    {
        int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.5f - 1;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float a = Mathf.Clamp01(1f - dist / radius);
                a = a * a * (3f - 2f * a); // smoothstep
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    #endregion

    #region Overlay Canvas (持久化UI)

    private void CreateOverlayCanvas()
    {
        // 持久化Canvas — 始终在最上层显示角色头像+对话
        var canvasObj = new GameObject("[CharOverlayCanvas]");
        DontDestroyOnLoad(canvasObj);
        overlayCanvas = canvasObj.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 999; // 最高层
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // 角色头像按钮 — 增大一倍
        avatarButton = new GameObject("CharAvatar");
        avatarButton.transform.SetParent(canvasObj.transform, false);
        avatarRect = avatarButton.AddComponent<RectTransform>();
        avatarRect.anchorMin = avatarRect.anchorMax = new Vector2(0.5f, 0.5f);
        avatarRect.pivot = new Vector2(0.5f, 0.5f);
        avatarRect.sizeDelta = new Vector2(90, 120);

        // 无背景框 — 透明Image仅用于点击检测
        var bgImg = avatarButton.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0);
        bgImg.raycastTarget = true;

        // 阴影 — 椭圆形半透明阴影，位于猫咪下方
        var shadowObj = new GameObject("CatShadow");
        shadowObj.transform.SetParent(avatarButton.transform, false);
        shadowRect = shadowObj.AddComponent<RectTransform>();
        shadowRect.anchorMin = shadowRect.anchorMax = new Vector2(0.5f, 0f);
        shadowRect.pivot = new Vector2(0.5f, 0.5f);
        shadowRect.sizeDelta = new Vector2(60, 12);
        shadowRect.anchoredPosition = new Vector2(0, 3f);
        shadowImg = shadowObj.AddComponent<Image>();
        // 生成圆形阴影纹理
        shadowImg.sprite = CreateCircleSprite();
        shadowImg.color = new Color(0.1f, 0.1f, 0.15f, 0.25f);
        shadowImg.raycastTarget = false;

        // 猫咪图片
        var catImgObj = new GameObject("CatImg");
        catImgObj.transform.SetParent(avatarButton.transform, false);
        avatarImgRect = catImgObj.AddComponent<RectTransform>();
        avatarImgRect.anchorMin = avatarImgRect.anchorMax = new Vector2(0.5f, 0.5f);
        avatarImgRect.pivot = new Vector2(0.5f, 0.5f);
        avatarImgRect.sizeDelta = new Vector2(90, 110);
        avatarImgRect.anchoredPosition = new Vector2(0, 10f);
        avatarImg = catImgObj.AddComponent<Image>();
        var catSprite = Resources.Load<Sprite>("CatAvatar");
        if (catSprite != null) avatarImg.sprite = catSprite;
        avatarImg.preserveAspect = true;
        avatarImg.raycastTarget = false;

        // 角色名标签
        personaLabel = new GameObject("PersonaLabel");
        personaLabel.transform.SetParent(avatarButton.transform, false);
        personaLabel.AddComponent<RectTransform>();
        var plR = personaLabel.GetComponent<RectTransform>();
        plR.anchorMin = new Vector2(0, 0);
        plR.anchorMax = new Vector2(1f, 0.25f);
        plR.offsetMin = new Vector2(4, 2);
        plR.offsetMax = new Vector2(-4, -2);
        var plTxt = personaLabel.AddComponent<Text>();
        plTxt.font = Fnt;
        if (plTxt.font == null) plTxt.font = Font.CreateDynamicFontFromOSFont("Arial", 12);
        plTxt.text = "守艺人"; plTxt.fontSize = 14;
        plTxt.color = ZhuRed; plTxt.alignment = TextAnchor.MiddleCenter;

        // 点击按钮 — 打开对话（仅在未拖动时触发）
        var btn = avatarButton.AddComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            if (hasDragged) return; // 拖动后不触发点击
            if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
            ToggleChat();
        });

        // 拖动支持 — EventTrigger
        var trigger = avatarButton.AddComponent<EventTrigger>();
        var beginEntry = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
        beginEntry.callback.AddListener((d) => OnAvatarBeginDrag((PointerEventData)d));
        trigger.triggers.Add(beginEntry);

        var dragEntry = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
        dragEntry.callback.AddListener((d) => OnAvatarDrag((PointerEventData)d));
        trigger.triggers.Add(dragEntry);

        var endEntry = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
        endEntry.callback.AddListener((d) => OnAvatarEndDrag((PointerEventData)d));
        trigger.triggers.Add(endEntry);

        // 设置初始位置
        SetAvatarInitialPosition();

        // AI对话UI
        var chatUIObj = new GameObject("[AIChatUI]");
        chatUIObj.transform.SetParent(canvasObj.transform, false);
        chatUIObj.AddComponent<RectTransform>();
        chatUI = chatUIObj.AddComponent<AIChatUI>();
    }

    #region Avatar Drag & Avoidance

    private void SetAvatarInitialPosition()
    {
        // 恢复保存的位置
        if (PlayerPrefs.HasKey("CharAvatarX") && PlayerPrefs.HasKey("CharAvatarY"))
        {
            var saved = new Vector2(PlayerPrefs.GetFloat("CharAvatarX"), PlayerPrefs.GetFloat("CharAvatarY"));
            avatarRect.anchoredPosition = saved;
            ClampAvatarToLeftRegion();
            return;
        }

        // 默认位置：左侧中间
        var canvasRect = overlayCanvas.transform as RectTransform;
        float leftX = -canvasRect.sizeDelta.x * 0.25f;
        avatarRect.anchoredPosition = new Vector2(leftX, 0);
        ClampAvatarToLeftRegion();
    }

    private void OnAvatarBeginDrag(PointerEventData data)
    {
        isDragging = true;
        hasDragged = false;
        dragStartPos = data.position;
    }

    private void OnAvatarDrag(PointerEventData data)
    {
        if (!isDragging) return;

        // 判断是否超过拖动阈值
        if (!hasDragged && Vector2.Distance(data.position, dragStartPos) > DragThreshold)
            hasDragged = true;

        if (!hasDragged) return;

        // 将屏幕坐标转换为 canvas 内的 anchoredPosition
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            overlayCanvas.transform as RectTransform,
            data.position,
            data.pressEventCamera,
            out Vector2 localPos);

        avatarRect.anchoredPosition = localPos;
        ClampAvatarToScreen();
    }

    private void OnAvatarEndDrag(PointerEventData data)
    {
        if (!isDragging) return;
        isDragging = false;

        if (hasDragged)
        {
            // 保存位置
            PlayerPrefs.SetFloat("CharAvatarX", avatarRect.anchoredPosition.x);
            PlayerPrefs.SetFloat("CharAvatarY", avatarRect.anchoredPosition.y);
            PlayerPrefs.Save();
        }

        // 延迟重置 hasDragged，确保 Button.onClick 能读到正确值
        StartCoroutine(ResetDragFlagNextFrame());
    }

    private IEnumerator ResetDragFlagNextFrame()
    {
        yield return null;
        hasDragged = false;
    }

    private void ClampAvatarToLeftRegion()
    {
        var canvasRect = overlayCanvas.transform as RectTransform;
        if (canvasRect == null) return;

        float halfW = avatarRect.sizeDelta.x * 0.5f;
        float halfH = avatarRect.sizeDelta.y * 0.5f;
        float canvasW = canvasRect.sizeDelta.x * 0.5f;
        float canvasH = canvasRect.sizeDelta.y * 0.5f;

        // 限制在屏幕范围内（允许拖到任意位置）
        float maxX = canvasW - halfW - 5f;
        float minX = -canvasW + halfW + 5f;
        float maxY = canvasH - halfH - 5f;
        float minY = -canvasH + halfH + 5f;

        Vector2 pos = avatarRect.anchoredPosition;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        avatarRect.anchoredPosition = pos;
    }

    private void ClampAvatarToScreen()
    {
        ClampAvatarToLeftRegion();
    }

    /// <summary>
    /// 检查头像是否与其他 Canvas 中的 UI 元素重叠
    /// </summary>
    private bool IsOverlappingUI(RectTransform checkRect)
    {
        var otherRects = GetOtherUIRects();
        var myCorners = GetWorldCorners(checkRect);

        foreach (var otherRect in otherRects)
        {
            if (RectsOverlap(myCorners, GetWorldCorners(otherRect)))
                return true;
        }
        return false;
    }

    /// <summary>
    /// 寻找不与 UI 重叠的最近位置
    /// </summary>
    private void FindNonOverlappingPosition(RectTransform rect)
    {
        Vector2 originalPos = rect.anchoredPosition;
        // Try spiral outward offsets
        Vector2[] offsets = {
            new Vector2(0, 0),
            new Vector2(-150, 0), new Vector2(150, 0),
            new Vector2(0, 150), new Vector2(0, -150),
            new Vector2(-200, 150), new Vector2(-200, -150),
            new Vector2(150, 150), new Vector2(150, -150),
            new Vector2(-300, 0), new Vector2(0, 250), new Vector2(0, -250),
            new Vector2(-300, 200), new Vector2(-300, -200),
        };

        foreach (var offset in offsets)
        {
            rect.anchoredPosition = originalPos + offset;
            ClampAvatarToLeftRegion();
            if (!IsOverlappingUI(rect))
                return;
        }

        // Try left edge positions
        var canvasRect = overlayCanvas.transform as RectTransform;
        float cw = -canvasRect.sizeDelta.x * 0.35f;
        float ch = canvasRect.sizeDelta.y * 0.5f - 60f;
        Vector2[] edgePositions = {
            new Vector2(cw, ch * 0.7f),
            new Vector2(cw, 0),
            new Vector2(cw, -ch * 0.7f),
        };

        foreach (var pos in edgePositions)
        {
            rect.anchoredPosition = pos;
            ClampAvatarToLeftRegion();
            if (!IsOverlappingUI(rect))
                return;
        }

        rect.anchoredPosition = originalPos;
    }

    /// <summary>
    /// 获取当前场景中所有其他 Canvas（非 overlayCanvas）下的 RaycastTarget Graphic
    /// </summary>
    private List<RectTransform> GetOtherUIRects()
    {
        var result = new List<RectTransform>();
        var allGraphics = Object.FindObjectsOfType<Graphic>();

        foreach (var g in allGraphics)
        {
            if (g == null || !g.raycastTarget || !g.gameObject.activeInHierarchy) continue;
            // 跳过自己的头像区域
            if (g.transform == avatarRect || g.transform.IsChildOf(avatarRect)) continue;
            // 跳过 overlay canvas 下的元素（对话面板等）
            var gCanvas = g.GetComponentInParent<Canvas>();
            if (gCanvas == overlayCanvas) continue;
            // 跳过 AIChatUI（属于 overlay canvas）
            if (g.transform.IsChildOf(overlayCanvas.transform)) continue;

            var rt = g.rectTransform;
            if (rt != null && rt.sizeDelta.magnitude > 1f)
                result.Add(rt);
        }

        return result;
    }

    private static Vector3[] GetWorldCorners(RectTransform rt)
    {
        var corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        return corners;
    }

    private static bool RectsOverlap(Vector3[] a, Vector3[] b)
    {
        // AABB check in world space
        float aMinX = Mathf.Min(a[0].x, a[2].x), aMaxX = Mathf.Max(a[0].x, a[2].x);
        float aMinY = Mathf.Min(a[0].y, a[2].y), aMaxY = Mathf.Max(a[0].y, a[2].y);
        float bMinX = Mathf.Min(b[0].x, b[2].x), bMaxX = Mathf.Max(b[0].x, b[2].x);
        float bMinY = Mathf.Min(b[0].y, b[2].y), bMaxY = Mathf.Max(b[0].y, b[2].y);

        return aMinX < bMaxX && aMaxX > bMinX && aMinY < bMaxY && aMaxY > bMinY;
    }

    #endregion

    private void UpdatePersonaLabel()
    {
        if (personaLabel == null) return;
        var persona = AIChatManager.Instance?.CurrentPersona;
        if (persona != null)
        {
            var txt = personaLabel.GetComponent<Text>();
            if (txt != null)
            {
                txt.text = string.IsNullOrEmpty(persona.category)
                    ? persona.name
                    : $"守艺人·{persona.name}";
            }
        }
    }

    private void EnsureAIChatManager()
    {
        // 确保AIChatManager存在
        if (AIChatManager.Instance == null)
        {
            var go = new GameObject("[AIChatManager]");
            go.AddComponent<AIChatManager>();
        }
    }

    #endregion

    private void OnDestroy()
    {
    }
}
