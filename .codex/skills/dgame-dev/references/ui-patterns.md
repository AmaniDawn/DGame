# UI 进阶模式

> **适用场景**：UIWidget 创建方式、节点绑定、列表刷新、弹窗队列、模态背景、安全区适配 | **关联文档**：[ui-lifecycle.md](ui-lifecycle.md)、[naming-rules.md](naming-rules.md)

## 节点绑定

手写或生成的 `ScriptGenerator` 通常使用：

```csharp
m_imgIcon = FindChildComponent<Image>("m_imgIcon");
m_textName = FindChildComponent<Text>("m_textName");
m_goEmpty = FindChild("m_goEmpty").gameObject;
```

节点命名前缀按 [naming-rules.md](naming-rules.md)。`Transform` 前缀是 `m_tf`，`Dropdown` 前缀是 `m_dropDown`。

---

## 端到端 UIWindow 示例

窗口用属性覆写设置层级；节点绑定和按钮 `onClick.AddListener` 通常由 `*_Gen.g.cs` 自动生成，业务写在 `partial` 类里。

```csharp
public partial class BagWindow : UIWindow
{
    protected override UILayer windowLayer => UILayer.UI;
    public override bool FullScreen => true;

    private readonly List<ItemWidget> m_items = new();

    // ScriptGenerator 与 m_btnClose.onClick.AddListener(OnClickCloseBtn)
    // 通常由 BagWindow_Gen.g.cs 自动生成

    protected override void RegisterEvent()
    {
        AddUIEvent<int>(IBag_Event.OnItemChanged, _ => OnRefresh());
    }

    protected override void OnCreate()
    {
        m_titleWidget = CreateWidget<TitleWidget>("m_tfTitle");
    }

    protected override void OnRefresh()
    {
        var data = BagData.Items;
        AdjustItemNum(m_items, data.Count, m_tfContent, m_itemPrefab);
        for (int i = 0; i < data.Count; i++)
            m_items[i].SetData(data[i]);
    }

    private partial void OnClickCloseBtn() => GameModule.UIModule.CloseWindow<BagWindow>();
}
```

## UIWidget 模板

```csharp
public class ItemWidget : UIWidget
{
    private Text  m_textName;
    private Image m_imgIcon;

    protected override void ScriptGenerator()
    {
        m_textName = FindChildComponent<Text>("m_textName");
        m_imgIcon  = FindChildComponent<Image>("m_imgIcon");
    }

    public void SetData(ItemConfig cfg)
    {
        m_textName.text = cfg.Name;
        m_imgIcon.SetSprite(cfg.IconPath, gameObject.GetCancellationTokenOnDestroy());  // 传入销毁令牌，避免窗口销毁后回调继续执行
    }
}
```

---

## UIWidget 创建方式

所有泛型 `T` 必须满足 `where T : UIWidget, new()`。

| # | 方法签名 | 用途 | 同步/异步 |
|---|---------|------|-----------|
| 1 | `CreateWidget<T>(string goPath, bool visible = true)` | 从当前 UI 下已有子节点路径创建 | 同步 |
| 2 | `CreateWidget<T>(Transform parentTrans, string goPath, bool visible = true)` | 从指定父节点下的子路径创建 | 同步 |
| 3 | `CreateWidget<T>(GameObject goRoot, bool visible = true)` | 从已存在 GameObject 创建 | 同步 |
| 4 | `CreateWidgetByPath<T>(Transform parentTrans, string assetLocation, bool visible = true)` | 同步加载资源并挂到父节点 | 同步 |
| 5 | `CreateWidgetByPathAsync<T>(Transform parentTrans, string assetLocation, bool visible = true)` | 异步加载资源并挂到父节点 | 异步 |
| 6 | `CreateWidgetByPrefab<T>(GameObject goPrefab, Transform parentTrans = null, bool visible = true)` | 从已有 prefab 克隆 | 同步 |
| 7 | `CreateWidgetByType<T>(Transform parentTrans, bool visible = true)` | 用 `typeof(T).Name` 作为资源地址 | 同步 |
| 8 | `CreateWidgetByTypeAsync<T>(Transform parentTrans, bool visible = true)` | 用 `typeof(T).Name` 异步加载 | 异步 |

示例：

```csharp
var w1 = CreateWidget<ItemWidget>("m_tfContent/m_goItem");
var w2 = CreateWidget<ItemWidget>(m_tfContent, "m_goItem");
var w3 = CreateWidget<ItemWidget>(itemRoot);
var w4 = CreateWidgetByPath<ItemWidget>(m_tfContent, "ItemWidget");
var w5 = await CreateWidgetByPathAsync<ItemWidget>(m_tfContent, "ItemWidget");
var w6 = CreateWidgetByPrefab<ItemWidget>(m_itemPrefab, m_tfContent);
var w7 = CreateWidgetByType<ItemWidget>(m_tfContent);
var w8 = await CreateWidgetByTypeAsync<ItemWidget>(m_tfContent);
```

---

## Widget 创建边界坑

