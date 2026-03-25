# DGame 客户端事件系统

当需求涉及事件应该如何定义、应该使用底层 `int eventID` 还是接口事件、UI 事件监听如何收口、`GameEventDriver` 的职责、`EEventGroup` 如何分组、事件定义命名规范，或排查事件重复注册/未注销/空引用等问题时，先阅读本文件。

本文档记录当前仓库中已确认的 DGame 客户端事件系统事实，并在这些事实基础上给出推荐用法。若后续生成器、注册器或事件包装层实现有新增，以实际代码为准，但当前底层和调用模式已经可以确定主要约束。

## 目录导航

- [DGame 客户端事件系统](#dgame-客户端事件系统)
  - [目录导航](#目录导航)
  - [整体结构概览](#整体结构概览)
  - [使用者需要知道的最小事实](#使用者需要知道的最小事实)
    - [`GameEvent` 的使用边界](#gameevent-的使用边界)
    - [Source Generator 生成的事件 ID](#source-generator-生成的事件-id)
  - [事件模式对比](#事件模式对比)
  - [推荐分层](#推荐分层)
  - [UI 事件监听](#ui-事件监听)
    - [`UIBase` 中的 UI 事件驱动](#uibase-中的-ui-事件驱动)
    - [`GameEventDriver` 的职责](#gameeventdriver-的职责)
  - [接口事件与事件分组](#接口事件与事件分组)
    - [`EEventGroup` 的当前定义](#eeventgroup-的当前定义)
    - [事件接口定义方式](#事件接口定义方式)
    - [当前已确认的接口事件样例](#当前已确认的接口事件样例)
  - [当前已确认的调用链样例](#当前已确认的调用链样例)
    - [语言切换事件链路](#语言切换事件链路)
    - [UI 控制器消息监听链路](#ui-控制器消息监听链路)
  - [事件定义规范](#事件定义规范)
  - [使用原则](#使用原则)
  - [常见错误与避坑](#常见错误与避坑)

## 整体结构概览

当前仓库中的客户端事件系统，建议先按下面三层理解：

```text
业务定义层（开发者先写接口）
  IEvent 接口 + EventInterfaceAttribute + 分组约定
  目标：表达语义、约束边界、作为事件定义源

编译期生成层（开发者直接使用它）
  ILoginUI_Event.ShowLoginUI / IXxx_Event.OnXxx
  目标：由 Source Generator 从接口生成 int 事件 ID

事件监听辅助层（主要给 UI 生命周期托管用）
  GameEventDriver / UIBase.AddUIEvent / RemoveAllUIEvents
  目标：把订阅和反订阅绑定到 UI 生命周期

底层承载层（使用者通常不需要展开）
  GameEvent / 生成器产物 / 运行时事件分发
  目标：承载 int 事件 ID 的注册和广播
```

应先明确一个事实：

- 当前底层分发器使用 `int` 作为事件键，不是强类型事件总线。
- 当前推荐的业务定义方向是“接口事件 + 分组属性 + Source Generator 生成 `_Event` 常量类”。
- 业务监听和派发时，直接使用生成的事件 ID，例如 `ILoginUI_Event.ShowLoginUI`，而不是手写 `int` 常量。
- UI 层还额外有一层 `GameEventDriver`，用于统一回收窗口/组件订阅的底层事件。

## 使用者需要知道的最小事实

正常业务开发时，不需要先理解底层分发器和包装层的内部实现再使用事件系统。本节只保留直接使用必须知道的内容。

### `GameEvent` 的使用边界

当前底层统一从 `GameUnity/Assets/DGame/Runtime/Core/GameEvent/GameEvent.cs` 暴露：

- `AddEventListener(...)`
- `RemoveEventListener(...)`
- `Send(...)`
- `Get<T>()`

可以先按下面方式理解：

| 能力 | 作用 |
| --- | --- |
| `AddEventListener(int, ...)` | 直接把生成出来的 `int eventID` 注册到监听函数上。 |
| `RemoveEventListener(int, ...)` | 从对应 `eventID` 上移除监听。 |
| `Send(...)` | 通过事件 ID 派发事件。 |
| `Get<T>()` | 获取接口事件包装后的能力入口。 |

业务上只需要知道：

- 接口是事件定义源。
- `*_Event` 是编译期生成的事件 ID 容器。
- 监听和派发直接使用生成的 `int` 事件 ID。
- UI 中优先使用 `AddUIEvent(...)` 托管监听生命周期。

### Source Generator 生成的事件 ID

当前项目的事件 ID 来源不需要开发者手写 `int` 常量。事件 ID 由 Source Generator 从带 `[EventInterface]` 的接口自动生成。

示例：

```csharp
// 1. 开发者手写接口
[EventInterface(EEventGroup.GroupUI)]
public interface ILoginUI
{
    void ShowLoginUI();
    void CloseLoginUI();
}

// 2. 编译期自动生成，不存在实体 .cs 文件
public partial class ILoginUI_Event
{
    public static readonly int ShowLoginUI =
        RuntimeId.ToRuntimeId("ILoginUI_Event.ShowLoginUI");

    public static readonly int CloseLoginUI =
        RuntimeId.ToRuntimeId("ILoginUI_Event.CloseLoginUI");
}
```

这意味着：

- 开发者的事件定义源是接口，不是手写数字常量。
- 开发者在监听和发送时直接使用生成的 `*_Event` 静态字段。
- `*_Event` 是编译期生成产物，不应手动维护同名实体文件。

当前生成产物应按职责分三类理解：

| 生成产物 | 作用 |
| --- | --- |
| `IXxx_Event.g.cs` | 为接口中的每个方法生成一个 `static readonly int` 事件 ID，供 `AddEventListener` / `Send` / `RemoveEventListener` / `AddUIEvent` 直接使用。 |
| `IXxx_Gen.g.cs` | 生成接口实现类并实现 `IXxx`，每个方法体内部调用 `dispatcher.Send(事件ID, ...)`。 |
| `GameEventHelper.g.cs` | 在 `Init()` 中实例化所有 `_Gen` 代理，并注册到 `GameEvent.Get<T>()` 对应入口。 |

初始化要求：

```csharp
// 项目主入口中，必须最先调用
GameEventHelper.Init();
```

这一步是必须项。若未先调用：

- 所有 `_Gen` 代理都不会被实例化
- `GameEvent.Get<T>()` 路径全部无响应
- 接口事件发送看起来“能写”，但实际不会进入对应代理链路

## 事件模式对比

当前项目主要有两种事件使用模式。建议按下表理解：

| 特性 | `int事件(底层)` | `接口事件(推荐)` |
| --- | --- | --- |
| 事件ID来源 | 直接使用 Source Generator 生成的 `*_Event` `int` 字段，例如 `ILoginUI_Event.ShowLoginUI` | 定义源是带 `[EventInterface]` 的接口；对应事件 ID 同样由 Source Generator 生成 |
| 类型安全 | 基于 `int` 使用，调用点本身只体现参数重载，不表达接口语义 | 有 `GameEventAnalyzer` 分析器做强检查，接口、方法、分组和使用方式更容易被静态约束 |
| 收发方式 | `GameEvent.Send(...)`、`GameEvent.AddEventListener(...)`、`GameEvent.RemoveEventListener(...)` 直接面向生成的 `int eventID` | 对外能力暴露优先通过 `GameEvent.Get<T>()` 面向接口调用；落到实际事件收发时仍使用生成的 `*_Event` `int` 字段 |
| 监听方式 | 直接监听生成的 `int eventID`，例如 `GameEvent.AddEventListener(ILoginUI_Event.ShowLoginUI, OnShowLoginUI)` | UI 内优先 `AddUIEvent(...)` 监听对应 `*_Event`；非 UI 对象可直接监听生成的 `*_Event`，但事件定义和组织仍按接口维度管理 |
| 适用场景 | 底层承载、已有事件监听点、需要直接接入事件总线的场景 | 业务模块协作、跨模块能力暴露、长期维护的事件定义、需要分析器约束和清晰语义边界的场景 |

更直接地说：

- 开发者先写接口。
- 编译期生成 `*_Event` `int` 字段。
- 直接监听/派发时使用生成的事件 ID。
- 业务设计和长期维护层面，优先按接口事件来组织。

## 推荐分层

在当前仓库中，推荐这样使用事件系统：

1. 先判断是不是 UI 内部生命周期监听。
2. 若是 UI 监听现有事件，优先走 `UIBase.AddUIEvent(...)`。
3. 若是模块之间暴露稳定能力，优先新增 `IEvent` 接口。
4. 监听和派发时，优先直接使用生成的 `*_Event` `int` 字段。

简单判断规则：

| 场景 | 推荐写法 |
| --- | --- |
| 窗口在 `RegisterEvent()` 里监听语言变更、登录状态等 | `AddUIEvent(ILocalization_Event.OnLanguageChanged, ...)` |
| 模块想暴露“显示某个 UI”“通知某个功能执行”这类稳定业务能力 | `IEvent` 接口 |
| UI 控制器统一监听某类系统消息 | 生成的 `*_Event` 字段 + `GameEvent.AddEventListener(...)`，但应保持集中管理 |

## UI 事件监听

### `UIBase` 中的 UI 事件驱动

当前 `GameUnity/Assets/Scripts/HotFix/GameLogic/Module/UIModule/UIBase.cs` 已确认：

- `UIBase` 内部持有 `GameEventDriver m_eventDriver`
- 通过 `EventDriver` 延迟创建 `GameEventDriver`
- 暴露了一组 `AddUIEvent(...)` 重载
- `RemoveAllUIEvents()` 会释放 `m_eventDriver`

这说明 `UIBase` 已经把“UI 订阅的底层事件回收”纳入了统一模型，不建议窗口自己再额外维护一套平行的订阅列表。

### `GameEventDriver` 的职责

当前 `GameEventDriver` 位于 `GameUnity/Assets/DGame/Runtime/Core/GameEvent/GameEventDriver.cs`。

它的职责非常明确：

- 记录当前 UI 实例注册过的 `(eventID, handler)` 列表。
- 注册成功后，把这条记录加入本地缓存。
- `OnRelease()` 时遍历所有缓存，统一调用 `GameEvent.RemoveEventListener(...)` 反注册。

它不是新的事件总线，只是监听记录器。可以先这样理解：

| 组件 | 职责 |
| --- | --- |
| `GameEvent` | 真正执行注册与分发 |
| `GameEventDriver` | 为 UI 实例记录“我订阅了哪些底层事件”，在销毁/释放时统一回收 |

因此：

- `GameEventDriver` 适合解决 UI 生命周期里的订阅清理问题。
- 它不负责业务分组、事件定义规范、接口边界。
- 不要把 `GameEventDriver` 当成跨模块事件中心来设计。

## 接口事件与事件分组

### `EEventGroup` 的当前定义

当前 `EEventGroup` 位于 `GameUnity/Assets/DGame/Runtime/Core/GameEvent/EventInterfaceAttribute.cs`。

当前仓库已确认有三类分组：

| 枚举值 | 含义 | 当前适用方向 |
| --- | --- | --- |
| `GroupUI` | UI 交互相关 | `ILoginUI`、`ICommonUI`、`ILocalization` 这类 UI/展示协作能力 |
| `GroupLogic` | 逻辑层内部交互相关 | 纯业务逻辑模块之间的事件接口 |
| `GroupBattle` | Battle 层交互相关 | `GameBattle` 侧纯逻辑域接口 |

当前事实：

- 分组定义本身已经存在于 Runtime。
- 接口通过 `[EventInterface(EEventGroup.Xxx)]` 标注归属。
- 分组首先是语义边界，不是性能优化手段。

### 事件接口定义方式

当前仓库中的接口事件定义模式已确认如下：

```csharp
[EventInterface(EEventGroup.GroupUI)]
public interface ILocalization
{
    void OnLanguageChanged(int language);
}
```

当前模式的核心点是：

1. 用接口名表达事件域，而不是用一个大而全的事件管理器堆方法。
2. 用方法名表达具体通知语义。
3. 用 `EventInterfaceAttribute` 显式标记分组。
4. 由 Source Generator 为每个接口方法生成对应的 `*_Event` `int` 字段。

### 当前已确认的接口事件样例

当前已确认的接口事件定义位于：

- `GameUnity/Assets/Scripts/HotFix/GameLogic/IEvent/ICommonUI.cs`
- `GameUnity/Assets/Scripts/HotFix/GameLogic/IEvent/ILoginUI.cs`
- `GameUnity/Assets/Scripts/HotFix/GameLogic/IEvent/ILocalization.cs`
- `GameUnity/Assets/Scripts/HotFix/GameBattle/IEvent/IBattle.cs`

可先按下表理解：

| 接口 | 分组 | 当前职责 |
| --- | --- | --- |
| `ICommonUI` | `GroupUI` | UI 通用能力，如显示等待界面 |
| `ILoginUI` | `GroupUI` | 登录 UI 相关能力 |
| `ILocalization` | `GroupUI` | 语言切换通知 |
| `IBattle` | `GroupBattle` | Battle 域事件接口占位/边界 |

从当前命名可以看出，项目更偏向“按能力域拆接口”，而不是“所有事件都塞进一个 `IGameEvent`”。

## 当前已确认的调用链样例

### 语言切换事件链路

当前已确认的一条完整事件链如下：

1. `DGameLocalizationHelper.SetLanguage(...)`
2. 调用 `GameEvent.Get<ILocalization>().OnLanguageChanged(...)`
3. `UITextIDBinder` 通过 `GameEvent.AddEventListener<int>(ILocalization_Event.OnLanguageChanged, OnLanguageChanged)` 监听
4. `MainWindow.RegisterEvent()` 通过 `AddUIEvent<int>(ILocalization_Event.OnLanguageChanged, ...)` 监听
5. UI 收到通知后刷新文本或界面

已确认代码位置：

| 行为 | 位置 |
| --- | --- |
| 触发语言事件 | `GameUnity/Assets/Scripts/HotFix/GameLogic/Module/LocalizationModule/DGameLocalizationHelper.cs` |
| 文本组件监听 | `GameUnity/Assets/Scripts/HotFix/GameLogic/Module/TextModule/UITextIDBinder.cs` |
| 主窗口监听 | `GameUnity/Assets/Scripts/HotFix/GameLogic/UI/Main/MainWindow.cs` |

这里能看出当前项目的典型组合用法：

- 对外触发时，业务更倾向通过接口事件暴露语义。
- 落到监听面时，直接使用 Source Generator 生成的 `*_Event` `int` 字段接入底层监听。
- UI 自己监听时，再用 `AddUIEvent(...)` 接管释放。

### UI 控制器消息监听链路

当前 `GameUnity/Assets/Scripts/HotFix/GameLogic/UI/UIController/CommonUIController.cs` 已确认：

- `RegUIMessage()` 中直接监听生成的 `ICommonUI_Event.ShowWaitingUI`
- 收到事件后显示 `WaitingUI`

这说明当前仓库中还存在一类“UI 控制器集中监听某组事件，再驱动某个窗口/界面行为”的模式。对这类集中监听器，推荐保持：

- 单点注册
- 单点转发
- 单点管理对应 UI 行为

不要把同一事件同时散落到多个控制器和多个窗口里重复承担同一种职责。

## 事件定义规范

当前仓库新增事件时，建议遵循以下规范。

### 核心规则

只定义接口，不手写 ID。

这里的“不手写 ID”包括：

- 不手写 `const int`
- 不手写 `enum` 事件值
- 不手写 `*_Event.cs`

开发者只负责：

- 定义接口
- 标注 `[EventInterface(EEventGroup.Xxx)]`
- 定义方法签名

事件 ID、接口代理实现和帮助类初始化入口都由 Source Generator 自动生成。

### 接口命名规范

接口命名必须表达事件域或能力域，不要写成无语义的全局事件中心。

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

- 接口统一使用 `I` 前缀。
- 名称表达“谁对外暴露什么能力”，而不是表达底层实现。
- 优先按稳定能力域拆分，不要把不相关的方法塞进同一个大接口。
- UI 相关接口使用 `XxxUI` 或清晰的展示域命名。
- 逻辑相关接口使用具体业务域命名，不要只写 `LogicEvent` 这种空泛名字。

### 添加新事件的完整流程

新增一个事件接口时，按下面流程执行：

1. 在 `IEvent/` 下新建接口文件。
2. 标记 `[EventInterface(EEventGroup.GroupUI/GroupLogic/GroupBattle)]`。
3. 定义方法签名。
4. 重新编译，让 Source Generator 自动生成 `IXxx_Event.g.cs`、`IXxx_Gen.g.cs`，并更新 `GameEventHelper.g.cs`。
5. 使用时：
   发送走 `GameEvent.Get<IXxx>().Method()`；
   接收走 `AddUIEvent(IXxx_Event.Method, cb)` 或 `GameEvent.AddEventListener(IXxx_Event.Method, cb)`。

示例：

```csharp
// 发送
GameEvent.Get<ILoginUI>().ShowLoginUI();

// 接收
AddUIEvent(ILoginUI_Event.ShowLoginUI, OnShowLoginUI);
```

## 使用原则

处理 DGame 客户端事件系统时，优先遵循以下原则：

1. 先定义接口，再依赖生成产物，不要反过来设计事件。
2. UI 监听优先挂在 `RegisterEvent()` 中，并通过 `AddUIEvent(...)` 托管释放。
3. 发送优先 `GameEvent.Get<T>().Method()`，监听优先使用生成的 `*_Event` 字段。
4. 同一语义只保留一份定义源，不要再补手写 ID 或平行事件中心。

## 常见错误与避坑

### 1. 在 UI 中直接 `GameEvent.AddEventListener(...)` 但忘记释放

当前 UI 已经有 `GameEventDriver` 和 `RemoveAllUIEvents()`，如果窗口/组件绕开这层直接注册：

- 更容易出现窗口关闭后仍然收到事件
- 更容易出现重复注册
- 更难统一收口

推荐：

- UI 中优先 `AddUIEvent(...)`

示例：

```csharp
// 不推荐：自己注册，生命周期结束时容易漏掉移除
protected override void RegisterEvent()
{
    GameEvent.AddEventListener(ILoginUI_Event.ShowLoginUI, OnShowLoginUI);
}

// 推荐：交给 UIBase + GameEventDriver 托管
protected override void RegisterEvent()
{
    AddUIEvent(ILoginUI_Event.ShowLoginUI, OnShowLoginUI);
}
```

### 2. 重复添加同一监听

常见触发方式：

- `RegisterEvent()` 被重复执行
- `OnEnable()` 重复订阅，但 `OnDisable()` 没对应移除
- 同一个窗口刷新时再次调用监听注册逻辑

示例：

```csharp
// 不推荐：每次刷新都重复注册
protected override void OnRefresh()
{
    AddUIEvent(ILoginUI_Event.ShowLoginUI, OnShowLoginUI);
}

// 推荐：只在 RegisterEvent 中注册一次
protected override void RegisterEvent()
{
    AddUIEvent(ILoginUI_Event.ShowLoginUI, OnShowLoginUI);
}
```

### 3. 移除不存在的监听

常见原因：

- 注册和反注册不是同一个委托实例
- 闭包重新构造，导致移除时拿到的是另一个 delegate
- 已经通过 `GameEventDriver` 自动回收，又手动再移除一次

示例：

```csharp
// 不推荐：注册和移除不是同一个 delegate 实例
private void OnEnable()
{
    GameEvent.AddEventListener<int>(ILocalization_Event.OnLanguageChanged, x => RefreshUI());
}

private void OnDisable()
{
    GameEvent.RemoveEventListener<int>(ILocalization_Event.OnLanguageChanged, x => RefreshUI());
}

// 推荐：使用命名方法
private void OnEnable()
{
    GameEvent.AddEventListener<int>(ILocalization_Event.OnLanguageChanged, OnLanguageChanged);
}

private void OnDisable()
{
    GameEvent.RemoveEventListener<int>(ILocalization_Event.OnLanguageChanged, OnLanguageChanged);
}

private void OnLanguageChanged(int language)
{
    RefreshUI();
}
```

### 4. 忽略 `GameEvent.Get<T>()` 可能返回 `null`

风险：

- `GameEvent.Get<ILocalization>().OnLanguageChanged(...)` 这类调用若初始化链没跑通，会直接空引用

排查方向：

- 先确认项目主入口是否执行到了 `GameEventHelper.Init()`
- 再确认是否已经重新编译，生成了对应的 `IXxx_Event.g.cs` / `IXxx_Gen.g.cs`
- 再确认 `GameEventHelper.g.cs` 是否已更新并注册该接口代理

示例：

```csharp
// 风险写法：初始化链没跑通时会直接空引用
GameEvent.Get<ILoginUI>().ShowLoginUI();

// 排查期可临时加保护，最终仍应修正初始化链
var loginUI = GameEvent.Get<ILoginUI>();
if (loginUI == null)
{
    DLogger.Error("ILoginUI not initialized. Check GameEventHelper.Init().");
    return;
}

loginUI.ShowLoginUI();
```

### 5. 在同一个 `eventID` 上混用不同签名

问题：

- 可读性差
- 容易遗漏某些监听
- 后续维护者很难判断这个事件到底应该带几个参数

结论：

- 一个事件 ID 对应一套固定签名，不要混用。

示例：

```csharp
// 不推荐：同一个事件 ID 混用不同签名
GameEvent.AddEventListener<int>(IXxx_Event.OnXxx, OnXxxInt);
GameEvent.AddEventListener<int, string>(IXxx_Event.OnXxx, OnXxxIntString);

// 推荐：一个事件 ID 对应一种固定签名
GameEvent.AddEventListener<int, string>(IXxx_Event.OnXxx, OnXxxIntString);
```

### 6. 手写 `*_Event.cs` 覆盖生成器职责

错误表现：

- 开发者手动创建 `ILoginUI_Event.cs`
- 手动维护 `ShowLoginUI = 1001`

问题：

- 与 Source Generator 的定义源冲突
- 重命名接口方法后容易不同步
- 同一语义出现双来源

正确做法：

- 只定义接口
- 直接使用编译期生成的 `*_Event` 字段

示例：

```csharp
// 不推荐：手写 ID
public static class ILoginUI_Event
{
    public const int ShowLoginUI = 1001;
}

// 推荐：只定义接口，重新编译后使用生成产物
[EventInterface(EEventGroup.GroupUI)]
public interface ILoginUI
{
    void ShowLoginUI();
}
```

### 7. 所有事件都塞进 `GroupUI`

`GroupUI` 不是默认垃圾桶。纯逻辑事件或 Battle 事件继续塞 `GroupUI`，会导致：

- 分组失去意义
- 后续生成器或包装层难以按域管理
- 业务边界越来越模糊

示例：

```csharp
// 不推荐：纯逻辑事件也塞进 GroupUI
[EventInterface(EEventGroup.GroupUI)]
public interface IInventoryLogic
{
    void OnItemChanged(int itemId);
}

// 推荐：按实际职责选择分组
[EventInterface(EEventGroup.GroupLogic)]
public interface IInventoryLogic
{
    void OnItemChanged(int itemId);
}
```

### 8. 为了“通用”设计一个巨型接口

例如：

- `IGlobalEvent`
- `IClientEventCenter`
- `IUIAndLogicEvent`

这种设计短期看似省事，长期一定会变成没有边界的大杂烩。当前仓库更合适的方向是：

- 小接口
- 明确分组
- 明确职责域

示例：

```csharp
// 不推荐：把不相关能力塞进一个大接口
[EventInterface(EEventGroup.GroupUI)]
public interface IGlobalEvent
{
    void ShowLoginUI();
    void OnLanguageChanged(int language);
    void OnBattleStart(int battleId);
}

// 推荐：按职责拆分
[EventInterface(EEventGroup.GroupUI)]
public interface ILoginUI
{
    void ShowLoginUI();
}

[EventInterface(EEventGroup.GroupUI)]
public interface ILocalization
{
    void OnLanguageChanged(int language);
}
```
