# DGame 模块 API 速查

> **适用场景**：使用 GameModule.ResourceModule、UIModule、AudioModule、SceneModule、GameTimerModule、RedDotModule 等模块 API | **关联文档**：[ui-lifecycle.md](ui-lifecycle.md)、[resource-api.md](resource-api.md)、[event-system.md](event-system.md)

## 核心 API：GameModule 统一访问入口

热更业务通过 `GameLogic.GameModule` 访问模块。`GameModule` 会缓存底层模块，避免业务代码散落 `ModuleSystem.GetModule<T>()`。

```csharp
GameModule.RootModule          // RootModule
GameModule.FsmModule           // IFsmModule
GameModule.SensitiveWordModule // ISensitiveWordModule
GameModule.AnimModule          // IAnimModule
GameModule.ResourceModule      // IResourceModule
GameModule.AudioModule         // IAudioModule
GameModule.SceneModule         // ISceneModule
GameModule.GameTimerModule     // IGameTimerModule
GameModule.Input               // GameLogic.IInputModule，ENABLE_INPUT_SYSTEM 下可用
GameModule.LocalizationModule  // ILocalizationModule
GameModule.GameObjectPool      // IGameObjectPoolModule
GameModule.UIModule            // UIModule.Instance
GameModule.RedDotModule        // RedDotModule.Instance

GameModule.Destroy()           // 清空缓存，仅退出/热更销毁时调用
```

> **注意**：`UIModule`、`RedDotModule` 是热更层单例；其他大多数模块来自 `ModuleSystem.GetModule<T>()`。

---

## 使用模式

### GameTimerModule 计时器

```csharp
private GameTimer m_timer;

protected override void OnCreate()
{
    m_timer = GameModule.GameTimerModule.CreateOnceGameTimer(1f, _ => Refresh());
}

protected override void OnDestroy()
{
    GameModule.GameTimerModule.DestroyGameTimer(m_timer);
    m_timer = null;
}
```

常用 API：

- 创建：`CreateOnceGameTimer`、`CreateLoopGameTimer`、`CreateUnscaledOnceGameTimer`、`CreateUnscaledLoopGameTimer`、`CreateLoopCountGameTimer`
- 控制：`Pause(timer)`、`Resume(timer)`、`Restart(timer)`、`Reset(timer, interval, isLoop, isUnscaled[, handler])`（两个重载）
- 查询：`IsRunning(timer)`、`GetTimerLeft(timer)`
- 销毁：`DestroyGameTimer(timer)`、`DestroyAllGameTimer()`

### SceneModule 场景管理

```csharp
// 主场景：Single 模式默认卸载旧场景并 GC
await GameModule.SceneModule.LoadSceneAsync("BattleScene",
    LoadSceneMode.Single, progressCallBack: p => SetProgress(p));

// 叠加场景：Additive 模式共存，用 ActivateScene 切换激活场景
await GameModule.SceneModule.LoadSceneAsync("UIScene", LoadSceneMode.Additive);
GameModule.SceneModule.ActivateScene("UIScene");

await GameModule.SceneModule.UnloadAsync("OldScene");
GameModule.SceneModule.Unload("OldScene");
```

`LoadSceneAsync(location, sceneMode, suspendLoad, priority, gcCollect, progressCallBack)`：`gcCollect` 仅主场景生效；`ActivateScene(location)` 在多场景共存时切换激活场景。

### AudioModule 音频

```csharp
GameModule.AudioModule.Play(DGame.AudioType.UISound, soundCfg.Location, isInPool: true);
GameModule.AudioModule.Play(DGame.AudioType.Music, bgmPath, isLoop: true);
GameModule.AudioModule.Stop(DGame.AudioType.Music, fadeout: true);
GameModule.AudioModule.StopAll(fadeout: true);

GameModule.AudioModule.Volume = 0.8f;
GameModule.AudioModule.MusicVolume = 0.6f;
GameModule.AudioModule.SoundVolume = 1.0f;
GameModule.AudioModule.UISoundVolume = 1.0f;
GameModule.AudioModule.VoiceVolume = 1.0f;
```

开关类属性：`Enabled`、`MusicEnable`、`SoundEnable`、`UISoundEnable`、`VoiceEnable`。

`Stop` / `StopAll` 的真实签名都需要 `bool fadeout` 参数。

### LocalizationModule 本地化

```csharp
GameModule.LocalizationModule.SetLocalizationHelper(new DGameLocalizationHelper());
GameModule.LocalizationModule.SetLanguage(LocalAreaType.CN);
```

语言变化通过 `ILocalization_Event.OnLanguageChanged` 通知 UI。

### SensitiveWordModule 敏感词

```csharp
GameModule.SensitiveWordModule.SetKeywords(keywords);
bool hasSensitiveWord = GameModule.SensitiveWordModule.ContainsSensitiveWord(input);
string safeText = GameModule.SensitiveWordModule.ReplaceSensitiveWords(input);
```

