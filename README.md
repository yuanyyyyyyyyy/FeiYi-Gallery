# FeiYi Gallery — 中国非物质文化遗产交互展示系统

## 一、项目概述

本项目是一个基于 Unity 引擎的中国非物质文化遗产交互展示系统，用户可以浏览瓷器、剪纸、书法、民族乐器、刺绣、茶艺、皮影戏、扎染蜡染八大品类，查看 3D 展品模型、阅读文化详情、收藏感兴趣的展品、学习文化知识、了解历史故事，并参与知识答题。

| 项目信息 | 详情 |
|---------|------|
| 项目名称 | FeiYi Gallery（了不起的非遗） |
| 引擎版本 | Unity 2022.3 / Tuanjie 1.9.1 |
| 编程语言 | C# |
| 渲染管线 | 内置管线（Built-in） |
| UI 框架 | UGUI（运行时代码创建，无 Prefab） |
| 设计风格 | 新中式（宣纸米白 + 赭石棕 + 中国红 + 墨黑），支持3种主题切换 |
| AI 对话 | 接入大语言模型 API，9种角色人设随品类自动切换，支持会话历史管理 |
| 音频系统 | 五声音阶BGM + 4种SFX |

---

## 二、项目结构

```
Assets/
├── Editor/
│   ├── PlayFromLogin.cs          # 编辑器脚本：点Play自动从登录页启动
│   ├── GameViewAutoFit.cs         # Game View缩放自动修正（0.3x~1.3x自由滑动）
│   └── FilterRawInputNoise.cs    # Console自动过滤Raw Input噪音日志
├── Scenes/
│   ├── LoginScene.scene           # 场景0：登录注册
│   ├── StartScene.scene           # 场景1：欢迎页
│   ├── MainScene.scene            # 场景2：品类选择主页
│   ├── ExhibitScene.scene         # 场景3：展品3D展示
│   ├── KnowledgeScene.scene       # 场景4：知识探索 + 答题
│   └── EventScene.scene           # 场景5：历史故事
├── Scripts/
│   ├── Core/                      # 核心基础设施
│   │   ├── GameManager.cs         # 全局状态管理（单例）
│   │   ├── SceneLoader.cs         # 场景异步加载（单例）
│   │   ├── SceneNames.cs          # 场景名常量
│   │   ├── DataHelper.cs          # JSON文件读写工具
│   │   ├── UIFont.cs              # 字体缓存加载
│   │   └── AudioManager.cs        # 音频管理器（BGM+SFX）
│   ├── Data/                      # 数据模型
│   │   ├── ExhibitData.cs         # 展品数据结构
│   │   ├── KnowledgeData.cs       # 知识数据结构 + 答题数据结构
│   │   └── EventData.cs           # 事件数据结构
│   ├── UI/
│   │   └── UIFrame.cs             # UI基类（所有场景管理器的父类，含主题系统+确认对话框）
│   ├── Login/
│   │   └── LoginManager.cs        # 登录场景管理器（含记住密码+自动填充）
│   ├── Start/
│   │   └── StartPanel.cs          # 欢迎页管理器（含首次使用自动引导）
│   ├── Main/
│   │   └── MainPanel.cs           # 主页管理器（卷轴画卷+背包+设置+3D角色+亮度调节+退出确认）
│   ├── Exhibit/
│   │   ├── ExhibitManager.cs      # 展品场景管理器（3D模型+抽屉面板+缩略图+点击交互）
│   │   └── ModelRotator.cs        # 3D模型旋转缩放+点击交互
│   ├── Knowledge/
│   │   └── KnowledgeManager.cs    # 知识场景管理器（浏览+答题）
│   ├── Event/
│   │   └── EventManager.cs        # 历史故事场景管理器
│   ├── Backpack/
│   │   └── BackpackManager.cs     # 背包/收藏管理器
│   ├── Character/
│   │   ├── CharacterController2D.cs # 3D角色控制器（4状态状态机：Idle/Walking/Jumping/Interacting）
│   │   └── CharacterState.cs      # 角色状态枚举
│   ├── AI/                        # AI对话系统
│   │   ├── CharacterManager.cs    # 跨场景猫咪头像单例（浮动动画+拖动+品类感知）
│   │   ├── AIChatManager.cs       # AI对话管理器（上下文+知识注入+角色切换+会话历史管理）
│   │   ├── AIChatUI.cs            # AI对话面板（消息气泡+打字机效果+考考我+新建会话+历史会话）
│   │   ├── AIChatClient.cs        # HTTP API客户端
│   │   ├── AIConfig.cs            # AI配置加载（API Key/URL/模型）
│   │   └── AIPersona.cs           # 9种角色人设定义（守艺人+8品类变身）
│   └── Help/
│       └── HelpManager.cs         # 帮助内容提供者（引导步骤+FAQ）
├── Resources/
│   └── CatAvatar.png              # 猫咪头像图片
└── StreamingAssets/
    ├── Exhibits.json              # 展品数据（17件展品）
    ├── Knowledge.json             # 文化知识（24篇）
    ├── Events.json                # 历史事件（23篇）
    ├── Quiz.json                  # 答题数据（24题）
    └── Audio/                     # 音频文件
        ├── bgm.wav                # 五声音阶BGM
        ├── click.wav              # 点击音效
        ├── flip.wav               # 翻页音效
        ├── collect.wav            # 收藏音效
        └── toast.wav              # 提示音效
```

