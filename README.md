# 了不起的非遗 — 中国非物质文化遗产展示系统

## 一、项目概述

本项目是一个基于 Unity 引擎的中国非物质文化遗产交互展示系统，用户可以浏览瓷器、剪纸、书法、民族乐器四大品类，查看 3D 展品模型、阅读文化详情、收藏感兴趣的展品。

| 项目信息 | 详情 |
|---------|------|
| 项目名称 | 了不起的非遗 |
| 引擎版本 | Unity 2022.3 / Tuanjie 1.9.1 |
| 编程语言 | C# |
| 渲染管线 | 内置管线（Built-in） |
| UI 框架 | UGUI（运行时代码创建，无 Prefab） |
| 设计风格 | 新中式（宣纸米白 + 赭石棕 + 中国红 + 墨黑） |

---

## 二、项目结构

```
Assets/
├── Editor/
│   └── PlayFromLogin.cs          # 编辑器脚本：点Play自动从登录页启动
├── Scenes/
│   ├── LoginScene.unity           # 场景0：登录注册
│   ├── StartScene.unity           # 场景1：欢迎页
│   ├── MainScene.unity            # 场景2：品类选择主页
│   ├── ExhibitScene.unity         # 场景3：展品3D展示
│   └── SettingsScene.unity        # 场景4：设置（未使用）
├── Scripts/
│   ├── Core/                      # 核心基础设施
│   │   ├── GameManager.cs         # 全局状态管理（单例）
│   │   ├── SceneLoader.cs         # 场景异步加载（单例）
│   │   ├── SceneNames.cs          # 场景名常量
│   │   ├── DataHelper.cs          # JSON文件读写工具
│   │   └── UIFont.cs              # 字体缓存加载
│   ├── Data/                      # 数据模型
│   │   ├── ExhibitData.cs         # 展品数据结构
│   │   └── UserData.cs            # 用户数据结构
│   ├── UI/
│   │   └── UIFrame.cs             # UI基类（所有场景管理器的父类）
│   ├── Login/
│   │   └── LoginManager.cs        # 登录场景管理器
│   ├── Start/
│   │   └── StartPanel.cs          # 欢迎页管理器
│   ├── Main/
│   │   ├── MainPanel.cs           # 主页管理器（卷轴画卷+背包）
│   │   └── CategoryCard.cs        # 品类卡片组件（未使用）
│   ├── Exhibit/
│   │   ├── ExhibitManager.cs      # 展品场景管理器（3D模型+抽屉面板）
│   │   ├── ModelRotator.cs        # 3D模型旋转缩放交互
│   │   └── DetailPanel.cs         # 旧版侧边详情面板（未使用）
│   ├── Backpack/
│   │   └── BackpackManager.cs     # 背包/收藏管理器
│   └── Help/
│       └── HelpManager.cs         # 帮助内容提供者
└── StreamingAssets/
    └── Exhibits.json              # 展品数据（9件展品）
```

---

## 三、核心架构设计

### 3.1 整体架构：UIFrame 继承体系

```
UIFrame : MonoBehaviour（抽象基类）
    ├── LoginManager   （登录场景）
    ├── StartPanel     （欢迎场景）
    ├── MainPanel      （主页场景）
    └── ExhibitManager（展品场景）
```

**为什么用 UIFrame 基类？**

所有场景的 UI 都是代码动态创建的，有大量公共逻辑：
- Canvas + EventSystem 初始化
- 新中式风格的 UI 组件（印章图标、分隔线、弹窗等）
- 动画效果（淡入淡出、脉冲、提示条）

把这些公共方法抽到基类，每个场景管理器只需实现自己的 `CreateUI()` 方法。

### 3.2 UIFrame 基类提供的能力

| 方法 | 作用 |
|------|------|
| `InitCanvas()` | 创建 Canvas + EventSystem + 根面板，返回根节点 |
| `NewUI()` / `Stretch()` | 创建空 UI 对象 / 拉伸填满父节点 |
| `AnchorTop()` / `AnchorBottom()` | 创建顶部/底部固定条 |
| `AddLabel()` / `AddBtn()` / `AddInputField()` | 文字/按钮/输入框 |
| `AddSealLogo()` / `AddSealIcon()` | 红色印章装饰 |
| `AddDivider()` | "─── 文字 ───" 风格分隔线 |
| `MakeOverlay()` | 模态弹窗 |
| `FadeIn()` / `FadeOut()` / `ShowToast()` | 动画效果 |
| `AddInkWashCorners()` | 水墨晕染四角 |
| `EnsureSingletons()` | 确保 GameManager + SceneLoader 存在 |
| `Font()` | 返回缓存字体 |

