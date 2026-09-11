# 代码规范与命名规则

> **适用场景**：命名、UI 节点前缀、异步、日志、禁止模式 | **关联文档**：[architecture.md](architecture.md)、[ui-lifecycle.md](ui-lifecycle.md)

## 基本规则

- 使用中文写提案和回答。
- 修改范围保持克制，不做无关重构。
- 优先复用 DGame 二次封装，不绕过到底层原始 API。
- 新增异步逻辑使用 `UniTask`。
- 业务日志使用 `DLogger`。

## 字段与方法命名

匹配现有代码，私有字段用 `m_`；新增长期私有静态字段优先用 `s_`，历史代码中仍有少量 `m_` 静态缓存：

```csharp
private GameTimer m_timer;                  // 私有实例字段：m_ 前缀
private static UIModule s_instance;         // 私有静态字段：s_ 前缀
public int NodeCount => m_nodeDict.Count;   // 公开属性：PascalCase
```

| 场景 | 约定 | DGame 示例 |
|------|------|------------|
| 私有实例字段 | `m_` + camelCase | `m_timer`、`m_cts` |
| 私有静态字段 | `s_` + camelCase | `s_instance` |
| 公开属性 | PascalCase | `NodeCount` |
| 公开异步 API | `Async` 后缀 | `SaveAllClientDataAsync`、`OnShowWaitingUIAsync`（`UniTaskVoid` 也带 `Async`） |
| 事件回调方法 | `On` 前缀 | `OnHpChanged`、`OnShowWaitingUIAsync` |
| 常量 | 全大写下划线 | `DATE_MASK_YEAR`、`DATE_MASK_MONTH`（`const`/`static readonly`） |

## 通用 C# 类型命名约定

类型名使用 PascalCase；接口保留 `I` 前缀；启动流程状态机使用 `XxxProcedure` 后缀形态（如 `SplashProcedure`）。

| 类型/场景 | 命名约定 | DGame 示例 | 备注 |
|-----------|----------|------------|------|
| 框架模块接口 | `IXxxModule` | `IResourceModule`、`IAudioModule`、`IFsmModule` | Runtime 模块公开契约 |
| 框架模块实现 | `XxxModule` | `ResourceModule`、`AudioModule`、`FsmModule` | 通常位于 `GameUnity/Assets/DGame/Runtime/Module/` |
| 热更业务模块接口 | `IXxxModule` | `ILocalizationModule`、`IInputModule` | 位于 `GameUnity/Assets/Scripts/HotFix/GameLogic/Module/` |
| 热更业务模块实现 | `XxxModule` | `LocalizationModule`、`InputModule`、`UIModule` | 通过 `GameModule.XXX` 暴露访问入口 |
| UIWindow 子类 | `XxxUI` / `XxxWindow` | `WaitingUI`、`TipsUI`、`MainWindow`、`GMPanel` | 新增窗口优先跟随所在目录现有命名 |
| UIWidget 子类 | `XxxWidget` / `XxxItem` | `RedDotItem`、`UIFrameWidget`、`UILoopItemWidget` | 列表项、子组件、可复用 UI 单元 |
| UI 控制器 | `XxxUIController` | `CommonUIController` | 实现 `IUIController`，集中注册 UI 事件 |
| 事件接口 | `IXxxUI` / `IXxx` + `[EventInterface]` | `ICommonUI`、`ILoginUI`、`ILocalization` | 放 `GameLogic/IEvent/`，由生成器生成 `Ixxx_Event`、`Ixxx_Gen`、`EventCenter` |
| 启动流程状态 | `XxxProcedure` | `SplashProcedure`、`InitPackageProcedure`、`InitResourceProcedure`、`StartGameProcedure` | 位于 AOT Procedure，继承 `ProcedureBase` |
| 普通 FSM 状态 | `XxxState` | `LoadingState`、`AnimState` | 继承 `BaseState<T>` 或实现 `IFsmState<T>` 时使用 |
| 内存池对象 | `Xxx` / `XxxObject` / `XxxAgent` + `MemoryObject` 或 `IMemory` | `AudioData : MemoryObject`、`UIFrameAnimatorAgent : MemoryObject`、`BasePoolObject : IMemory` | 通过 `MemoryPool.Spawn<T>()` / `MemoryPool.Release(...)` 成对使用 |
| Luban 表容器 | `TbXxxConfig` | `TbItemConfig`、`TbTextConfig`、`TbModelConfig` | 生成代码位于 `GameProto/LubanConfig/` |
| 配置封装管理器 | `XxxConfigMgr` | `TextConfigMgr`、`ModelConfigMgr`、`SoundConfigMgr` | 热更层消费 Luban 的业务封装 |
| 业务管理器 | `XxxMgr` | `ClientSaveDataMgr`、`SwitchPageMgr`、`FrameSpriteMgr` | 仅在现有模块模式需要时使用 |
| Helper/工具封装 | `XxxHelper` | `DGameLocalizationHelper`、`SetUISafeFitHelper`、`ClientSaveDataHelper` | 静态工具或策略适配器 |
| Define/常量定义 | `XxxDefine` | `InputDefine`、`FrameAnimParamDefine`、`RedDotPathDefine` | 生成或集中维护常量 |
| 枚举 | `E` + PascalCase | `EEventGroup`、`ELogLevel`、`EDeserializeError` | 现有 DGame 枚举使用 `E` 前缀 |
| 单例系统/静态门面 | `XxxSystem` / `GameModule` | `SingletonSystem`、`ModuleSystem`、`GameModule` | 不新增散落全局入口，优先复用 `GameModule.XXX` |

