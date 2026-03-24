# DGame 客户端代码规范和设计模式

当需求涉及代码命名、UI 节点命名、异步、日志、中文文本、模块设计、代码审查或 Git 协作时，优先阅读本文件。

本文件是面向 Codex skill 的压缩版 reference。目标是：

- 保留原始规范意图
- 删除重复和互相打架的描述
- 让 Codex 快速定位“应该怎么写”

## 使用原则

1. 先遵循目标模块附近的现有实现，再用本文件做统一收敛。
2. 热更业务层代码访问模块，统一通过 `GameModule` 获取，不直接保存底层模块裸引用。
3. 规则冲突时，以“更接近项目真实接口、更少歧义、更易维护”为准。
4. 新增代码优先最小完整实现，不顺手扩散无关重构。

## 命名规范

### C# 类型命名

| 类型类别 | 命名规则 | 正例 | 反例 |
| --- | --- | --- | --- |
| 模块接口 | `I` + 业务语义 + `Module` | `IMailModule` | `MailModuleInterface` |
| 模块实现 | 业务语义 + `Module` | `MailModule` | `MailSystem` |
| 事件接口 | `I` + 业务语义 + `Event` | `IBattleEvent` | `EventBattle` |
| `UIWindow` 子类 | 业务语义 + `UI` 或 `Window` | `LoginUI` `MainWindow` | `UILogin` |
| `UIWidget` 子类 | 业务语义 + `Item` 或 `Widget` | `RewardItem` `MailItemWidget` | `WidgetReward` |
| `BaseChildPage` 子类 | 业务语义 + `Page` | `MailPage` | `PageMail` |
| 流程状态 | 业务语义 + `Procedure` | `LaunchProcedure` | `LaunchState` |
| 状态机状态 | 业务语义 + `State` | `BattleState` | `StateBattle` |
| 系统类 | 业务语义 + `System` | `GuideSystem` | `GuideMgr` |
| Luban 配置 | 表类型 `TbXxxConfig`，行类型 `XxxConfig` | `TbItemConfig` `ItemConfig` | `ItemCfg` |
| 内存池对象 | 类型实现 `IMemory` | `BulletItem : IMemory` | `BulletPoolObject` |
| `Singleton` 管理类 | 业务语义 + `Mgr` 或 `Manager` | `AudioMgr` `SceneManager` | `MgrAudio` |
| 枚举 | `PascalCase` | `MailType.System` | `MAIL_TYPE.SYSTEM` |

### 字段命名

```csharp
// 私有字段：m_ + 小驼峰
private PlayerData m_playerData;

// readonly 私有字段：m_ + 小驼峰
private readonly CancellationToken m_destroyToken;

// 静态私有字段：s_ + 小驼峰
private static Dictionary<int, ItemConfig> s_itemConfigMap;

// 常量：全大写 + 下划线
private const int MAX_RETRY_COUNT = 3;

// 公开字段：大驼峰
public int RetryCount;

// 事件 / 委托实例：On + 语义名
public Action OnLoginCompleted;

// [SerializeField]：遵循目标区域现有风格；当前项目大量现有代码仍使用 m_
[SerializeField] private int m_fontSize;
```

### 方法命名

```csharp
public void RefreshView()
{
}

public void LoadConfig()
{
}

private void OnClickConfirm()
{
}

public void Init()
{
}

public void Initialize()
{
}

public RewardWidget CreateRewardWidget()
{
    return null;
}

public async UniTask LoadMailListAsync()
{
}
```

规则：

- 方法名统一使用大驼峰。
- 行为方法动词开头。
- 事件回调使用 `On + 方法名`。
- 初始化方法使用 `Init` / `Initialize`。
- 创建方法使用 `Create`。
- 异步方法使用 `UniTask` / `UniTask<T>`。

## UI 节点命名规范

UI 节点命名以 `Assets/Editor/UIScriptGenerator/UIScriptGeneratorSettings.asset` 中的生成规则为准。当前项目使用 `m_` 风格前缀。

