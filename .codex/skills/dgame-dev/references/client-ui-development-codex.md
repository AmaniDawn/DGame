# DGame 客户端 UI 开发指南（精简版）

当需求涉及 `UIWindow`、`UIWidget`、层级/模态、列表、Spine/粒子/序列帧 Widget、`IUIController`、`SwitchPageMgr` 或 `UIModule` 时，先读本文件。

目标：只保留 Codex 做 UI 落位和实现决策必须知道的信息。原始细节以 `references/originals/client-ui-development.md` 为准。

## 落位规则

- 完整页面/弹窗：`UIWindow`
- 局部复用组件：`UIWidget`
- Tab 子页面：`BaseChildPage`
- 普通列表：`UIListBase`
- 大数据滚动复用列表：`UILoopListWidget` / `UILoopGridWidget`
- 特殊展示：`UISpineWidget` / `UIParticleWidget` / `UIFrameWidget`

## UI 系统结构

```text
UIModule
  管理 UIRoot、窗口栈、显示/隐藏/关闭、Esc 关闭、控制器注册

UIWindow
  管理完整窗口生命周期、层级、模态、全屏、弹窗动画、安全区

UIWidget / BaseChildPage
  管理局部组件和子页面
```

## `UIModule`

- UI 总入口
- 从场景中查找 `UIRoot`，缓存 `UICanvas` / `UICamera`
- 注册所有 `IUIController`
- 管理窗口栈、显示/隐藏/关闭、Esc 关闭
- 若场景里没有 `UIRoot`，会直接报错

## `UIWindow`

### 生命周期

固定主线：

```text
ScriptGenerator()
-> BindMemberProperty()
-> RegisterEvent()
-> OnCreate()
-> OnRefresh()
-> OnDestroy()
```

约束：

- 绑定代码在 `ScriptGenerator()`
- 组件初始化在 `BindMemberProperty()`
- 事件注册放 `RegisterEvent()`
- 创建逻辑放 `OnCreate()`
- 刷新逻辑放 `OnRefresh()`
- 释放逻辑放 `OnDestroy()`

### 默认行为

- 默认层级：`UILayer.UI`
- 默认 `FullScreen => false`
- 默认 `NeedTweenPop => true`
- `NeedTweenPop && !FullScreen` 时执行弹窗动画
- 默认窗口可被 Esc 关闭

### `FullScreen`

影响：

- 默认模态类型
- 是否执行 `TweenPop()`
- 顶部全屏窗口出现时是否隐藏下层窗口

结论：

- 全屏窗口通常显式覆写 `FullScreen => true`
- 普通弹窗不要误标成全屏
- 顶部全屏窗口会把下层窗口隐藏但不销毁

### 安全区

常用入口：

- `SetUIFit(...)`
- `SetUINotFit(...)`

规则：

- 整个窗口适配安全区时，对主内容根节点调用 `SetUIFit(...)`
- 局部节点不想受影响时，再调用 `SetUINotFit(...)`

## `UIWidget`

- 局部 UI 子组件基类
- 生命周期与 `UIWindow` 接近
- 不负责模态、Canvas、窗口栈
- 子组件事件同样放 `RegisterEvent()`

常用创建：

- `CreateWidget<T>(...)`
- `CreateWidgetByPath<T>(...)`
- `CreateByPrefab(...)`

## 列表

### 普通列表

- 基础类：`UIListBase<TItem, TData>`
- 入口：`SetDataNum(...)` / `SetDatas(...)`
- 不要在外部自己维护一套并行的增删 Item 逻辑

### 无限循环列表

- 基于 `SuperScrollView`
- 使用 `UILoopListWidget` / `UILoopGridWidget`
- Item 默认复用
- Item 刷新必须支持“同一对象重复绑定不同索引”

实际项目常见写法更接近 `UILoopListViewWidget<T>`，例如登录服列表页面：

```csharp
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
```

创建回调里通过 `CreateItem(...)` 取复用 Item，再手动 `Init(...)` 当前数据。不要把旧状态留在复用出来的 Item 上。

双泛型封装的常见写法：

```csharp
public partial class MailLoopItem : UILoopItemWidget, IListDataItem<MailData>
{
    public void SetItemData(MailData data)
    {
        // 用 data 刷新全部显示状态
    }
}

public partial class MailLoopList : UILoopListWidget<MailLoopItem, MailData>
{
    public void Refresh(List<MailData> mailList)
    {
        AdjustItemNum(mailList?.Count ?? 0, mailList);
    }
}

private void RefreshMailList(List<MailData> mailList)
{
    m_mailLoopList.Refresh(mailList);
}
```

这类封装更适合“列表子类内部直接调用 `AdjustItemNum(...)`，业务层只管调用列表的封装刷新入口”的场景。

选择规则：

- 小量固定项：普通列表
- 大数据滚动复用：循环列表

## 特殊 Widget

### `UISpineWidget`

- Spine 动画/换皮/点击/缩放/镜像
- 依赖固定节点结构：
  `m_tfUISpineRoot` / `m_goSpineModel` / `m_tfEffRoot`
- 预制体结构不一致时不要硬套

### `UIParticleWidget`

- UI 内嵌粒子特效
- 适合 UI 粒子，不替代场景特效系统

### `UIFrameWidget`

- 模型配置驱动的序列帧控件
- 初始化入口：`Init(int modelID, ...)`
- 使用前要有有效 `modelID`

## 绑定工具

当前有两套绑定工具，处理现有窗口时先跟随已有模式，不要混切。

### `UIBindComponent + *_Gen.g.cs`

- 组件列表 + 索引绑定
- 适合稳定页面

### `UIScriptGenerator`

- 按节点命名规则扫描生成代码
- 更偏路径和命名驱动

节点命名规范直接看 `references/client-conventions-codex.md`。

## `IUIController`

- 只定义 `RegUIMessage()`
- `UIModule` 会统一调用 `RegisterAllController()`
- 注册代码由生成器处理

结论：

- 正常实现 `IUIController` 即可
- 不要再手写平行初始化链和控制器注册代码

## `SwitchPageMgr` / `BaseChildPage`

- `SwitchPageMgr`：管理 Tab、页面实例、切换、共享数据
- `BaseChildPage`：承载具体子页面逻辑

结论：

- 多页签窗口优先复用这套体系
- 不要自建平行 Tab 管理器

## 使用原则

1. 页面走 `UIWindow`，组件走 `UIWidget`，子页面走 `BaseChildPage`
2. 事件统一放 `RegisterEvent()`，并优先使用 `AddUIEvent(...)`
3. 绑定工具跟随现有窗口，不要混切
4. 列表优先复用现有封装，不要重复造轮子
5. Spine/粒子/序列帧分别走现有专用 Widget
6. `IUIController` 走统一自动注册入口
