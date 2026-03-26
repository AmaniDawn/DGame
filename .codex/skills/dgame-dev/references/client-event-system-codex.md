# DGame 客户端事件系统（精简版）

当需求涉及事件如何定义、何时用接口事件、UI 如何监听并自动回收事件、`GameEventDriver` 的职责、`EEventGroup` 如何分组、事件命名规范，或排查重复注册/未注销/空引用等问题时，先读本文件。

目标：让 Codex 以最短阅读成本掌握 DGame 当前事件系统的分层、推荐用法与主要约束。原始细节、完整样例和避坑说明以 `references/originals/client-event-system.md` 为准。

## 一图理解

```text
业务定义层（开发者先写接口）
  IEvent 接口 + EventInterfaceAttribute + EEventGroup
  目标：表达语义、约束边界、作为事件定义源

编译期生成层（开发者直接使用）
  ILoginUI_Event.ShowLoginUI / IXxx_Event.OnXxx
  目标：由 Source Generator 从接口生成 int 事件 ID

UI 监听托管层（主要给 UI 生命周期托管）
  UIBase.AddUIEvent(...) + GameEventDriver
  目标：让订阅和反订阅跟随 UI 生命周期统一回收

底层承载层（业务通常不需要展开）
  GameEvent / 生成器产物 / 运行时分发
  目标：承载 int 事件 ID 的注册和广播
```

## 核心事实

- 当前底层事件系统使用 `int` 作为事件 key，不是强类型事件总线。
- 业务定义源是带 `[EventInterface(EEventGroup.Xxx)]` 的接口。
- Source Generator 会自动生成 `*_Event` 静态字段作为事件 ID，不需要开发者手写 `int` 常量。
- 监听和派发直接使用生成的事件 ID，例如 `ILoginUI_Event.ShowLoginUI`。
- 对外能力暴露优先走 `GameEvent.Get<T>()` 的接口事件入口。
- UI 层优先使用 `AddUIEvent(...)` 托管监听生命周期。
- `*_Event`、`*_Gen`、`GameEventHelper.g.cs` 都是生成产物，不应手动维护同名实体文件。

## 初始化前提

项目主入口中必须最先调用：

```csharp
GameEventHelper.Init();
```

若未先调用：

- `_Gen` 代理不会被实例化
- `GameEvent.Get<T>()` 路径不会生效
- 接口事件“看起来能写”，实际不会进入代理链路

结论：`GameEvent.Get<T>()` 不工作时，先查 `GameEventHelper.Init()` 是否执行。

## 生成产物

当前生成产物按职责可分为三类：

- `IXxx_Event.g.cs`：为接口中的每个方法生成 `static readonly int` 事件 ID
- `IXxx_Gen.g.cs`：生成接口代理实现，方法体内部调用 `dispatcher.Send(事件ID, ...)`
- `GameEventHelper.g.cs`：在 `Init()` 中实例化所有 `_Gen` 代理，并注册到 `GameEvent.Get<T>()`

## `GameEvent` 的使用边界

当前底层统一从 `GameUnity/Assets/DGame/Runtime/Core/GameEvent/GameEvent.cs` 暴露：

- `AddEventListener(...)`
- `RemoveEventListener(...)`
- `Send(...)`
- `Get<T>()`

可以这样理解：

- `AddEventListener(int, ...)`：把生成的事件 ID 注册到监听函数
- `RemoveEventListener(int, ...)`：从对应事件 ID 移除监听
- `Send(...)`：通过事件 ID 派发事件
- `Get<T>()`：获取接口事件包装后的能力入口

开发者最少只需知道：

- 接口是定义源
- `*_Event` 是编译期生成的事件 ID 容器
- 监听和派发直接使用生成的事件 ID
- UI 中优先 `AddUIEvent(...)`

## 模式对比