### 3.3 单例模式

项目中有两个全局单例：

```
GameManager : MonoBehaviour（DontDestroyOnLoad）
    → 管理用户登录状态、展品数据、系统设置
    → 所有场景通过 GameManager.Instance 访问

SceneLoader : MonoBehaviour（DontDestroyOnLoad）
    → 异步场景切换
    → 所有场景跳转通过 SceneLoader.Instance.LoadScene() 调用
```

**自动创建机制**：`UIFrame.EnsureSingletons()` 在每个场景的 `Start()` 中调用，如果单例不存在会自动创建，不会重复。

---

## 四、场景流程

```
┌─────────────┐    登录/注册成功    ┌─────────────┐
│  LoginScene  │ ─────────────────→ │  StartScene  │
│  登录注册页   │                    │  欢迎启动页   │
└─────────────┘                    └──────┬──────┘
                                          │ "开始探索"
                                          ▼
                                   ┌──────────────┐
                          ┌─────── │  MainScene     │ ───────┐
                          │        │  品类选择主页   │         │
                          │        └──────────────┘         │
                          │  点击品类卡片                     │ 背包/设置/帮助
                          ▼                                  │
                   ┌──────────────┐                         │
                   │ ExhibitScene  │ ←──── 返回 ─────────────┘
                   │ 展品3D展示页   │
                   └──────────────┘
```

**场景间数据传递**：通过 `PlayerPrefs` 保存当前选择的品类：
```csharp
// MainPanel 点击卡片时
PlayerPrefs.SetString("CurrentCategory", "瓷器");

// ExhibitManager 读取时
string cat = PlayerPrefs.GetString("CurrentCategory", "瓷器");
```

---

## 五、各场景详细设计

### 5.1 LoginScene — 登录注册页

**脚本**：`LoginManager : UIFrame`

**UI 结构**：
```
Canvas (ScreenSpaceOverlay)
└── Root (全屏面板)
    ├── 背景（墨黑色）
    ├── 中央面板（宣纸色，圆角效果）
    │   ├── 印章 Logo "非遗"
    │   ├── 标题 "了不起的非遗"
    │   ├── 分隔线
    │   ├── 用户名输入框
    │   ├── 密码输入框
    │   ├── 登录按钮 / 注册按钮
    │   └── 提示消息文字
    └── 水墨晕染四角
```

**关键逻辑**：
- **注册**：用户名≥2字符，密码≥4字符，密码用 XOR(0x5A) + Base64 加密存入 `PlayerPrefs`
- **登录**：验证用户名是否存在，解密密码比对
- **跳转**：登录成功后 `FadeOut` 动画 → `SceneLoader.LoadScene(StartScene)`

**可能被问到的问题**：
> Q: 密码怎么加密的？
> A: 使用 XOR 异或加密（密钥 0x5A），然后 Base64 编码存储到 PlayerPrefs。这是简单的可逆加密，适合演示项目。

### 5.2 StartScene — 欢迎页

**脚本**：`StartPanel : UIFrame`

**UI 结构**：
```
Canvas (ScreenSpaceOverlay)
└── Root
    ├── 背景（墨黑色）
    ├── 大印章 Logo（"遗"）
    ├── 中英文标题
    ├── 分隔线
    ├── "开始探索" 按钮（带脉冲动画）
    ├── "使用引导" 按钮
    ├── 欢迎语
    └── 引导弹窗（6步使用指南）
```

### 5.3 MainScene — 品类选择主页

**脚本**：`MainPanel : UIFrame`

**UI 结构**：
```
Canvas (ScreenSpaceOverlay)
└── Root
    ├── 背景（宣纸米白 + 水墨晕染四角）
    ├── Header（印章小图标 + 标题 + 用户名）
    ├── ScrollView（横向滚动）
    │   └── Content (HorizontalLayoutGroup + ContentSizeFitter)
    │       ├── 卡片1：瓷器（绫布边框 + 宣纸内芯 + 印章 + 文字）
    │       ├── 卡片2：剪纸
    │       ├── 卡片3：书法
    │       └── 卡片4：民族乐器
    ├── NavBar（背包/设置/帮助/退出）
    ├── 背包弹窗（列表 + 删除按钮）
    ├── 设置弹窗
    └── 帮助弹窗
```

**关键设计**：
- **卷轴画卷式布局**：用 `ScrollRect` + `HorizontalLayoutGroup` + `ContentSizeFitter` 实现横向滚动
- **卡片结构**：外层绫布边框 + 内层宣纸白底（通过 margin 模拟装裱效果）
- **点击反馈**：放大 5% 弹回的 `CardClickFeedback()` 协程
- **Viewport 必须白色**：Mask 组件需要不透明的 Image 才能正常裁剪

