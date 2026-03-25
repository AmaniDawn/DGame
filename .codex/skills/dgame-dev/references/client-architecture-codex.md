# DGame 客户端架构指引（精简版）

当需求涉及客户端分层、启动流程、模块职责、UI 架构、HotFix 边界或代码落位时，先读本文件。

目标：让 Codex 用最短阅读成本理解当前仓库的客户端分层、依赖边界、启动链路、模块职责与默认落位规则。原始细节和补充说明仍以 `references/originals/client-architecture.md` 为准。

## 一图理解

```text
HotFix 业务层
  GameLogic / GameProto / GameBattle
  负责玩法、UI、配置消费、热更业务逻辑
          ↓ 仅允许向下依赖
Main 应用层
  DGame.AOT / GameEntry / Procedure / Launcher / LoadAssemblyProcedure
  负责主工程启动编排、资源更新、热更加载、进入 HotFix 前总控
          ↓
DGame 核心层
  DGame.Runtime / RootModule / ModuleSystem / Resource / Localization / Timer / Pool
  负责运行时模块系统、生命周期驱动和通用底层能力
          ↓
基础设施层
  Unity / YooAsset / UniTask / DOTween / HybridCLR / Luban
  负责引擎、资源、异步、热更和外部库支持
```

## 核心约束

- 四层架构只允许单向向下依赖，不允许反向引用。
- `HotFix` 可依赖 `Main`、`DGame`、基础设施。
- `Main` 可依赖 `DGame`、基础设施。
- `DGame` 可依赖 Unity 和第三方库。
- `DGame.Runtime`、`DGame.AOT` 等主工程运行时代码，禁止直接引用 `GameLogic`、`GameProto`、`GameBattle` 的业务实现类型。
- Main 进入 HotFix 只能通过程序集加载、反射入口、配置约定或通用接口边界完成，不能写死编译期引用。
- `Editor` 不属于运行时四层依赖链；可按工具用途引用 HotFix，但引用不得泄漏到任何 Runtime/AOT/发布路径。

### 热更程序集约束

- `GameLogic` 可依赖 `GameProto`、`GameBattle`。
- `GameBattle` 可依赖 `GameProto`，不能依赖 `GameLogic`。
- 热更代码不能引用主工程 `internal` 类型，跨程序集协作应使用公开类型、接口或稳定边界。
- `GameBattle` 只能写纯逻辑，不能依赖 Unity 引擎、UI、资源加载、场景对象、动画、特效、音频等表现层代码。

## 程序集与归属

| 程序集 | 路径 | 热更 | 主要职责 |
| --- | --- | --- | --- |
| `DGame.Runtime` | `GameUnity/Assets/DGame/Runtime/` | 否 | 框架核心运行时代码，承载 `RootModule`、`ModuleSystem`、资源、输入、本地化、对象池等底层模块。 |
| `DGame.Editor` | `GameUnity/Assets/DGame/Editor/` | 否 | 编辑器工具链，承载菜单、设置、发布、HybridCLR、Luban、Spine 等开发期工具。 |
| `DGame.AOT` | `GameUnity/Assets/DGame.AOT/` | 否 | Main 层主工程代码，承载 `GameEntry`、Procedure、启动器 UI、资源更新、热更程序集加载和进入 HotFix 前总控。 |
| `GameLogic` | `GameUnity/Assets/Scripts/HotFix/GameLogic/` | 是 | 主业务热更程序集，承载 UI、业务模块、配置管理器、输入扩展、文本、红点、数据中心等客户端逻辑。 |
| `GameProto` | `GameUnity/Assets/Scripts/HotFix/GameProto/` | 是 | 协议与配置程序集，承载 Luban 配置代码、`ConfigSystem` 及配置访问入口。 |
| `GameBattle` | `GameUnity/Assets/Scripts/HotFix/GameBattle/` | 是 | 战斗域热更程序集，只承载独立纯逻辑。 |