| 特性 | `int事件(底层)` | `接口事件(推荐)` |
| --- | --- | --- |
| 事件 ID 来源 | 直接使用 Source Generator 生成的 `*_Event` 字段 | 定义源是 `[EventInterface]` 接口，ID 同样由 Source Generator 生成 |
| 类型安全 | 基于 `int` 使用，语义主要靠调用约定 | 有分析器约束，接口、方法、分组更清晰 |
| 收发方式 | `GameEvent.Send(...)` / `AddEventListener(...)` / `RemoveEventListener(...)` | 对外能力优先通过 `GameEvent.Get<T>()` 调用；落到底层时仍使用生成的 `*_Event` |
| 监听方式 | 直接监听生成的 `int eventID` | UI 优先 `AddUIEvent(...)`；非 UI 对象可直接监听生成的 `*_Event` |
| 适用场景 | 底层承载、已有监听点、直接接入事件总线 | 业务模块协作、跨模块能力暴露、长期维护的事件定义 |

结论：

- 先写接口
- 编译期生成 `*_Event`
- 监听/派发直接使用生成 ID
- 长期维护层面优先按接口事件组织

## 推荐分层

推荐使用顺序：

1. 先判断是不是 UI 生命周期内的监听
2. 若是 UI 监听现有事件，优先 `UIBase.AddUIEvent(...)`
3. 若是模块之间暴露稳定能力，优先新增 `IEvent` 接口
4. 监听和派发时，直接使用生成的 `*_Event` 字段

简单判断：

- 窗口在 `RegisterEvent()` 里监听语言变更、登录状态等：`AddUIEvent(...)`
- 模块想暴露“显示某个 UI”“通知某个功能执行”：新增接口事件
- UI 控制器集中监听某类系统消息：生成的 `*_Event` 字段 + 集中注册/集中转发

## UI 事件监听

### `UIBase`

当前 `UIBase` 已确认：

- 内部持有 `GameEventDriver`
- 通过 `EventDriver` 延迟创建 `GameEventDriver`
- 暴露一组 `AddUIEvent(...)` 重载
- `RemoveAllUIEvents()` 会释放 `m_eventDriver`

结论：UI 订阅的底层事件已经纳入统一回收模型，不建议窗口自己再维护一套平行订阅列表。

### `GameEventDriver`

`GameEventDriver` 位于 `GameUnity/Assets/DGame/Runtime/Core/GameEvent/GameEventDriver.cs`。

职责非常明确：

- 记录当前 UI 实例注册过的 `(eventID, handler)` 列表
- 注册成功后把记录加入本地缓存
- 在 `OnRelease()` 中统一调用 `GameEvent.RemoveEventListener(...)` 反注册

它不是新的事件中心，只是 UI 监听记录器。

因此：

- 适合解决 UI 生命周期中的订阅清理问题
- 不负责业务分组、事件定义边界、跨模块事件中心职责

## `EEventGroup`

当前已确认的分组：

| 分组 | 用途 |
| --- | --- |
| `GroupUI` | UI 交互与展示协作 |
| `GroupLogic` | 逻辑层内部交互 |
| `GroupBattle` | Battle 层交互 |

分组首先是语义边界，不是性能优化手段。

使用建议：

- UI/展示协作放 `GroupUI`
- 纯逻辑模块事件放 `GroupLogic`
- Battle 域事件放 `GroupBattle`

## 事件定义规范

### 核心规则

- 只定义接口，不手写 ID
- 不手写 `const int`
- 不手写 `enum` 事件值
- 不手写 `*_Event.cs`
- 接口必须标注 `[EventInterface(EEventGroup.Xxx)]`
- 一个事件 ID 只对应一套固定签名，不要混用不同参数列表

### 接口命名

接口名必须表达事件域或能力域，不要写成全局事件中心。

推荐：

- `ILoginUI`
- `ICommonUI`
- `ILocalization`
- `IBattle`

不推荐：

- `IGlobalEvent`
- `IClientEventCenter`
- `IAllUIEvent`
- `IXxxMgrEvent`

命名原则：

- 统一使用 `I` 前缀
- 名称表达“谁对外暴露什么能力”
- 优先按稳定能力域拆小接口，不要把不相关方法塞进一个大接口

### 新增事件流程

