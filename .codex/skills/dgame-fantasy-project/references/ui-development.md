# UI Development

## Core Structure

UI 主要位于 `GameUnity/Assets/Scripts/HotFix/GameLogic/Module/UIModule`。

已确认的核心类型：

- `UIModule`
- `UIBase`
- `UIWindow`
- `UIWidget`
- `UIResourceLoader`
- `UILayer`
- `UIType`
- `IUIController`

已确认的扩展区域：

- `AutoBindComponent`
- `Widget`
- `SwitchPage`
- `Expansion/UIButton`
- `Expansion/UIImage`
- `Expansion/UIText`
- `Expansion/SuperScrollView`
- `Editor`

## UIModule Expectations

`UIModule` 的现有职责包括：

- 从场景中查找 `UIRoot`
- 管理 `UICanvas` 与 UI Camera
- 初始化 `UIResourceLoader`
- 管理窗口栈和窗口队列
- 在更新循环中驱动窗口 `InternalUpdate`
- 初始化红点系统
- 处理 Debugger ErrorLog 相关行为
- 在部分场景下处理安全区适配

因此：

- 不要绕过 `UIModule` 直接散落创建 UI 根节点
- 不要自己复制一套窗口栈逻辑
- 新窗口的打开、关闭、排序应复用现有框架

## UIWindow Expectations

`UIWindow` 已经负责大量共性逻辑：

- `Canvas` / `GraphicRaycaster` / `CanvasGroup` 处理
- 排序层级和子 Canvas 排序联动
- 可见性与交互切换
- 资源定位地址
- 全屏/遮罩/关闭计时器
- 异步创建与取消令牌

写窗口代码时遵守这些规则：

1. 优先继承 `UIWindow` 或 `UIWidget`，不要直接写裸 `MonoBehaviour` 充当窗口。
2. 资源路径、层级、全屏属性、遮罩行为放到现有覆盖点里实现。
3. 关闭窗口、延迟关闭、返回键处理要和当前窗口栈兼容。

## Auto Bind And Extensions

项目已有 UI 自动绑定和控件扩展能力：

- `UIBindComponent`
- `UIBindTextMeshProUGUI`
- `UIBindText`
- `UIBindContentSizeFitter`
- `UIButton`
- `UIImage`
- `UIText`

优先原则：

- 能用自动绑定就不要重复写 `transform.Find`
- 能复用扩展控件就不要在业务层复制点击、置灰、文本描边等细节
- SuperScrollView 相关列表优先复用已有封装

## Red Dot And Common UI

UI 与红点系统有明确耦合，`UIModule` 初始化时会触发 `RedDotModule.Instance.Initialize()`。

如果任务涉及：

- 菜单入口提示
- 页签提示
- 列表项提示
- GM 面板或通用等待框

优先检查：

- `GameLogic/Module/RedDotModule`
- `GameLogic/UI`
- `GameLogic/Module/UIModule/Widget`

## Unity Editor Operations

如果用户要求“创建/修改 UI Prefab、场景节点、组件挂载、Canvas 结构”，优先使用 Unity MCP 工具直接在 Editor 中执行，而不是只修改脚本：

- 查节点：`gameobject-find`
- 查/改组件：`gameobject-component-get`、`gameobject-component-modify`
- 创建节点：`gameobject-create`
- 改 Prefab：`assets-prefab-open`、`assets-prefab-save`