## UI 节点前缀

UI 脚本生成前缀以 `GameUnity/Assets/Editor/UIScriptGenerator/UIScriptGeneratorSettings.asset` 的 `scriptGenerateRulers` 为准。Transform 前缀是 `m_tf`，没有 `m_trans`。

配置中共有 25 条规则，按源码列表顺序匹配：

| 顺序 | 前缀 | `UIComponentName` | 生成字段类型 | 备注 |
|------|------|-------------------|--------------|------|
| 1 | `m_go` | `GameObject` | `GameObject` | 普通 GameObject |
| 2 | `m_item` | `GameObject` | `GameObject` | `isUIWidget: true`，同时作为 Widget 子树跳过标记 |
| 3 | `m_tf` | `Transform` | `Transform` | Transform 前缀，不是 `m_trans` |
| 4 | `m_rect` | `RectTransform` | `RectTransform` |  |
| 5 | `m_text` | `Text` | `Text` | uGUI Text |
| 6 | `m_richText` | `RichTextItem` | `RichTextItem` | DGame 富文本组件 |
| 7 | `m_btn` | `Button` | `Button` | 会生成 click 回调 |
| 8 | `m_img` | `Image` | `Image` |  |
| 9 | `m_rimg` | `RawImage` | `RawImage` |  |
| 10 | `m_scrollBar` | `Scrollbar` | `Scrollbar` | 必须在 `m_scroll` 前，大写 B |
| 11 | `m_scroll` | `ScrollRect` | `ScrollRect` |  |
| 12 | `m_input` | `InputField` | `InputField` | uGUI InputField |
| 13 | `m_grid` | `GridLayoutGroup` | `GridLayoutGroup` |  |
| 14 | `m_hlay` | `HorizontalLayoutGroup` | `HorizontalLayoutGroup` |  |
| 15 | `m_vlay` | `VerticalLayoutGroup` | `VerticalLayoutGroup` |  |
| 16 | `m_slider` | `Slider` | `Slider` | 会生成 slider 回调 |
| 17 | `m_group` | `ToggleGroup` | `ToggleGroup` |  |
| 18 | `m_curve` | `AnimationCurve` | `AnimationCurve` |  |
| 19 | `m_canvasGroup` | `CanvasGroup` | `CanvasGroup` | 必须在 `m_canvas` 前，大写 G |
| 20 | `m_canvas` | `Canvas` | `Canvas` |  |
| 21 | `m_toggle` | `Toggle` | `Toggle` | 会生成 toggle 回调 |
| 22 | `m_dropDown` | `Dropdown` | `Dropdown` | uGUI Dropdown，大写 D |
| 23 | `m_tmpInput` | `TMP_InputField` | `TMP_InputField` | TextMeshPro，需 `TextMeshPro` 编译符号 |
| 24 | `m_tmpDropdown` | `TMP_Dropdown` | `TMP_Dropdown` | TextMeshPro，Dropdown 大写 D |
| 25 | `m_tmp` | `TextMeshProUGUI` | `TextMeshProUGUI` | 必须在 TMP 复合前缀之后 |

以 `UIScriptGeneratorSettings.asset`、现有 `UI/Gen` 生成代码和窗口代码为准。

### 匹配顺序陷阱

生成器使用 `UIScriptGeneratorSettings.GetScriptGenerateRulers().Find(r => varName.StartsWith(r.uiElementRegex))`，命中第一条规则就停止。前缀存在包含关系时，长前缀必须排在短前缀前：

| 节点名 | 正确命中 | 错误顺序会导致 |
|--------|----------|----------------|
| `m_scrollBarVolume` | `m_scrollBar` → `Scrollbar` | 若 `m_scroll` 在前，会被当作 `ScrollRect` |
| `m_canvasGroupMask` | `m_canvasGroup` → `CanvasGroup` | 若 `m_canvas` 在前，会被当作 `Canvas` |
| `m_tmpInputName` | `m_tmpInput` → `TMP_InputField` | 若 `m_tmp` 在前，会被当作 `TextMeshProUGUI` |
| `m_tmpDropdownQuality` | `m_tmpDropdown` → `TMP_Dropdown` | 若 `m_tmp` 在前，会被当作 `TextMeshProUGUI` |

新增前缀时先检查是否被已有短前缀覆盖；不要把 `m_scroll`、`m_canvas`、`m_tmp` 这类短前缀移动到对应长前缀之前。

### UI 组件类型映射表

