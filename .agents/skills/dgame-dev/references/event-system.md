# DGame 事件系统

> **适用场景**：GameEvent、GameEventDriver、AddUIEvent、EEventGroup、接口事件 | **关联文档**：[ui-lifecycle.md](ui-lifecycle.md)、[event-antipatterns.md](event-antipatterns.md)

## 核心接口

| API | 用途 |
|-----|------|
| `GameEventHelper.AddEvent/RemoveEvent` | 非 UI 模块优先使用生成的强类型监听/移除门面，如 `GameEventHelper.AddEvent.Localization.OnLanguageChanged`；由生成器转发到 GameEvent |
| `GameEvent.AddEventListener` | 添加普通事件监听，返回 bool（监听是否成功；UI 内用 AddUIEvent 无需关心） |
| `GameEvent.RemoveEventListener` | 移除普通事件监听；另有 `RemoveEventListener(int, Delegate)` 重载 |
| `GameEvent.Send` | 通过字符串事件发送 |
| `GameEvent.Get<T>()` | 获取接口事件代理 |
| `GameEventHelper.AddEvent/RemoveEvent` | Source Generator 生成的监听/移除包装，内部调用 `GameEvent.AddEventListener/RemoveEventListener` |
| `UIBase.AddUIEvent` | UI 内事件监听，自动清理 |
| `GameEventDriver` | 记录 UI 事件，释放时统一移除 |

## 三件套架构

| 类型 | 路径 | 职责 | 使用边界 |
|------|------|------|----------|
| `GameEvent` | `DGame/Runtime/Core/GameEvent/GameEvent.cs` | 静态事件门面，提供 `AddEventListener`、`RemoveEventListener`、`Send`、`Get<T>()` | 业务可直接用于非 UI 监听和接口事件发送 |
| `EventMgr` | `DGame/Runtime/Core/GameEvent/EventMgr.cs` | 持有 `EventDispatcher` 和接口事件代理表 | 框架内部管理；业务不要假设存在 `GameEventMgr` |
| `GameEventDriver` | `DGame/Runtime/Core/GameEvent/GameEventDriver.cs` | 记录 UI 事件监听，释放时逐条 `RemoveEventListener` | 由 `UIBase.AddUIEvent` 间接使用 |
| `GameEventLauncher` | 生成器生成/热更入口调用 | 注册接口事件代理，让 `GameEvent.Get<T>()` 可用 | `GameStart.Entrance` 早期调用 `GameEventLauncher.Init()` |
| `GameEventHelper` | 生成器生成 | 生成 `AddEvent` / `RemoveEvent` 包装类，减少手写事件 ID 和泛型参数错误 | 非 UI 监听可用；不负责生命周期清理 |

DGame 的公开类名是 `EventMgr` 和 `GameEventDriver`，没有公开 `GameEventMgr` 类。

## 模式对比

| 模式 | 定义方式 | 发送 | 监听 | 清理责任 | 适用场景 |
|------|----------|------|------|----------|----------|
| 接口事件 | `IEvent` 接口 + `[EventInterface(EEventGroup...)]` | `GameEvent.Get<ICommonUI>().ShowWaitingUI(...)` | `AddUIEvent` / `GameEventHelper.AddEvent` / `GameEvent.AddEventListener` | 取决于监听写法 | 跨模块、UI 控制器、需要参数签名编译期约束 |
| UI 生命周期监听 | 使用生成的 `*_Event` ID | 外部发送 | `RegisterEvent()` 中 `AddUIEvent<T...>(eventId, handler)` | `UIBase` 自动清理 | `UIWindow` / `UIWidget` 内监听事件 |
| GameEventHelper 包装监听 | 由生成器从接口生成 | 外部发送 | `GameEventHelper.AddEvent.CommonUI.ShowWaitingUI(handler)` | 调用方必须配套 `RemoveEvent` | 非 UI 类想避免手写 `GameEvent.AddEventListener<T...>` |
| 普通事件监听 | 直接使用 `int` 事件 ID | `GameEvent.Send` 或接口代理发送 | `GameEvent.AddEventListener<T...>(eventId, handler)` | 调用方手动 `RemoveEventListener` | 框架/非 UI 临时监听，或现有代码兼容 |
| string 事件 | 字符串经 `StringId.StringToHash` 转 ID | `GameEvent.Send("EventName", ...)` | 本仓库未提供 string 监听重载 | 调用方手动管理 | 少用；6 参数不支持 string 发送 |

