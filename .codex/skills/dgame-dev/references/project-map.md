# DGame 项目地图

当需求不明确该把代码、资源或工具修改放在哪一层、哪个目录时，先阅读本文件。

本文件主要回答“东西在哪、应该先去哪个目录找”；如果需求涉及架构边界、启动链路、程序集依赖、资源运行时约束，应继续读取 `references/client-architecture.md`。

本文档先记录当前已确认的仓库事实，后续再逐步补充更多目录职责、资产组织约定、程序集边界和常见落位样例。

## 目录导航

- [DGame 项目地图](#dgame-项目地图)
  - [目录导航](#目录导航)
  - [仓库根目录结构](#仓库根目录结构)
  - [Unity 主工程目录结构](#unity-主工程目录结构)
  - [主工程代码与程序集目录](#主工程代码与程序集目录)
  - [HotFix 代码目录](#hotfix-代码目录)
  - [资源与配置相关目录](#资源与配置相关目录)
  - [场景与启动入口](#场景与启动入口)
  - [落位判断规则](#落位判断规则)
  - [使用原则](#使用原则)

## 仓库根目录结构

当前仓库根目录下，和开发最相关的主路径可先按下面方式理解：

```text
DGame/
├── GameUnity/      # Unity 主工程目录
├── GameConfig/     # 配置源数据、Luban 定义、模板与转表工具
├── GameRelease/    # 发布输出目录
├── Tools/          # 仓库级构建与辅助工具
└── .codex/         # Codex skill、reference 与自动化辅助配置
```

当前这些根目录的职责可先理解为：

- `GameUnity/`：Unity 主工程，包含运行时代码、AOT 启动代码、HotFix 程序集、资源和项目设置。
- `GameConfig/`：配置表源数据、Schema、Luban 模板和生成脚本目录。
- `GameRelease/`：发布产物输出目录。
- `Tools/`：仓库级工具和辅助脚本目录。

## Unity 主工程目录结构

当前 `GameUnity/Assets/` 下已确认存在这些重要一级目录：

```text
GameUnity/Assets/
├── AssetArt/           # 美术加工产物目录，当前已确认包含 SpriteAtlas 资源
├── BundleAssets/       # 运行时资源目录，交由 YooAsset 管理
├── DGame/              # 主工程框架代码，含 Runtime 和 Editor
├── DGame.AOT/          # 主工程 AOT 启动层代码
├── Editor/             # 通用编辑器工具目录
├── HybridCLRGenerate/  # HybridCLR 相关生成产物目录
├── Obfuz/              # 混淆相关资源与代码目录
├── Plugins/            # 第三方插件目录
├── ProjectSettings/    # 项目设置资源目录
├── Resources/          # Unity Resources 目录
├── Scenes/             # 场景目录
├── Scripts/            # 业务与 HotFix 代码目录
├── StreamingAssets/    # StreamingAssets 目录
└── YooAsset/           # YooAsset 相关目录
```

说明：

- `BundleAssets/` 是当前运行时资源主目录。
- `DGame/` 和 `DGame.AOT/` 都属于主工程代码，但职责不同。
- `Scripts/HotFix/` 是热更代码主目录。
- `Resources/` 在当前项目中存在，但运行时资源链路应优先遵循 `BundleAssets + YooAsset + GameModule.ResourceModule` 约定，而不是按普通 Unity 项目习惯直接使用 `Resources.Load()`。

## 主工程代码与程序集目录

当前主工程侧最重要的代码目录可先按下面方式理解：

| 目录 | 对应程序集/层级 | 作用 |
| --- | --- | --- |
| `GameUnity/Assets/DGame/Runtime/` | `DGame.Runtime` | 框架核心层运行时代码，负责 `RootModule`、`ModuleSystem`、基础模块、生命周期驱动和底层能力。 |
| `GameUnity/Assets/DGame/Editor/` | `DGame.Editor` | 主工程编辑器工具代码，负责菜单、设置、发布、HybridCLR、Luban、Spine 等工具链。 |
| `GameUnity/Assets/DGame.AOT/` | `DGame.AOT` | 框架应用层 AOT 代码，负责 `GameEntry`、Procedure 流程、启动器 UI、资源更新和热更程序集加载。 |
| `GameUnity/Assets/Editor/` | Unity Editor 目录 | 通用编辑器工具目录，不属于运行时链路。 |

当前 `DGame.AOT/` 内已确认存在两个关键子目录：

```text
GameUnity/Assets/DGame.AOT/
├── Launcher/   # 启动器 UI 与启动流程辅助界面
└── Procedure/  # AOT 启动流程节点
```

## HotFix 代码目录

当前热更代码位于 `GameUnity/Assets/Scripts/HotFix/`，已确认存在以下一级目录：

```text
GameUnity/Assets/Scripts/HotFix/
├── GameBattle/  # 战斗域热更程序集
├── GameLogic/   # 客户端业务主热更程序集
└── GameProto/   # 协议与配置热更程序集
```

可以先按下面方式理解三者职责：

| 目录 | 对应程序集 | 作用 |
| --- | --- | --- |
| `GameLogic/` | `GameLogic` | 业务主热更程序集，负责 UI、业务模块、数据中心、配置管理器、输入扩展、红点、文本和客户端功能逻辑。 |
| `GameProto/` | `GameProto` | 协议与配置热更程序集，负责 Luban 配置代码、`ConfigSystem` 和相关协议/配置访问入口。 |
| `GameBattle/` | `GameBattle` | 战斗域热更程序集，负责独立战斗逻辑，按当前约束应保持纯逻辑，不依赖表现层。 |

当前 `GameLogic/` 下已确认存在这些一级子目录：

```text
GameUnity/Assets/Scripts/HotFix/GameLogic/
├── Common/           # 通用业务逻辑
├── ConfigMgr/        # 配置管理器封装
├── DataCenter/       # 玩家与业务数据归属
├── Editor/           # 业务侧编辑器工具
├── GameTickWatcher/  # Tick/帧监听相关
├── IEvent/           # 事件抽象与封装
├── Module/           # 高层业务模块
├── UI/               # 窗口与界面实现
└── Utility/          # 业务辅助工具类
```

## 资源与配置相关目录

当前和资源、配置、生成链路最相关的目录可先按下面方式理解：

| 目录 | 作用 |
| --- | --- |
| `GameUnity/Assets/BundleAssets/` | 运行时资源目录，交由 YooAsset 管理。 |
| `GameUnity/Assets/AssetArt/` | 美术加工产物目录，当前已确认包含 SpriteAtlas 资源。 |
| `GameUnity/Assets/Scenes/` | 场景资源目录。 |
| `GameUnity/Assets/Resources/` | Unity Resources 目录，存在于项目中，但运行时资源链路不应默认走这里。 |
| `GameUnity/Assets/StreamingAssets/` | StreamingAssets 目录。 |
| `GameConfig/Datas/` | Excel 配置源表目录。 |
| `GameConfig/Defines/` | Luban Schema 与类型定义目录。 |
| `GameConfig/CustomTemplate/` | Luban 自定义模板目录。 |
| `GameConfig/GenerateTool_Binary/` | 二进制转表脚本目录。 |
| `GameConfig/GenerateTool_Json/` | Json 转表脚本目录。 |
| `GameConfig/Tools/` | Luban 工具本体与辅助资源目录。 |

如果需求涉及配置表、Luban、源数据、模板、生成脚本或配置消费链路，应继续读取 `references/luban-game-config.md`。

如果需求涉及运行时资源组织、BundleAssets、AssetArt、预加载标签或资源加载约束，应继续读取 `references/client-architecture.md`。

## 场景与启动入口

当前项目已确认的主启动场景位于：

- `GameUnity/Assets/Scenes/GameStart`

当前只在这里记录最小入口事实：

- 主启动场景位于 `Scenes/GameStart`
- 主工程启动代码位于 `DGame.AOT/`
- HotFix 主入口位于 `Scripts/HotFix/GameLogic/GameStart.cs`

如果需求涉及启动流程、AOT、Procedure、热更装配、GameStart、模块边界或调用链，应继续读取 `references/client-architecture.md`。

## 落位判断规则

新增或修改内容时，优先按下面规则判断落位：

- 如果代码属于主工程底层运行时能力、模块系统、生命周期驱动或全局基础设施，优先放到 `GameUnity/Assets/DGame/Runtime/`。
- 如果代码属于主工程编辑器工具、菜单、检查器、导出或发布辅助，优先放到 `GameUnity/Assets/DGame/Editor/` 或 `GameUnity/Assets/Editor/`。
- 如果代码属于 AOT 启动器、Procedure 流程、热更 DLL 加载、启动前资源更新或进入 HotFix 前总控逻辑，优先放到 `GameUnity/Assets/DGame.AOT/`。
- 如果代码属于客户端业务逻辑、UI、配置管理、红点、文本、输入扩展或高层玩法功能，优先放到 `GameUnity/Assets/Scripts/HotFix/GameLogic/`。
- 如果代码属于协议、配置代码、Luban 生成结果或配置加载入口，优先放到 `GameUnity/Assets/Scripts/HotFix/GameProto/` 或 `GameConfig/`。
- 如果代码属于战斗域纯逻辑，优先检查 `GameUnity/Assets/Scripts/HotFix/GameBattle/` 是否已经承接。
- 如果内容属于运行时资源，优先进入 `GameUnity/Assets/BundleAssets/`。
- 如果内容属于美术图集加工产物，优先检查 `GameUnity/Assets/AssetArt/`。
- 如果文件来自配置生成或工具产物，修改前先回溯到 `GameConfig/` 或相关生成脚本，不要直接改自动生成结果。

## 使用原则

处理 DGame 项目结构与落位问题时，优先遵循以下原则：

1. 先判断需求属于主工程 Runtime、主工程 AOT、Editor、HotFix、资源目录还是配置生成链路。
2. 本文件优先解决“落在哪个目录”；若和架构规则冲突，以 `references/client-architecture.md` 中的架构约束为准。
3. 如果仍然无法判断落位，先结合 `references/client-architecture.md` 的程序集边界和启动链路，再决定具体目录。
4. 如果需求涉及配置表、Luban、模板、生成脚本或配置消费链路，转到 `references/luban-game-config.md`。