---

## 三、核心架构设计

### 3.1 整体架构：UIFrame 继承体系

```
UIFrame : MonoBehaviour（抽象基类）
    ├── LoginManager      （登录场景）
    ├── StartPanel        （欢迎场景）
    ├── MainPanel         （主页场景）
    ├── ExhibitManager    （展品场景）
    ├── KnowledgeManager  （知识场景）
    └── EventManager      （历史故事场景）
```

**为什么用 UIFrame 基类？**

所有场景的 UI 都是代码动态创建的，有大量公共逻辑：
- Canvas + EventSystem 初始化
- 新中式风格的 UI 组件（印章图标、分隔线、弹窗等）
- 动画效果（淡入淡出、脉冲、提示条）
- 主题切换系统（3种主题色板）
- 确认对话框（`MakeConfirmDialog`）

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
| `MakeOverlay()` | 模态弹窗（仅 X 按钮关闭，ScrollRect 可滚动） |
| `MakeConfirmDialog()` | 确认对话框（标题+提示+确认/取消按钮） |
| `FadeIn()` / `FadeOut()` / `ShowToast()` | 动画效果 |
| `AddInkWashCorners()` | 水墨晕染四角 |
| `EnsureSingletons()` | 确保 GameManager + SceneLoader + AudioManager 存在 |
| `Font()` | 返回缓存字体 |
| `SfxClick/Flip/Collect/Toast()` | 音效快捷方法 |

### 3.3 主题系统

UIFrame 内置 3 种主题色板，通过 `GameManager.themeStyle` 切换：

| 主题 | style 值 | 特点 |
|------|---------|------|
| 默认 | `default` | 宣纸米白底 + 朱红 + 赭石棕 |
| 古典 | `classic` | 深褐底 + 暗朱红 + 金色文字 |
| 简约 | `minimal` | 浅灰白底 + 柔红 + 深灰文字 |

颜色属性（`ZhuRed`、`XuanPaper`、`InkBlack`、`GoldColor`、`JadeGreen`、`DarkBar`）为静态属性，根据当前主题动态返回对应色值，场景加载时自动生效。

### 3.4 单例模式

项目中有三个全局单例：

```
GameManager : MonoBehaviour（DontDestroyOnLoad）
    → 管理用户登录状态、展品/知识/问答/事件数据、系统设置、主题
    → 所有场景通过 GameManager.Instance 访问

SceneLoader : MonoBehaviour（DontDestroyOnLoad）
    → 异步场景切换
    → 所有场景跳转通过 SceneLoader.Instance.LoadScene() 调用

AudioManager : MonoBehaviour（DontDestroyOnLoad）
    → 五声音阶BGM循环播放 + 4种SFX
    → 音量实时调节
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
                    ┌───────────── │  MainScene     │ ────────────┐
                    │              │  品类选择主页   │              │
                    │              └──────────────┘              │
                    │  点击品类卡片            ┌────────────────┐ │
                    ▼                         │ 背包/设置/帮助  │ │
             ┌──────────────┐                 └────────────────┘ │
             │ ExhibitScene  │ ←──── 返回 ────────────────────────┘
             │ 展品3D展示页   │
             └──────────────┘

             ┌────────────────┐  ┌────────────────┐
             │KnowledgeScene  │  │  EventScene     │
             │ 知识探索+答题   │  │  历史故事       │
             └────────────────┘  └────────────────┘
               ↑ 点击"知识探索"    ↑ 点击"历史故事"
               └────────── 均从 MainScene 进入 ──────────┘
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
    │   ├── 记住密码 Toggle（勾选后保存加密密码，下次自动填充）
    │   ├── 登录按钮 / 注册按钮
    │   └── 提示消息文字
    └── 水墨晕染四角
```

