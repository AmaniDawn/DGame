# DGame 热更代码开发规范（精简版）

当需求涉及 HotFix 程序集划分、热更入口、Procedure 流程、HybridCLR 限制、AOT 泛型补充、新增热更 UI 或热更开发流程时，先读本文件。

目标：只保留 Codex 做热更落位和实现决策必须知道的信息。原始细节以 `references/originals/client-hotfix-development.md` 为准。

## 程序集落位

- `GameLogic`：主业务热更程序集，承载 UI、模块、数据中心、主业务逻辑
- `GameProto`：协议与配置程序集，承载 Luban 配置代码、配置系统入口、协议/配置消费类型
- `GameBattle`：战斗域热更程序集，只承载独立战斗逻辑

判断规则：

- UI、主客户端业务、业务模块：`GameLogic`
- 配置、协议、Luban 消费：`GameProto`
- 战斗域纯逻辑：`GameBattle`

## 依赖边界

- `GameLogic` 可以依赖 `GameProto`、`GameBattle`
- `GameBattle` 可以依赖 `GameProto`
- `GameBattle` 不应依赖 `GameLogic`
- 热更代码不能反向让主工程运行时代码依赖自身内部实现
- 热更代码不能引用主工程 `internal` 类型
- `GameBattle` 不应依赖 UI、资源加载、场景对象、特效、音频等表现层代码

## 启动边界

### AOT 前

`DGame.AOT/Procedure/` 负责进入 HotFix 前的流程总控，包括：

- 启动流程
- 资源更新/下载
- 清缓存
- 预加载
- 加载热更程序集和 AOT 元数据

流程切换统一使用：

```csharp
SwitchState<TProcedure>();
```

结论：启动前资源、下载、清缓存、程序集加载相关需求，应改 AOT Procedure，不要塞进 HotFix。

### HotFix 后

热更入口是：

- `GameUnity/Assets/Scripts/HotFix/GameLogic/GameStart.cs`

`LoadAssemblyProcedure` 会反射调用：

```csharp
GameStart.Entrance(object[] objects)
```

`GameStart.Entrance(...)` 当前承担：

- 保存热更程序集列表
- 初始化 `GameLogic.GameEventLauncher`
- 初始化 `GameBattle.GameEventLauncher`
- 绑定销毁监听
- 初始化语言设置
- 调用 `StartGame()`

结论：进入 HotFix 后的业务启动逻辑应接到 `GameStart` 或 HotFix 业务模块，不要并行发明另一条启动链。

## HybridCLR / AOT

### 核心约束

- 不要假设编辑器能跑，AOT 平台就一定没问题
- 不要在高频业务路径滥用反射
- 不要默认使用所有 Unity/AOT 反射初始化特性
- 若某能力需要额外生成器或收集器支持，先沿用项目现有方案

### AOT 泛型补充

高风险区：

- `UniTask<T>` / `UniTaskVoid`
- 泛型 UI 异步创建/显示
- 新的泛型集合组合
- 新的泛型委托/回调组合
- 跨 AOT/热更边界的泛型实例
- 战斗逻辑里的泛型容器、状态节点、命令队列、快照结构

项目内重点文件：

- `GameUnity/Assets/HybridCLRGenerate/AOTGenericReferences.cs`
- `GameUnity/Assets/DGame.AOT/Launcher/Scripts/AOT/HybridCLROptimizer.cs`

结论：

- `AOTGenericReferences.cs` 是工具链自动生成文件，不是手写维护文件
- 新增热更代码引入新的 AOT 泛型组合时，要重新走 HybridCLR 工具链生成
- 不要手改 `AOTGenericReferences.cs`
- UI 和异步泛型本身就是当前项目的高风险区

### iOS

- iOS 平台必须走 AOT
- 热更代码在 iOS 上由 HybridCLR 解释执行
- 必须依赖元数据补充和 AOT 泛型预声明

结论：iOS 上新增热更代码时更要检查 AOT 泛型、元数据补充和只在编辑器稳定的能力。

### 战斗逻辑

- 核心战斗逻辑优先考虑确定性和跨平台一致性
- 尽量避免把关键结果建立在浮点细节上

## 日常工作流

1. 先判断代码落在 `GameLogic`、`GameProto` 还是 `GameBattle`
2. 先看同目录已有实现
3. 实现最小改动
4. 若涉及生成内容，不要手改生成产物
5. 检查 HybridCLR、AOT 泛型、跨程序集边界影响

## 新增功能

### 新功能

1. 先确认属于主业务、配置消费还是战斗域
2. 若涉及 UI，沿用现有 `UI` 与 `UIModule` 模式
3. 若涉及配置，先确认是否落到 `GameProto`
4. 若涉及跨模块协作，优先复用现有模块、事件系统和 UI 体系
5. 若新增泛型异步或跨程序集调用，补查 AOT 泛型和 HybridCLR 风险

### 新增热更 UI

1. 确认窗口放到 `GameLogic/UI/` 的哪一块
2. 新建 `UIWindow` 或 `UIWidget`
3. 沿用现有节点绑定工具
4. 在 `RegisterEvent()` 中注册 UI 监听
5. 通过 `GameModule.UIModule` 接入显示逻辑
6. 若涉及事件、红点、配置、异步资源加载，分别沿用对应现有体系

## 常见错误

- 把 `GameBattle` 写成依赖 `GameLogic` 的程序集
- 让热更代码依赖主工程 `internal` 类型
- 把进入 HotFix 前的逻辑乱塞进 `GameStart`
- 忽略 AOT 泛型补充
- 手改 `AOTGenericReferences.cs` 或其他生成产物来规避真正问题