生成字段使用 `UIComponentName.ToString()` 作为 C# 类型名；实际组件查找由 `UIScriptAutoGenerator.GetComponentTypeFromEnumName` 映射。

| `UIComponentName` | 实际类型/命名空间 | 备注 |
|-------------------|------------------|------|
| `GameObject` | `UnityEngine.GameObject` | 绑定时取 `RectTransform.gameObject` |
| `Transform` | `UnityEngine.Transform` |  |
| `RectTransform` | `UnityEngine.RectTransform` |  |
| `Text` | `UnityEngine.UI.Text` |  |
| `RichTextItem` | `GameLogic.RichTextItem` | DGame 扩展组件 |
| `Button` | `UnityEngine.UI.Button` |  |
| `Image` | `UnityEngine.UI.Image` |  |
| `RawImage` | `UnityEngine.UI.RawImage` |  |
| `ScrollRect` | `UnityEngine.UI.ScrollRect` |  |
| `Scrollbar` | `UnityEngine.UI.Scrollbar` |  |
| `InputField` | `UnityEngine.UI.InputField` |  |
| `GridLayoutGroup` | `UnityEngine.UI.GridLayoutGroup` |  |
| `HorizontalLayoutGroup` | `UnityEngine.UI.HorizontalLayoutGroup` |  |
| `VerticalLayoutGroup` | `UnityEngine.UI.VerticalLayoutGroup` |  |
| `Slider` | `UnityEngine.UI.Slider` |  |
| `Toggle` | `UnityEngine.UI.Toggle` |  |
| `ToggleGroup` | `UnityEngine.UI.ToggleGroup` |  |
| `AnimationCurve` | `UnityEngine.AnimationCurve` |  |
| `CanvasGroup` | `UnityEngine.CanvasGroup` |  |
| `TextMeshProUGUI` | `TMPro.TextMeshProUGUI` | 需 `TextMeshPro` 编译符号和 `using TMPro;` |
| `Canvas` | `UnityEngine.Canvas` |  |
| `Dropdown` | `UnityEngine.UI.Dropdown` |  |
| `TMP_InputField` | `TMPro.TMP_InputField` | 需 `TextMeshPro` 编译符号和 `using TMPro;` |
| `TMP_Dropdown` | `TMPro.TMP_Dropdown` | 需 `TextMeshPro` 编译符号和 `using TMPro;` |

字段名由节点名去掉代码风格前缀后再加当前代码风格前缀生成，组件语义片段会保留。当前 `UIScriptGeneratorSettings.asset` 的 `codeStyle` 是 `MPrefix`，例如 `m_btnClose` 生成 `private Button m_btnClose;`，`m_dropDownLanguage` 生成 `private Dropdown m_dropDownLanguage;`。

## 使用模式

### 异步编程规范

```csharp
// UniTask 替代 Task，UniTaskVoid 替代 void async
public async UniTask<int> GetDataAsync() { }
public async UniTaskVoid StartBattleAsync() { }  // 调用方加 .Forget()

// CancellationToken 防止销毁后回调
private CancellationTokenSource m_cts = new();
protected override void OnDestroy() { m_cts.Cancel(); m_cts.Dispose(); }

// 并发加载
var (a, b, c) = await UniTask.WhenAll(LoadA(), LoadB(), LoadC());
```

失败或异常统一用 `DLogger.Error` 记录，不用 `Debug.Log`。

## 禁用/慎用模式

| 模式 | 替代 |
|------|------|
| `Resources.Load` 加载业务资源 | `GameModule.ResourceModule` |
| `Instantiate` 直接实例化资源体 | `GameModule.ResourceModule.LoadGameObjectAsync` |
| `FindObjectOfType` / `GameObject.Find` 查全局对象 | `GameModule.XXX` 模块入口或已有引用 |
| `Update` / 高频回调里 `new` 临时对象 | `MemoryPool.Spawn<T>()` 复用后 `Release` |
| 静态字段长期持有 `Asset` 引用 | 用完 `UnloadAsset`，实例随 GameObject 销毁回收 |
| Coroutine 新增异步工作流 | `UniTask` |
| 业务层散落 `ModuleSystem.GetModule<T>()` | `GameModule.XXX` |
| UI 手动注册 `GameEvent.AddEventListener` | `AddUIEvent` |
| 修改 Luban 生成代码 | 修改配置源/模板并导表 |
| 未验证资源地址 | 搜索 `BundleAssets` 和 YooAsset 设置 |

## 常见错误

| 错误 | 正确做法 |
|------|---------|
| `Debug.Log` 写入业务代码 | `DLogger.Info/Warning/Error` |
| 大功能一次性改多个层 | 先确认落位和边界 |
| 释放后继续访问 MemoryPool 对象 | Release 后禁止再访问 |
| 发现无关死代码直接删除 | 只说明，不顺手改 |

---

## 交叉引用

- 架构总览见 [architecture.md](architecture.md)
- UI 生命周期见 [ui-lifecycle.md](ui-lifecycle.md)
- UI 进阶模式见 [ui-patterns.md](ui-patterns.md)