### 默认归属判断

- 常驻主工程、无具体业务语义的底层能力：`DGame.Runtime`
- 非热更启动器、流程状态、资源更新、程序集加载、进入 HotFix 前总控：`DGame.AOT`
- 只在编辑器运行：`DGame.Editor`
- 客户端业务逻辑、UI、配置封装、红点、输入扩展：`GameLogic`
- 配置生成结果或配置系统入口：`GameProto`
- 战斗域纯逻辑：`GameBattle`

## 启动链路

### 1. Runtime 根驱动

运行时 Unity 根节点是 `DGame.RootModule`，位于 `GameUnity/Assets/DGame/Runtime/Module/RootModule.cs`。

职责：

- 初始化内存池严格检查配置。
- 初始化字符串、日志、Json 等 helper。
- 设置帧率、游戏速度、后台运行和休眠行为。
- 订阅低内存回调。
- 在 `Update()` 中调用 `ModuleSystem.Update(...)` 驱动所有运行时模块。

结论：`RootModule` 是 Runtime 层 Unity 生命周期入口，不是业务入口。

### 2. AOT 启动入口

非热更启动入口是 `GameUnity/Assets/DGame.AOT/GameEntry.cs` 中的 `GameEntry`。

`GameEntry.Awake()` 已确认会：

1. 初始化 `IMonoDriver`
2. 初始化 `IResourceModule`
3. 初始化 `IFsmModule`
4. 调用 `Settings.ProcedureSettings.StartProcedure().Forget()` 启动流程状态机
5. 对自身执行 `DontDestroyOnLoad(...)`

结论：Main 层启动核心是 `DGame.AOT` 中的 `GameEntry + Procedure`，不是 `GameModule`。

### 3. Procedure 流程层

`DGame.AOT/Procedure/` 当前确认的主链路节点：

1. `LaunchProcedure`
2. `SplashProcedure`
3. `InitPackageProcedure`
4. `InitResourceProcedure`
5. 如需更新：
   `CreateDownloaderProcedure -> DownloadFileProcedure -> DownloadOverProcedure -> ClearCacheProcedure`
6. `PreloadProcedure`
7. `LoadAssemblyProcedure`
8. `StartGameProcedure`

`DGame.AOT` 负责：

- 启动前置流程管理
- YooAsset 包初始化与资源更新
- 下载器创建与补丁下载
- 预加载
- 热更 DLL 与 AOT 元数据加载
- 进入 HotFix 前最后收口

### 4. HotFix 装配入口

热更入口位于 `GameUnity/Assets/Scripts/HotFix/GameLogic/GameStart.cs`，类型名 `GameStart`。

`LoadAssemblyProcedure` 会在程序集准备完成后：

1. 切换到 `StartGameProcedure`
2. 从主业务程序集查找 `GameStart`
3. 反射调用 `GameStart.Entrance(object[] objects)`
4. 把热更程序集列表作为参数传入

`GameStart.Entrance(object[] objects)` 会：

1. 保存热更程序集列表
2. 初始化 `GameLogic.GameEventLauncher` 和 `GameBattle.GameEventLauncher`
3. 绑定销毁监听
4. 初始化语言设置
5. 调用 `StartGame()`

结论：

- `GameStart` 是 HotFix 主业务入口。
- `LoadAssemblyProcedure` 是 Main 进入 HotFix 的桥接点。
- Runtime 负责基础驱动，AOT 负责启动编排与热更加载，HotFix 负责实际业务。

### 5. 默认进入游戏

`GameStart.StartGame()` 当前直接调用：

```csharp
GameModule.UIModule.ShowWindow<MainWindow>();
```

完整主链路应理解为：

```text
RootModule
  -> GameEntry
  -> Procedure
  -> LoadAssemblyProcedure
  -> 反射调用 GameStart.Entrance
  -> GameModule.UIModule.ShowWindow<MainWindow>()
```