1. 在 `IEvent/` 下新建接口文件
2. 标记 `[EventInterface(EEventGroup.GroupUI/GroupLogic/GroupBattle)]`
3. 定义方法签名
4. 重新编译，自动生成 `IXxx_Event.g.cs`、`IXxx_Gen.g.cs`，并更新 `GameEventHelper.g.cs`
5. 使用时：
   发送 `GameEvent.Get<IXxx>().Method()`；
   接收 `AddUIEvent(IXxx_Event.Method, cb)` 或 `GameEvent.AddEventListener(IXxx_Event.Method, cb)`

示例：

```csharp
GameEvent.Get<ILoginUI>().ShowLoginUI();
AddUIEvent(ILoginUI_Event.ShowLoginUI, OnShowLoginUI);
```

## 已确认的接口事件样例

当前已确认的接口事件包括：

- `ICommonUI`
- `ILoginUI`
- `ILocalization`
- `IBattle`

已确认位置：

- `GameUnity/Assets/Scripts/HotFix/GameLogic/IEvent/ICommonUI.cs`
- `GameUnity/Assets/Scripts/HotFix/GameLogic/IEvent/ILoginUI.cs`
- `GameUnity/Assets/Scripts/HotFix/GameLogic/IEvent/ILocalization.cs`
- `GameUnity/Assets/Scripts/HotFix/GameBattle/IEvent/IBattle.cs`

结论：项目更偏向“按能力域拆接口”，而不是“所有事件塞进一个 `IGameEvent`”。

## 已确认调用链

### 语言切换

当前已确认链路：

1. `DGameLocalizationHelper.SetLanguage(...)`
2. 调用 `GameEvent.Get<ILocalization>().OnLanguageChanged(...)`
3. `UITextIDBinder` 通过 `GameEvent.AddEventListener<int>(ILocalization_Event.OnLanguageChanged, ...)` 监听
4. `MainWindow.RegisterEvent()` 通过 `AddUIEvent<int>(ILocalization_Event.OnLanguageChanged, ...)` 监听
5. UI 收到通知后刷新文本或界面

结论：

- 对外触发时更倾向接口事件语义
- 落到监听面时直接使用生成的 `*_Event`
- UI 自己监听时再用 `AddUIEvent(...)` 托管释放

### UI 控制器集中监听

当前 `CommonUIController` 已确认：

- 在 `RegUIMessage()` 中直接监听 `ICommonUI_Event.ShowWaitingUI`
- 收到事件后显示 `WaitingUI`

结论：集中监听器应保持单点注册、单点转发、单点管理，不要把同一语义散落到多个控制器和窗口重复承担。

## 使用原则

1. 先定义接口，再依赖生成产物，不要反过来设计事件
2. UI 监听优先挂在 `RegisterEvent()` 中，并通过 `AddUIEvent(...)` 托管释放
3. 发送优先 `GameEvent.Get<T>().Method()`，监听优先使用生成的 `*_Event` 字段
4. 同一语义只保留一份定义源，不要再补手写 ID 或平行事件中心

## 常见坑

- 不要在 UI 中直接 `GameEvent.AddEventListener(...)` 却忘记释放
- 不要在 `RegisterEvent()` 之外的高频路径重复注册同一监听
- 不要用闭包注册、再用另一个 delegate 实例移除
- 不要忽略 `GameEvent.Get<T>()` 可能因初始化链未跑通而返回 `null`
- 不要在同一个事件 ID 上混用不同签名
- 不要手写 `*_Event.cs` 覆盖生成器职责
- 不要把所有事件都塞进 `GroupUI`
- 不要为了“通用”设计巨型事件接口

## 排查顺序

若事件不生效，优先按这个顺序查：

1. 是否执行到了 `GameEventHelper.Init()`
2. 是否重新编译并生成了对应 `IXxx_Event.g.cs` / `IXxx_Gen.g.cs`
3. `GameEventHelper.g.cs` 是否已更新并注册该接口代理
4. UI 监听是否走了 `AddUIEvent(...)`，还是手动注册后忘了释放
5. 是否发生重复注册、错误移除或签名不一致