| 场景 | 风险 | 正确处理 |
|------|------|----------|
| `CreateWidget<T>(goPath)` | 只查找已有节点，不会加载资源 | Prefab 内已有节点才用它 |
| `CreateWidgetByPathAsync<T>` | UI 销毁后异步回调继续执行 | 内部使用 `gameObject.GetCancellationTokenOnDestroy()`；调用方仍要避免销毁后访问结果 |
| `CreateWidgetByType<T>` | 资源地址固定为 `typeof(T).Name` | 资源名和 Widget 类型名必须一致 |
| `CreateWidgetByPrefab<T>` | 克隆 prefab 后需要纳入 UIWidget 生命周期 | 用该 API，不要裸 `Instantiate` 后散落管理 |
| `AdjustItemNum` + `assetLocation` | 同步分支无 prefab 时不会使用 `assetLocation` | 需要指定地址时用异步列表 API 或手动创建 |
| `AsyncAdjustItemNum` | 返回 void，内部 `.Forget()`，不能 await | 需要等待时用 `AsyncAwaitAdjustItemNum` |
| 异步列表无 prefab 且 `assetLocation` 为空 | 会尝试异步加载空地址 | 传有效 `assetLocation` 或 prefab |
| Widget 泛型缺 `new()` | 编译失败 | `public class XxxWidget : UIWidget` 且保留无参构造 |

---

## 列表项管理

DGame 列表 API 在 `UIBase` 中：

```csharp
void AdjustItemNum<T>(List<T> itemList, int count, Transform parentTrans,
    GameObject prefab = null, string assetLocation = "")
    where T : UIWidget, new()

void AsyncAdjustItemNum<T>(List<T> itemList, int count, Transform parentTrans,
    GameObject prefab = null, string assetLocation = "",
    int maxNumPerFrame = 5, Action<T, int> updateAction = null)
    where T : UIWidget, new()

UniTask AsyncAwaitAdjustItemNum<T>(List<T> itemList, int count, Transform parentTrans,
    GameObject prefab = null, string assetLocation = "",
    int maxNumPerFrame = 5, Action<T, int> updateAction = null)
    where T : UIWidget, new()
```

普通列表：

```csharp
AdjustItemNum(m_items, data.Count, m_tfContent, m_itemPrefab);
for (int i = 0; i < data.Count; i++)
{
    m_items[i].Refresh(data[i]);
}
```

分帧刷新并等待完成：

```csharp
await AsyncAwaitAdjustItemNum(m_items, data.Count, m_tfContent, m_itemPrefab,
    maxNumPerFrame: 5,
    updateAction: (item, index) => item.Refresh(data[index]));
```

只触发异步创建，不等待：

```csharp
AsyncAdjustItemNum(m_items, data.Count, m_tfContent, m_itemPrefab,
    maxNumPerFrame: 5,
    updateAction: (item, index) => item.Refresh(data[index]));
```

SuperScrollView 代码位于 `Assets/Scripts/HotFix/GameLogic/Module/UIModule/Expansion/SuperScrollView`，大量列表优先复用它。

---

## 弹窗队列

```csharp
GameModule.UIModule.PushWindowToQueue<RewardWindow>();
GameModule.UIModule.StartPopWindowQueue();
GameModule.UIModule.ClearWindowQueue();
```

窗口关闭后 `UIModule` 会自动弹出下一个队列窗口。

---

## 模态背景

覆写 `GetModelType()` 控制背景：

```csharp
protected override ModelType GetModelType() => ModelType.NormalHaveClose;
```

可用值：

- `NormalType`
- `TransparentType`
- `NormalType75`
- `UndertintHaveClose`
- `NormalHaveClose`
- `TransparentHaveClose`
- `NoneType`

模态背景资源默认是 `ModelSprite`。

---

## 安全区适配

```csharp
SetUIFit(m_tfRoot as RectTransform, liuHaiFit: true, bottomFit: true);
SetUINotFit(m_tfFixed as RectTransform);
```

---

## 常见错误

| 错误 | 正确做法 |
|------|---------|
| 列表增删直接 `Destroy/Instantiate` | 使用 `AdjustItemNum`、`AsyncAwaitAdjustItemNum` 或 SuperScrollView |
| 重复打开同一窗口创建多个实例 | `UIModule` 按 `WindowFullName` 复用窗口 |
| 模态背景自己复制一套 Prefab | 复用 `GetModelType()` 和 `ModelSprite` |
| 弹窗队列自己写全局队列 | 使用 `PushWindowToQueue` |
| Widget 资源放到 `Assets/GameScripts` | DGame 热更 UI 在 `Assets/Scripts/HotFix/GameLogic/UI`，资源在 `Assets/BundleAssets` |

---

## 交叉引用

| 主题 | 文档 |
|------|------|
| UIWindow 生命周期、层级、属性 | [ui-lifecycle.md](ui-lifecycle.md) |
| 事件系统（AddUIEvent / GameEvent） | [event-system.md](event-system.md) |
| 节点前缀命名规范 | [naming-rules.md](naming-rules.md) |
| 资源加载/卸载 API | [resource-api.md](resource-api.md) |
| UI 红点绑定 | [reddot-system.md](reddot-system.md) |