**可能被问到的问题**：
> Q: ScrollView 怎么实现的？
> A: ScrollRect 控制滚动，Viewport 上加 Mask 实现裁剪，Content 用 HorizontalLayoutGroup 横向排列卡片，ContentSizeFitter 自动计算 Content 宽度。

### 5.4 ExhibitScene — 展品 3D 展示页

**脚本**：`ExhibitManager : UIFrame`

**UI 结构**：
```
Canvas (ScreenSpaceCamera, planeDistance=20)
└── Root
    ├── Header（返回 + 展品名 + 收藏按钮）
    ├── Quote（引用语浮层，anchor y=0.28）
    ├── Drawer（底部抽屉面板）
    │   ├── Handle 拉手柄（"▲ 详情 ▲" / "▼ 收起 ▼"）
    │   ├── TabBar（历史背景 / 制作工艺 / 文化寓意）
    │   └── DrawerContent（正文文字）
    └── Footer（上一个 / 收藏 / 下一个）
```

**3D 模型**：
```
Camera (backgroundColor=宣纸米白)
    └── Pedestal（展台底座：扁平圆柱）
    └── Exhibit_{id}（3D模型，缩放3x，z=5）
        └── ModelRotator（自动旋转+鼠标拖拽+滚轮缩放）
```

**3D 模型全部用 Unity 基础几何体拼接**：
| 模型 | 拼接方式 |
|------|---------|
| 青花瓷瓶 | 5个 Cylinder（瓶身+瓶颈+蓝纹+底座+花边） |
| 景德镇茶杯 | Cylinder + 2个 Sphere（杯身+杯盖+壶钮） |
| 窗花剪纸 | 2个 Quad（红色主体+米白衬底） |
| 书法卷轴 | Quad + 2个 Cylinder（画心+轴杆） |
| 编钟 | Cylinder 横梁 + 5个 Cylinder 钟体 + Cube 底座 |
| 古筝 | Cube 琴身 + 8个 Cube 琴弦 |
| 二胡 | Cylinder 琴杆 + Cylinder 琴筒 |

**抽屉面板设计**：
- **收起状态**：只显示拉手柄 + Tab栏，高 80px
- **展开状态**：抽屉上滑到屏幕 30% 高度，显示选中 Tab 的正文内容
- **动画**：0.3秒 SmoothStep 缓动
- **抽屉展开时自动隐藏** Quote 引用语，避免遮挡

**Canvas 渲染模式**：`ScreenSpaceCamera`，`planeDistance=20`
- Canvas 在 3D 模型（z=5）后面渲染，所以 3D 模型在 UI 前面可见
- 相机背景色为宣纸米白，不使用 Canvas 全屏背景

**可能被问到的问题**：
> Q: 3D 模型为什么能在 UI 前面显示？
> A: Canvas 使用 ScreenSpaceCamera 模式，planeDistance=20，意味着 Canvas 在距离相机 20 单位处渲染。而 3D 模型在 z=5，距离相机更近，所以渲染在 Canvas 前面。

> Q: 为什么不用 Overlay 模式？
> A: ScreenSpaceOverlay 会覆盖所有 3D 内容，无法看到模型。

> Q: 抽屉怎么实现的？
> A: 抽屉面板锚定在屏幕底部（anchorMin/Max 的 y=0），通过修改 sizeDelta.y 控制高度，用 SmoothStep 插值实现平滑动画。

---

## 六、数据层设计

### 6.1 展品数据

**文件**：`Assets/StreamingAssets/Exhibits.json`

**数据结构**：
```csharp
[Serializable]
public class ExhibitData {
    public string id;          // 如 "porcelain_01"
    public string name;        // 如 "青花瓷瓶"
    public string category;    // 如 "瓷器"
    public string description; // 简短描述
    public string history;    // 历史背景
    public string craft;      // 制作工艺
    public string meaning;    // 文化寓意
    public string modelType;  // 3D模型类型（vase/cup/papercut等）
    public string imageName;  // 图片名（预留）
}
```

**加载流程**：
```
GameManager.Awake()
    → DataHelper.LoadFromStreamingAssets<ExhibitDataList>("Exhibits.json")
    → JsonUtility.FromJson() 反序列化
    → 存入 exhibitDict（按 id 索引）
```

### 6.2 用户数据

**存储方式**：`PlayerPrefs`（本地键值对）