**关键逻辑**：
- **注册**：用户名≥2字符，密码≥4字符，密码用 XOR(0x5A) + Base64 加密存入 `PlayerPrefs`
- **登录**：验证用户名是否存在，解密密码比对
- **记住密码**：勾选后保存加密密码到 `PlayerPrefs`，下次启动自动填充用户名和密码
- **跳转**：登录成功后 `FadeOut` 动画 → `SceneLoader.LoadScene(StartScene)`

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
    └── 引导弹窗（8步使用指南，含AI对话与设置说明）
```

**首次使用引导**：检测 `PlayerPrefs.HasKey("HasSeenGuide")`，首次启动自动弹出引导弹窗（8步：选择品类→浏览展品→3D交互→查看详情→问守艺人→收藏展品→管理背包→设置与个性化），后续可手动点击"使用引导"查看。

### 5.3 MainScene — 品类选择主页

**脚本**：`MainPanel : UIFrame`

**UI 结构**：
```
Canvas (ScreenSpaceOverlay)
└── Root
    ├── 背景（宣纸米白 + 水墨晕染四角）
    ├── 亮度遮罩层（半透明黑色叠加，拖动亮度滑块时实时调整）
    ├── Header（印章小图标 + 标题 + 头像 + 用户名）
    ├── ScrollView（横向滚动，8张品类卡片）
    │   └── Content (HorizontalLayoutGroup + ContentSizeFitter)
    │       ├── 卡片1：瓷器
    │       ├── 卡片2：剪纸
    │       ├── 卡片3：书法
    │       ├── 卡片4：民族乐器
    │       ├── 卡片5：刺绣
    │       ├── 卡片6：茶艺
    │       ├── 卡片7：皮影戏
    │       └── 卡片8：扎染蜡染
    ├── 3D角色展示区（RenderTexture + 正交相机 + 自动走动小人，点击触发跳跃）
    ├── 功能入口（知识探索 + 历史故事）
    ├── NavBar（背包/设置/帮助/退出，竹简风格）
    ├── 设置弹窗（Tab分页：音量亮度/主题/头像/密码/AI设置，每页独立滚动）
    ├── 帮助弹窗（Tab分页：使用引导/常见问题/关于系统，每页独立滚动）
    ├── 背包弹窗（560×680大面板，收藏列表 + 搜索 + 品类筛选 + 滚轮滚动 + 删除）
    └── 退出确认对话框（确认/取消，Toast 提示"已安全退出"）
```

**关键设计**：
- **卷轴画卷式布局**：`ScrollRect` + `HorizontalLayoutGroup` + `ContentSizeFitter`
- **3D角色**：程序化几何体拼接的低多边形小人，4种状态（Idle/Walking/Jumping/Interacting），自动走动+点击跳跃
- **设置面板**：5个 Tab 分页（音量亮度/主题/头像/密码/AI设置），每页独立 ScrollRect 滚动
- **亮度调节**：拖动滑块调整半透明黑色遮罩层的 alpha（0~0.55），视觉上实现屏幕变暗/变亮
- **AI测试连接**：持久进度条浮层，模拟连接进度，成功/失败视觉反馈
- **退出确认**：点击退出弹出 `MakeConfirmDialog` 确认对话框，确认后 `ShowToast("已安全退出")`
- **帮助弹窗**：Tab 分页（使用引导8步/常见问题8条/关于系统），引导步骤带编号印章卡片，FAQ带问号印章，关于系统展示版本信息

### 5.4 ExhibitScene — 展品 3D 展示页

**脚本**：`ExhibitManager : UIFrame`

**UI 结构**：
```
Canvas (ScreenSpaceCamera, planeDistance=20)
└── Root
    ├── Header（返回 + 展品名 + 收藏按钮）
    ├── Quote（引用语浮层）
    ├── Drawer（底部抽屉面板，点击3D模型自动展开）
    │   ├── Handle 拉手柄
    │   ├── TabBar（历史背景 / 制作工艺 / 文化寓意）
    │   ├── 缩略图（3D模型RenderTexture预览）
    │   └── TextScroll（正文文字，ScrollRect可滚动）
    └── Footer（上一个 / 分享 / 收藏 / 下一个）
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
| 苏绣双面绣 | Quad 绣布 + 4个 Cube 绣架 |
| 蜀绣圆框 | Quad 绣布 + Cylinder 圆框 + 2个 Cube 支架 |
| 紫砂壶 | Sphere 壶身 + Sphere 壶盖 + Cylinder 壶嘴 + Cylinder 壶把 |
| 建盏兔毫盏 | Cylinder 碗身 + Cylinder 釉面 |
| 关公皮影 | Quad 幕布 + Quad 人物 + Cylinder 操纵杆 |
| 孙悟空皮影 | Quad 幕布 + Quad 人物 + 2个 Cylinder 操纵杆 |
| 白族扎染布 | Quad 蓝底 + 3个 Quad 白花 |
| 苗族蜡染布 | Quad 蓝底 + 3个 Quad 白纹 |

