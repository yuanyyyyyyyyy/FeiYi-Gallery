# 了不起的非遗 — FeiYi Gallery

基于 Unity 引擎的中国非物质文化遗产交互展示系统，涵盖瓷器、剪纸、书法、民族乐器、刺绣、茶艺、皮影戏、扎染蜡染八大品类。

## 项目信息

| 项目 | 详情 |
|------|------|
| 引擎 | Unity 2022.3 / Tuanjie 1.9.1 |
| 语言 | C# |
| 渲染管线 | 内置管线（Built-in） |
| UI 框架 | UGUI（运行时代码创建） |
| AI 对话 | 大语言模型 API，9种角色人设随品类切换 |

## 项目结构

```
Assets/
├── Editor/                      # 编辑器扩展
├── Scenes/                      # 6个场景（登录/欢迎/主页/展品/知识/事件）
├── Scripts/
│   ├── Core/                    # GameManager, SceneLoader, AudioManager 等单例
│   ├── Data/                    # 展品/知识/事件数据结构
│   ├── UI/                      # UIFrame 基类（主题系统 + 公共UI组件）
│   ├── Login/ Start/ Main/      # 各场景管理器
│   ├── Exhibit/ Knowledge/ Event/
│   ├── Backpack/                # 收藏管理
│   ├── Character/               # 3D角色状态机
│   ├── AI/                      # AI对话系统（9种角色人设）
│   └── Help/                    # 帮助/引导
├── Resources/                   # 头像图片
└── StreamingAssets/             # JSON数据 + 音频文件
```

## 核心架构

- **UIFrame 继承体系**：所有场景管理器继承 `UIFrame` 基类，复用 Canvas 初始化、新中式 UI 组件、主题切换、动画效果等公共逻辑
- **三个全局单例**：`GameManager`（状态/数据）、`SceneLoader`（场景切换）、`AudioManager`（音频），通过 `UIFrame.EnsureSingletons()` 自动创建
- **主题系统**：3种主题色板（默认/古典/简约），静态属性动态返回色值，场景加载时自动生效
- **3D模型**：全部用 Unity 基础几何体程序化拼接，无外部模型文件
- **AI对话**：猫咪头像随品类自动切换角色身份，注入品类知识上下文，支持会话历史管理

## 场景流程

```
LoginScene → StartScene → MainScene → ExhibitScene（展品3D展示）
                              ├── KnowledgeScene（知识探索+答题）
                              ├── EventScene（历史故事）
                              └── 背包/设置/帮助
```

## 数据层

- **展品/知识/事件/答题**：JSON 文件（`StreamingAssets/`），`JsonUtility` 反序列化
- **用户数据**：`PlayerPrefs` 本地键值对（账号、密码加密、头像、收藏等）
- **背包系统**：PlayerPrefs + JSON 文件双重持久化

## 运行说明

1. 用 Unity 打开项目
2. 打开 `LoginScene`，点击 Play
3. 注册新用户（用户名≥2字符，密码≥4字符）→ 登录 → 开始探索

## 技术栈

Unity 2022.3 · C# · UGUI · JSON · PlayerPrefs · 程序化几何体 · 程序化五声音阶BGM
