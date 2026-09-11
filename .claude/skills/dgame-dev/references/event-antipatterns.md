# 事件系统反模式与避坑指南

> **适用场景**：事件内存泄漏排查、接口事件无响应调试、事件风暴问题定位 | **关联文档**：[event-system.md](event-system.md)（核心 API）、[ui-lifecycle.md](ui-lifecycle.md)（AddUIEvent 自动清理）

> 本文档是 [event-system.md](event-system.md) 的进阶补充，聚焦于难以排查的陷阱和反模式。

---

## 一、内存泄漏反模式

### 反模式 1：UIWindow 外部直接 AddEventListener

```csharp
// 错误：窗口销毁后监听不会随 UI 生命周期清理
public class BagWindow : UIWindow
{
    protected override void OnCreate()
    {
        GameEventHelper.AddEvent.Player.OnGoldChanged(OnGoldChanged);
    }
}

// 正确：UI 事件在 RegisterEvent 内使用 AddUIEvent
public class BagWindow : UIWindow
{
    protected override void RegisterEvent()
    {
        AddUIEvent<int>(IPlayer_Event.OnGoldChanged, OnGoldChanged);
    }
}
```

**为何危险**：`AddUIEvent` 由 `UIBase` 持有 `GameEventDriver`，UI 销毁时统一移除；直接调用 `GameEvent.AddEventListener` 不会跟随窗口生命周期。

---

### 反模式 2：非 UI 类注册后不移除

```csharp
// 错误：模块或普通类注册事件但没有释放点
public class PlayerService
{
    public void Init()
    {
        GameEvent.AddEventListener<int>(IPlayer_Event.OnGoldChanged, OnGoldChanged);
    }
}

// 正确：保存 handler，并在 Dispose/OnDestroy 中手动移除
public class PlayerService : IDisposable
{
    public void Init()
    {
        GameEvent.AddEventListener<int>(IPlayer_Event.OnGoldChanged, OnGoldChanged);
    }

    public void Dispose()
    {
        GameEventHelper.RemoveEvent.Player.OnGoldChanged(OnGoldChanged);
    }
}
```

DGame 没有公开的 `GameEventMgr.Clear()` 业务入口。非 UI 生命周期需要自己定义清理边界。

---

### 反模式 3：Lambda 捕获导致无法精确移除

```csharp
// 错误：RemoveEventListener 传入新 Lambda 时无法匹配原委托
GameEvent.AddEventListener<int>(eventId, value => Refresh(value));
GameEvent.RemoveEventListener<int>(eventId, value => Refresh(value));

// 正确：使用命名方法
private void OnValueChanged(int value) => Refresh(value);

public void Init() => GameEventHelper.AddEvent.Player.OnValueChanged(OnValueChanged);
public void Dispose() => GameEventHelper.RemoveEvent.Player.OnValueChanged(OnValueChanged);
```

---

## 二、接口事件无响应

### 反模式 4：遗忘 GameEventLauncher.Init()

```csharp
// 错误：接口事件生成器没有初始化
public static void Entrance(object[] objects)
{
    StartGame();
}

// 正确：GameStart.Entrance 内先初始化事件系统
public static void Entrance(object[] objects)
{
    GameEventLauncher.Init();
    StartGame();
}
```

`GameStart.Entrance` 位于 `Assets/Scripts/HotFix/GameLogic/GameStart.cs`，接口事件初始化调用 `GameEventLauncher.Init()`。

---

### 反模式 5：接口缺少 [EventInterface] 特性

```csharp
// 错误：不会生成 Ixxx_Event
public interface IBattleEvent
{
    void OnHpChanged(int hp);
}

// 正确：指定 DGame 已有事件组
[EventInterface(EEventGroup.GroupLogic)]
public interface IBattleEvent
{
    void OnHpChanged(int hp);
}
```

当前 `EEventGroup` 只有 `GroupUI`、`GroupLogic`。新增组前先确认生成器和分层用途。

---

## 三、类型不匹配导致运行时异常

### 反模式 6：Send 与 AddEventListener 泛型参数不一致

```csharp
// 错误：string 事件发送 int，监听 float
GameEvent.Send<int>("OnGoldChanged", 100);
GameEvent.AddEventListener<float>(StringId.StringToHash("OnGoldChanged"), OnGoldChanged);

// 错误：string 事件参数顺序不一致
GameEvent.Send<int, string>("OnItemChanged", 1, "item");
GameEvent.AddEventListener<string, int>(StringId.StringToHash("OnItemChanged"), OnItemChanged);

// 正确：优先使用接口事件，编译期约束签名
GameEvent.Get<IPlayer>().OnGoldChanged(100);
```

---

### 反模式 7：混用 int 和 string 事件 ID

```csharp
// 错误：注册和发送走不同 ID
GameEvent.AddEventListener<int>(1001, OnGoldChanged);
GameEvent.Send<int>("OnGoldChanged", 50);

// 正确：统一使用生成的事件常量
AddUIEvent<int>(IPlayer_Event.OnGoldChanged, OnGoldChanged);
GameEvent.Get<IPlayer>().OnGoldChanged(50);
```