**3D 模型交互**：
- **拖拽旋转**：鼠标左键拖拽旋转模型
- **滚轮缩放**：鼠标滚轮缩放模型大小
- **点击交互**：短距离点击（<8px位移）触发弹跳动画，自动展开详情抽屉
- **自动旋转**：模型默认缓慢自转，拖拽时暂停
- **缩略图系统**：抽屉面板顶部展示 3D 模型的 RenderTexture 缩略图预览

### 5.5 KnowledgeScene — 知识探索 + 答题

**脚本**：`KnowledgeManager : UIFrame`

**3 种视图**：
1. **CategorySelect** — 8个品类卡片选择（4列2行网格）
2. **KnowledgeBrowse** — 24篇文化知识卡片浏览（上一个/下一个）
3. **Quiz** — 4选1答题模式（24题），答题后评分+评级

### 5.6 EventScene — 历史故事

**脚本**：`EventManager : UIFrame`

**3 种视图**：
1. **CategorySelect** — 8个品类卡片选择（4列2行网格）
2. **EventList** — 编号列表展示该品类下的历史故事（ScrollRect 可滚动）
3. **EventDetail** — 故事详情页（印章+标题+时代+轮播卡片，上一个/下一个）

---

## 六、数据层设计

### 6.1 展品数据

**文件**：`Assets/StreamingAssets/Exhibits.json`（17件展品）

**数据结构**：
```csharp
[Serializable]
public class ExhibitData {
    public string id;          // 如 "porcelain_01"
    public string name;        // 如 "青花瓷瓶"
    public string category;    // 如 "瓷器"
    public string description; // 简短描述
    public string history;     // 历史背景
    public string craft;       // 制作工艺
    public string meaning;     // 文化寓意
    public string modelType;   // 3D模型类型（vase/cup/papercut等）
    public string imageName;   // 图片名（预留）
}
```

### 6.2 知识 + 答题数据

**文件**：`Assets/StreamingAssets/Knowledge.json`（24篇）、`Quiz.json`（24题）

### 6.3 事件数据

**文件**：`Assets/StreamingAssets/Events.json`（23篇）

### 6.4 用户数据

**存储方式**：`PlayerPrefs`（本地键值对）

| Key | 内容 | 示例 |
|-----|------|------|
| `User_{用户名}_Password` | XOR加密+Base64的密码 | |
| `User_{用户名}_RememberPwd` | 记住的加密密码（勾选"记住密码"时保存） | |
| `User_{用户名}_Avatar` | 头像索引 (0-5) | 0 |
| `LastUser` | 上次登录的用户名 | "admin" |
| `LoginTime` | 上次登录时间 | "2026-06-10" |
| `CurrentCategory` | 当前选择的品类 | "瓷器" |
| `Backpack_{用户名}` | 收藏的展品ID列表 | "porcelain_01,instrument_01" |
| `Backpack/{用户名}_backpack.json` | 背包JSON备份文件 | persistentDataPath 下 |
| `Volume` | 音量设置 | 0.8 |
| `Brightness` | 亮度设置 | 1.0 |
| `ThemeStyle` | 主题风格 | "default"/"classic"/"minimal" |
| `HasSeenGuide` | 是否已看过新手引导 | 1 |
| `CharAvatarX` | 猫咪头像屏幕X坐标 | -200 |
| `CharAvatarY` | 猫咪头像屏幕Y坐标 | 0 |

### 6.5 背包系统

**脚本**：`BackpackManager`（纯 C# 单例，非 MonoBehaviour）

**双重持久化**：
- `PlayerPrefs`：快速读写
- JSON 文件：完整备份（`persistentDataPath/Backpack/{username}_backpack.json`）