| 节点前缀 | 组件类型 | 是否 Widget | 示例 |
| --- | --- | --- | --- |
| `m_go` | `GameObject` | 否 | `m_goContent` |
| `m_item` | `GameObject` | 是 | `m_itemReward` |
| `m_tf` | `Transform` | 否 | `m_tfRoot` |
| `m_rect` | `RectTransform` | 否 | `m_rectContent` |
| `m_text` | `Text` | 否 | `m_textTitle` |
| `m_richText` | `RichTextItem` | 否 | `m_richTextDesc` |
| `m_btn` | `Button` | 否 | `m_btnConfirm` |
| `m_img` | `Image` | 否 | `m_imgIcon` |
| `m_scroll` | `ScrollRect` | 否 | `m_scrollMailList` |
| `m_input` | `InputField` | 否 | `m_inputName` |
| `m_slider` | `Slider` | 否 | `m_sliderProgress` |
| `m_group` | `ToggleGroup` | 否 | `m_groupTab` |
| `m_canvasGroup` | `CanvasGroup` | 否 | `m_canvasGroupRoot` |
| `m_toggle` | `Toggle` | 否 | `m_toggleAuto` |
| `m_tmp` | `TextMeshProUGUI` | 否 | `m_tmpTitle` |

补充约束：

- 同一窗口内不要混用另一套命名体系。
- 只有需要进入生成器绑定的节点才使用这些前缀。
- 其他低频前缀以生成器配置为准。

## 异步编程规范

### 基本规则

```csharp
// ✅ 无返回值异步：UniTask
public async UniTask LoadMailListAsync()
{
    await RequestMailListAsync();
    BindMailData();
    RefreshView();
}

// ✅ 有返回值异步：UniTask<T>
public async UniTask<MailDetailData> LoadMailDetailAsync(int mailId)
{
    return await RequestMailDetailAsync(mailId);
}

// ✅ async 调用要么 await
public async UniTask EnterGameAsync()
{
    await LoadConfigAsync();
    await PreloadAsync();
}

// ✅ 要么 Forget() + 错误处理
public void Open()
{
    OpenSafeAsync().Forget();
}

private async UniTaskVoid OpenSafeAsync()
{
    try
    {
        await LoadMailListAsync();
    }
    catch (Exception e)
    {
        DLogger.Error(e.ToString());
    }
}
```

### CancellationToken 使用

```csharp
// ✅ 异步链路透传 CancellationToken
public async UniTask LoadMailDetailAsync(int mailId, CancellationToken cancellationToken)
{
    var detailData = await RequestMailDetailAsync(mailId, cancellationToken);
    cancellationToken.ThrowIfCancellationRequested();
    RefreshDetail(detailData);
}

// ✅ 热更业务层通过 GameModule 获取模块
public async UniTask LoadPrefabAsync(CancellationToken cancellationToken)
{
    GameObject prefab = await GameModule.ResourceModule.LoadAssetAsync<GameObject>("MailPanel", cancellationToken);
    cancellationToken.ThrowIfCancellationRequested();
    CreateView(prefab);
}

// ✅ 自建 CancellationTokenSource 生命周期结束时 Cancel + Dispose
private CancellationTokenSource m_destroyCts;

public void Init()
{
    m_destroyCts = new CancellationTokenSource();
}

public void Dispose()
{
    m_destroyCts?.Cancel();
    m_destroyCts?.Dispose();
    m_destroyCts = null;
}
```

### 并发加载

```csharp
// ✅ 多资源并发加载：UniTask.WhenAll
public async UniTask PreloadAsync(CancellationToken cancellationToken)
{
    UniTask panelTask = GameModule.ResourceModule.LoadAssetAsync<GameObject>("MailPanel", cancellationToken);
    UniTask atlasTask = GameModule.ResourceModule.LoadAssetAsync<SpriteAtlas>("CommonAtlas", cancellationToken);
    UniTask audioTask = GameModule.ResourceModule.LoadAssetAsync<AudioClip>("Click", cancellationToken);
    await UniTask.WhenAll(panelTask, atlasTask, audioTask);
}

// ✅ 批量加载：Select + UniTask.WhenAll
public async UniTask<IList<GameObject>> LoadItemsAsync(IEnumerable<string> assetNames, CancellationToken cancellationToken)
{
    UniTask<GameObject>[] tasks = assetNames
        .Select(assetName => GameModule.ResourceModule.LoadAssetAsync<GameObject>(assetName, cancellationToken))
        .ToArray();
    return await UniTask.WhenAll(tasks);
}
```

### 禁止的异步模式

```csharp
// ❌ 禁止 Task
public async Task LoadConfigAsync()
{
    await Task.Delay(1000);
}

// ❌ 禁止 Coroutine 业务异步
public IEnumerator LoadDataCoroutine()
{
    yield return null;
}

// ❌ 禁止 async void 普通业务方法
public async void LoadData()
{
    await RequestDataAsync();
}

// ❌ 禁止阻塞等待
public void Init()
{
    var config = LoadConfigAsync().GetAwaiter().GetResult();
}

// ❌ 禁止在 Update 中 await
private async void Update()
{
    await LoadDataAsync();
}
```