| Key | 内容 | 示例 |
|-----|------|------|
| `User_{用户名}_Password` | XOR加密+Base64的密码 |  |
| `LastUser` | 上次登录的用户名 | "admin" |
| `LoginTime` | 上次登录时间 | "2026-06-08" |
| `CurrentCategory` | 当前选择的品类 | "瓷器" |
| `Backpack_{用户名}` | 收藏的展品ID列表（逗号分隔）| "porcelain_01,instrument_01" |
| `Volume` / `Brightness` | 系统设置 | 0.8 |

### 6.3 背包系统

**脚本**：`BackpackManager`（纯 C# 单例，非 MonoBehaviour）

**双重持久化**：
- `PlayerPrefs`：快速读写（`Backpack_{username}` 键）
- JSON 文件：完整备份（`persistentDataPath/Backpack/{username}_backpack.json`）

**API**：
```csharp
BackpackManager.Instance.AddToBackpack(username, exhibitId);
BackpackManager.Instance.RemoveFromBackpack(username, exhibitId);
BackpackManager.Instance.IsInBackpack(username, exhibitId);
BackpackManager.Instance.GetBackpackItems(username);
```

---

## 七、关键技术点总结

### 7.1 运行时 UI 创建

**为什么不用 Prefab？**

本项目选择纯代码创建 UI，原因：
1. 所有场景共享 `UIFrame` 基类的方法，代码复用率高
2. 新中式风格组件（印章、分隔线、水墨晕染）可统一管理
3. 修改样式只需改基类常量，无需逐个修改 Prefab

**创建流程**（以按钮为例）：
```csharp
// 1. 创建空 GameObject
var btn = NewUI("MyBtn", parent);
// 2. 设置 RectTransform 位置
var r = btn.GetComponent<RectTransform>();
r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
r.sizeDelta = new Vector2(120, 40);
// 3. 添加视觉组件
btn.AddComponent<Image>().color = OchreBrown;
// 4. 添加交互组件
btn.AddComponent<Button>().onClick.AddListener(OnClick);
// 5. 添加文字子对象
var t = NewUI("T", btn.transform); Stretch(t);
var txt = t.AddComponent<Text>();
txt.text = "按钮"; txt.color = Color.white;
```

### 7.2 3D 模型程序化生成

不使用外部 3D 模型文件，全部用 Unity 基础几何体（Cylinder、Sphere、Cube、Quad）拼接：

```csharp
private GameObject AddPart(PrimitiveType pt, GameObject parent,
    Vector3 scale, Vector3 pos, Color color)
{
    var obj = GameObject.CreatePrimitive(pt);
    obj.transform.SetParent(parent.transform, false);
    obj.transform.localScale = scale;
    obj.transform.localPosition = pos;
    obj.GetComponent<Renderer>().material.color = color;
    return obj;
}
```

### 7.3 坑与经验

| 问题 | 原因 | 解决方案 |
|------|------|---------|
| 按钮点击无响应 | 装饰性 Image 的 `raycastTarget=true` 拦截了点击 | 所有非交互 Image 设 `raycastTarget=false` |
| ScrollView 不裁剪 | Viewport 的 Image 全透明导致 Mask 失效 | Viewport Image 设白色不透明，`showMaskGraphic=false` |
| ScrollView 白色隙缝 | Content 的 Image 是白色 | Content Image 设 `(0,0,0,0)` + `raycastTarget=false` |
| 3D 模型被 Canvas 遮挡 | ScreenSpaceOverlay 完全覆盖 3D | 改用 ScreenSpaceCamera + planeDistance=20 |
| 同一 GameObject 两个 Graphic | `Text` + `Image` 不能共存 | 拆为父子对象：父对象 `Image`，子对象 `Text` |
| 点 Play 进错场景 | Unity 从当前活动场景运行 | 添加 `PlayFromLogin.cs` 编辑器脚本自动切换 |

---

## 八、运行说明

1. 用 Unity 打开项目
2. 打开任意场景（推荐 LoginScene）
3. 点击 Play 运行
4. 注册新用户 → 登录 → 开始探索 → 选择品类 → 查看 3D 展品

**测试账号**：任意用户名（≥2字符）+ 任意密码（≥4字符）先注册再登录

---

## 九、技术栈

| 类别 | 技术 |
|------|------|
| 游戏引擎 | Unity 2022.3 |
| 编程语言 | C# |
| UI 系统 | UGUI（运行时代码创建） |
| 数据格式 | JSON（JsonUtility 反序列化） |
| 本地存储 | PlayerPrefs |
| 3D 渲染 | 程序化几何体拼接 |
| 架构模式 | 单例 + 继承（UIFrame 基类） |