## 事件分组

```csharp
public enum EEventGroup
{
    GroupUI,
    GroupLogic,
}
```

接口事件示例：

```csharp
[EventInterface(EEventGroup.GroupUI)]
public interface ILocalization
{
    void OnLanguageChanged(int language);
}
```

生成代码提供 `ILocalization_Event.OnLanguageChanged` 这类事件 ID。

## 事件定义规范

| 项 | DGame 规范 |
|----|------------|
| 文件位置 | 热更业务事件接口放 `GameUnity/Assets/Scripts/HotFix/GameLogic/IEvent/` |
| 命名空间 | 使用 `namespace GameLogic` |
| using | 事件接口需要 `using DGame;` 以使用 `[EventInterface]` 和 `EEventGroup` |
| 接口命名 | `I` + 业务域名，如 `ICommonUI`、`ILoginUI`、`ILocalization` |
| 特性 | 必须标注 `[EventInterface(EEventGroup.GroupUI/GroupLogic)]` |
| 方法命名 | 业务动作或状态变化名，如 `ShowWaitingUI`、`ShowLoginUI`、`OnLanguageChanged` |
| 参数 | 0 到 6 个参数；发送、监听、移除签名必须完全一致 |
| 生成产物 | `Ixxx_Event` 事件 ID、`Ixxx_Gen` 接口代理、`GameEventLauncher.g.cs`、`GameEventHelper.g.cs` |
| 禁止 | 手写硬编码事件 ID、使用不存在的 `GameEventMgr` / `GameEventHelper.Init()` / `GameEvent.Shutdown` |

## UI 事件监听

```csharp
protected override void RegisterEvent()
{
    AddUIEvent<int>(ILocalization_Event.OnLanguageChanged, OnLanguageChanged);
}
```

`AddUIEvent` 支持 0 到 6 个泛型参数。UI 销毁时 `RemoveAllUIEvents()` 会释放 `GameEventDriver`，自动移除监听。

## 泛型档位表

| 目的 | 0 参数 | 1 参数 | 2 参数 | 3 参数 | 4 参数 | 5 参数 | 6 参数 |
|------|--------|--------|--------|--------|--------|--------|--------|
| 监听 | `AddEventListener(int, Action)` | `AddEventListener<T>(int, Action<T>)` | `AddEventListener<T1,T2>(int, Action<T1,T2>)` | 支持 | 支持 | 支持 | 支持 |
| 移除 | `RemoveEventListener(int, Action)` | `RemoveEventListener<T>(int, Action<T>)` | `RemoveEventListener<T1,T2>(int, Action<T1,T2>)` | 支持 | 支持 | 支持 | 支持 |
| UI 监听 | `AddUIEvent(int, Action)` | `AddUIEvent<T>(int, Action<T>)` | `AddUIEvent<T1,T2>(int, Action<T1,T2>)` | 支持 | 支持 | 支持 | 支持 |
| string 发送 | `Send(string)` | `Send<T>(string, T)` | `Send<T1,T2>(string, T1, T2)` | 支持 | 支持 | 支持 | 不支持 |
| int 发送 | 通过 `EventDispatcher` 或生成常量配套 API | - | - | - | - | - | `Send<T1,T2,T3,T4,T5,T6>(int, T1, T2, T3, T4, T5, T6)` |

注意源码不对称：`GameEvent.Send<T1,...,T6>` 的第一参数是 `int eventType`，不是 `string eventType`。不要写 `GameEvent.Send<T1,...,T6>("EventName", ...)`。

## 跨模块事件

### 接口事件全链路

定义接口：

```csharp
using DGame;

namespace GameLogic
{
    [EventInterface(EEventGroup.GroupUI)]
    public interface ICommonUI
    {
        void ShowWaitingUI(uint waitFuncID, uint textID, System.Action callback);
    }
}
```

编译时生成的关键形态：