## 日志输出接口规范

```csharp
// ✅ 运行时代码统一使用 DLogger
public void LoadMailList()
{
    DLogger.Info("LoadMailList start");
    DLogger.Warning("[MailModule] mailbox is empty");
    DLogger.Error("[MailModule] LoadMailList failed, mailboxId=1001");
}

// ❌ 运行时代码禁止 UnityEngine.Debug
public void RefreshView()
{
    Debug.Log("RefreshView");
}

// ✅ 编辑器代码无约束
[MenuItem("DGame Tools/TestLog")]
private static void TestLog()
{
    Debug.Log("Editor test log");
}
```

补充约束：

- 临时测试日志使用完后要及时清理。

## 中文文本使用规范

```csharp
// ✅ 运行时优先使用 G.R(TextDefine.xxx) 或 TextConfigMgr.Instance.GetText(...)
public void RefreshView()
{
    m_textTitle.text = G.R(TextDefine.ID_LABEL_START_GAME);
    m_descText.text = TextConfigMgr.Instance.GetText(TextDefine.ID_LABEL_COMMON_HOUR_MIN_TEXT, 1, 30);
}

// ✅ 走文本提取流程时，可先写 G.R("中文")
public void RefreshTitle()
{
    m_textTitle.text = G.R("开始游戏");
}

// ❌ 运行时禁止直接硬编码中文文案
public void RefreshView()
{
    m_textTitle.text = "确认";
}

// ❌ 代码中禁止中文命名
public class 邮件模块
{
}
```

补充约束：

- 项目内 `G.R("中文")` 存在编辑器提取流程，最终应收敛到 `TextDefine`。
- 历史代码可能尚未完全收敛；新增运行时代码按本规范执行。

## 禁止的代码模式

```csharp
// ❌ 禁止 Resources
public GameObject LoadView()
{
    return Resources.Load<GameObject>("UI/MailPanel");
}

// ✅ 使用 GameModule.ResourceModule
public async UniTask<GameObject> LoadViewAsync(CancellationToken cancellationToken)
{
    return await GameModule.ResourceModule.LoadGameObjectAsync("MailUI", null, cancellationToken);
}

// ❌ 禁止直接 Instantiate 运行时对象
public GameObject CreateRole(GameObject prefab)
{
    return Object.Instantiate(prefab);
}

// ✅ 使用 LoadGameObjectAsync 或对象池
public async UniTask<GameObject> CreateRoleAsync(CancellationToken cancellationToken)
{
    return await GameModule.ResourceModule.LoadGameObjectAsync("Player", null, cancellationToken);
}

// ❌ 禁止 FindObjectOfType
public MainWindow FindWindow()
{
    return Object.FindObjectOfType<MainWindow>();
}

// ✅ UI 外部访问窗口使用 GetWindow / GetWindowAsyncAwait
public MainWindow GetWindow()
{
    return GameModule.UIModule.GetWindow<MainWindow>();
}

// ❌ 禁止 UI 外部直接访问生成字段
public void RefreshWindow()
{
    var ui = Object.FindObjectOfType<MainWindow>();
    ui.m_textTitle.text = "100";
}

// ✅ UI 外部通过公开接口交互
public void RefreshWindow()
{
    var ui = GameModule.UIModule.GetWindow<MainWindow>();
    ui?.RefreshView();
}

// ❌ 禁止跨模块强引用
public class MailModule
{
    private BattleModule m_battleModule;
}

// ✅ 使用 GameEvent.Get<接口>() 解耦
public void NotifyBattleResult()
{
    GameEvent.Get<IBattleEvent>()?.OnBattleFinish();
}

// ❌ GameBattle 禁止表现层代码
public class BattleViewSystem
{
    public void PlayHitEffect(GameObject effectPrefab)
    {
        Object.Instantiate(effectPrefab);
    }
}

// ✅ GameBattle 只保留纯逻辑
public class BattleDamageSystem
{
    public int CalcDamage(int attack, int defense)
    {
        return Mathf.Max(attack - defense, 1);
    }
}

// ❌ 禁止在 Update 高频创建对象
private void Update()
{
    var request = new MailRequest();
    var effect = new GameObject("Effect");
}

// ✅ 使用对象池和内存池
private void Update()
{
    MailRequest request = MemoryPool.Spawn<MailRequest>();
    GameObject effect = GameModule.GameObjectPool.SpawnSync("Effect");
}
```

