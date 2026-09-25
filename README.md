![DGame](https://socialify.git.ci/AmaniDawn/DGame/image?custom_description=%E6%A0%B9%E6%8D%AETEngine%E6%A1%86%E6%9E%B6%EF%BC%8C%E7%BB%93%E5%90%88%E5%AE%9E%E9%99%85%E5%B7%A5%E4%BD%9C%E7%BB%8F%E9%AA%8C%EF%BC%8C%E5%AF%B9%E6%A1%86%E6%9E%B6%E8%BF%9B%E8%A1%8C%E4%BA%86%E4%B8%80%E4%BA%9B%E8%B0%83%E6%95%B4%EF%BC%88%E5%8C%85%E6%8B%AC%E4%BD%86%E4%B8%8D%E9%99%90%E4%BA%8E%E5%B7%A5%E5%85%B7%E3%80%81%E6%8B%93%E5%B1%95%E3%80%81%E9%83%A8%E5%88%86%E5%BA%95%E5%B1%82%E9%80%BB%E8%BE%91%E8%B0%83%E6%95%B4%E4%BC%98%E5%8C%96%EF%BC%89%E3%80%82%E4%B8%8D%E6%8F%90TEngine%E7%9A%84PR%EF%BC%8C%E4%BF%9D%E6%8C%81TEngine%E7%AE%80%E6%B4%81%E7%BA%AF%E5%87%80%E3%80%82&description=1&font=JetBrains+Mono&language=1&logo=https%3A%2F%2Funity.com%2F_next%2Fimage%3Furl%3Dhttps%253A%252F%252Fcdn.sanity.io%252Fimages%252Ffuvbjjlp%252Fproduction%252Faa5ce86e0add266e09ffa222609af4cc94befc7b-250x282.png%26w%3D256%26q%3D75&name=1&pattern=Signal&theme=Light)

<div align="center">

**Unity 游戏框架解决方案**

> 基于 TEngine 深度优化演进，为商业级游戏开发打造

[![Unity Version](https://img.shields.io/badge/Unity-2022.3.62f3-blue.svg?style=flat-square)](https://unity3d.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg?style=flat-square)](LICENSE)
[![Last Commit](https://img.shields.io/github/last-commit/AmaniDawn/DGame?style=flat-square)](https://github.com/AmaniDawn/DGame/commits)
[![Issues](https://img.shields.io/github/issues/AmaniDawn/DGame?style=flat-square)](https://github.com/AmaniDawn/DGame/issues)
[![Stars](https://img.shields.io/github/stars/AmaniDawn/DGame?style=flat-square)](https://github.com/AmaniDawn/DGame/stargazers)

</div>

---

## 📖 简介

**DGame** 是在 [TEngine](https://github.com/ALEXTANGXIAO/TEngine) 基础上深度优化演进的 Unity 游戏框架。保留 TEngine 核心优势的同时，针对商业游戏开发需求**新增了多个核心模块**，并对现有系统进行了**部分优化**。

> 📝 本文档按当前仓库源码和项目配置维护；API、路径和验证方式以实际源码及 `AGENTS.md` 为准。
>
> 新增功能均在多个商业化游戏及百万DAU项目经过验证。
>
> Fantasy线和TE同步接入服务器框架[Fantasy](https://github.com/qq362946/Fantasy)，区别在于TE接入的就更纯净简洁，本项目可能会做更多的项目细节和实用工具方法处理。
>
> 本仓库通过 `dgame-dev`、`unity-cli` 和 `luban-dev` 提供项目内开发与验证辅助，详细规则见 `AGENTS.md`。

### ✨ 核心特性

| 特性                   | 描述                                                  | 状态           |
| ---------------------- | ----------------------------------------------------- | -------------- |
| 🚀 **模块化设计**      | 高内聚低耦合的模块化架构，基于接口的模块管理           | 继承自 TEngine |
| 🔥 **热更新支持**      | 集成 HybridCLR，支持全平台 C# 代码热更新               | 继承自 TEngine |
| 📦 **资源管理**        | 集成 YooAsset，支持资源分包、边玩边下                  | 继承自 TEngine |
| 📋 **配置系统**        | 基于 Luban 的配置表解决方案                            | 继承自 TEngine |
| 🧩 **流程管理**        | 基于状态机的流程系统，管理游戏生命周期                 | 继承自 TEngine |
| 🔄 **MonoDriver**      | 替代 UpdataDriver，统一生命周期驱动                   | ⚡ 优化改进    |
| 🌍 **多语言**          | ILocalizationModule，接口驱动、7 种语言支持            | ⚡ 重新实现    |
| ⏱️ **GameTimer**       | 替代 TimerModule，支持循环次数限制                     | ⚡ 优化改进    |
| 🎲 **MemoryCollector** | 替代 MemoryCollection，Spawn/Release 语义             | ⚡ 优化改进    |
| 🛠️ **编辑器工具**      | 工具栏扩展、Spine 助手、Odin 支持、快速创建入口        | 🆕 新增功能    |
| 🎯 **AnimModule**      | 封装 Playable API，与 FsmModule 深度集成               | 🆕 新增功能    |
| 🕹️ **InputModule**     | 统一输入处理、多点触控、手势识别                        | 🆕 新增功能    |
| 🏊 **GameObjectPool**  | 专用 GameObject 对象池                                 | 🆕 新增功能    |
| 🎨 **UIBindComponent** | UI 代码自动生成、UIButton/UIImage/UIText 组件扩展      | 🆕 新增功能    |
| 🔴 **RedDotModule**    | 树状结构红点管理，编辑器可视化配置                     | 🆕 新增功能    |
| 🎬 **FrameSprite**     | 序列帧动画系统，对象池复用                             | 🆕 新增功能    |
| 🎮 **GMPanel**         | GM 调试面板，配置表驱动                                | 🆕 新增功能    |
| 💾 **DataCenter**      | 统一数据管理，生命周期事件通知                         | 🆕 新增功能    |
| 💽 **ClientSaveDataMgr** | 客户端存档系统，JSON 序列化、按角色存档             | 🆕 新增功能    |
| 📜 **SuperScrollView** | 高性能循环滚动视图                                     | 🆕 新增功能    |
| 📝 **Text 系统**        | 多语言文本系统（G.cs, TextConfigMgr）                  | 🆕 新增功能    |
| 🔧 **SingletonSystem** | 单例系统                                               | 🆕 新增功能    |
| 👁️ **GameTickWatcher** | 基于 Stopwatch 的耗时统计工具                         | 🆕 新增功能    |

---

## 📚 目录

- [🆕 新增功能一览](#-新增功能一览)
- [⚡ 优化改进一览](#-优化改进一览)
- [快速开始](#-快速开始)
- [新增功能详解](#-新增功能详解)
- [优化改进详解](#-优化改进详解)
- [核心模块](#-核心模块)
- [项目结构](#-项目结构)
- [系统要求](#-系统要求)
- [开发指南](#-开发指南)

---

## 🆕 新增功能一览

### Runtime 层新增

| 模块                | 文件路径              | 说明                               |
| ------------------- | --------------------- | ---------------------------------- |
| 🎯 **AnimModule**    | Runtime/Module/       | 封装 Playable API，与 FsmModule 集成 |
| 🏊 **GameObjectPool** | Runtime/Module/       | 专用 GameObject 对象池              |
| ⏱️ **GameTimer**    | Runtime/Module/       | 替代 TimerModule，支持循环次数限制  |
| 🔄 **MonoDriver**   | Runtime/Module/       | 替代 UpdataDriver                  |

### HotFix 层新增

| 模块                      | 文件路径                      | 说明                                           |
| ------------------------- | ----------------------------- | ---------------------------------------------- |
| 🔴 **RedDotModule**       | GameLogic/Module/             | 树状结构红点管理，编辑器可视化配置             |
| 🕹️ **InputModule**       | GameLogic/Module/InputModule/ | 统一输入处理、多点触控、手势识别              |
| 💾 **DataCenter**         | GameLogic/DataCenter/         | 统一数据管理，生命周期事件通知                 |
| 💽 **ClientSaveDataMgr**  | GameLogic/DataCenter/         | 客户端存档系统，支持按角色存档                 |
| 🎬 **FrameSprite**        | GameLogic/Module/FrameAnimModule/ | 序列帧动画系统，对象池复用                  |
| 🎮 **GMPanel**            | GameLogic/UI/GMPanel/         | GM 调试面板，配置表驱动                        |
| 📜 **SuperScrollView**    | GameLogic/Module/UIModule/    | 高性能循环滚动视图（ListView/GridView/StaggeredGridView） |
| 📝 **Text 系统**          | GameLogic/Module/TextModule/ | 多语言文本系统（G.cs, TextConfigMgr）          |
| 🔧 **SingletonSystem**    | GameLogic/Module/SingletonSystem/ | 单例系统                                    |
| 👤 **PlayerData**         | GameLogic/DataCenter/PlayerData/ | 玩家数据管理                              |
| 👁️ **GameTickWatcher**    | GameLogic/GameTickWatcher/    | Stopwatch 耗时统计工具                        |

### Editor 层新增

| 模块                       | 文件路径              | 说明                           |
| -------------------------- | --------------------- | ------------------------------ |
| 🛠️ **UnityToolBarExtend**  | Editor/               | 工具栏扩展（场景启动、播放模式） |
| 🎨 **SpineModelHelper**    | Editor/               | Spine 模型辅助处理             |
| ⚙️ **CreateGameEntry**     | Editor/Settings/      | 快速创建 GameEntry 对象        |
| 📦 **Odin**                | Editor/Odin/          | Odin 插件支持                  |
| 🚪 **OpenFolderHelper**    | Editor/OpenFolder/    | 快速打开项目文件夹             |

---

## ⚡ 优化改进一览

| 模块            | TEngine                | DGame                         | 主要改进点                                        |
| --------------- | ---------------------- | ----------------------------- | ------------------------------------------------- |
| ⏱️ **计时器**   | TimerModule            | GameTimer                     | 返回对象可操作、支持循环次数限制、DGameLinkedList  |
| 🌍 **多语言**   | LocalizationModule     | ILocalizationModule           | 接口驱动设计、7 种语言支持、事件通知机制          |
| 🎲 **内存池**   | MemoryCollection       | MemoryPool / MemoryCollector  | Spawn/Release 语义、支持批量释放                   |
| 🎨 **UI 系统**  | 基础组件               | UIBindComponent + 组件扩展    | 代码自动生成、UIButton/UIImage/UIText 组件扩展    |
| 📡 **事件系统** | GameEventMgr           | EventDispatcher               | 分离设计、接口包装支持                             |
| 📝 **日志系统** | Log                    | DGameLog                      | 更简洁的日志封装                                   |

---

## 🚀 快速开始

### 环境要求

| 项目           | 要求                                   |
| -------------- | -------------------------------------- |
| **Unity 版本** | 2022.3.62f3                             |
| **开发环境**   | .NET SDK（代码构建）/ .NET Standard 2.1 |
| **支持平台**   | Windows、Android、iOS                  |
| **IDE**        | Visual Studio 2019+ 或 JetBrains Rider |

### 快速上手

1. **克隆项目**

   ```bash
   git clone https://github.com/AmaniDawn/DGame.git
   ```

2. **打开项目**
   - 使用 Unity 2022.3.62f3 打开 `GameUnity` 目录

3. **运行项目**
   - 打开 `Assets/Scenes/GameStart/GameStart.unity` 启动场景
   - 点击运行按钮启动游戏

4. **热更新设置**（可选）
   - 运行菜单 `HybridCLR/Install...` 安装 HybridCLR
   - 运行菜单 `HybridCLR/Generate/All` 生成必要的代码
   - 构建热更新 DLL 和资源包

---

## 🤖 AI 协作开发（Claude Code / Codex）

本仓库使用共享的 Agent Skills，技能位于 `.agents/skills/dgame-dev/`、`.agents/skills/unity-cli/` 和 `.agents/skills/luban-dev/`。Claude Code 与 Codex 都先读取 `AGENTS.md`，再按任务读取对应 `SKILL.md`；Unity 工程内还要读取 `GameUnity/AGENTS.md`。

### 推荐工作流

1. 根据任务读取对应的 skill 和 reference。
2. 使用项目 Workflow 执行结构检查、代码构建、配置导表和 Unity 验证。
3. 交付时记录实际执行的命令、结果和未执行阶段。

```powershell
cd DGame
codex
python .agents/scripts/workflow.py check
python .agents/scripts/workflow.py verify --profile docs
python .agents/scripts/workflow.py verify --profile code
python .agents/scripts/workflow.py verify --profile unity --test-filter GameLogic
python .agents/scripts/workflow.py verify --profile full
```

`doctor` 和 `unity/full` 验证优先使用工程目录的 `GameUnity/Tools/unity.exe`，缺少时自动从系统 `PATH` 查找 `unity`；Workflow 不会自动安装或替换 Unity CLI。完整验证还会在隔离目录执行两次 Luban 客户端导表并比较产物哈希。目录说明见 `.agents/README.md`。

### Codex CLI 安装

#### 1. 安装 Node.js

- 建议使用 Node.js 20+，并确保 `npm` 可用

#### 2. 全局安装 Codex CLI

```bash
npm install -g @openai/codex
```

#### 3. 登录 Codex

可任选一种方式：

```bash
# 使用 ChatGPT 账号登录
codex --login
```

如果需要通过 API Key 使用，推荐优先使用 `ccswitch` 管理不同 Provider、模型配置与密钥，再切换到 Codex 使用。

`ccswitch` 仓库：

- https://github.com/farion1231/cc-switch

或手动配置 OpenAI API Key：

```bash
# macOS / Linux
export OPENAI_API_KEY="<YOUR_OPENAI_API_KEY>"

# Windows PowerShell
$env:OPENAI_API_KEY="<YOUR_OPENAI_API_KEY>"
```

#### 4. 在仓库根目录启动

```bash
codex
```

> Codex CLI 的安装和登录方式以官方文档为准；本仓库只约束项目内的技能、路径和 Workflow。

### 仓库验证流程

从仓库根目录运行：

```powershell
python .agents/scripts/workflow.py doctor
python .agents/scripts/workflow.py check
python .agents/scripts/workflow.py verify --profile code
python .agents/scripts/workflow.py verify --profile full
```

### 参考文档

- Codex CLI: https://help.openai.com/en/articles/11096431-openai-codex-ci-getting-started
- Codex CLI 登录: https://help.openai.com/en/articles/11381614

---

## 🎁 新增功能详解

### 🛠️ 编辑器工具 (Editor)

| 工具 | 功能 |
|------|------|
| **UnityToolBarExtend** | 左右工具栏增强、场景快速启动、播放模式控制 |
| **SpineModelHelper** | Spine 模型辅助处理 |
| **CreateGameEntry** | 一键创建 GameEntry 对象 |
| **Odin** | Odin 插件支持 |
| **OpenFolderHelper** | 快速打开项目文件夹 |

### 🔴 红点系统 (RedDotModule)

树状结构红点管理，编辑器可视化配置，代码自动生成。

```csharp
// 创建红点
var redDot = CreateRedDot(RedDotPathDefine.Main.Bag.Equipment, parentTransform);

// 更新红点数量
GameModule.RedDotModule.SetValue(RedDotPathDefine.Main.Bag.Equipment, 1);

// 设置红点数量
GameModule.RedDotModule.SetValue(RedDotPathDefine.Main.Quest.Daily, 5);
```

### 🎬 序列帧动画 (FrameSprite)

高效序列帧播放，对象池复用，异步加载。

```csharp
var animator = FrameAnimatorAgent.Create();
await animator.Init(modelConfig);
animator.StartAnim();
```

### 🎮 GM 系统 (GMPanel)

配置表驱动的调试面板。

```csharp
ClientGM.Instance.AddCommendCfg(commandConfig);
GameModule.UIModule.ShowWindowAsync<GMPanel>();
```

### 💾 数据中心 (DataCenter)

统一数据管理，生命周期事件通知。

```csharp
public sealed class BagDataCenter : DataCenterModule<BagDataCenter>
{
    public int Gold;
}

var playerData = DataCenterSys.Instance.CurPlayerData;
if (playerData != null)
{
    DLogger.Info($"当前角色：{playerData.RoleName}");
}
```

### 💽 客户端存档系统 (ClientSaveDataMgr)

基于 PlayerPrefs 的存档系统，支持 JSON 序列化和按角色存档。

**特性：**
- JSON 序列化/反序列化
- PlayerPrefs 本地存储
- 支持按角色 ID 存档（多角色支持）
- 自动加载、批量保存

```csharp
// 定义存档类
[ClientSaveData("Setting", PerRoleID = false)]
public class SettingSaveData : BaseClientSaveData
{
    public int MusicVolume = 50;
    public bool IsFirstLogin = true;
}

// 获取存档（自动加载）
var setting = ClientSaveDataMgr.Instance.GetSaveData<SettingSaveData>();
setting.MusicVolume = 80;
setting.Save();  // 保存

// 批量保存所有存档
ClientSaveDataMgr.Instance.SaveAllClientData();
```

### 🎯 动画模块 (AnimModule)

封装 Playable API，与 FsmModule 深度集成。

```csharp
public class IdleState : BaseState<Character>
{
    public override void OnEnter()
    {
        PlayAnimation("Idle");  // 状态切换自动播放动画
    }
}
```

### 🕹️ 输入模块 (InputModule)

统一输入处理，多点触控支持。

```csharp
var inputComponent = GameModule.Input.AddInputComponent<MyInputComponent>();
GameModule.Input.Enable();
```

### 🏊 GameObject 对象池 (GameObjectPoolModule)

专用 GameObject 对象池。

```csharp
await GameModule.GameObjectPool.CreateGameObjectPoolAsync("Bullet", initCapacity: 5);
var obj = await GameModule.GameObjectPool.SpawnAsync("Bullet");
GameModule.GameObjectPool.Recycle(obj);
```

### 📜 SuperScrollView

高性能循环滚动视图。

```csharp
loopListView.InitListView(itemCount, (listView, index) => {
    var item = listView.NewListViewItem("MyItem");
    var itemLogic = item.GetComponent<MyItem>();
    itemLogic.SetItemData(index);
    return item;
});
```

### 📝 Text 系统

多语言文本系统（G.cs, TextConfigMgr, TextDefine）。

### 🔧 SingletonSystem

单例系统，统一管理单例模式。

### 👁️ GameTickWatcher

基于 Stopwatch 的耗时统计，用于性能监控和调试。

---

## ⚡ 优化改进详解

### ⏱️ GameTimer（替代 TimerModule）

- 返回可操作对象，支持循环次数限制
- 使用 DGameLinkedList 优化链表结构

```csharp
var timer = GameModule.GameTimerModule.CreateLoopCountGameTimer(
    1f, 5, _ => DLogger.Info("每秒执行"));
GameModule.GameTimerModule.Pause(timer);
GameModule.GameTimerModule.DestroyGameTimer(timer);
```

### 🌍 ILocalizationModule（替代 LocalizationModule）

- 接口驱动设计
- 7 种语言支持（中/英/繁/韩/日/越/印尼）
- 事件通知机制

### 🎲 MemoryCollector（替代 MemoryCollection）

- Spawn/Release 语义更清晰
- 支持批量释放

```csharp
public sealed class MyPayload : MemoryObject
{
    public int Value;

    public override void OnRelease() => Value = 0;
}

var obj = MemoryPool.Spawn<MyPayload>();
MemoryPool.Release(obj);
```

### 🎨 UI 系统（UIBindComponent + 组件扩展）

**组件扩展：**

| 组件 | 功能 |
|------|------|
| UIButton | 点击保护、缩放效果、音效、长按、双击 |
| UIImage | 镜像翻转、圆角、遮罩 |
| UIText | 渐变色、字间距、圆形排列、阴影 |
| RichTextItem | 图文混排、超链接、动画表情 |

### 📡 EventDispatcher（替代 GameEventMgr）

- 分离设计
- 接口包装支持

---

## 🧩 核心模块

| 模块             | 接口                    | 描述                   | 来源        |
| ---------------- | ----------------------- | ---------------------- | ----------- |
| 🎮 Mono 驱动     | `IMonoDriver`           | Unity 生命周期驱动     | TEngine     |
| 📦 资源管理      | `IResourceModule`       | YooAsset 资源管理      | TEngine     |
| 🔄 状态机        | `IFsmModule`            | 有限状态机管理         | TEngine     |
| 🔊 音频          | `IAudioModule`          | 背景音乐与音效管理     | TEngine     |
| 🎬 场景          | `ISceneModule`          | 场景加载与切换         | TEngine     |
| 🌍 多语言        | `ILocalizationModule`   | 7 种语言支持，接口驱动 | ⚡ 重新实现 |
| 🏊 对象池        | `IObjectPoolModule`     | 对象池管理             | TEngine     |
| ⏱️ 计时器        | `IGameTimerModule`      | 游戏计时器             | ⚡ 优化     |
| 🎲 内存池        | `MemoryPool`            | 托管对象复用池         | ⚡ 优化     |
| 🎯 动画          | `IAnimModule`           | 动画控制               | 🆕 新增     |
| 🕹️ 输入          | `IInputModule`          | 输入处理               | 🆕 新增     |
| 🏊 GameObject 池 | `IGameObjectPoolModule` | GameObject 对象池      | 🆕 新增     |

---

## 📁 项目结构

```
DGame/
├── GameConfig/              # 游戏配置（Luban）
├── GameRelease/             # 发布版本输出
├── GameUnity/               # Unity 主项目
│   ├── Assets/
│   │   ├── DGame/           # DGame 框架核心
│   │   │   ├── Editor/      # 🆕 编辑器工具（工具栏扩展、Spine 助手等）
│   │   │   └── Runtime/     # 框架运行时代码
│   │   │       ├── Core/    # 核心系统（日志、内存池、模块系统、事件）
│   │   │       └── Module/  # 功能模块（GameTimer、AnimModule、InputModule等）
│   │   ├── DGame.AOT/       # AOT 启动与流程代码
│   │   └── Scripts/
│   │       ├── HotFix/      # 热更新代码
│   │       │   ├── GameBattle/    # 战斗程序集
│   │       │   ├── GameLogic/     # 业务逻辑程序集
│   │       │   └── GameProto/     # 配置协议程序集
│   └── ...
├── Tools/                   # 开发工具
└── .agents/                 # 技能与 Workflow 验证工具
```

---

## 💻 系统要求

| 项目 | 要求 |
|------|------|
| **Unity 版本** | 2022.3.62f3 |
| **开发环境** | .NET SDK（代码构建）/ .NET Standard 2.1 |
| **支持平台** | Windows、Android、iOS |

---

## 📖 开发指南

### 计时器使用（⚡ 优化版）

```csharp
var timer = GameModule.GameTimerModule.CreateLoopCountGameTimer(
    1f, 5, _ => DLogger.Info("每秒执行"));
```

### 红点系统使用（🆕 新增）

```csharp
var redDot = CreateRedDot(RedDotPathDefine.Main.Bag.Equipment, parentTransform);
GameModule.RedDotModule.SetValue(RedDotPathDefine.Main.Bag.Equipment, 1);
```

### 序列帧动画（🆕 新增）

```csharp
var animator = FrameAnimatorAgent.Create();
await animator.Init(modelConfig);
animator.StartAnim();
```

### GM 命令（🆕 新增）

```csharp
GameModule.UIModule.ShowWindowAsync<GMPanel>();
```

### 存档系统（🆕 新增）

```csharp
// 定义存档类
[ClientSaveData("Setting", PerRoleID = false)]
public class SettingSaveData : BaseClientSaveData
{
    public int MusicVolume = 50;
}

// 获取存档
var setting = ClientSaveDataMgr.Instance.GetSaveData<SettingSaveData>();
setting.MusicVolume = 80;
setting.Save();

// 批量保存
ClientSaveDataMgr.Instance.SaveAllClientData();
```

### UI 开发

```csharp
public class MyWindow : UIWindow
{
    private UIButton m_btnClose;

    protected override void ScriptGenerator()
    {
        m_btnClose = FindChildComponent<UIButton>("m_btnClose");
        m_btnClose.ClickScaleExtend.IsUseClickScale = true;
        m_btnClose.ClickScaleExtend.ClickScale = new Vector3(0.9f, 0.9f, 0.9f);
    }
}

GameModule.UIModule.ShowWindowAsync<MyWindow>(userData);
```

---

## 🙏 致谢

本项目基于 [TEngine](https://github.com/ALEXTANGXIAO/TEngine) 框架进行深度优化演进，感谢 TEngine 项目的优秀架构设计和核心功能实现。

---

## 🤝 开源协议

本项目采用 [MIT License](LICENSE) 开源协议。