**功能**：
- 按品类筛选（全部/瓷器/剪纸/书法/民族乐器/刺绣/茶艺/皮影戏/扎染蜡染）
- 搜索展品（实时过滤）
- 删除收藏
- 列表区域支持滚轮滚动 + 可见滚动条（ScrollRect + VerticalLayoutGroup + ContentSizeFitter）

---

## 七、关键技术点总结

### 7.1 运行时 UI 创建

本项目选择纯代码创建 UI，原因：
1. 所有场景共享 `UIFrame` 基类的方法，代码复用率高
2. 新中式风格组件（印章、分隔线、水墨晕染）可统一管理
3. 修改样式只需改基类常量/属性，无需逐个修改 Prefab
4. 主题切换只需修改色板，下次场景加载自动生效

### 7.2 3D 模型程序化生成

不使用外部 3D 模型文件，全部用 Unity 基础几何体拼接：

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

### 7.3 3D 角色系统

主页展示的低多边形小人，使用状态机控制：
- **Idle** — 站立等待（轻微上下浮动+手臂微摆）
- **Walking** — 走向随机目标位置（腿臂交替摆动）
- **Jumping** — 跳跃（抛物线Y轴位移+腿收起+手臂上举，0.6秒后回到Idle）
- **Interacting** — 点击品类卡片时触发挥手（1.2秒后回到Idle）

角色通过 `RenderTexture` + 正交相机渲染到 UI 上，自动在场景中走动。点击角色展示区可触发跳跃动画。

### 7.4 音频系统

- **BGM**：中国五声音阶（宫商角徵羽）程序化生成旋律 + 低音伴奏，12秒循环
- **SFX**：4种（click/flip/collect/toast）
- **音量控制**：设置面板滑块实时调节，AudioListener.volume 全局控制

### 7.5 亮度调节系统

设置面板中的亮度滑块控制一个全屏半透明黑色遮罩层的 alpha 值：
- 亮度 1.0（最亮）→ 遮罩 alpha = 0（完全透明）
- 亮度 0.3（最暗）→ 遮罩 alpha = 0.55（半透明黑色叠加）

遮罩层设 `raycastTarget=false` 不拦截点击，`SetAsLastSibling()` 始终在最顶层渲染。

### 7.6 AI 对话系统

基于大语言模型 API 的智能对话，猫咪头像随品类自动切换角色身份：

**角色人设（9种）**：
| 角色 | 品类 | 名称 | 说话风格 |
|------|------|------|---------|
| 守艺人 | 无（默认） | 守艺人 | 半文半白，儒雅亲切 |
| 老窑工 | 瓷器 | 老窑工 | 朴实自豪，老匠人口吻 |
| 剪纸匠 | 剪纸 | 剪纸匠 | 温柔细腻，民间智慧 |
| 书童 | 书法 | 书童 | 文雅谦逊，常引典故 |
| 知音 | 民族乐器 | 知音 | 洒脱超然，乐理比喻 |
| 绣娘 | 刺绣 | 绣娘 | 温婉细致，以针法喻万物 |
| 茶师 | 茶艺 | 茶师 | 淡泊从容，以茶性喻人道 |
| 影戏人 | 皮影戏 | 影戏人 | 说书人口吻，绘声绘色 |
| 染匠 | 扎染蜡染 | 染匠 | 质朴自然，以蓝白喻人生 |

**核心机制**：
- `CharacterManager`（DontDestroyOnLoad 单例）每帧检测 `CurrentCategory` 变化，自动切换角色人设并更新头像标签
- 猫咪头像使用 `Resources.Load<Sprite>("CatAvatar")` 加载图片，带浮动呼吸动画 + 阴影闪烁
- 头像可拖动到屏幕任意位置，坐标持久化到 `PlayerPrefs`
- 点击头像打开对话面板，角色切换时清空旧对话并显示新角色开场白
- `AIChatManager` 将角色描述、说话风格、品类知识注入 System Prompt，实现身份化对话
- 对话面板含打字机效果、考考我出题、思考中提示（动态状态文字+进度条+计时器）等交互
- **网络重试**：首次请求失败自动重试（超时翻倍，等待模型加载），解决 Ollama 冷启动问题
- **思考状态指示器**：发送消息后显示「守艺人思考中 ··· (12s)」+ 红色进度条，状态分阶段更新（思考中→模型加载中重试→收到回复）
- **会话历史管理**：新建会话（保存当前→清空→显示新开场白）、历史会话（浏览/恢复/删除），持久化到 `persistentDataPath/chat_sessions.json`，最多保留20条