## 模块设计规范

### 新增业务模块

```csharp
// ✅ 纯逻辑业务模块：Singleton<T>
public sealed class MailSystem : Singleton<MailSystem>
{
    protected override void OnInit()
    {
    }
}

// ✅ 需要轮询：实现 IUpdate / IFixedUpdate / ILateUpdate
public sealed class GuideSystem : Singleton<GuideSystem>, IUpdate, IFixedUpdate
{
    public void OnUpdate()
    {
    }

    public void OnFixedUpdate()
    {
    }
}

// ✅ 需要场景对象生命周期：MonoSingleton<T>
public sealed class BattleMonoSystem : MonoSingleton<BattleMonoSystem>
{
    protected override void OnInit()
    {
    }
}
```

### 新增热更模块

```csharp
// ✅ 热更模块：Module + 业务接口
public interface IMailModule
{
    void Refresh();
}

public sealed class MailModule : Module, IMailModule
{
    public override void OnCreate()
    {
    }

    public override void OnDestroy()
    {
    }

    public void Refresh()
    {
    }
}

// ✅ 需要轮询再额外实现 IUpdateModule
public sealed class InputModule : Module, IInputModule, IUpdateModule
{
    public override void OnCreate()
    {
    }

    public override void OnDestroy()
    {
    }

    public void Update(float elapseSeconds, float realElapseSeconds)
    {
    }
}
```

## 代码审查清单

### 资源管理

- [ ] 运行时资源是否都通过 `GameModule.ResourceModule` 或项目资源模块接口加载。
- [ ] 每个 `LoadAssetAsync` 是否都有对应 `UnloadAsset`，或改用 `LoadGameObjectAsync`。
- [ ] `GameObject`、特效、UI 资源是否优先复用对象池。
- [ ] 是否存在静态持有 `Asset` 引用。

### 异步编程

- [ ] 是否统一使用 `UniTask` / `UniTask<T>`。
- [ ] async 调用是否统一为 `await` 或 `.Forget() + 错误处理`。
- [ ] 是否正确传递 `CancellationToken`，并在生命周期结束时 `Cancel` / `Dispose`。
- [ ] 是否避免 `Task`、`Coroutine`、`async void`、阻塞等待、`Update await`。

### 事件系统

- [ ] 模块间通信是否优先通过 `GameEvent.Get<接口>()` 解耦。
- [ ] 非 `UIWindow` 类注册事件后，是否有对应 `RemoveEventListener`，或统一通过 `GameEventMgr` 管理。
- [ ] UI 内部事件是否统一在 `RegisterEvent()` 中通过 `AddUIEvent` 注册。

### 热更代码

- [ ] 热更代码是否保持在正确层级，没有反向依赖主工程实现细节。
- [ ] `GameBattle` 中是否完全没有 UI、资源加载、场景对象、动画、特效等表现层代码。
- [ ] 新增业务模块是否使用 `Singleton<T>` / `MonoSingleton<T>`。
- [ ] 新增热更模块是否使用 `Module + 业务接口`，需要轮询时再实现 `IUpdateModule`。
- [ ] 玩家可见文本是否通过 `G.R(...)` / `TextDefine` / `TextConfigMgr` 获取。

### 性能

- [ ] `Update`、高频回调、循环体中是否避免频繁 `new`、`new GameObject`、字符串拼接和无条件日志。
- [ ] 是否避免 `Object.FindObjectOfType`、外部直接访问 UI 私有组件。
- [ ] 配置读取是否单次获取并判空，没有重复 `Get`。
- [ ] 是否优先使用对象池、内存池、缓存和事件驱动。
- [ ] 列表或滚动视图是否使用 Widget 复用（如 `AdjustIconNum`）或 `SuperScrollView`。

## Git 工作流

```text
main / master       ← 稳定版本，禁止直接提交
feature/xxx         ← 功能开发分支
fix/xxx             ← Bug 修复分支
hotfix/xxx          ← 线上紧急修复
```

**提交信息格式**：

```text
feat: 添加登录界面 UI
fix: 修复资源加载后引用计数未减一的问题
refactor: 重构战斗系统事件通信方式
perf: 优化技能列表 Widget 复用逻辑
docs: 更新热更开发规范文档
```

**分支合并前检查**：

1. Unity 编译无错误无警告
2. 真机（Android/iOS）运行正常
3. 内存占用无明显增长（调试器模块监控）
4. 已自查代码审查清单