## 模块系统

### Runtime 模块系统

模块总入口：`GameUnity/Assets/DGame/Runtime/Core/ModuleSystem/ModuleSystem.cs`

`ModuleSystem` 负责：

- 根据接口类型自动推导并实例化模块实现
- 维护模块映射表
- 按优先级注册模块
- 为 `IUpdateModule` 建立更新列表
- 在 `RootModule.Update` 中统一驱动模块更新
- 销毁时逆序清理模块、内存池和缓存内存

接口到实现的现有约定：

- 使用 `ModuleSystem.GetModule<IModuleX>()`
- 系统按“接口名去掉 `I` 前缀”推导实现类
- 例如 `ILocalizationModule -> LocalizationModule`

结论：新增 Runtime 模块优先遵循“接口 + 同名实现类”约定，不要发明额外注册方式。

### HotFix 业务门面

业务常用聚合入口：`GameUnity/Assets/Scripts/HotFix/GameLogic/GameModule.cs`

`GameModule` 的定位：

- 不是 Main 层
- 不是底层模块系统本身
- 是 HotFix 业务层对常用 Runtime/HotFix 模块的静态聚合访问门面

当前常见聚合能力包括：

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
- `RedDotModule`

结论：HotFix 访问底层模块时，优先先看 `GameModule` 是否已有统一入口，避免在业务代码里到处直接写 `ModuleSystem.GetModule<T>()`。

## HotFix 业务层结构

`GameUnity/Assets/Scripts/HotFix/GameLogic/` 当前已确认的一级目录：

- `Common`
- `ConfigMgr`
- `DataCenter`
- `Editor`
- `GameTickWatcher`
- `IEvent`
- `Module`
- `UI`
- `Utility`

可按下述职责理解：

- `Common`：通用业务逻辑
- `ConfigMgr`：面向业务的配置封装层
- `DataCenter`：玩家数据和业务状态管理
- `Editor`：仅编辑器使用的业务工具
- `GameTickWatcher`：帧/Tick/周期监听逻辑
- `IEvent`：事件抽象与封装
- `Module`：高层业务模块，例如输入、UI、红点、文本、单例体系
- `UI`：窗口、控制器和界面实现
- `Utility`：业务侧工具类

### 已确认的业务模式

- 配置访问通常不会只停留在 `TbXXX`，而会通过 `ConfigMgr` 再封装，例如 `ModelConfigMgr`、`SoundConfigMgr`。
- 业务层有自己的单例体系，如 `Singleton<T>`、`SingletonSystem`。
- UI 不是简单 MonoBehaviour 面板集合，而是有独立窗口栈、层级和异步加载流程。
- 文本、多语言、红点、输入扩展都有独立模块，不应散落到窗口脚本。
- 红点系统采用树状节点组织，节点定义、编辑器配置和生成代码集中在 `GameLogic/Module/RedDotModule/`，优先复用现有入口与节点定义。

## 配置协作

客户端配置系统位于 `GameProto`，入口是 `ConfigSystem`，生成表位于 `LubanConfig/`。

推荐协作方式：

- `GameProto` 负责生成表与底层读取
- `GameLogic/ConfigMgr` 负责业务友好的读取接口

当前已有封装趋势：

- `ModelConfigMgr`
- `SoundConfigMgr`
- `TextConfigMgr`

结论：功能若需频繁访问配置，优先新增或扩展 `ConfigMgr`，不要把 `ConfigSystem.Instance.Tables` 或 `TbXXX` 访问散落到多个 UI/模块里。

## 使用原则