```csharp
namespace GameLogic
{
    public partial class ICommonUI_Event
    {
        public static readonly int ShowWaitingUI = StringId.StringToHash("ICommonUI_Event.ShowWaitingUI");
    }

    public partial class ICommonUI_Gen : ICommonUI
    {
        public void ShowWaitingUI(uint waitFuncID, uint textID, System.Action callback)
        {
            m_dispatcher.Send(ICommonUI_Event.ShowWaitingUI, waitFuncID, textID, callback);
        }
    }

    public partial class GameEventHelper
    {
        public partial class AddEvent
        {
            public partial class CommonUI
            {
                public static void ShowWaitingUI(System.Action<uint, uint, System.Action> action)
                {
                    GameEvent.AddEventListener(ICommonUI_Event.ShowWaitingUI, action);
                }
            }
        }

        public partial class RemoveEvent
        {
            public partial class CommonUI
            {
                public static void ShowWaitingUI(System.Action<uint, uint, System.Action> action)
                {
                    GameEvent.RemoveEventListener(ICommonUI_Event.ShowWaitingUI, action);
                }
            }
        }
    }
}
```

热更入口初始化：

```csharp
public static void Entrance(object[] objects)
{
    GameEventLauncher.Init();
}
```

发送接口事件：

```csharp
GameEvent.Get<ICommonUI>().ShowWaitingUI(waitFuncID, textID, callback);
```

UI 内监听，优先使用 `AddUIEvent` 自动清理：

```csharp
protected override void RegisterEvent()
{
    AddUIEvent<uint, uint, Action>(ICommonUI_Event.ShowWaitingUI, OnShowWaitingUI);
}
```

非 UI 类监听，可用 `GameEventHelper` 包装减少泛型签名错误：

```csharp
public void Init()
{
    GameEventHelper.AddEvent.CommonUI.ShowWaitingUI(OnShowWaitingUI);
}

public void Destroy()
{
    GameEventHelper.RemoveEvent.CommonUI.ShowWaitingUI(OnShowWaitingUI);
}

private void OnShowWaitingUI(uint waitFuncID, uint textID, Action callback)
{
    // ...
}
```

普通监听仍然可用，但非 UI 模块优先使用 `GameEventHelper.AddEvent.Xxx.Xxx` 与 `GameEventHelper.RemoveEvent.Xxx.Xxx`；只有生成门面不适用或框架底层场景才直接调用 `GameEvent.AddEventListener/RemoveEventListener`。

```csharp
GameEvent.AddEventListener<uint, uint, Action>(ICommonUI_Event.ShowWaitingUI, OnShowWaitingUI);
GameEvent.RemoveEventListener<uint, uint, Action>(ICommonUI_Event.ShowWaitingUI, OnShowWaitingUI);
```

非 UI 类必须自己移除监听。

## 初始化

热更入口必须调用：

```csharp
GameEventLauncher.Init();
```

如果接口事件无响应，优先检查：

1. `GameEventLauncher.Init()` 是否执行。
2. 接口是否带 `[EventInterface(EEventGroup...)]`。
3. 生成的 `*_Event` ID 是否存在。
4. 发送和监听的参数签名是否一致。

## 常见错误

| 错误 | 正确做法 |
|------|---------|
| UI 使用 `GameEvent.AddEventListener` 后忘记移除 | `RegisterEvent` 中用 `AddUIEvent` |
| 非 UI 类使用 `AddUIEvent` | 使用 `GameEvent.AddEventListener` 并自行移除 |
| 接口事件未加 `[EventInterface]` | 按职责选择 `EEventGroup` |
| 手写事件 ID | 优先使用生成的 `*_Event` 常量 |
| 6 参数 `Send` 传字符串 | 使用生成的 int 事件 ID，或改用接口事件 |

## 交叉引用

| 关联主题 | 文档 | 说明 |
|---------|------|------|
| 事件反模式 | event-antipatterns.md | 监听未清理、幻觉 API、生命周期时序等常见错误 |
| UI 内事件 | ui-lifecycle.md | `AddUIEvent` 在 `RegisterEvent` 中注册、随窗口自动清理 |
| 模块访问 | modules.md | 跨模块通过 `GameModule.XXX` 获取，配合事件解耦 |
