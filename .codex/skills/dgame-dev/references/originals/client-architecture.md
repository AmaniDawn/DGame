# DGame 客户端架构指引

当需求涉及客户端分层、启动流程、模块职责、UI 架构、HotFix 边界或功能应该落在哪一层时，先阅读本文件。

本文档先记录当前已确认的仓库事实，后续再逐步补充程序集依赖、资源加载链路、启动 Procedure、输入系统和常见功能落位样例。

## 目录导航

- [DGame 客户端架构指引](#dgame-客户端架构指引)
  - [目录导航](#目录导航)
  - [整体分层概览](#整体分层概览)
  - [程序集划分与代码归属](#程序集划分与代码归属)
  - [客户端启动链路](#客户端启动链路)
    - [Unity 根驱动](#unity-根驱动)
    - [AOT 启动入口](#aot-启动入口)
    - [Procedure 流程层](#procedure-流程层)
    - [热更装配入口](#热更装配入口)
    - [游戏开始后的默认行为](#游戏开始后的默认行为)
  - [核心模块列表](#核心模块列表)
    - [主工程核心模块](#主工程核心模块)
    - [热更工程核心模块](#热更工程核心模块)
  - [运行时模块系统](#运行时模块系统)
    - [ModuleSystem 的职责](#modulesystem-的职责)
    - [GameModule 的职责](#gamemodule-的职责)
    - [当前已确认的常用模块](#当前已确认的常用模块)
  - [HotFix 业务层结构](#hotfix-业务层结构)
    - [GameLogic 目录职责](#gamelogic-目录职责)
    - [当前已确认的业务模式](#当前已确认的业务模式)
  - [配置与数据访问协作](#配置与数据访问协作)
  - [使用原则](#使用原则)
  - [常见落位建议](#常见落位建议)
  - [资源目录组织](#资源目录组织)
    - [`Assets/BundleAssets`](#assetsbundleassets)
    - [`Assets/AssetArt`](#assetsassetart)

## 整体分层概览

当前仓库中的客户端代码，推荐先按“自上而下依赖”的方式理解：

```text
┌─────────────────────────────────────────────┐
│               游戏业务层 (HotFix)             │
│  GameLogic / GameProto / GameBattle         │
│  ↑ 热更代码、业务逻辑、高频变更                  │
└─────────────────────────────────────────────┘
                     │
                     │ 依赖
                     v
┌─────────────────────────────────────────────┐
│               框架应用层 (Main)               │
│  DGame.AOT / GameEntry / Procedure/         │
│  Launcher / LoadAssemblyProcedure           │
│  ↑ 启动器、流程管理、热更新加载、进入HotFix       │
└─────────────────────────────────────────────┘
                     │
                     │ 依赖
                     v
┌─────────────────────────────────────────────┐
│               框架核心层 (DGame)              │
│  DGame.Runtime / RootModule / ModuleSystem  │
│  ResourceModule / LocalizationModule        │
│  GameEvent / Timer / ObjectPool             │
│  ↑ DGame.Runtime程序集、核心底层逻辑           │
└─────────────────────────────────────────────┘
                     │
                     │ 依赖
                     v
┌─────────────────────────────────────────────┐
│         基础设施层 (Unity + 第三方)            │
│  UnityEngine / UGUI / TextAsset /Luban      │
│  YooAsset / UniTask / DOTween / HybridCLR   │
│  ↑ 引擎能力、资源系统、异步、热更与外部库支持      │
└─────────────────────────────────────────────┘
```

可以把这四层先理解为：

- 游戏业务层（HotFix）：承载具体玩法、UI、配置消费和热更业务逻辑。
- 框架应用层（Main）：承载非热更启动代码、流程状态机、补丁与程序集加载、启动器 UI，以及进入 HotFix 前的总编排。
- 框架核心层（DGame）：承载模块系统、生命周期驱动、基础运行时模块和底层框架能力。
- 基础设施层（Unity + 第三方）：承载 Unity 引擎能力、资源系统、异步、热更和外部库。

依赖方向要求保持单向：

- HotFix 可以依赖 Main、DGame 和基础设施。
- Main 可以依赖 DGame 和基础设施。
- DGame 可以依赖 Unity 和第三方基础设施。
- 不应反向让 DGame 直接依赖具体 HotFix 业务实现。

这是严格架构约束，不是建议项：

- 四层架构只允许单向向下依赖，不允许跨层反向引用。
- 下层不能引用上层程序集或上层内部类型。
- 主工程运行时代码严禁反向依赖 HotFix 代码内部类型。
- `DGame.AOT`、`DGame.Runtime` 或其他主工程运行时代码，不应直接引用 `GameLogic`、`GameProto`、`GameBattle` 中的业务实现类型。
- Main 层进入 HotFix 时，应通过程序集加载、反射入口、配置约定或通用接口边界完成，而不是在主工程里直接写死对 HotFix 内部类型的编译期引用。
- `Editor` 代码不属于运行时四层依赖链。若为了开发工具、检查器、生成器、菜单或自动化流程需要，可以按工具用途引用 HotFix 类型。
- 但 `Editor` 对 HotFix 的引用只能停留在编辑器域，不能泄漏到 `Runtime`、`DGame.AOT` 或任何发布时代码路径。

热更程序集之间的当前约束也应明确遵守：

- `GameLogic` 可以依赖 `GameProto` 和 `GameBattle`。
- `GameBattle` 可以依赖 `GameProto`。
- `GameBattle` 不应依赖 `GameLogic`。
- 热更代码不应引用主工程中的 `internal` 类型；跨程序集访问应通过公开类型、公开接口或明确的边界层完成。
- `GameBattle` 不能依赖 Unity 引擎相关代码，只能编写纯逻辑代码。
- `GameBattle` 不应依赖 UI、资源加载、场景对象、动画、特效、音频或其他任何表现层代码。

## 程序集划分与代码归属

当前项目已确认的客户端相关程序集与代码归属如下：

| 程序集 | 路径 | 热更 | 职责 |
| --- | --- | --- | --- |
| `DGame.Runtime` | `GameUnity/Assets/DGame/Runtime/` | 否 | 框架核心层运行时代码，负责 `RootModule`、`ModuleSystem`、资源、输入、本地化、对象池等基础模块与生命周期驱动。 |
| `DGame.Editor` | `GameUnity/Assets/DGame/Editor/` | 否 | 编辑器工具代码，负责菜单、设置、发布、HybridCLR、Luban、Spine 等开发期工具链。 |
| `DGame.AOT` | `GameUnity/Assets/DGame.AOT/` | 否 | 框架应用层主工程代码，负责 `GameEntry`、Procedure 流程、启动器 UI、资源更新、热更程序集加载，以及进入 HotFix 前的总控逻辑。 |
| `GameLogic` | `GameUnity/Assets/Scripts/HotFix/GameLogic/` | 是 | 游戏业务层主程序集，负责 UI、业务模块、配置管理器、输入扩展、文本、红点、数据中心和客户端功能逻辑。 |
| `GameProto` | `GameUnity/Assets/Scripts/HotFix/GameProto/` | 是 | 游戏业务层的协议与配置程序集，负责 Luban 配置代码、`ConfigSystem`、协议或配置访问入口。 |
| `GameBattle` | `GameUnity/Assets/Scripts/HotFix/GameBattle/` | 是 | 游戏业务层的战斗相关热更程序集，负责独立战斗域逻辑，纯逻辑代码。 |

可以先按下面规则判断归属：

- 如果能力需要常驻于 Unity 主工程、并且不依赖具体业务语义，优先落到 `DGame.Runtime`。
- 如果能力是非热更启动器、流程状态管理、资源更新、程序集加载或进入 HotFix 前的总控逻辑，优先落到 `DGame.AOT`。
- 如果能力只在编辑器中运行，优先落到 `DGame.Editor`。
- 如果能力是客户端业务逻辑、UI、配置封装、红点或输入扩展，优先落到 `GameLogic`。
- 如果能力是配置生成结果或配置系统入口，优先落到 `GameProto`。
- 如果能力是战斗域内逻辑，纯逻辑代码，优先检查 `GameBattle` 是否已经承接。

## 客户端启动链路

### Unity 根驱动

当前客户端运行时根节点是 `DGame.RootModule`，位于 `GameUnity/Assets/DGame/Runtime/Module/RootModule.cs`。

当前已确认 `RootModule` 负责：

- 初始化内存池严格检查配置。
- 初始化字符串、日志、Json 等 helper。
- 设置帧率、游戏速度、后台运行和休眠行为。
- 订阅低内存回调。
- 在 `Update()` 中调用 `ModuleSystem.Update(...)` 驱动所有运行时模块更新。

这意味着 `RootModule` 是 Runtime 层的 Unity 生命周期入口，而不是业务层入口。

### AOT 启动入口

当前非热更启动入口位于 `GameUnity/Assets/DGame.AOT/GameEntry.cs`，类型名为 `GameEntry`。

当前已确认 `GameEntry.Awake()` 会执行以下行为：

1. 初始化 `IMonoDriver`。
2. 初始化 `IResourceModule`。
3. 初始化 `IFsmModule`。
4. 调用 `Settings.ProcedureSettings.StartProcedure().Forget()` 启动流程状态机。
5. 对 `GameEntry` 自身执行 `DontDestroyOnLoad(...)`。

这说明项目里的 Main 层并不是 `GameModule` 或 `UIModule`，而是 `DGame.AOT` 中由 `GameEntry + Procedure` 组成的非热更启动层。

### Procedure 流程层

当前 `DGame.AOT/Procedure/` 下已确认存在以下流程节点：

- `LaunchProcedure`：启动主流程的早期入口，用于承接从启动展示到后续初始化链路的切换。
- `SplashProcedure`：启动展示阶段，用于进入正式初始化前的开场或占位流程。
- `InitPackageProcedure`：初始化 YooAsset 包与更新配置，准备后续资源系统可用状态。
- `InitResourceProcedure`：初始化资源版本与清单，检查资源状态并决定是否进入更新流程。
- `CreateDownloaderProcedure`：创建补丁下载器，统计待下载文件数和总大小，并决定是否需要下载。
- `DownloadFileProcedure`：执行补丁下载，更新下载进度、速度、剩余时间，并处理下载失败重试。
- `DownloadOverProcedure`：下载完成后的收口节点，记录版本信息，并决定进入清缓存或预加载阶段。
- `ClearCacheProcedure`：清理旧缓存资源，为后续资源使用和版本切换做收尾处理。
- `PreloadProcedure`：执行进入游戏前的预加载，提前准备运行期需要的关键资源或配置。
- `LoadAssemblyProcedure`：加载热更 DLL 与 AOT 元数据，并在完成后桥接进入 HotFix 主入口。
- `StartGameProcedure`：AOT 侧启动流程的最后一步，关闭启动器 UI，完成进入游戏前的主工程收口。

从这些命名和当前实现可以确认，`DGame.AOT` 负责：

- 启动前置流程管理。
- YooAsset 包初始化与资源更新。
- 下载器创建与补丁下载。
- 预加载。
- 热更 DLL 与 AOT 元数据加载。
- 进入 HotFix 前的最后收口流程。

按当前已确认的主线启动顺序，可先理解为：

1. `LaunchProcedure`
2. `SplashProcedure`
3. `InitPackageProcedure`
4. `InitResourceProcedure`
5. 若需要资源更新：
   `CreateDownloaderProcedure -> DownloadFileProcedure -> DownloadOverProcedure -> ClearCacheProcedure`
6. 若不需要资源更新，可直接进入 `PreloadProcedure`
7. `PreloadProcedure`
8. `LoadAssemblyProcedure`
9. `StartGameProcedure`

### 热更装配入口

当前热更入口位于 `GameUnity/Assets/Scripts/HotFix/GameLogic/GameStart.cs`，类型名为 `GameStart`。

当前已确认 `LoadAssemblyProcedure` 会在程序集准备完成后：

1. 切换到 `StartGameProcedure`。
2. 从主业务程序集里查找 `GameStart` 类型。
3. 反射调用 `GameStart.Entrance(object[] objects)`。
4. 把热更程序集列表作为参数传入。

随后 `GameStart.Entrance(object[] objects)` 会执行以下行为：

1. 保存热更程序集列表。
2. 初始化 `GameLogic.GameEventLauncher` 和 `GameBattle.GameEventLauncher`。
3. 绑定销毁监听。
4. 初始化语言设置。
5. 调用 `StartGame()` 进入业务启动逻辑。

说明：

- `GameStart` 是 HotFix 主业务入口。
- `LoadAssemblyProcedure` 才是“从 Main 层进入 HotFix 层”的桥接点。
- Runtime 层负责基础驱动，AOT Main 层负责启动编排与热更加载，HotFix 层负责实际业务逻辑。

### 游戏开始后的默认行为

当前 `GameStart.StartGame()` 已确认直接调用：

```csharp
GameModule.UIModule.ShowWindow<MainWindow>();
```

这说明当前仓库里的完整主链路应先理解为：

```text
RootModule
  -> GameEntry
  -> Procedure 流程
  -> LoadAssemblyProcedure
  -> 反射调用 GameStart.Entrance
  -> GameModule.UIModule.ShowWindow<MainWindow>()
```

也就是说，Procedure 栈并没有缺失，而是在 `DGame.AOT` 层先完成，再交给 HotFix 层继续。

## 核心模块列表

在阅读 `ModuleSystem`、`GameModule` 或判断功能应落在哪一层前，可以先看 `GameModule` 当前实际聚合的模块分类。

### 主工程核心模块

| 模块分类 | 对应类型 | 来源层级 | 职责 |
| --- | --- | --- | --- |
| 基础模块 | `RootModule` | `DGame.Runtime` | Unity 运行时根驱动，负责主工程运行环境、帧驱动和基础 helper 初始化。 |
| 有限状态机模块 | `IFsmModule` | `DGame.Runtime` | 状态机能力，AOT Procedure 流程依赖该模块驱动。 |
| 敏感词模块 | `ISensitiveWordModule` | `DGame.Runtime` | 敏感词检测与过滤能力。 |
| 动画模块 | `IAnimModule` | `DGame.Runtime` | 动画相关基础能力。 |
| 资源管理模块 | `IResourceModule` | `DGame.Runtime` | 资源加载、包初始化、更新、下载器和资源访问入口。 |
| 音频模块 | `IAudioModule` | `DGame.Runtime` | 音频播放与管理能力。 |
| 场景管理模块 | `ISceneModule` | `DGame.Runtime` | 场景切换与场景相关基础能力。 |
| 计时器模块 | `IGameTimerModule` | `DGame.Runtime` | 游戏计时器与定时任务能力。 |
| 主工程输入模块 | `DGame.IInputModule` | `DGame.Runtime` | 底层输入能力。 |
| 多语言模块 | `ILocalizationModule` | `DGame.Runtime` | 语言状态、本地化切换和本地化基础能力。 |
| 游戏对象对象池模块 | `IGameObjectPoolModule` | `DGame.Runtime` | 游戏对象池能力与复用管理。 |

### 热更工程核心模块

| 模块分类 | 对应类型 | 来源层级 | 职责 |
| --- | --- | --- | --- |
| HotFix 业务门面 | `GameModule` | `GameLogic` | HotFix 业务层统一访问入口，聚合主工程模块并缓存常用模块引用。 |
| 输入模块 | `GameLogic.IInputModule` | `GameLogic` | 业务输入层，在主工程输入基础上封装业务输入上下文与组件能力。 |
| UI 模块 | `UIModule` | `GameLogic` | HotFix UI 总模块，负责窗口栈、窗口生命周期、异步显示和 UI 根节点管理。 |
| 红点模块 | `RedDotModule` | `GameLogic` | HotFix 红点系统总模块，负责红点树节点管理、数值更新、聚合通知，以及与红点 UI 组件的协作入口。 |

说明：

- 上表按 `GameModule.cs` 当前实际暴露的属性分类整理。
- `GameModule` 当前实际聚合的属性包括：
  `RootModule`、`FsmModule`、`SensitiveWordModule`、`AnimModule`、`ResourceModule`、`AudioModule`、`SceneModule`、`GameTimerModule`、`InputModule`、`Input`、`LocalizationModule`、`GameObjectPool`、`UIModule`。
- `ConfigSystem`、`TextConfigMgr`、`SingletonSystem` 等属于 HotFix 侧高层能力；其中 `RedDotModule` 当前已经作为常用入口被 `GameModule` 聚合暴露。

## 运行时模块系统

### ModuleSystem 的职责

当前模块总入口位于 `GameUnity/Assets/DGame/Runtime/Core/ModuleSystem/ModuleSystem.cs`。

当前已确认 `ModuleSystem` 负责：

- 根据接口类型自动推导并实例化对应模块类型。
- 维护模块映射表。
- 按优先级注册模块。
- 对实现 `IUpdateModule` 的模块建立更新执行列表。
- 在 RootModule 的每帧更新中统一调用模块的 `Update`。
- 在销毁时按逆序清理模块、内存池和缓存内存。

当前接口到实现的约定是：

- 调用 `ModuleSystem.GetModule<IModuleX>()`
- 系统会按“接口名去掉前缀 `I`”推导实现类
- 例如 `ILocalizationModule` 对应 `LocalizationModule`

这意味着新增 Runtime 模块时，优先遵循“接口 + 同名实现类”的现有约定，而不是另外发明注册方式。

### GameModule 的职责

当前业务层常用的模块聚合入口位于 `GameUnity/Assets/Scripts/HotFix/GameLogic/GameModule.cs`。

`GameModule` 不是 Main 层，也不是底层模块系统本身，而是 HotFix 业务层对常用 Runtime 模块的静态聚合访问门面。

当前已确认它封装了这些常见能力：

- `RootModule`
- `IFsmModule`
- `ISensitiveWordModule`
- `IAnimModule`
- `IResourceModule`
- `IAudioModule`
- `ISceneModule`
- `IGameTimerModule`
- `DGame.IInputModule`
- `GameLogic.IInputModule`
- `ILocalizationModule`
- `IGameObjectPoolModule`
- `UIModule`

实际开发中，如果 HotFix 代码需要访问底层模块，优先先看 `GameModule` 是否已经提供统一入口；不要在业务代码里到处重复写 `ModuleSystem.GetModule<T>()`。

## HotFix 业务层结构

### GameLogic 目录职责

当前 `GameUnity/Assets/Scripts/HotFix/GameLogic/` 下已确认的一级目录包括：

- `Common`
- `ConfigMgr`
- `DataCenter`
- `Editor`
- `GameTickWatcher`
- `IEvent`
- `Module`
- `UI`
- `Utility`

可先按下面方式理解：

- `Common`：通用业务侧逻辑。
- `ConfigMgr`：对生成配置表的业务封装层。
- `DataCenter`：玩家数据、业务数据归属和状态管理。
- `Editor`：只在编辑器使用的业务配套工具。
- `GameTickWatcher`：帧、Tick 或周期监听相关逻辑。
- `IEvent`：事件抽象与封装。
- `Module`：高层业务模块，例如输入、UI、红点、文本、单例体系等。
- `UI`：具体窗口、控制器和界面实现。
- `Utility`：业务侧辅助工具类。

### 当前已确认的业务模式

从现有代码可以先确认几个已经被使用的模式：

- 配置访问不会只停留在 `TbXXX`，而是额外提供 `ConfigMgr` 封装，例如 `ModelConfigMgr`、`SoundConfigMgr`。
- 业务层存在自己的单例体系，例如 `Singleton<T>`、`SingletonSystem`。
- UI 不是简单的 MonoBehaviour 面板集合，而是有独立窗口栈、层级和异步加载流程。
- 文本、多语言、红点、输入扩展都已经有自己的模块目录，不建议把类似逻辑散落到任意窗口脚本里。
- 红点系统当前采用树状节点组织方式，节点定义、编辑器配置和生成代码集中放在 `GameLogic/Module/RedDotModule/`，业务层优先复用现有入口与节点定义。

## 配置与数据访问协作

客户端配置系统位于 `GameProto` 层，入口是 `ConfigSystem`，具体生成表位于 `LubanConfig/`。

业务层当前并不鼓励到处直接访问 `ConfigSystem.Instance.Tables`，而是已经存在再封装趋势，例如：

- `ModelConfigMgr`
- `SoundConfigMgr`
- `TextConfigMgr`

推荐理解为：

- `GameProto` 负责“生成表和底层读取”。
- `GameLogic/ConfigMgr` 负责“业务友好的读取接口”。

如果一个功能需要频繁访问配置，优先新增或扩展 `ConfigMgr`，不要把 `TbXXX` 访问散落到多个 UI 或模块里。

## 使用原则

处理客户端架构相关任务时，优先遵循以下原则：

1. 先判断需求属于 Runtime、HotFix、Editor 还是生成产物。
2. Runtime 层改“底层能力”，HotFix 层改“业务行为”，不要混写职责。
3. 四层关系严格遵守单向向下依赖，上层不能被下层引用。
4. 主工程运行时代码严禁反向依赖 HotFix 内部类型；如果需要进入 HotFix，只能通过入口约定、反射、资源或通用边界完成。
5. 热更代码不能引用主工程的 `internal` 类型；跨程序集协作时优先使用公开接口和稳定入口。
6. `GameLogic` 可依赖 `GameProto` 和 `GameBattle`，`GameBattle` 仅可依赖 `GameProto`，不要反向建立热更程序集耦合。
7. `GameBattle` 只写纯逻辑，不依赖 Unity 引擎相关代码，也不承载任何表现层逻辑。
8. 访问底层模块时，先看 `GameModule` 是否已经提供统一入口。
9. 新增 UI 时优先接入 `UIModule` 和 `UIWindow`，不要绕开现有窗口栈。
10. 新增配置访问逻辑时优先补 `ConfigMgr`，不要直接把 `TbXXX` 到处暴露给业务代码。
11. 输入、多语言、红点、文本等能力已经有现成模块时，先复用再扩展。
12. 遇到生成文件、`Gen` 目录或 `LubanConfig` 目录时，先回溯源头，不直接手改生成产物。
13. `Editor` 若引用 HotFix 类型，必须确保引用只用于编辑器工具链，不进入运行时程序集依赖。

## 常见落位建议

下面是当前仓库下较安全的默认落位方式：

- 新的底层 Unity 运行时模块：`GameUnity/Assets/DGame/Runtime/Module/`
- 新的底层框架能力或系统：`GameUnity/Assets/DGame/Runtime/Core/`
- 新的业务配置管理器：`GameUnity/Assets/Scripts/HotFix/GameLogic/ConfigMgr/`
- 新的业务模块：`GameUnity/Assets/Scripts/HotFix/GameLogic/Module/`
- 新的业务窗口：`GameUnity/Assets/Scripts/HotFix/GameLogic/UI/`
- 新的通用 UI 组件扩展：先检查 `GameLogic/Module/UIModule/Expansion/`
- 新的编辑器工具：`GameUnity/Assets/DGame/Editor/` 或 `GameLogic/Editor/`

如果需求同时跨越 Runtime、HotFix 和配置层，输出时应明确标出跨层影响，避免只改一层造成初始化链路或程序集依赖不一致。

## 资源目录组织

当前客户端资源目录可先重点关注 `Assets/BundleAssets` 和 `Assets/AssetArt`。前者更偏运行时加载资源组织，后者更偏美术资源加工产物组织。

### `Assets/BundleAssets`

```text
GameUnity/Assets/BundleAssets/
├── Actor/                                    # 角色相关资源目录
│   ├── FrameSprite/                          # 角色序列帧表现资源
│   └── Spine/                                # 角色 Spine 表现资源
├── Audios/                                   # 音频资源目录
├── Configs/                                  # 运行时配置数据资源目录
├── DLL/                                      # 热更程序集与相关 .dll.bytes 资源目录
├── Effects/                                  # 特效资源目录 
├── Fonts/                                    # 字体资源目录
├── FrameSprite/                              # 通用序列帧资源目录 
├── Materials/                                # 材质资源目录，当前主要包含 UI 材质   
├── Prefabs/                                  # 通用预制体目录 
│   └── FrameConfig/                          # 帧配置相关预制体目录       
├── Scenes/                                   # 场景资源目录  
├── UI/                                       # UI Prefab 目录  
└── UIRaw/                                    # UI 原始资源目录
    ├── Atlas/                                # UI 图集源资源目录  
    │   ├── Background/                       # 背景类图集源图片
    │   └── Common/                           # 公共 UI 图集源图片
    └── Raw/                                  # 不走图集或保持原始形态使用的 UI 原图目录     
```

### `Assets/AssetArt`

```text
GameUnity/Assets/AssetArt/
└── Atlas/                                    # Unity SpriteAtlas 资源目录
    └── Atlas_Common.spriteatlasv2            # 当前已确认的公共图集产物        
```

当前可先这样理解两者关系：

- `BundleAssets/UIRaw/Atlas/` 更偏图集源图片输入目录。
- `AssetArt/Atlas/` 更偏 Unity SpriteAtlas 产物目录。
- 如果需求涉及 UI 图片来源、图集合图或图集资源引用关系，通常需要同时检查这两个目录。

重要约束：

- 所有运行时会使用到的资源，必须放在 `Assets/BundleAssets/` 目录下，由 YooAsset 统一管理。
- 运行时禁止使用 `Resources.Load()`、`Resources.LoadAsync()` 或其他直接从 `Resources` 体系加载资源的方式。
- 运行时资源加载必须统一通过 `GameModule.ResourceModule` 或其下游封装执行。
- `AssetArt` 更适合作为美术加工产物或图集产物目录理解；若资源最终要参与运行时加载，仍应确认其是否进入 `BundleAssets` 管理链路。
- 上述限制只针对运行时资源链路；Editor 工具、构建脚本、导出流程等非运行时场景可按工具需求单独处理。

资源标签约定：

- `PRELOAD`：启动时预加载资源，当前主要用于配置数据、公共 UI 等启动后高频或基础依赖资源。
- 资源 `Location`（地址）等于资源文件名，不包含路径和扩展名。
- YooAsset 通过 `AssetBundleCollector` 自动收集资源，处理资源组织时应遵循当前收集规则，默认开启可寻址模式，不要自行发明额外的运行时寻址约定。