1. 先判断需求属于 Runtime、HotFix、Editor 还是生成产物。
2. Runtime 改底层能力，HotFix 改业务行为，不要混写职责。
3. 严格遵守四层单向依赖。
4. 主工程进入 HotFix 只能通过入口约定、反射、资源或通用边界完成。
5. 热更代码不能引用主工程 `internal` 类型；跨程序集优先使用公开接口和稳定入口。
6. `GameLogic -> GameProto / GameBattle` 可以，`GameBattle -> GameLogic` 不可以。
7. `GameBattle` 只写纯逻辑，不承载任何表现层逻辑。
8. 访问底层模块先看 `GameModule`。
9. 新增 UI 优先接入 `UIModule` 和 `UIWindow`，不要绕开现有窗口栈。
10. 新增配置访问优先补 `ConfigMgr`，不要直接向业务层散落 `TbXXX`。
11. 输入、多语言、红点、文本等已有模块优先复用再扩展。
12. 遇到生成文件、`Gen` 或 `LubanConfig`，先回溯源头，不直接手改生成产物。
13. `Editor` 若引用 HotFix 类型，必须确保引用只停留在编辑器工具链。

## 常见落位

- 底层 Unity 运行时模块：`GameUnity/Assets/DGame/Runtime/Module/`
- 底层框架能力或系统：`GameUnity/Assets/DGame/Runtime/Core/`
- 业务配置管理器：`GameUnity/Assets/Scripts/HotFix/GameLogic/ConfigMgr/`
- 业务模块：`GameUnity/Assets/Scripts/HotFix/GameLogic/Module/`
- 业务窗口：`GameUnity/Assets/Scripts/HotFix/GameLogic/UI/`
- 通用 UI 组件扩展：先检查 `GameLogic/Module/UIModule/Expansion/`
- 编辑器工具：`GameUnity/Assets/DGame/Editor/` 或 `GameLogic/Editor/`

若需求跨越 Runtime、HotFix、配置层，输出时必须明确标出跨层影响，避免只改单层导致启动链路或程序集依赖不一致。

## 资源目录组织

重点关注：

- `GameUnity/Assets/BundleAssets/`：运行时加载资源组织
- `GameUnity/Assets/AssetArt/`：美术加工或图集产物组织

### `BundleAssets` 关键目录

- `Actor/`：角色资源
- `Audios/`：音频
- `Configs/`：运行时配置数据
- `DLL/`：热更程序集与 `.dll.bytes`
- `Effects/`：特效
- `Fonts/`：字体
- `FrameSprite/`：通用序列帧
- `Materials/`：材质
- `Prefabs/`：通用预制体
- `Scenes/`：场景
- `UI/`：UI Prefab
- `UIRaw/Atlas/`：UI 图集源图片
- `UIRaw/Raw/`：不走图集或需保留原始形态的 UI 原图

### `AssetArt`

- `AssetArt/Atlas/`：Unity SpriteAtlas 产物目录

### 两者关系

- `BundleAssets/UIRaw/Atlas/` 更偏图集源图片输入
- `AssetArt/Atlas/` 更偏 SpriteAtlas 产物
- 涉及 UI 图片来源、图集合图或图集资源引用关系时，通常要同时检查两边

### 资源链路约束

- 所有运行时资源必须放在 `Assets/BundleAssets/` 下，由 YooAsset 统一管理。
- 运行时禁止使用 `Resources.Load()`、`Resources.LoadAsync()` 或任何 `Resources` 体系直接加载方式。
- 运行时资源加载必须统一通过 `GameModule.ResourceModule` 或其下游封装。
- `AssetArt` 更适合作为美术加工或图集产物目录；若资源最终参与运行时加载，仍要确认是否进入 `BundleAssets` 管理链路。
- 上述限制仅针对运行时；Editor 工具、构建脚本、导出流程可按工具需求单独处理。

### 资源标签约定

- `PRELOAD`：启动时预加载资源，主要用于配置数据、公共 UI 等高频基础依赖资源。
- 资源 `Location` 等于资源文件名，不包含路径和扩展名。
- YooAsset 通过 `AssetBundleCollector` 自动收集资源；处理资源组织时应遵循当前收集规则，默认开启可寻址模式，不要另发明运行时寻址约定。