---

## 四、生命周期时序陷阱

### 反模式 8：在 RegisterEvent 中依赖 OnCreate 初始化的数据

```csharp
// UIWindow 生命周期：ScriptGenerator -> BindMemberProperty -> RegisterEvent -> OnCreate/OnRefresh

// 错误：_currentGold 在 OnCreate 才赋值
protected override void RegisterEvent()
{
    AddUIEvent<int>(IPlayer_Event.OnGoldChanged, RefreshGold);
    RefreshGold(_currentGold);
}

// 正确：RegisterEvent 只注册，刷新放到 OnCreate/OnRefresh
protected override void RegisterEvent()
{
    AddUIEvent<int>(IPlayer_Event.OnGoldChanged, RefreshGold);
}

protected override void OnRefresh()
{
    RefreshGold(PlayerModel.Gold);
}
```

---

### 反模式 9：Widget 销毁阶段依赖父窗口事件

```csharp
// UIWindow 销毁顺序：RemoveAllUIEvents → 子 Widget.OnDestroy → UIWindow.OnDestroy
// 错误：销毁阶段发送事件给父窗口，父窗口可能已经清理 UI 事件
protected override void OnDestroy()
{
    GameEvent.Send(ICommonUI_Event.OnWidgetDestroyed);
}

// 正确：Widget 与 Window 通过公开方法或刷新入口协作
public void SetOwner(BagWindow owner)
{
    m_owner = owner;
}
```

Widget 不要把 `OnDestroy` 当作业务通信入口；销毁时只释放自己持有的资源和监听。

---

## 五、事件风暴反模式

### 反模式 10：事件回调中触发同类事件

```csharp
// 错误：回调内再次触发同类事件，可能递归或形成状态震荡
private void OnGoldChanged(int gold)
{
    m_textGold.text = gold.ToString();
    GameEvent.Get<IPlayer>().OnGoldChanged(gold - 1);
}

// 正确：事件回调只做视图刷新或轻量分发
private void OnGoldChanged(int gold)
{
    m_textGold.text = gold.ToString();
}
```

---

### 反模式 11：高频事件直接更新大量 UI

```csharp
// 错误：每帧广播位置变化，所有监听 UI 都刷新
private void Update()
{
    GameEvent.Get<IBattle>().OnHeroPositionChanged(m_hero.position);
}

// 正确：只在值真正变化或固定节奏下刷新
protected override void OnUpdate()
{
    if (Time.time < m_nextRefreshTime)
    {
        return;
    }

    m_nextRefreshTime = Time.time + 0.1f;
    RefreshPosition(m_hero.position);
}
```

高频数据优先本地轮询、节流或聚合。不要把事件系统当作每帧数据总线。固定节奏刷新也可用 `GameModule.GameTimerModule` 定时器替代 `OnUpdate` 手写计时：

```csharp
// 每 0.1 秒刷新一次，而不是每帧或每次事件都刷
private GameTimer m_refreshTimer;

protected override void OnCreate()
{
    m_refreshTimer = GameModule.GameTimerModule.CreateLoopGameTimer(0.1f, _ => RefreshPosition(m_hero.position));
}

protected override void OnDestroy()
{
    GameModule.GameTimerModule.DestroyGameTimer(m_refreshTimer);
    m_refreshTimer = null;
}
```

---

## 六、DGame 不存在的 API（AI 常见幻觉）

```csharp
// 以下 API 在 DGame 中不存在，不要作为正确示例：
GameEventMgr.Clear();
GameEvent.Shutdown();
GameEvent.UnRegisterAll();
GameEvent.ClearAll();
GameEvent.RemoveAll(eventId);
GameEvent.RegisterListener<IPlayer>(impl);
GameEventLauncher.Init();
```

正确替代：

| 目的 | DGame 正确做法 |
|------|---------------|
| UI 生命周期内监听 | `AddUIEvent(eventId, handler)` |
| 非 UI 手动监听 | `GameEvent.AddEventListener(eventId, handler)` |
| 非 UI 手动移除 | `GameEvent.RemoveEventListener(eventId, handler)` |
| GameEventHelper 包装监听 | `GameEventHelper.AddEvent.<组>.<方法>(handler)` |
| GameEventHelper 包装移除 | `GameEventHelper.RemoveEvent.<组>.<方法>(handler)`，与 AddEvent 配套调用 |
| 接口事件初始化 | `GameEventLauncher.Init()` |
| 接口事件发送 | `GameEvent.Get<IEvent>().Method(args)` |
| 全局销毁事件系统 | 框架内部 `GameEvent.EventMgr.Destroy()`；普通业务不要调用 |

---

## 交叉引用

| 主题 | 文档 |
|------|------|
| 事件系统核心 API | [event-system.md](event-system.md) |
| UIWindow 生命周期与 AddUIEvent | [ui-lifecycle.md](ui-lifecycle.md) |
| 高频刷新替代方案 | [modules.md](modules.md) |
| 问题排查 | [troubleshooting.md](troubleshooting.md) |
