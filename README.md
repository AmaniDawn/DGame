![DGame](https://socialify.git.ci/AmaniDawn/DGame/image?custom_description=%E6%A0%B9%E6%8D%AETEngine%E6%A1%86%E6%9E%B6%EF%BC%8C%E7%BB%93%E5%90%88%E5%AE%9E%E9%99%85%E5%B7%A5%E4%BD%9C%E7%BB%8F%E9%AA%8C%EF%BC%8C%E5%AF%B9%E6%A1%86%E6%9E%B6%E8%BF%9B%E8%A1%8C%E4%BA%86%E4%B8%80%E4%BA%9B%E8%B0%83%E6%95%B4%EF%BC%88%E5%8C%85%E6%8B%AC%E4%BD%86%E4%B8%8D%E9%99%90%E4%BA%8E%E5%B7%A5%E5%85%B7%E3%80%81%E6%8B%93%E5%B1%95%E3%80%81%E9%83%A8%E5%88%86%E5%BA%95%E5%B1%82%E9%80%BB%E8%BE%91%E8%B0%83%E6%95%B4%E4%BC%98%E5%8C%96%EF%BC%89%E3%80%82%E4%B8%8D%E6%8F%90TEngine%E7%9A%84PR%EF%BC%8C%E4%BF%9D%E6%8C%81TEngine%E7%AE%80%E6%B4%81%E7%BA%AF%E5%87%80%E3%80%82&description=1&font=JetBrains+Mono&language=1&logo=https%3A%2F%2Funity.com%2F_next%2Fimage%3Furl%3Dhttps%253A%252F%252Fcdn.sanity.io%252Fimages%252Ffuvbjjlp%252Fproduction%252Faa5ce86e0add266e09ffa222609af4cc94befc7b-250x282.png%26w%3D256%26q%3D75&name=1&pattern=Signal&theme=Light)

<div align="center">

**Unity 游戏框架解决方案**

> 基于 TEngine 深度优化演进，为商业级游戏开发打造

[![Unity Version](https://img.shields.io/badge/Unity-2022.3.62%2B-blue.svg?style=flat-square)](https://unity3d.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg?style=flat-square)](LICENSE)
[![Last Commit](https://img.shields.io/github/last-commit/AmaniDawn/DGame?style=flat-square)](https://github.com/AmaniDawn/DGame/commits)
[![Issues](https://img.shields.io/github/issues/AmaniDawn/DGame?style=flat-square)](https://github.com/AmaniDawn/DGame/issues)
[![Stars](https://img.shields.io/github/stars/AmaniDawn/DGame?style=flat-square)](https://github.com/AmaniDawn/DGame/stargazers)

</div>

---

## 📖 简介

**DGame** 是在 [TEngine](https://github.com/ALEXTANGXIAO/TEngine) 基础上深度优化演进的 Unity 游戏框架。保留 TEngine 核心优势的同时，针对商业游戏开发需求**新增了多个核心模块**，并对现有系统进行了**部分优化**。

> 📝 本文档由 AI（Claude）自动生成，内容可能存在偏差，如有疑问请参考源代码或提交 Issue。
>
> 新增功能均在多个商业化游戏及百万DAU项目经过验证。
>
> Fantasy线和TE同步接入服务器框架[Fantasy](https://github.com/qq362946/Fantasy)，区别在于TE接入的就更纯净简洁，本项目可能会做更多的项目细节和实用工具方法处理。
>
> 本框架已支持 Codex 的基础 skill，其中 `dgame-dev` 可用于辅助理解本仓库的目录结构、客户端架构与常见开发流程。

### ✨ 核心特性

| 特性                   | 描述                                                  | 状态           |
| ---------------------- | ----------------------------------------------------- | -------------- |
| 🚀 **模块化设计**      | 高内聚低耦合的模块化架构，基于接口的模块管理           | 继承自 TEngine |
| 🔥 **热更新支持**      | 集成 HybridCLR，支持全平台 C# 代码热更新               | 继承自 TEngine |
| 📦 **资源管理**        | 集成 YooAsset，支持资源分包、边玩边下                  | 继承自 TEngine |
| 📋 **配置系统**        | 基于 Luban 的配置表解决方案                            | 继承自 TEngine |
| 🧩 **流程管理**        | 基于状态机的流程系统，管理游戏生命周期                 | 继承自 TEngine |
| 🔄 **MonoDriver**      | 替代 UpdataDriver，统一生命周期驱动                   | ⚡ 优化改进    |
| 🌍 **多语言**          | ILocalizationModule，接口驱动、8 种语言支持            | ⚡ 重新实现    |
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
| 👁️ **GameTickWatcher** | 游戏帧监听                                             | 🆕 新增功能    |

---

## 📚 目录

- [🆕 新增功能一览](#-新增功能一览)
- [⚡ 优化改进一览](#-优化改进一览)
- [快速开始](#-快速开始)
- [🤖 AI 协作开发（Codex + OpenSpec）](#-ai-协作开发codex--openspec)
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
| 🕹️ **InputModule**  | Runtime/Module/       | 统一输入处理、多点触控、手势识别    |
| 🏊 **GameObjectPool** | Runtime/Module/       | 专用 GameObject 对象池              |
| ⏱️ **GameTimer**    | Runtime/Module/       | 替代 TimerModule，支持循环次数限制  |
| 🔄 **MonoDriver**   | Runtime/Module/       | 替代 UpdataDriver                  |

### HotFix 层新增

| 模块                      | 文件路径                      | 说明                                           |
| ------------------------- | ----------------------------- | ---------------------------------------------- |
| 🔴 **RedDotModule**       | GameLogic/Module/             | 树状结构红点管理，编辑器可视化配置             |
| 💾 **DataCenter**         | GameLogic/DataCenter/         | 统一数据管理，生命周期事件通知                 |
| 💽 **ClientSaveDataMgr**  | GameLogic/DataCenter/         | 客户端存档系统，支持按角色存档                 |
| 🎬 **FrameSprite**        | GameLogic/Common/             | 序列帧动画系统，对象池复用                     |
| 🎮 **GMPanel**            | GameLogic/GMPanel/            | GM 调试面板，配置表驱动                        |
| 📜 **SuperScrollView**    | GameLogic/Module/UIModule/    | 高性能循环滚动视图（ListView/GridView/StaggeredGridView） |
| 📝 **Text 系统**           | GameLogic/Text/               | 多语言文本系统（G.cs, TextConfigMgr）          |
| 🔧 **SingletonSystem**    | GameLogic/SingletonSystem/    | 单例系统                                       |
| 👤 **PlayerData**         | GameLogic/PlayerData/         | 玩家数据管理                                   |
| 👁️ **GameTickWatcher**    | GameLogic/GameTickWatcher/    | 游戏帧监听                                     |

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
| 🌍 **多语言**   | LocalizationModule     | ILocalizationModule           | 接口驱动设计、8 种语言支持、事件通知机制          |
| 🎲 **内存池**   | MemoryCollection       | MemoryCollector               | Spawn/Release 语义、支持批量释放、Capacity 属性   |
| 🎨 **UI 系统**  | 基础组件               | UIBindComponent + 组件扩展    | 代码自动生成、UIButton/UIImage/UIText 组件扩展    |
| 📡 **事件系统** | GameEventMgr           | EventDispatcher               | 分离设计、接口包装支持                             |
| 📝 **日志系统** | Log                    | DGameLog                      | 更简洁的日志封装                                   |

---

## 🚀 快速开始

### 环境要求

| 项目           | 要求                                   |
| -------------- | -------------------------------------- |
| **Unity 版本** | 2021.3.30f1c1（推荐）或更高            |
| **开发环境**   | .NET 4.x / .NET Standard 2.1           |
| **支持平台**   | Windows、Android、iOS                  |
| **IDE**        | Visual Studio 2019+ 或 JetBrains Rider |

### 快速上手

1. **克隆项目**

   ```bash
   git clone https://github.com/AmaniDawn/DGame.git
   ```

2. **打开项目**
   - 使用 Unity 2021.3.30f1c1 打开 `GameUnity` 目录

3. **运行项目**
   - 打开 `Assets/Scenes` 下的 `LaunchScene` 启动场景
   - 点击运行按钮启动游戏

4. **热更新设置**（可选）
   - 运行菜单 `HybridCLR/Install...` 安装 HybridCLR
   - 运行菜单 `HybridCLR/Generate/All` 生成必要的代码
   - 构建热更新 DLL 和资源包

---

## 🤖 AI 协作开发（Codex + OpenSpec）

本仓库已经完成了基础的 `codex skills` 支持，相关能力位于项目根目录的 `.codex/skills/`，可用于辅助理解 DGame 的目录结构、客户端架构、热更开发流程，以及基于 OpenSpec 的变更提案、实现与归档工作流。

### 推荐使用方式

推荐优先使用 **Codex CLI**，并结合 **OpenSpec** 进行规格驱动开发：

1. 使用 `Codex CLI` 在仓库内完成代码阅读、修改、调试与文档维护
2. 使用 `OpenSpec` 先沉淀需求、设计和任务拆解
3. 再通过 `Codex + OpenSpec` 按任务逐步实现、验证与归档

适合本仓库的典型流程如下：

```bash
# 1. 进入仓库
cd DGame

# 2. 初始化或更新 OpenSpec 结构
openspec init
# 初始化时推荐选择 Codex 作为 AI 工具

# 3. 启动 Codex CLI
codex

# 4. 在 Codex 中按 OpenSpec 工作流推进
# 例如：先提 proposal，再实现，再 archive
```

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

常用模式示例：

```bash
codex --auto-edit
codex --full-auto
```

> 提示：`Codex CLI` 官方对 macOS / Linux 支持更完整；如在 Windows 环境下遇到兼容性问题，建议优先使用 WSL。

### OpenSpec 安装与初始化

#### 1. 全局安装 OpenSpec CLI

```bash
npm install -g @fission-ai/openspec@latest
```

#### 2. 验证安装

```bash
openspec --version
```

#### 3. 在仓库根目录初始化

```bash
openspec init # 初始化时推荐选择 Codex 作为 AI 工具
```

初始化过程中，当 CLI 询问要集成的 AI 工具时，**推荐优先选择 `Codex`**。

初始化后通常会生成或更新以下内容：

- `openspec/`：用于存放当前变更、规范与归档
- `AGENTS.md`：提供给 AI 助手的统一协作说明
- 针对所选 AI 工具的辅助命令或集成配置

#### 4. 常用 OpenSpec 命令

```bash
# 查看当前变更
openspec list

# 查看某个变更状态
openspec status --change "<change-name>"

# 校验某个变更
openspec validate "<change-name>" --strict

# 归档已完成变更
openspec archive "<change-name>" --yes
```

### 本仓库中的建议工作流

1. 先运行 `openspec init`，保证仓库具备标准化规格目录与 AI 协作入口
2. 使用 `codex` 进入仓库，在需求较明确时优先走 OpenSpec 流程
3. 先产出 proposal / design / tasks，再进入实现阶段
4. 实现过程中优先复用本仓库已有的 `.codex/skills/` 与 `AGENTS.md`
5. 开发完成后执行验证，并归档对应 OpenSpec change

### 参考文档

- Codex CLI: https://help.openai.com/en/articles/11096431-openai-codex-ci-getting-started
- Codex CLI 登录: https://help.openai.com/en/articles/11381614
- OpenSpec 安装: https://thedocs.io/openspec/installation/
- OpenSpec 快速开始: https://thedocs.io/openspec/quick_start/

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
var redDot = CreateRedDot(RedDotPathDefine_Gen.Main.Bag.Equipment, parentTransform);

// 更新红点状态
RedDotModule.Instance.SetState(RedDotPathDefine_Gen.Main.Bag.Equipment, true);

// 设置红点数量
RedDotModule.Instance.SetNumber(RedDotPathDefine_Gen.Main.Quest.Daily, 5);
```

### 🎬 序列帧动画 (FrameSprite)

高效序列帧播放，对象池复用，异步加载。

```csharp
var frameSprite = await FrameSpriteMgr.PlayAsync(
    parent: transform,
    atlasName: "EffectAtlas",
    spriteNames: new[] { "frame_0", "frame_1", "frame_2" },
    frameRate: 30,
    loopCount: 1
);
```

### 🎮 GM 系统 (GMPanel)

配置表驱动的调试面板。

```csharp
GMModule.ExecuteCommand("AddGold 1000");
GMModule.ExecuteClientGm(GmCommandId.AddGold);
```

### 💾 数据中心 (DataCenter)

统一数据管理，生命周期事件通知。

```csharp
public class PlayerData : DataBase
{
    public int Level;
    public long Gold;
}

var playerData = DataCenterModule.GetData<PlayerData>();
playerData.Gold += 100;
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
var inputModule = ModuleSystem.GetModule<IInputModule>();
inputModule.OnClick += (position) => { };
inputModule.OnLongPress += (position, duration) => { };
inputModule.OnDrag += (delta) => { };
```

### 🏊 GameObject 对象池 (GameObjectPoolModule)

专用 GameObject 对象池。

```csharp
var pool = ModuleSystem.GetModule<IGameObjectPoolModule>();
var obj = pool.Spawn("Bullet");
pool.Despawn(obj);
```

### 📜 SuperScrollView

高性能循环滚动视图。

```csharp
loopListView.InitListView(itemCount, (index, item) => {
    var itemLogic = item as MyItem;
    itemLogic.SetItemData(index);
    return itemLogic;
});
```

### 📝 Text 系统

多语言文本系统（G.cs, TextConfigMgr, TextDefine）。

### 🔧 SingletonSystem

单例系统，统一管理单例模式。

### 👁️ GameTickWatcher

游戏帧监听，用于性能监控和调试。

---

## ⚡ 优化改进详解

### ⏱️ GameTimer（替代 TimerModule）

- 返回可操作对象，支持循环次数限制
- 使用 DGameLinkedList 优化链表结构

```csharp
var timer = GameTimer.CreateLoopGameTimer(1f, () => DLogger.Info("每秒执行"));
timer.LoopCount = 5;  // 只执行 5 次
timer.Paused = true;  // 暂停
```

### 🌍 ILocalizationModule（替代 LocalizationModule）

- 接口驱动设计
- 8 种语言支持（中/英/繁/韩/日/越/印尼）
- 事件通知机制

### 🎲 MemoryCollector（替代 MemoryCollection）

- Spawn/Release 语义更清晰
- 支持批量释放

```csharp
var obj = MemoryCollector.Spawn<MyClass>();
MemoryCollector.Release(obj);
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
| 🌍 多语言        | `ILocalizationModule`   | 8 种语言支持，接口驱动 | ⚡ 重新实现 |
| 🏊 对象池        | `IObjectPoolModule`     | 对象池管理             | TEngine     |
| ⏱️ 计时器        | `IGameTimerModule`      | 游戏计时器             | ⚡ 优化     |
| 🎲 内存池        | `MemoryPool`            | 非托管内存池           | ⚡ 优化     |
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
│   │   └── Scripts/
│   │       ├── HotFix/      # 热更新代码
│   │       │   ├── GameBase/      # 基础框架程序集
│   │       │   ├── GameProto/     # 配置协议程序集
│   │       │   └── GameLogic/     # 业务逻辑程序集
│   │       │       ├── Module/    # 🆕 红点系统、数据中心、UI 系统等
│   │       │       ├── Common/    # 🆕 序列帧动画等
│   │       │       └── GMPanel/   # 🆕 GM 系统
│   │       └── Launcher/    # 启动器代码（AOT）
│   └── ...
├── Tools/                   # 开发工具
└── UnityPackage/            # Unity 包导出
```

---

## 💻 系统要求

| 项目 | 要求 |
|------|------|
| **Unity 版本** | 2021.3.30f1c1（推荐）或更高 |
| **开发环境** | .NET 4.x / .NET Standard 2.1 |
| **支持平台** | Windows、Android、iOS |

---

## 📖 开发指南

### 计时器使用（⚡ 优化版）

```csharp
var timer = GameTimer.CreateLoopGameTimer(1f, () => DLogger.Info("每秒执行"));
timer.LoopCount = 5;  // 只执行 5 次
```

### 红点系统使用（🆕 新增）

```csharp
RedDotModule.Instance.SetState(RedDotPathDefine_Gen.Main_Bag_Equipment, true);
AddUIEvent(RedDotPathDefine_Gen.Main_Bag, OnRedDotChanged);
```

### 序列帧动画（🆕 新增）

```csharp
var frameSprite = await FrameSpriteMgr.PlayAsync(transform, "EffectAtlas", spriteNameArray, 30, 1);
```

### GM 命令（🆕 新增）

```csharp
GMModule.ExecuteCommand("AddGold 1000");
GMModule.ExecuteClientGm(GmCommandId.AddGold);
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
    private Button m_btnClose;

    protected override void ScriptGenerator()
    {
        m_btnClose = FindChildComponent<Button>("m_btnClose");
        m_btnClose.ClickProtect = true;
        m_btnClose.ClickScale = 0.9f;
    }
}

await UIModule.Instance.ShowWindowAsync<MyWindow>(userData);
```

---

## 🙏 致谢

本项目基于 [TEngine](https://github.com/ALEXTANGXIAO/TEngine) 框架进行深度优化演进，感谢 TEngine 项目的优秀架构设计和核心功能实现。

---

## 🤝 开源协议

本项目采用 [MIT License](LICENSE) 开源协议。