### 7.7 坑与经验

| 问题 | 原因 | 解决方案 |
|------|------|---------|
| 按钮点击无响应 | 装饰性 Image 的 `raycastTarget=true` 拦截了点击 | 所有非交互 Image 设 `raycastTarget=false` |
| ScrollView 不裁剪 | Viewport 的 Image 全透明导致 Mask 失效 | Viewport Image 设白色不透明，`showMaskGraphic=false` |
| 3D 模型被 Canvas 遮挡 | ScreenSpaceOverlay 完全覆盖 3D | 改用 ScreenSpaceCamera + planeDistance=20 |
| 场景无法加载 | 场景未添加到 Build Settings | 确保 EditorBuildSettings.asset 包含所有场景 |
| 展品旋转极慢 | 鼠标 delta 乘了 Time.deltaTime 导致双重缩小 | 去掉 deltaTime，直接用 delta * rotateSpeed |
| 主题切换不生效 | 颜色常量为 readonly | 改为静态属性，根据 GameManager.themeStyle 动态返回 |
| 2个 AudioListener 警告 | AudioManager 创建时场景已有 Listener | 自动检测并销毁多余的 Listener |
| 背包展品不显示 | MakeOverlay 改为 ScrollRect 后路径变更 | `Panel/C` → `Panel/Viewport/C`，并销毁 ContentSizeFitter 重置 RectTransform |
| 弹窗点击穿透关闭 | 遮罩层添加了 Button 组件 | 移除遮罩 Button，设 `raycastTarget=true` 仅拦截点击 |
| 抽屉文字空白 | 文字用固定 offsetMax=-110px，小屏幕下高度变负 | 改为 ScrollRect + 锚点比例布局，自适应任意高度 |
| X 按钮不显示文字 | ✕ 字符不在 LegacyRuntime 字体中 | 改为 ASCII "X" |
| 设置面板X按钮竖向拉长 | 锚点 `(1,0)~(1,1)` 在标题行内拉伸 | 改为右上角锚点 `(1,1)` + 固定 28×28 |
| 乐器品类角色不切换 | MainPanel 用 "民族乐器"，AIPersona 用 "乐器" | 统一为 "民族乐器" |
| 返回主页角色不恢复 | CurrentCategory 未清除 | MainPanel.Start() 中清空 CurrentCategory |
| 对话开场白不随角色变 | hasGreeting 标志只显示一次 | 改用 lastGreetingPersonaId 检测角色变化 |
| 标签首次不更新 | DeferredInit 延迟创建时 lastCategory 已被设置 | 初始化末尾重置 lastCategory 并强制刷新 |
| 品类卡片只显示4个 | 循环硬编码 `i < 4` 而非 `Categories.Length` | 全部改为 `i < Categories.Length` |
| 知识探索空白卡片 | replace_all 误把测验选项循环也改成 Categories.Length(8) 导致数组越界 | 将4处测验选项循环改回 `i < 4` |
| 事件列表无法滚动 | 绝对定位超出屏幕不可滚动 | 改为 ScrollRect + VerticalLayoutGroup + ContentSizeFitter |
| 事件详情印章标题重叠 | 印章 anchor 0.88 与标题 0.84 间距过小 | 印章上移至 0.92，标题下移至 0.74~0.82 |
| 历史面板X按钮无响应 | EmptyHint 文字铺满面板且 raycastTarget=true 拦截点击 | 设置 `raycastTarget=false` |
| 背包面板元素紧凑无滚动 | 复用 MakeOverlay(440×380) 太小，列表用锚点百分比定位会重叠 | 改为自建560×680大面板 + ScrollRect + LayoutGroup 固定行高 |
| 登录按钮溢出底部红线 | 按钮 320×48 位置 -510 超出面板底部 | 缩小为 280×40，上移至 -440/-492，底部留出间距 |

---

## 八、运行说明

1. 用 Unity 打开项目
2. 打开任意场景（推荐 LoginScene）
3. 点击 Play 运行
4. 注册新用户 → 登录 → 开始探索 → 选择品类 → 查看 3D 展品
5. 可在设置中切换主题风格（默认/古典/简约）
6. 勾选"记住密码"后下次启动自动填充

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
| 架构模式 | 单例 + 继承（UIFrame 基类 + 主题系统） |
| 音频系统 | 程序化五声音阶BGM + WAV SFX |
