# DGame 客户端代码规范和设计模式（精简版）

当需求涉及命名、UI 节点命名、异步编排、日志接口、中文文本、本地代码模式、模块设计、代码审查或 Git 协作时，先读本文件。

目标：让 Codex 以最短阅读成本掌握 DGame 当前客户端代码约束。若与目标模块现有稳定模式冲突，先遵循目标模块附近实现，再做最小收敛。原始细节仍以 `references/originals/client-conventions.md` 为准。

## 使用原则

1. 先与目标模块、程序集、目录中的现有风格保持一致，再补统一约束。
2. 命名、异步、日志和模块边界优先追求可读性、可维护性、可排查性。
3. 业务代码优先做最小完整实现，不顺手重构无关区域。
4. 若现有代码与规范冲突，优先局部收敛，不在无明确需求时全局推翻旧风格。

## 命名规范

### 类型命名

- 类型统一 `PascalCase`。
- 优先表达职责边界，不用无语义缩写。
- 接口统一 `I` 前缀。
- 热更模块接口使用 `I + 业务语义 + Module`。
- 热更模块实现使用 `业务语义 + Module`。
- `UIWindow` 子类使用 `XxxUI` 或 `XxxWindow`，同区域保持一致。
- `UIWidget` 子类使用 `XxxItem` 或 `XxxWidget`，同类保持一致。
- 子页面使用 `XxxPage`。
- AOT 启动流程节点使用 `XxxProcedure`。
- 纯状态机状态使用 `XxxState`。
- 只有承担系统级编排职责时才用 `XxxSystem`。
- Luban 生成类型命名沿用生成结果，如 `TbItemConfig`、`ItemConfig`，不要自行改缩写。
- 继承 `Singleton` 的管理类使用 `XxxMgr` 或 `XxxManager`，同一区域保持一致。
- 枚举与枚举值统一 `PascalCase`。

### 字段命名

- 私有字段：`m_ + camelCase`
- 静态私有字段：`s_ + camelCase`
- 常量：全大写 + 下划线
- 公开字段：`PascalCase`
- 事件/委托实例：`On + 语义名`
- `[SerializeField]` 字段：小驼峰，不加 `m_`

示例：

```csharp
private PlayerData m_playerData;
private static Dictionary<int, ItemConfig> s_itemConfigMap;
private const int MAX_RETRY_COUNT = 3;
public Action OnLoginCompleted;
[SerializeField] private string titleText;
```

### 方法命名

- 方法统一 `PascalCase`
- 行为方法优先动词开头
- 事件回调统一 `On + 语义名`
- 初始化方法使用 `Init` / `Initialize`
- 创建方法使用 `Create`
- 异步方法优先 `UniTask` 风格，如 `LoadMailListAsync`

## UI 节点命名

UI 节点命名以项目 UI 代码生成器规则为准，不是建议项，是实际生效约束。

当前项目使用 `m_` 风格前缀，来源于 `Assets/Editor/UIScriptGenerator/UIScriptGeneratorSettings.asset`，`codeStyle = MPrefix`。

常用前缀：

- `m_go`：`GameObject`
- `m_item`：Widget 子节点
- `m_tf`：`Transform`
- `m_rect`：`RectTransform`
- `m_text`：`Text`
- `m_richText`：`RichTextItem`
- `m_btn`：`Button`
- `m_img`：`Image`
- `m_rimg`：`RawImage`
- `m_scrollBar`：`Scrollbar`
- `m_scroll`：`ScrollRect`
- `m_input`：`InputField`
- `m_grid`：`GridLayoutGroup`
- `m_hlay`：`HorizontalLayoutGroup`
- `m_vlay`：`VerticalLayoutGroup`
- `m_slider`：`Slider`
- `m_group`：`ToggleGroup`
- `m_curve`：`AnimationCurve`
- `m_canvasGroup`：`CanvasGroup`
- `m_canvas`：`Canvas`
- `m_toggle`：`Toggle`
- `m_dropDown`：`Dropdown`
- `m_tmp`：`TextMeshProUGUI`

约束：

- 同一窗口内必须严格沿用生成器前缀，不要混入另一套命名体系。
- 前缀后的业务名使用 `PascalCase`，如 `m_btnConfirm`、`m_scrollRewardList`。
- 节点名优先表达用途，不要只写位置或序号。
- 只有需要进入生成器绑定规则的节点才使用这些前缀。
- 若要新增前缀规则，先改生成器设置，再批量调整预制体。

## 异步编程

目标：可取消、可追踪、可收口。

### 基本规则

- 业务异步统一使用 `UniTask` / `UniTask<T>`。
- 需要等待的异步必须显式 `await`。
- `UniTaskVoid` 只能用于明确 fire-and-forget 的场景，并显式 `.Forget()`。
- 不要在业务异步中混用 `Task`、`Coroutine`、`async void`。
- 不要阻塞等待异步结果。
- 不要在 `Update` 中 `await`。
- 不要启动没有取消和收口的后台任务。
- 跨帧后不要直接假设对象仍然有效。

### `CancellationToken`

- 异步链路优先透传 `CancellationToken`。
- 异步资源加载后应 `ThrowIfCancellationRequested()` 再继续。
- 若自己创建 `CancellationTokenSource`，生命周期结束时要 `Cancel + Dispose`。

### 并发