常用 API：`SetKeywords(keywords[, blacklistTypes])` 设词库、`ContainsSensitiveWord`/`ReplaceSensitiveWords(content[, replaceChar])` 检测替换、`FindAllSensitiveWords`/`FindFirst`/`FindAll` 黑名单分级查询。

### AnimModule 动画图

```csharp
var playable = GameModule.AnimModule.CreateAnimPlayable(animator, animations);
playable.Play("Idle", fadeDuration: 0.2f);
GameModule.AnimModule.DestroyAnimPlayable(playable);
```

常用 API：`ContainsAnimPlayable`/`GetAnimPlayable(name)`、`CreateAnimPlayable(animator[, animations])`（三个重载，clip 列表可选）、`DestroyAnimPlayable(animPlayable | name)`。创建的 `IAnimPlayable` 必须有明确销毁点，避免 PlayableGraph 长期保留。

### FsmModule 有限状态机

入口：

```csharp
var fsm = GameModule.FsmModule.CreateFsm(owner,
    new IdleState(),
    new BattleState());

fsm.Start<IdleState>();
```

常用 API：

- `ContainsFsm<T>()` / `ContainsFsm<T>(string name)`
- `GetFsm<T>()` / `GetFsm<T>(string name)`
- `CreateFsm<T>(owner, params IFsmState<T>[] states)`
- `CreateFsm<T>(string name, owner, params IFsmState<T>[] states)`
- `DestroyFsm<T>()` / `DestroyFsm<T>(string name)` / `DestroyFsm(fsm)`

规则：业务层通过 `GameModule.FsmModule` 获取，不散落 `ModuleSystem.GetModule<IFsmModule>()`。创建的状态机必须有明确销毁点，避免 owner 已销毁但状态仍在更新。

状态定义（`IFsmState<T>`）：

```csharp
public sealed class IdleState : IFsmState<MyOwner>
{
    public void OnCreate(IFsm<MyOwner> fsm) { }   // 状态被 CreateFsm 时构建
    public void OnEnter() { }                     // 进入状态
    public void OnUpdate(float elapse, float real) { }
    public void OnFixedUpdate() { }
    public void OnExit() { }                      // 退出状态
    public void OnDestroy() { }
}
```

状态切换与共享数据（在状态内通过持有的 `IFsm<T>` 调用）：

- 切换：`fsm.SwitchState<BattleState>()` / `fsm.SwitchState(type)`
- 共享数据：`AddShareData`、`GetShareData<TData>`、`TryGetShareData<TData>`、`UpdateShareData`、`RemoveShareData` / `ClearShareData`

> **注意**：状态切换用 `SwitchState`、共享数据用 `AddShareData`/`GetShareData`；`OnEnter`/`OnExit` 无参，`fsm` 在 `OnCreate` 时注入。

### GameObjectPool 对象池

入口：

```csharp
await GameModule.GameObjectPool.CreateGameObjectPoolAsync(
    location,
    initCapacity: 5,
    maxCapacity: 50,
    autoDestroyTime: 30f,
    ct: cancellationToken);

var go = await GameModule.GameObjectPool.SpawnAsync(location, parent, cancellationToken);
GameModule.GameObjectPool.Recycle(go);
```

常用 API：

- `CreateGameObjectPoolAsync(location, initCapacity, maxCapacity, autoDestroyTime, dontDestroy, allowMultiSpawn, ct)`
- `SpawnAsync(location, ct)`
- `SpawnAsync(location, parent, ct)`
- `SpawnAsync(location, parent, position, rotation, ct)`
- `Recycle(gameObject)`：回收到池。
- `Remove(gameObject)`：从池管理中移除并丢弃。
- `DestroyPool(location)` / `DestroyAllPool(includeAll)`
- `TryGetGameObjectPool(location, out pool)` / `GetDebugInfos(results)`

规则：池化对象不要手动 `Destroy` 后再 `Recycle`。对象不再复用时用 `Remove`，整池不用时用 `DestroyPool`。

### DataCenterModule 数据中心模块

热更业务的角色数据、跨 UI/模块共享状态可收口到 `DataCenterModule<T>`：继承 `DataCenterModule<自身类型>` 的类由 `DataCenterModuleGenerator` 源生成器自动扫描注册（勿手改生成文件 `DataCenterModule_Gen.g.cs`），无需手动挂接。生命周期回调：`OnInit`（注册时触发）、`OnRoleLogin`、`OnRoleLogout`（登出/切角色清理）、`OnMainPlayerMapChange`。

示例：

```csharp
namespace GameLogic
{
    public sealed class BagDataCenter : DataCenterModule<BagDataCenter>
    {
        private readonly Dictionary<int, int> m_itemCounts = new Dictionary<int, int>();

        public override void OnInit() => m_itemCounts.Clear();
        public override void OnRoleLogin() { /* 拉取或重建当前角色背包缓存。 */ }
        public override void OnRoleLogout() => m_itemCounts.Clear();
        public override void OnMainPlayerMapChange() { /* 地图切换后刷新的角色状态。 */ }

        public int GetCount(int itemId) => m_itemCounts.TryGetValue(itemId, out var c) ? c : 0;
    }
}
```

