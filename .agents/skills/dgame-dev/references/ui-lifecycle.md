# UI 生命周期与核心 API

> **适用场景**：UIWindow/UIWidget 生命周期、UILayer、ShowWindow/CloseWindow/HideWindow API、ScriptGenerator 节点绑定 | **关联文档**：[event-system.md](event-system.md)、[ui-patterns.md](ui-patterns.md)、[naming-rules.md](naming-rules.md)

---

## 一、核心 API

### UILayer 层级

| 值 | 层级 | 用途 |
|----|------|------|
| 0 | `Bottom` | 底层 |
| 1 | `UI` | 普通 UI |
| 2 | `Top` | 顶层弹窗 |
| 3 | `Tips` | Toast、飘字 |
| 4 | `System` | 系统层、等待界面 |

窗口通过覆写属性设置层级：

```csharp
public class BagWindow : UIWindow
{
    protected override UILayer windowLayer => UILayer.UI;
    public override bool FullScreen => true;
    public override int HideTimeToClose => 10;
}
```

### UIWindow 生命周期

```
GameModule.UIModule.ShowWindow<T>()
    │
    ▼
InternalCreateWindow()      ← new T()，AssetLocation 默认 typeof(T).Name
    │
    ▼
InternalLoad()              ← UIModule.ResourceLoader.LoadGameObject
    │
    ▼
ScriptGenerator()           ← 绑定 UI 节点引用
    │
    ▼
BindMemberProperty()        ← 初始 UI 属性
    │
    ▼
RegisterEvent()             ← 注册 UI 事件，AddUIEvent 自动清理
    │
    ▼
OnCreate()                  ← 首次创建初始化
    │
    ▼
OnRefresh()                 ← 每次 Show/复用窗口刷新
    │
    ▼
OnUpdate()                  ← 仅确实需要每帧更新时覆写
    │
    ▼
OnVisible() / OnHidden()    ← 可见性变化或被全屏窗口遮挡时触发
    │
    ▼
Hide() / Close()
    ├── Hide：隐藏并启动 HideTimeToClose 关闭计时器
    └── Close：
        RemoveAllUIEvents()
        子 Widget Destroy()
        OnDestroy()
        Object.Destroy(gameObject)
```

关键规则：

- DGame 窗口没有 `[Window]` 特性，资源地址默认窗口类名。
- `RegisterEvent` 使用 `AddUIEvent`，不要手写清理 UI 事件。
- `OnRefresh` 每次显示都会执行。
- `OnUpdate` 默认实现会把 `m_hasOverrideUpdate` 置为 false；未覆写且子 Widget 不需要更新时，UI 会退出持续 Update 队列。
- `OnDestroy` 不要再依赖子 Widget 状态。

### 扩展虚方法

| 方法 | 触发时机 | 常见用途 |
|------|----------|----------|
| `OnVisible()` | 窗口或 Widget 变为可见；上层全屏遮挡解除也会触发 | 恢复动效、刷新可见状态 |
| `OnHidden()` | 窗口或 Widget 隐藏；被上层全屏界面覆盖也会触发 | 暂停动效、停止临时刷新 |
| `OnSortingOrderChange()` | 窗口层级排序变化，`_OnSortingOrderChange()` 会先通知子 Widget | 依赖 SortingOrder 的粒子、特效、Canvas 调整 |

### UIModule 核心 API

```csharp
GameModule.UIModule.ShowWindow<MainWindow>();
GameModule.UIModule.ShowWindowAsync<GMPanel>();
var waiting = await GameModule.UIModule.ShowWindowAsyncAwait<WaitingUI>();

// 传参：ShowWindow 系列都是 params object[]，窗口内用 UserData / UserDatas 读取
GameModule.UIModule.ShowWindow<ItemDetailWindow>(itemId, from);

var mainWindow = GameModule.UIModule.GetWindow<MainWindow>();
var loadedWindow = await GameModule.UIModule.GetWindowAsyncAwait<MainWindow>();
GameModule.UIModule.GetWindowAsync<MainWindow>(window => window.RefreshData());

GameModule.UIModule.CloseWindow<GMPanel>();
GameModule.UIModule.CloseAllWindows();
GameModule.UIModule.CloseAllWindowsWithOut(mainWindow);
GameModule.UIModule.CloseAllWindowsWithOut(new List<UIWindow> { mainWindow, waiting });
GameModule.UIModule.CloseAllWindowsWithOut<MainWindow>();
bool loading = GameModule.UIModule.IsAnyLoading();

// 已持有窗口实例时，也可直接隐藏/关闭
waiting.Hide();   // 隐藏并启动 HideTimeToClose 关闭计时器
waiting.Close();  // 直接关闭
```

`CloseAllWindowsWithOut` 有 3 个公开重载：`List<UIWindow>`、单个 `UIWindow`、泛型 `T`。

