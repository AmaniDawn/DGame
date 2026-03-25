# DGame 客户端代码规范和设计模式

当需求涉及代码应该怎么命名、UI 节点如何命名、异步逻辑如何编排、日志应该调用哪个接口、中文文本是否可以直写、哪些代码模式应该避免、模块应该如何设计，或在提交前如何做代码审查与 Git 协作时，先阅读本文件。

本文件记录 DGame 客户端开发中应优先遵守的代码规范与协作约定。若规范与现有模块实现冲突，优先阅读目标模块附近已有实现，并以不破坏现有稳定模式为前提做最小收敛。

## 目录导航

- [DGame 客户端代码规范和设计模式](#dgame-客户端代码规范和设计模式)
  - [目录导航](#目录导航)
  - [使用原则](#使用原则)
  - [命名规范](#命名规范)
    - [C# 类型命名](#c-类型命名)
    - [字段命名](#字段命名)
    - [方法命名](#方法命名)
  - [UI 节点命名规范](#ui-节点命名规范)
  - [异步编程规范](#异步编程规范)
    - [基本规则](#基本规则)
    - [CancellationToken 使用](#cancellationtoken-使用)
    - [并发加载](#并发加载)
    - [禁止的异步模式](#禁止的异步模式)
  - [日志输出接口规范](#日志输出接口规范)
  - [中文文本使用规范](#中文文本使用规范)
  - [禁止的代码模式](#禁止的代码模式)
  - [推荐的代码模式](#推荐的代码模式)
  - [模块设计规范](#模块设计规范)
    - [新增业务模块](#新增业务模块)
    - [新增热更模块](#新增热更模块)
  - [代码审查](#代码审查)
    - [资源管理](#资源管理)
    - [异步编程](#异步编程)
    - [事件系统](#事件系统)
    - [热更代码](#热更代码)
    - [性能](#性能)
  - [Git 工作流](#git-工作流)

## 使用原则

处理 DGame 客户端代码时，优先遵循以下原则：

1. 先保持与目标模块、目标程序集、目标目录中的现有风格一致，再补充本文件中的统一约束。
2. 命名、异步、日志和模块边界优先追求可读性、可维护性和可排查性，不为了“写法炫技”引入额外复杂度。
3. 业务代码优先做最小完整实现，不在一次需求中顺手重构无关区域。
4. 若发现现有代码与本规范冲突，修改时优先在局部新增代码保持收敛，不要在无明确需求时全局推翻旧风格。

## 命名规范

### C# 类型命名

类型命名统一使用 `PascalCase`，名称优先表达职责边界，而不是实现细节或个人习惯缩写。

| 类型类别 | 命名规则 | 正例 | 反例 | 说明 |
| --- | --- | --- | --- | --- |
| 类 | 使用 `PascalCase`，优先“业务语义 + 类型后缀” | `MailModule`、`LoginWindow`、`PlayerDataCenter` | `mailmodule`、`MailMgr`、`DoLoginCls` | 后缀应稳定表达职责，如 `Module`、`Window`、`Widget`、`Manager`、`Config`。 |
| 接口 | 使用 `I` + `PascalCase` | `IResourceModule`、`ILocalizationModule` | `ResourceModuleInterface`、`resourceModule` | 与项目现有接口命名保持一致。 |
| 模块接口 | 使用 `I` + 业务语义 + `Module` | `IMailModule`、`IInputModule`、`IRedDotModule` | `MailModuleInterface`、`IModuleMail` | 模块对外能力接口统一以 `I` 开头，并保留 `Module` 后缀。 |
| 模块实现 | 使用业务语义 + `Module` | `MailModule`、`InputModule`、`RedDotModule` | `MailMgr`、`ModuleMail`、`MailSystem` | 模块实现类与模块接口保持一一对应的语义关系。 |
| 事件接口 | 使用 `I` + 业务语义 + `Event` 或项目既有事件接口命名 | `IPlayerEvent`、`ILoginEvent` | `PlayerEventInterface`、`EventLogin` | 事件接口命名应表达事件域，不要写成具体回调实现名。 |
| `UIWindow` 子类 | 使用业务语义 + `UI` 或 `Window` | `LoginUI`、`MainWindow`、`MailWindow` | `WindowLogin`、`UILogin`、`LoginPanel` | 窗口类允许使用 `UI` 或 `Window` 结尾，但同一模块内应保持一致，不要混用多套窗口后缀。 |
| `UIWidget` 子类 | 使用业务语义 + `Item` 或 `Widget` | `RewardItem`、`MailItemWidget`、`PlayerInfoWidget` | `WidgetReward`、`RewardUIItem`、`RewardPanel` | 可复用局部 UI 组件允许使用 `Item` 或 `Widget` 结尾，但同类组件命名应保持一致。 |
| `BaseChildPage` 子类 | 使用业务语义 + `Page` | `MailPage`、`NoticePage`、`SettingPage` | `PageMail`、`MailChild`、`MailPanel` | 子页面类统一使用 `Page` 后缀。 |
| 流程状态 | 使用业务语义 + `Procedure` | `LaunchProcedure`、`SplashProcedure`、`DownloadFileProcedure` | `ProcedureLaunch`、`LaunchState`、`LaunchFlow` | AOT 启动流程节点统一以 `Procedure` 结尾。 |
| 状态机状态 | 使用业务语义 + `State` | `LoginState`、`BattleState`、`MoveState` | `StateLogin`、`LoginStatus`、`BattleNode` | 纯状态机状态优先使用 `State` 后缀，与 Procedure 区分。 |
| 系统类 | 使用业务语义 + `System` | `ConfigSystem`、`CombatSystem`、`GuideSystem` | `SystemConfig`、`ConfigMgr`、`GuideModuleEx` | 只有在该类确实承担系统级编排职责时才使用 `System`。 |
| 配置表（Luban 生成） | 表类型使用 `TbXxxConfig`，行类型使用 `XxxConfig` | `TbItemConfig`、`ItemConfig`、`TbLanguageConfig` | `ItemCfg`、`item_config`、`ConfigItem` | 手写代码引用配置表类型时，应沿用 Luban 生成命名，不自行改缩写或改前缀。 |
| 内存池对象 | 类型应实现 `IMemory` | `BulletItem : IMemory`、`DamageTextNode : IMemory` | `BulletPoolObject`、`DamageTextPooledObj` | 内存池对象只约束接入 `IMemory`，命名本身按具体业务类型语义决定。 |
| 继承 `Singleton` 的 `Mgr` 类 | 使用业务语义 + `Mgr` 或 `Manager` | `AudioMgr`、`SceneManager` | `MgrAudio`、`AudioManagerSingleton` | 继承 `Singleton` 的管理类统一以 `Mgr` 或 `Manager` 结尾，同一区域内应保持一致。 |
| 结构体 | 使用 `PascalCase` | `RewardItemData`、`DamageResult` | `reward_item_data`、`damageStruct` | 仅在值语义明确时使用结构体。 |
| 枚举 | 使用 `PascalCase`，枚举值也使用 `PascalCase` | `MailType.System`、`WindowLayer.Normal` | `MAIL_TYPE.SYSTEM`、`window_layer.normal` | 枚举名应是类别，枚举值应是可读状态。 |
| 泛型类型参数 | 使用单个大写字母或带语义前缀的 `T` 命名 | `T`、`TData`、`TItem` | `Type1`、`GenericParam` | 简单泛型用 `T`，有多泛型时补足语义。 |

### 字段命名

字段命名应优先表达职责，不要使用无语义缩写。

```csharp
// 私有字段使用 m_ + 小驼峰
private PlayerData m_playerData;
private string m_loginToken;
private Button m_confirmButton;

// readonly 私有字段仍然使用 m_ + 小驼峰
private readonly MailModule m_mailModule;
private readonly CancellationToken m_destroyToken;

// 静态私有字段使用 s_ + 小驼峰
private static Dictionary<int, ItemConfig> s_itemConfigMap;
private static Queue<RequestData> s_pendingRequests;

// 常量使用全大写 + 下划线
private const int MAX_RETRY_COUNT = 3;
private const float DEFAULT_TIMEOUT_SECONDS = 10f;

// 公开字段使用大驼峰
public int RetryCount;
public string PlayerName;

// 事件 / 委托实例使用 On + 语义名
public Action OnLoginCompleted;
public Action<int> OnRewardClaimed;

// 序列化字段使用小驼峰
[SerializeField] private int retryCount;
[SerializeField] private string titleText;
```

补充约束：

- 私有字段统一使用 `m_` 前缀，不再混用 `_camelCase`。
- 静态私有字段统一使用 `s_` 前缀。
- 常量统一使用全大写加下划线，不使用 `PascalCase` 常量风格。
- 事件 / 委托实例使用 `On + 语义名`，例如 `OnLoginCompleted`、`OnRewardClaimed`。
- `[SerializeField]` 字段使用小驼峰，不加 `m_` 前缀。
- 新增字段名应先表达职责，再考虑长度，不能为了省几个字符牺牲可读性。

### 方法命名

方法命名统一使用 `PascalCase`，优先表达职责和行为语义。

```csharp
// 方法名统一使用 PascalCase
public void RefreshView()
{
}

// 行为方法优先使用动词开头
public void LoadConfig()
{
}

public void PlayAnimation()
{
}

// 事件回调使用 On 前缀 + 方法名
private void OnClickConfirm()
{
}

private void OnLanguageChanged()
{
}

// 初始化类的方法使用 Init / Initialize
public void Init()
{
}

public void Initialize()
{
}

// 工厂 / 创建类的方法使用 Create
public RewardWidget CreateRewardWidget()
{
    return null;
}

// 异步方法使用 UniTask 的异步实现
public async UniTask LoadMailListAsync()
{
}
```

补充约束：

- 行为方法优先用动词开头，不使用无语义缩写。
- 事件回调统一使用 `On + 语义名`。
- 初始化方法统一使用 `Init` 或 `Initialize`。
- 创建类方法统一使用 `Create`。
- 异步方法优先使用 `UniTask` 的异步实现。

## UI 节点命名规范

UI 节点命名要以项目里的 UI 代码生成器规则为准。当前生成器配置要求节点名前缀直接参与组件绑定与代码生成，因此这里不是“推荐前缀”，而是实际生效的命名约束。

当前项目实际使用 `m_` 风格前缀，配置来源为 `Assets/Editor/UIScriptGenerator/UIScriptGeneratorSettings.asset` 中的 `scriptGenerateRulers` 与 `codeStyle = MPrefix`。

| 节点前缀 | 绑定组件类型 | 是否 Widget | 命名示例 | 使用说明 |
| --- | --- | --- | --- | --- |
| `m_go` | `GameObject` | 否 | `m_goContent` | 普通对象节点、显隐节点、容器节点使用该前缀。 |
| `m_item` | `GameObject` | 是 | `m_itemReward` | 独立 Widget 子节点使用该前缀，生成器会按 Widget 规则处理。 |
| `m_tf` | `Transform` | 否 | `m_tfRoot` | 仅在确实需要 `Transform` 语义时使用。 |
| `m_rect` | `RectTransform` | 否 | `m_rectContent` | 需要直接操作布局尺寸或锚点时使用。 |
| `m_text` | `Text` | 否 | `m_textTitle` | UGUI `Text` 使用该前缀。 |
| `m_richText` | `RichTextItem` | 否 | `m_richTextDesc` | 富文本组件使用该前缀。 |
| `m_btn` | `Button` | 否 | `m_btnConfirm` | 按钮节点统一使用该前缀。 |
| `m_img` | `Image` | 否 | `m_imgIcon` | `Image` 组件使用该前缀。 |
| `m_rimg` | `RawImage` | 否 | `m_rimgPreview` | `RawImage` 组件使用该前缀。 |
| `m_scrollBar` | `Scrollbar` | 否 | `m_scrollBarVolume` | 滚动条使用该前缀。 |
| `m_scroll` | `ScrollRect` | 否 | `m_scrollMailList` | 滚动列表根节点使用该前缀。 |
| `m_input` | `InputField` | 否 | `m_inputName` | UGUI 输入框使用该前缀。 |
| `m_grid` | `GridLayoutGroup` | 否 | `m_gridReward` | 网格布局节点使用该前缀。 |
| `m_hlay` | `HorizontalLayoutGroup` | 否 | `m_hlayTabs` | 横向布局节点使用该前缀。 |
| `m_vlay` | `VerticalLayoutGroup` | 否 | `m_vlayContent` | 纵向布局节点使用该前缀。 |
| `m_slider` | `Slider` | 否 | `m_sliderProgress` | 滑条节点使用该前缀。 |
| `m_group` | `ToggleGroup` | 否 | `m_groupTab` | `ToggleGroup` 节点使用该前缀。 |
| `m_curve` | `AnimationCurve` | 否 | `m_curveAlpha` | 曲线引用使用该前缀。 |
| `m_canvasGroup` | `CanvasGroup` | 否 | `m_canvasGroupRoot` | 批量控制显隐与交互时使用。 |
| `m_canvas` | `Canvas` | 否 | `m_canvasRoot` | `Canvas` 节点使用该前缀。 |
| `m_toggle` | `Toggle` | 否 | `m_toggleAuto` | `Toggle` 节点使用该前缀。 |
| `m_dropDown` | `Dropdown` | 否 | `m_dropDownQuality` | UGUI 下拉框使用该前缀。 |
| `m_tmp` | `TextMeshProUGUI` | 否 | `m_tmpTitle` | TMP 文本使用该前缀。 |

- 同一窗口内必须严格沿用生成器前缀，不要混入 `BtnClose`、`TxtTitle`、`ImgIcon` 这类另一套命名体系。
- 前缀之后的业务名使用 `PascalCase`，例如 `m_btnConfirm`、`m_scrollRewardList`、`m_itemMailCell`。
- 节点名优先表达用途，不要只写位置或序号。优先 `m_btnConfirm`、`m_imgQuality`，避免 `m_btn1`、`m_imgLeft`。
- 若节点不需要生成绑定字段，不要为了“统一”硬套前缀；只有需要进入生成器规则的节点才使用上述命名。
- 新增前缀规则前，应先更新 UI 代码生成器设置，再批量调整相关预制体，避免文档规范和生成器配置不一致。

## 异步编程规范

项目内异步逻辑优先保持可取消、可追踪、可收口。

### 基本规则

```csharp
// ✅ 无返回值异步使用 UniTask
public async UniTask LoadMailListAsync()
{
    await RequestMailListAsync();
    BindMailData();
    RefreshView();
}

// ✅ 有返回值异步直接返回业务结果
public async UniTask<MailDetailData> LoadMailDetailAsync(int mailId)
{
    MailDetailData detailData = await RequestMailDetailAsync(mailId);
    return detailData;
}

// ✅ 需要等待的异步行为必须显式 await
public async UniTask EnterGameAsync()
{
    await LoadConfigAsync();
    await PreloadAsync();
    StartGame();
}

// ✅ 调用 UniTaskVoid 方法时必须显式 Forget()，用于忽略警告并表达不等待
public async UniTaskVoid PlayGuideAsync()
{
    await ShowGuideStepAsync();
}

public void Open()
{
    PlayGuideAsync().Forget();
}

```

### CancellationToken 使用

```csharp
// ✅ 异步链路优先透传 CancellationToken
public async UniTask LoadMailDetailAsync(int mailId, CancellationToken cancellationToken)
{
    MailDetailData detailData = await RequestMailDetailAsync(mailId, cancellationToken);
    cancellationToken.ThrowIfCancellationRequested();
    RefreshDetail(detailData);
}

// ✅ 异步加载时要透传 CancellationToken，防止对象销毁后回调继续执行
public async UniTask LoadPrefabAsync(CancellationToken cancellationToken)
{
    GameObject prefab = await GameModule.ResourceModule.LoadAssetAsync<GameObject>("MailPanel", cancellationToken);
    cancellationToken.ThrowIfCancellationRequested();
    CreateView(prefab);
}

public async UniTask RefreshAsync(CancellationToken cancellationToken)
{
    MailData mailData = await RequestMailDataAsync(cancellationToken);
    cancellationToken.ThrowIfCancellationRequested();
    RefreshView(mailData);
}

// ✅ 如果自己创建了 CancellationTokenSource，要在生命周期结束时及时 Cancel 和 Dispose
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
// ✅ 多个资源并发加载用于提速
public async UniTask PreloadAsync(CancellationToken cancellationToken)
{
    UniTask loadPanelTask = GameModule.ResourceModule.LoadAssetAsync<GameObject>("MailPanel", cancellationToken);
    UniTask loadAtlasTask = GameModule.ResourceModule.LoadAssetAsync<SpriteAtlas>("CommonAtlas", cancellationToken);
    UniTask loadAudioTask = GameModule.ResourceModule.LoadAssetAsync<AudioClip>("Click", cancellationToken);

    await UniTask.WhenAll(loadPanelTask, loadAtlasTask, loadAudioTask);
}

// ✅ 批量加载使用 Select + UniTask.WhenAll
public async UniTask<IList<GameObject>> LoadItemsAsync(IEnumerable<string> assetNames, CancellationToken cancellationToken)
{
    UniTask<GameObject>[] tasks = assetNames
        .Select(assetName => GameModule.ResourceModule.LoadAssetAsync<GameObject>(assetName, cancellationToken))
        .ToArray();

    GameObject[] results = await UniTask.WhenAll(tasks);
    return results;
}
```

### 禁止的异步模式

```csharp
// ❌ 不要在业务异步中使用 Task，应使用 UniTask 的 async/await
public async Task LoadConfigAsync()
{
    await Task.Delay(1000);
}

// ❌ 不要使用 Coroutine 承载业务异步逻辑，应使用 UniTask 的 async/await
public IEnumerator LoadDataCoroutine()
{
    yield return new WaitForSeconds(1f);
    RefreshView();
}

// ❌ 普通业务方法不要使用 async void
public async void LoadData()
{
    await RequestDataAsync();
}

// ❌ 不要阻塞等待异步结果
public void Init()
{
    var config = LoadConfigAsync().GetAwaiter().GetResult();
}

// ❌ 不要启动没有取消和收口的后台任务
public void Open()
{
    LoadMailListAsync().Forget();
}

// ❌ 不要在 Update 中使用 await
private async void Update()
{
    await LoadDataAsync();
}

// ❌ 不要跨帧后直接假设对象仍然有效
public async UniTask RefreshAsync()
{
    await LoadDataAsync();
    m_view.SetActive(true);
}
```

## 日志输出接口规范

日志不是装饰，它首先服务于排障和线上定位。

```csharp
// ✅ 运行时代码统一使用 DLogger
public void LoadMailList()
{
    DLogger.Log("LoadMailList start");
    DLogger.Warning("[MailModule] mailbox is empty");
    DLogger.Error("[MailModule] LoadMailList failed, mailboxId=1001");
}

// ❌ 运行时代码不允许使用 UnityEngine.Debug 相关接口
public void RefreshView()
{
    Debug.Log("RefreshView");
    Debug.LogWarning("mailbox is empty");
    Debug.LogError("LoadMailList failed");
}

// ✅ 编辑器代码无约束，可以使用 UnityEngine.Debug
[MenuItem("DGame Tools/TestLog")]
private static void TestLog()
{
    Debug.Log("Editor test log");
}

// ❌ 临时测试使用的 Debug 代码在使用完后必须及时清理
public void Test()
{
    DLogger.Log("temp test log");
}
```

## 中文文本使用规范

中文文本必须和本地化、策划配置、可维护性一起考虑，不能为了省事直接到处硬编码。

```csharp
// ✅ 运行时中文文本优先使用 G.R(TextDefine.xxx) 或 TextConfigMgr.Instance.GetText(...)
public void RefreshView()
{
    m_titleText.text = G.R(TextDefine.ID_LABEL_START_GAME);
    m_descText.text = TextConfigMgr.Instance.GetText(TextDefine.ID_LABEL_COMMON_HOUR_MIN_TEXT, 1, 30);
}

// ✅ 若处于文本提取流程中，可先写 G.R("中文")，再通过编辑器工具提取替换为 TextDefine
public void RefreshTitle()
{
    m_titleText.text = G.R("开始游戏");
    m_titleText1.text = G.R("这是测试{0}代码{1}", ",", "。");
}

// ❌ 运行时中文文本不能直接硬编码到 UI 或业务逻辑里
public void RefreshView()
{
    m_titleText.text = "确认";
    m_descText.text = "领取奖励后无法撤回";
}

// ✅ 注释中可以使用中文说明
// 这里刷新邮件标题文本
public void RefreshDesc()
{
}

// ❌ 代码中禁止出现任何中文形式的命名
public class 邮件模块
{
    private string 标题;

    public void 刷新界面()
    {
    }
}
```

## 禁止的代码模式

```csharp
// ❌ 禁止使用 Resources 相关接口加载运行时资源
public GameObject LoadView()
{
    return Resources.Load<GameObject>("UI/MailPanel");
}

// ✅ 运行时资源使用 GameModule.ResourceModule 相关接口
public async UniTask<GameObject> LoadViewAsync(CancellationToken cancellationToken)
{
    return await GameModule.ResourceModule.LoadGameObjectAsync("MailUI", null, cancellationToken);
}

// ❌ 禁止直接使用 Instantiate 创建运行时对象
public GameObject CreateRole(GameObject prefab)
{
    return Object.Instantiate(prefab);
}

// ✅ GameObject 创建优先使用 ResourceModule / 对象池
public async UniTask<GameObject> CreateRoleAsync(CancellationToken cancellationToken)
{
    return await GameModule.ResourceModule.LoadGameObjectAsync("Player", null, cancellationToken);
}

// ❌ 禁止使用 Object.FindObjectOfType，性能差
public MainWindow FindWindow()
{
    return Object.FindObjectOfType<MainWindow>();
}

// ✅ UI 外部访问窗口使用 GameModule.UIModule.GetWindow / GetWindowAsyncAwait
public MainWindow GetWindow()
{
    return GameModule.UIModule.GetWindow<MainWindow>();
}

public async UniTask<MainWindow> GetWindowAsync(CancellationToken cancellationToken)
{
    return await GameModule.UIModule.GetWindowAsyncAwait<MainWindow>(cancellationToken);
}

// ❌ 禁止 UI 外部直接访问 UI 组件
public void RefreshBattleHP()
{
    var ui = Object.FindObjectOfType<MainWindow>();
    ui.m_textTitle.text = "100";
}

// ✅ UI 外部通过窗口公开接口交互，不直接碰私有组件
public void RefreshBattleHP()
{
    var ui = GameModule.UIModule.GetWindow<MainWindow>();
    ui?.RefreshView();
}

// ❌ 禁止跨模块强引用，造成模块间耦合
public class MailModule
{
    private BattleModule m_battleModule;

    public void Init(BattleModule battleModule)
    {
        m_battleModule = battleModule;
    }
}

// ✅ 跨模块协作使用 GameEvent.Get<接口>() 解耦
public void NotifyBattleResult()
{
    GameEvent.Get<IBattleEvent>()?.OnBattleFinish();
}

// ❌ 禁止在 GameBattle 中编写任何表现层代码
public class BattleViewSystem
{
    public void PlayHitEffect(GameObject effectPrefab)
    {
        Object.Instantiate(effectPrefab);
    }
}

// ✅ GameBattle 中只能保留纯逻辑代码
public class BattleDamageSystem
{
    public int CalcDamage(int attack, int defense)
    {
        return Mathf.Max(attack - defense, 1);
    }
}

// ❌ 禁止在 Update 中频繁创建类对象和游戏对象
private void Update()
{
    var request = new MailRequest();
    var effect = new GameObject("Effect");
}

// ✅ 高频路径使用对象池和内存池
private void Update()
{
    MailRequest request = MemoryPool.Spawn<MailRequest>();
    GameObject effect = GameModule.GameObjectPool.SpawnSync("Effect");
}

// ❌ 禁止静态持有 Asset 引用
private static GameObject s_mailPanelPrefab;

// ❌ 禁止直接忽略 async 返回值
public void Open()
{
    LoadMailListAsync();
}

// ✅ async 调用要么 await，要么 Forget() + 错误处理
public async UniTask OpenAsync()
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

public void Open()
{
    LoadMailListSafeAsync().Forget();
}

private async UniTaskVoid LoadMailListSafeAsync()
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

## 推荐的代码模式

```csharp
// ✅ 对象池和内存池复用频繁创建的对象
private void Update()
{
    MailRequest request = MemoryPool.Spawn<MailRequest>();
    GameObject effect = GameModule.GameObjectPool.SpawnSync("Effect");
}

// ✅ 模块间通信优先使用事件驱动解耦
public void NotifyBattleFinish()
{
    GameEvent.Get<IBattleEvent>()?.OnBattleFinish();
}

// ✅ 通过 GameModule 访问模块，不直接持有模块强引用
public void RefreshLanguage()
{
    var language = GameModule.LocalizationModule.CurrentLanguage;
    DLogger.Log(language.ToString());
}

// ✅ 读取配置表时单次获取，避免重复 Get，并做好判空
public string GetItemName(int itemId)
{
    ItemConfig itemConfig = TbItemConfig.GetOrDefault(itemId);
    if (itemConfig == null)
    {
        return string.Empty;
    }
    return itemConfig.Name;
}

// ✅ 场景间 / 模块间数据通过 XxxDataMgr 传递，不放全局变量
public PlayerData GetPlayerData()
{
    PlayerData playerData = PlayerDataMgr.Instance.GetPlayerData();
    if (playerData == null)
    {
        return null;
    }
    return playerData;
}
```

## 模块设计规范

### 新增业务模块

```csharp
// ✅ 纯逻辑业务模块使用 XxxSystem，继承 Singleton<T>
public sealed class MailSystem : Singleton<MailSystem>
{
    protected override void OnInit()
    {
    }

    public void Refresh()
    {
    }
}

// ✅ 如果业务模块需要轮询，可额外实现 IUpdate / IFixedUpdate / ILateUpdate
public sealed class GuideSystem : Singleton<GuideSystem>, IUpdate, IFixedUpdate
{
    protected override void OnInit()
    {
    }

    public void OnUpdate()
    {
    }

    public void OnFixedUpdate()
    {
    }
}

// ✅ 如果业务模块需要挂在场景对象生命周期上，使用 MonoSingleton<T>
public sealed class BattleMonoSystem : MonoSingleton<BattleMonoSystem>
{
    protected override void OnInit()
    {
    }
}

// ❌ 不要把业务模块写成无边界的普通工具类
public class MailHelper
{
}
```

### 新增热更模块

```csharp
// ✅ 新增热更模块使用 XxxModule，继承 DGame.Module，并实现自己的模块接口
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

// ✅ 如果模块需要 Update，再额外实现 IUpdateModule
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

// ❌ 不要把热更模块写成和模块系统无关的普通类，也不要漏掉模块接口
public class MailManager
{
}
```

## 代码审查

完成功能后和提交代码前，按下面清单自查：

### 资源管理

- [ ] 运行时资源是否全部通过 `GameModule.ResourceModule` 或项目资源模块接口加载，而不是 `Resources` / 直接 `Instantiate`。
- [ ] 每个 `LoadAssetAsync` 加载的对象是否都有对应的 `UnloadAsset`；若适合自动管理释放，是否改用 `LoadGameObjectAsync`。
- [ ] `GameObject`、特效、UI 资源是否优先复用对象池，而不是反复创建和销毁。
- [ ] 资源句柄、对象池对象、内存池对象是否都有明确释放、回收或销毁路径。
- [ ] 是否存在静态持有 `Asset` 引用、导致资源无法释放的问题。

### 异步编程

- [ ] 业务异步是否统一使用 `UniTask` / `UniTask<T>`，没有混入 `Task` 或 `Coroutine`。
- [ ] 需要等待的异步是否都有明确 `await`；不等待的 `UniTaskVoid` 是否显式 `.Forget()`。
- [ ] 异步链路是否正确传递 `CancellationToken`，并在对象销毁时或OnDestroy中及时 `Cancel` / `Dispose`。
- [ ] 是否存在直接忽略异步返回值、没有错误处理或没有生命周期收口的问题。
- [ ] 是否避免了在 `Update` 中 `await`、阻塞等待、无控制后台任务等高风险写法。

### 事件系统

- [ ] 模块间通信是否优先通过 `GameEvent.Get<接口>()` 或事件系统解耦，而不是直接强引用其他模块。
- [ ] 非 `UIWindow` 类中注册事件后，是否都有对应的 `RemoveEventListener`，或统一通过 `GameEventMgr` 管理。
- [ ] UI 内部事件是否统一在 `RegisterEvent()` 中通过 `AddUIEvent` 注册。
- [ ] 事件监听、事件注册、计时器回调是否都有对应解除注册或释放逻辑。
- [ ] 是否存在跨模块双向依赖、事件只注册不移除、退出场景后仍回调的问题。

### 热更代码

- [ ] 热更代码是否保持在正确层级，没有反向依赖主工程实现细节。
- [ ] `GameBattle` 中是否完全没有 UI、资源加载、场景对象、动画、特效等表现层代码。
- [ ] 新增模块是否符合项目实际模式：业务模块使用 `Singleton` / `MonoSingleton`，热更模块使用 `Module + 接口`。
- [ ] 玩家可见文本是否通过 `G.R(...)`、`TextDefine`、`TextConfigMgr` 获取，而不是直接硬编码。

### 性能

- [ ] `Update`、高频回调、循环体中是否避免了频繁 `new` 对象、`new GameObject`、字符串拼接和无条件日志。
- [ ] 是否避免了 `Object.FindObjectOfType`、外部直接访问 UI 私有组件等高成本或高耦合写法。
- [ ] 配置表读取是否单次获取、做好判空，没有重复 `Get` 同一份配置。
- [ ] 是否优先使用对象池、内存池、缓存和事件驱动，而不是在高频路径重复创建对象。
- [ ] 列表或滚动视图是否使用了 Widget 复用（如 `AdjustIconNum`）或无限列表 `SuperScrollView`。

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