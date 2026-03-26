# DGame 热更代码开发规范

当需求涉及 HotFix 程序集划分、热更入口、流程状态机、HybridCLR 限制、AOT 泛型补充、日常热更开发流程或新增热更 UI 时，先阅读本文件。

本文档记录当前仓库中已确认的 DGame 热更开发事实，并给出按现有工程组织进行热更开发的推荐方式。若局部模块已有稳定模式，优先跟随目标区域现有实现。

## 目录导航

- [DGame 热更代码开发规范](#dgame-热更代码开发规范)
  - [目录导航](#目录导航)
  - [程序集划分与职责](#程序集划分与职责)
  - [依赖规则](#依赖规则)
  - [热更入口 `GameStart`](#热更入口-gamestart)
  - [热更流程状态机使用与流程切换](#热更流程状态机使用与流程切换)
  - [HybridCLR 注意事项](#hybridclr-注意事项)
    - [AOT 泛型预先声明](#aot-泛型预先声明)
    - [热更代码不能引用主工程 `internal` 类](#热更代码不能引用主工程-internal-类)
    - [反射在热更中的限制](#反射在热更中的限制)
    - [不支持或需规避的特性](#不支持或需规避的特性)
    - [iOS AOT 模式要求](#ios-aot-模式要求)
  - [热更开发工作流](#热更开发工作流)
    - [日常开发步骤](#日常开发步骤)
    - [新功能开发流程](#新功能开发流程)
    - [添加新 UI 步骤](#添加新-ui-步骤)
  - [AOT 泛型补充](#aot-泛型补充)
    - [常见需要 AOT 补充的场景](#常见需要-aot-补充的场景)
  - [使用原则](#使用原则)

## 程序集划分与职责

当前 HotFix 相关程序集主要分成三块：

| 程序集 | 路径 | 职责 |
| --- | --- | --- |
| `GameLogic` | `GameUnity/Assets/Scripts/HotFix/GameLogic/` | 主业务热更程序集，承载 UI、模块、数据中心、业务逻辑、红点、文本、工具类等客户端主逻辑。 |
| `GameProto` | `GameUnity/Assets/Scripts/HotFix/GameProto/` | 协议与配置程序集，承载 Luban 配置代码、配置系统入口和协议/配置消费相关类型。 |
| `GameBattle` | `GameUnity/Assets/Scripts/HotFix/GameBattle/` | 战斗域热更程序集，承载独立战斗逻辑。 |

当前项目里新增热更代码时，优先这样判断归属：

- UI、主客户端业务、业务模块：`GameLogic`
- 配置、协议、Luban 产物消费：`GameProto`
- 战斗域纯逻辑：`GameBattle`

## 依赖规则

当前热更程序集依赖方向应遵守：

- `GameLogic` 可以依赖 `GameProto`、`GameBattle`
- `GameBattle` 可以依赖 `GameProto`
- `GameBattle` 不应依赖 `GameLogic`

另外还有这些运行时边界：

- HotFix 代码不能反向让主工程运行时代码依赖自身内部实现
- 热更代码不应引用主工程 `internal` 类型
- `GameBattle` 不应依赖 UI、资源加载、场景对象、特效、音频等表现层代码

## 热更入口 `GameStart`

当前热更入口位于：

- `GameUnity/Assets/Scripts/HotFix/GameLogic/GameStart.cs`

已确认 `LoadAssemblyProcedure` 在完成程序集和 AOT 元数据准备后，会：

1. 反射找到 `GameStart`
2. 反射调用 `GameStart.Entrance(object[] objects)`
3. 传入热更程序集列表

`GameStart.Entrance(...)` 当前已确认会做这些事：

- 保存热更程序集列表
- 初始化 `GameLogic.GameEventLauncher.Init()`
- 初始化 `GameBattle.GameEventLauncher.Init()`
- 绑定销毁监听
- 初始化语言设置
- 调用 `StartGame()`

这意味着：

- `GameStart` 是 HotFix 主入口
- 新增热更启动级逻辑，应先判断是不是应该接到 `GameStart` 初始化链，而不是随便散落到任意模块

## 热更流程状态机使用与流程切换

当前启动流程状态机位于主工程 AOT 层：

- `GameUnity/Assets/DGame.AOT/Procedure/`

当前流程节点包括：

- `LaunchProcedure`
- `SplashProcedure`
- `InitPackageProcedure`
- `InitResourceProcedure`
- `CreateDownloaderProcedure`
- `DownloadFileProcedure`
- `DownloadOverProcedure`
- `ClearCacheProcedure`
- `PreloadProcedure`
- `LoadAssemblyProcedure`
- `StartGameProcedure`

当前流程切换方式统一使用：

```csharp
SwitchState<TProcedure>();
```

可从现有流程确认：

- 这些 Procedure 负责进入 HotFix 前的 AOT 启动总控
- HotFix 业务逻辑不要随意去新增一条并行的 AOT 启动链
- 若需求是启动前资源、下载、清缓存、程序集加载相关，应改 AOT Procedure
- 若需求是进入热更后的业务启动，应改 `GameStart` 或 HotFix 业务模块

## HybridCLR 注意事项

### AOT 泛型预先声明

当前项目存在：

- `GameUnity/Assets/HybridCLRGenerate/AOTGenericReferences.cs`
- `GameUnity/Assets/DGame.AOT/Launcher/Scripts/AOT/HybridCLROptimizer.cs`

其中 `AOTGenericReferences.cs` 不是手写维护文件，而是 HybridCLR 工具链在打包时分析热更代码后，自动收集需要 AOT 补充的泛型引用产物。

结论：

- 若新增热更代码引入新的 AOT 泛型组合，必须关注 AOT 泛型补充是否完整
- 需要补充时，应走 HybridCLR 工具链重新生成，不要手改 `AOTGenericReferences.cs`
- 不要假设编辑器模式能跑就代表 AOT 平台一定没问题

### 热更代码不能引用主工程 `internal` 类

这是当前热更边界里的硬约束。

热更代码应只依赖：

- 主工程公开类型
- 主工程公开接口
- 明确暴露给 HotFix 的边界层

不应：

- 直接引用主工程 `internal` 类
- 通过“改访问修饰符方便调用”来破坏层次边界

### 反射在热更中的限制

当前项目里主工程到 HotFix 的桥接本身就依赖反射，例如 `LoadAssemblyProcedure` 通过反射调用 `GameStart.Entrance(...)`。

但热更业务内仍应遵循：

- 反射优先只用于必要边界桥接
- 不要在高频业务路径里滥用反射
- 若有明确静态类型边界，优先接口和显式调用，不要把业务逻辑建立在大量运行时反射上

### 不支持或需规避的特性

当前 `GameStart.cs` 已明确保留注释：

```csharp
// HybridCLR 不支持的特性
// RuntimeInitializeOnLoadMethodCollector.ExecuteMethods();
```

当前项目还存在：

- `HybRidCLRNoSupportAttributeCollector.cs`

这说明项目里已经把“部分特性在 HybridCLR 下需要额外收集或规避”当成实际问题处理。

对热更开发来说，应优先记住：

- 不要默认使用所有 Unity/AOT 反射初始化特性
- 若某种特性需要额外生成器或收集器支持，先沿用项目现有方案
- 对“编辑器可运行但真机/AOT 不可靠”的特性保持保守

### iOS AOT 模式要求

结合当前 `LoadAssemblyProcedure` 和 AOT 元数据加载逻辑，可以明确一条工程要求：

- iOS 平台必须使用 AOT 模式
- 所有代码最终都需要经过 AOT 编译
- 热更代码在 iOS 上由 HybridCLR 解释执行，性能会略低于 JIT
- 必须依赖补充元数据和 AOT 泛型预先声明来保证热更泛型路径可运行

这部分结论来自当前项目实际加载逻辑：

- `LoadMetadataForAotAssembly()`
- `HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(...)`

因此在 iOS AOT 模式下，新增热更代码时更要谨慎检查：

- 是否新增了 AOT 泛型需求
- 是否依赖了运行时补元数据
- 是否误用了只在编辑器/非 AOT 环境下才稳定的能力

另外有一条业务侧的重要建议：

- 核心战斗逻辑优先使用确定性定点数，尽量避免把关键结果建立在浮点运算一致性上

原因：

- iOS AOT + 热更解释执行路径下，性能和运行环境都更敏感
- 战斗域若依赖浮点细节，后续跨平台一致性问题更难排查
- `GameBattle` 本身就应保持纯逻辑，更适合提前按确定性思路设计

## 热更开发工作流

### 日常开发步骤

当前仓库里，日常热更开发建议按下面顺序：

1. 先判断代码应该落到 `GameLogic`、`GameProto` 还是 `GameBattle`
2. 先看同目录已有实现和模式
3. 实现最小改动
4. 若涉及生成内容，先走生成流程，不要手改生成产物
5. 关注 HybridCLR / AOT 泛型 / 热更边界影响

### 新功能开发流程

新增 HotFix 功能时，建议按这个顺序：

1. 确认功能属于主业务、配置消费还是战斗域
2. 若涉及 UI，先看现有 `UI` 与 `UIModule` 模式
3. 若涉及配置，先确认是否落到 `GameProto`
4. 若涉及跨模块事件或模块能力，优先复用现有 `GameModule` / 事件系统 / UI 体系
5. 若新增泛型异步或跨程序集调用，补查 AOT 泛型和 HybridCLR 风险

### 添加新 UI 步骤

热更里新增 UI，推荐流程：

1. 确认窗口放到 `GameLogic/UI/` 的哪一块
2. 新建 `UIWindow` 或 `UIWidget`
3. 按现有窗口选择节点绑定生成工具
4. 在 `RegisterEvent()` 中注册 UI 监听
5. 通过 `GameModule.UIModule` 接入显示逻辑
6. 若窗口涉及事件、红点、配置、异步资源加载，分别沿用对应现有体系

## AOT 泛型补充

当前项目里，AOT 泛型补充主要需要关注：

- `HybridCLRGenerate/AOTGenericReferences.cs`
- `HybridCLROptimizer`
- `LoadMetadataForAotAssembly()`

其中 `HybridCLRGenerate/AOTGenericReferences.cs` 的正确使用方式是：

1. 新增或修改热更代码
2. 走项目当前 HybridCLR 打包/生成工具链
3. 让工具链自动重新分析热更代码并生成新的 AOT 泛型引用

不要直接手改这个文件来“补一条类型”。

新增以下类型的代码时，要重点考虑补充：

- 新的泛型异步方法
- 新的泛型集合组合
- 新的跨 AOT/热更边界泛型实例
- UI 异步泛型创建路径

因为当前 `AOTGenericReferences.cs` 里已经能看到很多典型热更泛型实例，例如：

- `UIBase.CreateRedDotAsync`
- `UIBase.CreateWidgetByTypeAsync`
- `UIModule.ShowWindowAsyncAwait`

这说明：

- UI 和异步泛型本身就是当前项目的 AOT 泛型高风险区
- 新增类似模式时，需要特别注意是否要补充泛型声明
- AOT 泛型补充以工具链分析结果为准，不以手工维护为准

### 常见需要 AOT 补充的场景

在当前项目里，以下场景通常都应该优先怀疑是否需要补充 AOT 泛型：

1. 新增 `UniTask<T>`、`UniTaskVoid` 或异步泛型链路。
2. 新增 `CreateWidgetByTypeAsync<T>()`、`ShowWindowAsyncAwait<T>()` 这类泛型 UI 异步调用。
3. 新增 `Dictionary<TKey, TValue>`、`List<T>`、`HashSet<T>` 等新的泛型组合，且这些组合会跨 AOT/热更边界运行。
4. 新增事件、回调或委托的泛型组合，例如新的 `Action<T>`、`Action<T1, T2>` 形态。
5. 新增反射、序列化或配置读取后会实际实例化的泛型类型。
6. 新增战斗逻辑里的泛型容器、状态机节点、命令队列或数据快照结构。

结合当前 `AOTGenericReferences.cs`，已经能看到一批典型高风险区：

- UI 异步创建
- `UniTask` 泛型状态机
- 泛型集合
- 委托与回调组合

因此新增代码时，若满足下面任一条件，就应主动检查 AOT 泛型补充：

- 编辑器正常，AOT 平台异常
- 非热更环境正常，热更路径异常
- iOS 上特定代码路径首次执行时报缺失、反射失败或泛型相关错误

## 使用原则

处理 DGame 热更开发时，优先遵循以下原则：

1. 先按程序集职责落位，不要把 HotFix 当成一个扁平目录。
2. 热更主入口和启动流程边界要清晰：AOT Procedure 负责进 HotFix 前，`GameStart` 负责进 HotFix 后。
3. 新增热更功能时，同时考虑 HybridCLR、AOT 泛型和跨程序集边界。
4. 不要让热更代码依赖主工程 `internal` 实现。
5. 不要手改生成产物来规避真正的热更/AOT 问题。
6. 核心战斗逻辑优先考虑确定性和跨平台一致性，避免随意依赖浮点结果。