使用：`int count = BagDataCenter.Instance.GetCount(itemId);`

规则：

- 类放在 `GameLogic` 命名空间，继承 `DataCenterModule<自身类型>`，且可 `new()`（不依赖有参构造）。
- 不要手动改 `DataCenterSys.m_dataCenterModuleList`、`InitModule()` 或生成文件。
- 只放角色级/客户端级状态和生命周期回调；纯配置查询仍走 `ConfigMgr`，不要用 DataCenter 替代。
- 需要登出清理的数据放 `OnRoleLogout()`，避免切账号后旧角色状态残留。

### MemoryPool 内存池

DGame 内存池入口是 `MemoryPool.Spawn<T>()` / `MemoryPool.Release(...)`，不是 `Acquire`。

```csharp
public sealed class MyPayload : MemoryObject
{
    public int Value;

    public override void OnRelease()
    {
        Value = 0;
    }
}

var payload = MemoryPool.Spawn<MyPayload>();
payload.Value = 100;
MemoryPool.Release(payload);
```

`MemoryObject` 提供同名封装：

```csharp
var driver = MemoryObject.Spawn<GameEventDriver>();
MemoryObject.Release(driver);
```

常用 API：

- `MemoryPool.Spawn<T>() where T : class, IMemory, new()`
- `MemoryPool.Release(IMemory memory)`
- `MemoryPool.Release<T>(List<T> memories)`
- `MemoryPool.Add<T>(int count)` / `MemoryPool.Remove<T>(int count)`
- `MemoryPool.ClearMemoryCollector<T>()` / `MemoryPool.ClearAll()`
- `MemoryPool.EnableStrictCheck`：严格检查开关，调试时使用。

规则：

- 可池化对象实现 `IMemory`，或继承 `MemoryObject` 并在 `OnRelease()` 中清理状态。
- `Release` 后该对象已回池，禁止再访问其字段或再次 `Release`（二次 Release 会污染池、导致对象被多处复用）；`Release` 后立刻把本地引用置 `null`。
- 不要把 Unity `GameObject` 生命周期和 `MemoryPool` 混用；GameObject 复用使用 `GameObjectPool`。

### DLogger 日志

入口位于 `DGame/Runtime/Core/DGameLog/DLogger.cs`：

```csharp
DLogger.Log("debug message");
DLogger.Info("module ready");
DLogger.Warning("asset missing: {0}", location);
DLogger.Error("load failed: {0}", error);
DLogger.Fatal(exception);
DLogger.Assert(condition, "unexpected state");
```

`DLogger` 的 debug 级方法带 `[Conditional]` 编译宏，发布包会自动剥离（具体宏以 `DLogger.cs` 标注为准）。新增业务日志优先用 `DLogger`，不要直接散落 `UnityEngine.Debug.Log`。

---

## 新增模块规则

1. 先搜索 DGame 是否已有封装：`GameTimer`、`InputModule`、`AnimModule`、`MemoryPool`、`GameObjectPool`、`GameEvent`、`LocalizationModule`。
2. 框架模块放 `DGame/Runtime/Module` 并注册到 `ModuleSystem`。
3. 业务单例模块放 `GameLogic/Module`，沿用 `Singleton<T>`。
4. 需要业务统一访问时，在 `GameModule` 增加缓存属性。

---

## 常见错误

| 错误写法 | 正确写法 | 原因 |
|---------|---------|------|
| `ModuleSystem.GetModule<IResourceModule>()` | `GameModule.ResourceModule` | 业务层使用统一缓存入口 |
| 计时器不销毁 | `DestroyGameTimer(m_timer)` | 回调可能引用已销毁 UI |
| `GameModule.UI` | `GameModule.UIModule` | DGame 实际属性名是 `UIModule` |
| `GameModule.Timer` | `GameModule.GameTimerModule` | DGame 实际属性名是 `GameTimerModule` |
| `MemoryPool.Acquire<T>()` | `MemoryPool.Spawn<T>()` | DGame 实际方法名是 `Spawn` |
| `AudioModule.StopAll()` | `AudioModule.StopAll(bool fadeout)` | DGame 需要显式 fadeout |
| Runtime 模块引用 GameLogic 单例 | 通过接口/事件解耦 | 保持程序集边界 |
| 池化对象直接 `Destroy` | `GameModule.GameObjectPool.Recycle/Remove` | 保持池状态一致 |
| 业务直接 `Debug.Log` | `DLogger` | 保持日志宏和项目日志通道一致 |

---

## 交叉引用

| 关联主题 | 文档 | 说明 |
|---------|------|------|
| UI 管理 | ui-lifecycle.md | `GameModule.UIModule` 的窗口生命周期与层级 |
| 资源加载/卸载 | resource-api.md | `GameModule.ResourceModule` 的完整 API 与生命周期 |
| 事件系统 | event-system.md | `GameEvent` 模块间解耦，`AddUIEvent` UI 内部事件 |
| 红点系统 | reddot-system.md | `GameModule.RedDotModule` 红点树注册与刷新 |