`ShowWindow` / `ShowWindowAsync` / `ShowWindowAsyncAwait` 均为 `params object[] userDatas`。窗口内读取：`UserData`（首个参数 `m_userDatas[0]`，无则 null）、`UserDatas`（完整数组）。在 `OnRefresh` 里按需取值刷新界面。

### IUIController 全局 UI 消息

`IUIController` 用于注册随 `UIModule` 生命周期常驻的 UI 消息处理器，适合“跨模块事件触发 UI”的入口，例如显示等待界面、登录界面、语言变化后的 UI 统一处理。

接口定义：

```csharp
public interface IUIController
{
    void RegUIMessage();
}
```

继承 `IUIController` 的类会由 `UIControllerGenerator` 源代码生成器自动注册到 `UIModule` 生成代码中的 `m_uiControllers`。生成器会生成：

- `private List<IUIController> m_uiControllers`
- `partial void RegisterAllController()`
- `AddUIController<T>() where T : IUIController, new()`

`UIModule.OnInit()` 调用 `RegisterAllController()`，每个 controller 会 `new T()`、加入 `m_uiControllers`，并立即执行 `controller.RegUIMessage()`。业务代码不要手动改 `m_uiControllers`，也不要手写 `RegisterAllController()`。

示例：

```csharp
namespace GameLogic
{
    public class CommonUIController : IUIController
    {
        public void RegUIMessage()
        {
            GameEvent.AddEventListener<uint, uint, System.Action>(
                ICommonUI_Event.ShowWaitingUI,
                OnShowWaitingUI);
        }

        private void OnShowWaitingUI(uint waitFuncID, uint textID, System.Action callback)
        {
            OnShowWaitingUIAsync(waitFuncID, textID, callback).Forget();
        }

        private async UniTaskVoid OnShowWaitingUIAsync(
            uint waitFuncID,
            uint textID,
            System.Action callback)
        {
            var ui = await GameModule.UIModule.ShowWindowAsyncAwait<WaitingUI>();
            ui?.Init(waitFuncID, textID, callback);
        }
    }
}
```

使用规则：

- Controller 类放在 `GameLogic` 命名空间下，并直接实现 `IUIController`；生成器按 `: IUIController` 识别。
- 类必须可 `new()`，不要依赖有参构造。
- `RegUIMessage()` 注册的是全局常驻监听；窗口/Widget 自身生命周期内的监听仍写在 `RegisterEvent()`，用 `AddUIEvent` 自动清理。
- `IUIController` 适合集中处理“事件 -> 打开/刷新 UI”的入口，不适合放具体窗口节点绑定、列表刷新或临时 UI 状态。

### UIWidget 子组件

```csharp
var widget = CreateWidget<ItemWidget>("m_goItem");
var widget = CreateWidgetByPath<ItemWidget>(parent, "ItemWidget");
var widget = await CreateWidgetByPathAsync<ItemWidget>(parent, "ItemWidget");
var widget = CreateWidgetByPrefab<ItemWidget>(prefab, parent);

AdjustItemNum(m_items, count, parent, prefab);
await AsyncAwaitAdjustItemNum(m_items, count, parent, prefab, maxNumPerFrame: 5, updateAction: RefreshItem);
```

UIWidget 生命周期与 UIWindow 一致：`ScriptGenerator → BindMemberProperty → RegisterEvent → OnCreate → OnRefresh → OnDestroy`。

---

## 二、使用模式

### UI 内部事件

```csharp
protected override void RegisterEvent()
{
    AddUIEvent<int>(ILocalization_Event.OnLanguageChanged, OnLanguageChanged);
}
```

`AddUIEvent` 支持 0 到 6 个泛型参数，随窗口/Widget 销毁自动移除。

### 典型窗口骨架

```csharp
public partial class BagWindow : UIWindow
{
    protected override void ScriptGenerator()
    {
        // 绑定节点，或由 Gen partial 文件实现
    }

    protected override void RegisterEvent()
    {
        AddUIEvent<int>(IBag_Event.OnItemChanged, OnItemChanged);
    }

    protected override void OnCreate()
    {
    }

    protected override void OnRefresh()
    {
    }

    protected override void OnDestroy()
    {
    }
}
```

---

## 三、常见错误

| 错误 | 正确做法 |
|------|---------|
| 使用 `[Window(UILayer.UI, "BagUI")]` | DGame 不使用该特性，覆写属性即可 |
| 调用 `ShowUIAsync` | DGame API 是 `ShowWindowAsync` |
| 在 UI 中直接 `Object.Instantiate` | 使用 `ShowWindow` 或 `CreateWidget...` |
| 在 UI 中手动 `GameEvent.AddEventListener` | 在 `RegisterEvent` 中 `AddUIEvent` |
| 使用 `Resources.Load` 加载 UI | 默认通过 `UIModule.ResourceLoader` |

---

## 四、交叉引用

- UI 进阶模式见 [ui-patterns.md](ui-patterns.md)
- 事件系统见 [event-system.md](event-system.md)
- 资源加载见 [resource-api.md](resource-api.md)
