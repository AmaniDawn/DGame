# DGame 客户端UI开发指南

当需求涉及 `UIWindow`、`UIWidget`、窗口生命周期、层级与模态、窗口关闭行为、安全区适配、节点绑定代码生成、列表与无限循环列表、`UISpineWidget`、`UIParticleWidget`、`UIFrameWidget`、`IUIController`、`SwitchPageMgr`、`BaseChildPage` 或 `UIModule` 时，先阅读本文件。

本文档记录当前仓库中已确认的 DGame 客户端 UI 系统事实，并给出按现有框架落代码的推荐方式。若局部实现与本文档有出入，优先以目标窗口、目标 Widget 和其附近已有模式为准。

## 目录导航

- [DGame 客户端UI开发指南](#dgame-客户端ui开发指南)
  - [目录导航](#目录导航)
  - [整体结构概览](#整体结构概览)
  - [UIModule 模块](#uimodule-模块)
  - [UIWindow](#uiwindow)
    - [生命周期图](#生命周期图)
    - [`UILayer` 层级](#uilayer-层级)
    - [`UIType` 窗口类型](#uitype-窗口类型)
    - [`ModelType` 窗口模态类型](#modeltype-窗口模态类型)
    - [`NeedTweenPop`](#needtweenpop)
    - [`CanEscClose` 和 `OnEscCloseLastOneWindowCallback`](#canescclose-和-onesccloselastonewindowcallback)
    - [`SetUISafeFitHelper` 刘海屏适配](#setuisafefithelper-刘海屏适配)
    - [`FullScreen`](#fullscreen)
    - [UI 内部事件](#ui-内部事件)
    - [UIWindow 节点绑定类型](#uiwindow-节点绑定类型)
    - [节点绑定命名规范](#节点绑定命名规范)
  - [UIWidget 子组件](#uiwidget-子组件)
    - [生命周期](#生命周期)
    - [创建方式](#创建方式)
    - [列表数量自动调整](#列表数量自动调整)
  - [无限循环列表](#无限循环列表)
  - [`UISpineWidget`](#uispinewidget)
  - [`UIParticleWidget`](#uiparticlewidget)
  - [`UIFrameWidget`](#uiframewidget)
  - [`IUIController`](#iuicontroller)
  - [`SwitchPageMgr` 和 `BaseChildPage`](#switchpagemgr-和-basechildpage)
  - [使用原则](#使用原则)

## 整体结构概览

当前仓库中的 UI 系统，建议先按下面几层理解：

```text
UIModule
  负责 UI 根节点、窗口栈、层级排序、显示/隐藏/关闭、Esc 关闭与控制器注册

UIWindow
  负责完整窗口生命周期、模态、层级、Canvas、显示/隐藏、TweenPop、安全区适配

UIWidget / BaseChildPage / SwitchTabItem
  负责局部 UI 组件、子页面、Tab 和可复用界面块

List / LoopList / LoopGrid / Spine / Particle / Frame
  负责列表、无限循环列表和特殊表现型 Widget

Editor 绑定工具
  负责 ScriptGenerator / UIBindComponent 两套绑定代码生成
```

可以先按这条规则判断落位：

- 完整页面或弹窗：`UIWindow`
- 局部复用组件：`UIWidget`
- Tab 下的子页面：`BaseChildPage`
- 列表容器：`UIListBase` 或 `UILoopListWidget` / `UILoopGridWidget`
- 特殊展示控件：`UISpineWidget` / `UIParticleWidget` / `UIFrameWidget`

## UIModule 模块

当前 `UIModule` 位于 `GameUnity/Assets/Scripts/HotFix/GameLogic/Module/UIModule/UIModule.cs`，是 UI 系统的总入口。

当前已确认职责：

- 查找场景里的 `UIRoot`
- 缓存 `UICanvas` 和 `UICamera`
- 初始化 `UIResourceLoader`
- 常驻 `UIRoot`
- 注册所有 `IUIController`
- 维护窗口栈、窗口查表和弹窗队列
- 每帧驱动窗口 `InternalUpdate()`
- 监听 `Escape` 并尝试关闭顶部窗口
- 提供安全区设置、窗口显示/隐藏/关闭和栈管理能力

当前项目里，UI 根节点不是窗口自己创建的，而是由 `UIModule.OnInit()` 从场景中的 `UIRoot` 取得。若场景里没有 `UIRoot`，`UIModule` 会直接报错。

## UIWindow

当前 `UIWindow` 位于 `GameUnity/Assets/Scripts/HotFix/GameLogic/Module/UIModule/UIWindow.cs`，是所有窗口的统一基类。

### 生命周期图

当前窗口生命周期可先按下面流程理解：

```text
new T()
  -> Initialize(windowName, assetLocation)
  -> InternalLoad(...)
     -> Handle_Completed(...)
  -> InternalCreate()
     -> ScriptGenerator()
     -> BindMemberProperty()
     -> RegisterEvent()
     -> OnCreate()
     -> SetModelState(GetModelType())
     -> 如果 NeedTweenPop 且非 FullScreen，则 TweenPop()
  -> InternalRefresh()
     -> OnRefresh()
  -> Show()/Hide()/Close()
  -> Destroy()
     -> RemoveAllUIEvents()
     -> 销毁全部 Child Widget
     -> OnDestroy()
```

可以直接记住：

- `ScriptGenerator()` 负责绑定生成代码。
- `BindMemberProperty()` 负责取组件、初始化字段。
- `RegisterEvent()` 负责注册 UI 事件和消息监听。
- `OnCreate()` 负责创建时逻辑。
- `OnRefresh()` 负责刷新表现和数据。
- `OnDestroy()` 负责窗口级释放。

### `UILayer` 层级

当前 `UILayer` 位于 `GameUnity/Assets/Scripts/HotFix/GameLogic/Module/UIModule/UILayer.cs`。

| 枚举值 | 含义 |
| --- | --- |
| `Bottom` | 底层 |
| `UI` | 普通 UI 层 |
| `Top` | 顶层 |
| `Tips` | 提示层 |
| `System` | 系统级层 |

`UIWindow` 默认层级是：

```csharp
protected virtual UILayer windowLayer => UILayer.UI;
```

因此新增窗口若不覆写，默认都在普通 `UI` 层。

### `UIType` 窗口类型

当前 `UIType` 位于 `GameUnity/Assets/Scripts/HotFix/GameLogic/Module/UIModule/UIType.cs`。

| 枚举值 | 含义 |
| --- | --- |
| `None` | 无类型 |
| `Window` | 窗口 |
| `Widget` | 组件 |

当前约定：

- `UIWindow.Type` 固定返回 `UIType.Window`
- `UIWidget.Type` 固定返回 `UIType.Widget`

### `ModelType` 窗口模态类型

当前 `ModelType` 位于 `GameUnity/Assets/Scripts/HotFix/GameLogic/Module/UIModule/ModelType.cs`。

| 枚举值 | 作用 |
| --- | --- |
| `NormalType` | 普通模态 |
| `TransparentType` | 透明模态 |
| `NormalType75` | 75% 透明度普通模态 |
| `UndertintHaveClose` | 浅色遮罩并带点击关闭 |
| `NormalHaveClose` | 普通模态并带点击关闭 |
| `TransparentHaveClose` | 透明模态并带点击关闭 |
| `NoneType` | 非模态 |

`UIWindow.GetModelType()` 默认行为：

- `FullScreen == true` 时返回 `TransparentType`
- `WindowLayer == UILayer.Top` 时返回 `TransparentType`
- 其他情况默认 `NormalType`

`SetModelState(ModelType)` 会：

- 根据模态类型计算遮罩透明度
- 按需创建 `ModelSprite` 作为模态背景
- 在可关闭模态类型下给背景挂关闭按钮

### `NeedTweenPop`

`UIWindow` 默认：

```csharp
protected virtual bool NeedTweenPop => true;
```

在 `InternalCreate()` 中：

- 若 `NeedTweenPop == true`
- 且 `FullScreen == false`

则会执行 `TweenPop()`：

- 初始缩放 `0.8`
- `DOScale` 到 `1`
- 使用 `Ease.OutBack`

因此：

- 普通弹窗默认带弹出动画
- 全屏窗口默认不会走这段弹窗动画
- 若某个窗口不需要弹出动画，覆写 `NeedTweenPop => false`

### `CanEscClose` 和 `OnEscCloseLastOneWindowCallback`

当前 `UIWindow` 暴露：

- `CanEscClose`
- `OnEscCloseLastOneWindowCallback`
- `SetEscCloseLastOneWindowCallback(...)`

`UIModule.OnUpdate()` 在检测到 `Escape` 后，会调用：

- `GetAndCloseTopWindow((int)UILayer.System)`

其逻辑是：

- 找到排除指定层后的顶部窗口
- 若 `CanEscClose == true`，直接关闭
- 否则触发 `OnEscCloseLastOneWindowCallback`

这意味着：

- 默认窗口可被 Esc 关闭
- 若某窗口不允许 Esc 关闭，需要调整 `CanEscClose`
- 若不允许关闭但要响应 Esc，可设置 `OnEscCloseLastOneWindowCallback`

### `SetUISafeFitHelper` 刘海屏适配

当前窗口内的安全区适配通过 `SetUISafeFitHelper` 完成，相关入口在 `UIWindow`：

- `SetUIFit(...)`
- `SetUINotFit(RectTransform rect)`
- `SetUINotFit(RectTransform rect, RectTransform refRect)`

`SetUISafeFitHelper` 当前已确认支持：

- 顶部刘海适配
- 底部安全区适配
- 特定机型和平台偏移调整
- 指定节点脱离适配影响

因此：

- 整个窗口需要刘海适配时，优先对主内容根节点调用 `SetUIFit(...)`
- 某个局部节点不想被安全区拉伸影响时，再调用 `SetUINotFit(...)`

### `FullScreen`

当前 `UIWindow` 默认：

```csharp
public virtual bool FullScreen => false;
```

它直接影响：

- 默认模态类型
- 是否执行 `TweenPop()`
- 是否在顶部全屏窗口出现时隐藏下层窗口

当前 `UIModule.OnSetWindowVisible()` 已确认会从顶部往下遍历窗口栈：

- 遇到第一个可见的 `FullScreen` 窗口后
- 把其下方窗口统一 `Show(false)`

这里不是销毁下层窗口，而是通过 `UIWindow.Visible` 走 `CanvasGroup` 显隐和交互开关，把底部窗口隐藏起来。

这意味着 `FullScreen` 不只是影响弹窗动画，还参与“被遮挡窗口隐藏”的性能优化：

- 顶部全屏窗口打开时，底部全屏窗口或其下方窗口会被隐藏
- 被隐藏窗口不会销毁，顶部全屏窗口关闭后可恢复显示
- 这样可以减少被完全遮挡窗口的渲染和交互开销

因此：

- 全屏窗口通常应显式覆写 `FullScreen => true`
- 不要把普通弹窗误标成全屏
- 若窗口打开后应该压住下层并隐藏底部显示时，才应设置为全屏

### UI 内部事件

UI 开发里还有一类“界面内部交互事件”，例如按钮点击、Tab 点击、列表项点击和 `UIEventItem` 这类交互封装。

这一部分在当前仓库中确实属于 UI 开发的日常内容，但事件中心、`AddUIEvent(...)`、接口事件和消息监听的细节应以 `references/originals/client-event-system.md` 为准。处理窗口内部交互时，这里只需要记住：

- 控件交互事件属于 UI 实现层
- 全局消息监听属于事件系统层
- 两者不要混着设计

### UIWindow 节点绑定类型

当前项目存在两套自动生成绑定代码的工具。

#### 方式 1：`UIBindComponent + *_Gen.g.cs`

当前已确认样例：

- `GameUnity/Assets/Scripts/HotFix/GameLogic/Module/UIModule/AutoBindComponent/UIBindComponent.cs`
- `GameUnity/Assets/Scripts/HotFix/GameLogic/UI/Gen/MainWindow_Gen.g.cs`

这套方式的特点：

- 窗口上挂 `UIBindComponent`
- 通过索引缓存组件列表
- 生成的 `*_Gen.g.cs` 在 `ScriptGenerator()` 中按索引取组件
- 点击事件也在生成代码里自动绑定

适合：

- 稳定页面
- 需要把组件绑定明确固化到生成文件

#### 方式 2：`UIScriptGenerator`

当前编辑器工具位于：

- `GameUnity/Assets/Editor/UIScriptGenerator/UIScriptGenerator.cs`
- `GameUnity/Assets/Editor/UIScriptGenerator/UIScriptGeneratorSettings.cs`

这套方式的特点：

- 按节点命名规则扫描层级
- 生成字段、`ScriptGenerator()` 代码和按钮/Toggle/Slider 回调模板
- 默认直接生成到剪贴板
- 支持普通生成和 `UniTask` 回调模板
- 在 `UseBindComponent == false` 时启用

可以理解为：

- `UIBindComponent` 更偏“组件列表 + 索引绑定”
- `UIScriptGenerator` 更偏“按节点路径和命名规则生成代码”

处理现有窗口时，不要混改成另一套工具链；先跟随当前窗口已有模式。

### 节点绑定命名规范

节点命名规范不要在 UI 指南里重复展开，直接以 `references/originals/client-conventions.md` 中的“UI 节点命名规范”一节为准。

这里只保留一条使用规则：

- 在做绑定代码生成前，先确认节点命名符合项目当前生成器规则，再决定使用哪套绑定工具

## UIWidget 子组件

当前 `UIWidget` 位于 `GameUnity/Assets/Scripts/HotFix/GameLogic/Module/UIModule/UIWidget.cs`，是局部 UI 子组件基类。

### 生命周期

`UIWidget` 的核心创建流程：

```text
Create()/CreateByPath()/CreateByPrefab()
  -> CreateImp()
     -> CreateBase()
     -> ResetChildCanvas()
     -> Parent.AddChild(this)
     -> ScriptGenerator()
     -> BindMemberProperty()
     -> RegisterEvent()
     -> OnCreate()
     -> OnRefresh()
     -> IsPrepared = true
     -> Show(true/false)
```

销毁流程：

```text
Destroy()
  -> Parent.RemoveChild(this)
  -> OnDestroy()
  -> RemoveAllUIEvents()
  -> Destroy all child widgets
  -> Destroy(gameObject)
```

因此：

- `UIWidget` 生命周期和 `UIWindow` 很接近，但不负责模态、Canvas 和窗口栈
- 子组件里的事件监听同样建议放 `RegisterEvent()`

### 创建方式

当前常用创建方式：

| 方式 | 入口 | 适用场景 |
| --- | --- | --- |
| 通过父节点路径创建 | `CreateWidget<T>(string goPath, ...)` | 父窗口里已有挂点 |
| 通过现成 `GameObject` 创建 | `CreateWidget<T>(goRoot, ...)` | 宿主节点已经存在 |
| 通过资源路径创建 | `CreateWidgetByPath<T>(...)` | 运行时加载 Widget 预制体 |
| 通过预制体创建 | `CreateByPrefab(...)` | 编辑器或已持有 prefab 的场景 |
| 通过类型创建 Tab/子页面等 | 父窗口封装方法 | 现有 UI 框架辅助入口 |

### 列表数量自动调整

当前 `UIListBase<TItem, TData>` 是普通列表基础类。

关键入口：

- `SetDataNum(int n, ...)`
- `SetDatas(List<TData> dataList, int n = -1)`
- `AdjustItemNum(...)`
- `UpdateListItem(...)`

这类列表的核心思想是：

- 先设置数量或数据
- 再由派生类调整 Item 数量和刷新内容

因此，普通列表的“数量自动调整”应沿用 `SetDataNum` / `SetDatas` 入口，不要在外部手写一套并行的增删 Item 逻辑。

## 无限循环列表

当前项目里的无限循环列表基于 `SuperScrollView`，主要封装在：

- `UILoopListWidget<TItem, TData>`
- `UILoopListViewWidget<T>`
- `UILoopGridWidget<TItem, TData>`
- `UILoopGridItemWidget`

当前已确认特征：

- `LoopListView2` / `LoopGridView` 作为底层组件
- 通过 `InitListView` / `InitGridView` 初始化
- 通过 `OnGetItemByIndex(...)` 动态创建和复用 Item
- 使用 `m_itemCache` 缓存已经创建过的 Widget

这意味着：

- 无限循环列表不是普通 `Instantiate` 列表
- Item 复用是默认行为
- Item 刷新必须支持“同一个对象被重复绑定不同索引”

若需求只是小量固定项列表，优先普通 `UIListBase`。若需求是大数据、滚动复用，优先 `UILoopListWidget` / `UILoopGridWidget`。

### 使用例子

参考 `DGame_Fantasy/GameUnity/Assets/Scripts/HotFix/GameLogic/UI/Login/AllServerPage.cs`
和 `RecommendServerPage.cs`，实际项目里更常见的写法是直接使用
`UILoopListViewWidget<TItem>`，然后在页面里自己持有数据，并把 Item 创建回调传给
`InitListView(...)`。

- 页面自己持有数据源
- 通过 `CreateWidget<UILoopListViewWidget<TItem>>(scroll.gameObject)` 创建循环列表 Widget
- 通过 `LoopRectView.InitListView(0, CreateItem)` 注册回调
- 数据变化后调用 `SetListItemCount(...)` 和 `RefreshAllShownItem()`
- Item 在回调里用 `CreateItem(...)` 创建或复用，再手动 `Init(...)`

```csharp
using System.Collections.Generic;
using GameLogic;
using SuperScrollView;
using UnityEngine;
using UnityEngine.UI;

public class MailItemData
{
    public string Title;
}

public partial class MailItem : UILoopItemWidget
{
    [SerializeField] private Text m_txtTitle;

    public void Init(MailItemData data, bool isSelected)
    {
        m_txtTitle.text = data?.Title ?? string.Empty;
        SetSelected(isSelected);
    }
}

public partial class MailPage
{
    private UILoopListViewWidget<MailItem> m_mailLoopListView;
    private readonly List<MailItemData> m_mailList = new();

    protected override void BindMemberProperty()
    {
        m_itemMail.SetActive(false);
        m_mailLoopListView = CreateWidget<UILoopListViewWidget<MailItem>>(m_scrollMail.gameObject);
        m_mailLoopListView.LoopRectView.InitListView(0, CreateMailItem);
    }

    private void RefreshMailList()
    {
        m_mailLoopListView.LoopRectView.SetListItemCount(m_mailList.Count);
        m_mailLoopListView.LoopRectView.RefreshAllShownItem();
    }

    private LoopListViewItem2 CreateMailItem(LoopListView2 listView, int index)
    {
        if (index < 0 || index >= m_mailList.Count)
        {
            return null;
        }

        var item = m_mailLoopListView.CreateItem(m_itemMail);
        if (item == null)
        {
            return null;
        }

        item.Init(m_mailList[index], false);
        return item.LoopItem;
    }
}
```

如果使用 `UILoopListWidget<TItem, TData>` / `UILoopGridWidget<TItem, TData>` 这类二次封装，也仍然要遵守同一个原则：Item 会被复用，刷新逻辑必须能在同一个对象上反复绑定不同索引或不同数据。

如果 Item 需要额外状态，例如选中、高亮、红点或按钮事件，也要在每次 `Init(...)` / 刷新时完整重置，不能把旧状态残留在复用出来的 Item 上。

如果业务侧更适合“列表自己持有数据并统一调用 `SetDatas(...)`”，可以使用双泛型封装。常见写法如下：

```csharp
using System.Collections.Generic;
using GameLogic;
using UnityEngine;
using UnityEngine.UI;

public class MailData
{
    public string Title;
    public bool IsRead;
}

public partial class MailLoopItem : UILoopItemWidget, IListDataItem<MailData>
{
    [SerializeField] private Text m_txtTitle;
    [SerializeField] private GameObject m_goUnread;

    public void SetItemData(MailData data)
    {
        m_txtTitle.text = data?.Title ?? string.Empty;
        m_goUnread.SetActive(data != null && !data.IsRead);
    }
}

public partial class MailLoopList : UILoopListWidget<MailLoopItem, MailData>
{
    public void Refresh(List<MailData> mailList)
    {
        AdjustItemNum(mailList?.Count ?? 0, mailList);
    }
}

public partial class MailWindow
{
    private MailLoopList m_mailLoopList;

    protected override void BindMemberProperty()
    {
        m_mailLoopList = CreateWidget<MailLoopList>(m_scrollMail.gameObject);
        m_mailLoopList.BaseItemPrefab = m_itemMail;
        m_itemMail.SetActive(false);
    }

    private void RefreshMailList(List<MailData> mailList)
    {
        m_mailLoopList.Refresh(mailList);
    }
}
```

这套写法里：

- 列表 Widget 自己负责 `OnGetItemByIndex(...)`、Item 复用和 `UpdateListItem(...)`
- 业务层只调用列表子类自己封装的 `Refresh(...)`
- 列表子类内部直接调用 `AdjustItemNum(...)` 更新数量和数据
- Item 如果实现了 `IListDataItem<TData>`，默认会在 `UpdateListItem(...)` 里收到 `GetData(index)` 返回的数据
- 如果除了数据绑定还要补充选中态、额外点击回调等，可以在派生类里继续封装刷新入口，最终仍然落到 `AdjustItemNum(...)`

## `UISpineWidget`

当前 `UISpineWidget` 受编译宏控制：

- `SPINE_UNITY`
- `SPINE_CSHARP`

它当前支持：

- 切换动画
- 延迟播放动画
- 获取动画时长
- 单皮和组合皮切换
- 改颜色
- 改缩放、镜像、位置
- 绑定点击事件

当前 `ScriptGenerator()` 内部固定查找：

- `m_tfUISpineRoot`
- `m_goSpineModel`
- `m_tfEffRoot`

因此使用时：

- 预制体层级要符合它当前写死的节点结构
- 若节点结构不一致，不要硬套，应先改脚本或另做 Widget

## `UIParticleWidget`

当前 `UIParticleWidget` 基于 `Coffee.UIExtensions.UIParticle`。

当前支持能力：

- `Play` / `Pause` / `Resume` / `Stop`
- `StartEmission` / `StopEmission`
- `RefreshParticles`
- `Clear`
- 2D/3D 缩放
- 镜像翻转
- 本地位置调整
- `Maskable` / `UseCustomView` / `AutoScalingMode` / `PositionMode` / `MeshSharing`

它适合做 UI 内嵌粒子特效，不适合替代普通场景特效系统。

## `UIFrameWidget`

当前 `UIFrameWidget` 基于：

- `UIFrameAnimatorAgent`
- `ModelConfigMgr`

当前支持：

- 异步初始化帧动画模型
- 绑定点击事件
- 切换动画状态

它的初始化入口是：

```csharp
Init(int modelID, Action<UIWidget> clickAction = null)
```

因此：

- `UIFrameWidget` 更偏“模型配置驱动的序列帧显示控件”
- 使用前要有有效 `modelID`

## `IUIController`

当前 `IUIController` 只定义一个入口：

```csharp
void RegUIMessage();
```

`UIModule` 中存在：

```csharp
partial void RegisterAllController();
```

并在 `OnInit()` 中直接调用 `RegisterAllController()`。

结合当前项目约定，可按下面方式理解：

- `IUIController` 只需要正常实现并编写 `RegUIMessage()`
- 注册代码已经由源代码生成器统一处理
- 控制器内部只负责注册自己管理的 UI 消息

因此新增 `IUIController` 实现时：

- 正常继承接口即可
- 不要再手写平行初始化入口
- 不要手工补一套控制器注册代码

## `SwitchPageMgr` 和 `BaseChildPage`

当前 Tab + 子页面体系由：

- `SwitchPageMgr`
- `BaseChildPage`

共同组成。

`SwitchPageMgr` 当前支持：

- 绑定一个 Tab 对应一个或多个 `BaseChildPage`
- 创建 Tab
- 切换页面
- 共享数据传递
- 获取当前页面、刷新页面、设置 Tab 红点/图标

`BaseChildPage` 当前负责：

- 持有 `ChildPageShareData`
- 持有 `SwitchPageMgr`
- 暴露 `OnPageShowed(...)`
- 暴露 `RefreshCurrentChildPage()`

可先按下面方式理解：

| 组件 | 职责 |
| --- | --- |
| `SwitchPageMgr` | 管理 Tab、页面实例、切换和共享数据 |
| `BaseChildPage` | 承载具体子页面逻辑 |

因此：

- 子页面不要自己再发明一套 Tab 管理器
- 同一窗口内的多页签切换优先复用 `SwitchPageMgr`

## 使用原则

处理 DGame UI 开发时，优先遵循以下原则：

1. 完整页面走 `UIWindow`，局部复用块走 `UIWidget`，子页面走 `BaseChildPage`。
2. 事件监听统一放在 `RegisterEvent()`，并依赖 `AddUIEvent(...)` 做生命周期托管。
3. 先沿用目标窗口当前使用的绑定代码工具，不要混切两套生成方式。
4. 列表优先复用现有 `UIListBase` / `UILoopListWidget` / `UILoopGridWidget`，不要重复造轮子。
5. 特效表现优先使用已有专用 Widget：Spine、粒子、序列帧分别走各自封装。
6. `IUIController` 走统一自动注册入口，不要再手写平行初始化链。