- 多资源并发加载优先 `UniTask.WhenAll(...)`。
- 批量加载可用 `Select(...).ToArray()` + `UniTask.WhenAll(...)`。

## 日志规范

- 运行时代码统一使用 `DLogger`
- 运行时代码禁止使用 `UnityEngine.Debug`
- 编辑器代码可使用 `Debug`
- 临时测试日志用完即删

示例：

```csharp
DLogger.Log("LoadMailList start");
DLogger.Warning("[MailModule] mailbox is empty");
DLogger.Error("[MailModule] LoadMailList failed, mailboxId=1001");
```

## 中文文本规范

- 运行时中文文本优先使用 `G.R(TextDefine.xxx)` 或 `TextConfigMgr.Instance.GetText(...)`
- 若处于文本提取流程中，可先写 `G.R("中文")`，再由工具提取替换
- 运行时 UI 或业务逻辑中禁止直接硬编码中文
- 注释中可以写中文
- 代码命名中禁止出现中文

## 禁止的代码模式

- 禁止使用 `Resources.Load()`、`Resources.LoadAsync()` 等运行时资源加载方式
- 禁止直接 `Instantiate` 创建运行时对象
- 禁止使用 `Object.FindObjectOfType`
- 禁止 UI 外部直接访问 UI 私有组件
- 禁止跨模块强引用造成耦合
- 禁止在 `GameBattle` 中写任何表现层代码
- 禁止在 `Update` 或高频路径频繁 `new`、`new GameObject`
- 禁止静态持有 `Asset` 引用
- 禁止直接忽略异步返回值

对应推荐替代：

- 运行时资源统一走 `GameModule.ResourceModule`
- 对象创建优先对象池/资源模块
- 窗口访问通过 `GameModule.UIModule.GetWindow / GetWindowAsyncAwait`
- 模块间通信优先 `GameEvent.Get<接口>()`
- `GameBattle` 只保留纯逻辑
- 高频路径使用对象池、内存池、缓存
- async 调用要么 `await`，要么 `.Forget()` 并做错误处理

## 推荐模式

- 高频对象优先对象池和内存池
- 模块间通信优先事件驱动解耦
- 访问底层模块优先通过 `GameModule`
- 配置读取单次获取并判空，避免重复 `Get`
- 场景间/模块间数据通过 `XxxDataMgr` 传递，不放全局变量

## 模块设计

### 新增业务模块

- 纯逻辑业务模块：`XxxSystem : Singleton<XxxSystem>`
- 如需轮询：额外实现 `IUpdate` / `IFixedUpdate` / `ILateUpdate`
- 若依附场景对象生命周期：`MonoSingleton<T>`
- 不要写成无边界的普通 `Helper`

### 新增热更模块

- 热更模块使用 `XxxModule`
- 继承 `DGame.Module`
- 实现对应模块接口
- 如需更新循环，再实现 `IUpdateModule`
- 不要写成和模块系统无关的普通类，也不要漏模块接口

## 代码审查清单

### 资源管理

- 运行时资源是否全部通过 `GameModule.ResourceModule` 或项目资源接口加载
- 是否避免 `Resources` / 直接 `Instantiate`
- 异步加载对象是否有明确释放路径
- 池对象、内存池对象是否有回收路径
- 是否存在静态持有 `Asset` 导致资源无法释放

### 异步

- 是否统一使用 `UniTask`
- 需要等待的异步是否都 `await`
- `UniTaskVoid` 是否显式 `.Forget()`
- 是否正确传递 `CancellationToken`
- 是否避免 `Update await`、阻塞等待、无控制后台任务

### 事件

- 模块间通信是否优先用 `GameEvent.Get<接口>()` 解耦
- 非 `UIWindow` 注册事件后是否有对应移除
- UI 内部事件是否统一在 `RegisterEvent()` 中通过 `AddUIEvent` 注册
- 定时器/监听/回调是否有收口
- 是否存在跨模块双向依赖或退出后仍回调

### 热更代码

- 热更代码是否保持在正确层级
- `GameBattle` 是否完全没有表现层代码
- 新增模块是否符合项目模式：业务模块 `Singleton/MonoSingleton`，热更模块 `Module + 接口`
- 玩家可见文本是否走 `G.R(...)`、`TextDefine`、`TextConfigMgr`

### 性能

- `Update`、循环体、高频回调里是否避免频繁分配
- 是否避免 `FindObjectOfType` 和外部直碰 UI 私有组件
- 配置读取是否单次获取并判空
- 是否优先使用对象池、内存池、缓存、事件驱动
- 列表/滚动视图是否考虑 Widget 复用或 `SuperScrollView`

## Git 工作流

分支约定：

- `main / master`：稳定版本，禁止直接提交
- `feature/xxx`：功能开发
- `fix/xxx`：Bug 修复
- `hotfix/xxx`：线上紧急修复

提交信息格式：

- `feat: 添加登录界面 UI`
- `fix: 修复资源加载后引用计数未减一的问题`
- `refactor: 重构战斗系统事件通信方式`
- `perf: 优化技能列表 Widget 复用逻辑`
- `docs: 更新热更开发规范文档`

合并前检查：

1. Unity 编译无错误无警告
2. 真机（Android/iOS）运行正常
3. 内存占用无明显增长
4. 已自查代码审查清单
