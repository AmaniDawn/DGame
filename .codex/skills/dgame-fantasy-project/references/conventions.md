# Conventions

## Layering

- 基础设施改动放在 `DGame/Runtime`
- 业务封装与玩法逻辑放在 `GameLogic`
- 网络底层接入放在 `Fantasy.Unity`
- 编辑器功能放在 `DGame/Editor` 或项目级 `Assets/Editor`

不要把业务临时逻辑塞进 Runtime，也不要让 Runtime 反向依赖 GameLogic。

## Module Access

- HotFix 业务层优先通过 `GameModule`
- 只有封装基础设施时才直接用 `ModuleSystem.GetModule<T>()`
- 新增业务单例时先确认项目已有 `Singleton` / `MonoSingleton` 体系能否复用

## Async Style

- 资源异步 API 走 `UniTask`
- Fantasy 网络异步流程沿用其现有 `FTask`
- 不要在同一调用链里随意混写多套异步抽象而不处理边界

## UI Style

- 新窗口优先继承 `UIWindow`
- 新控件优先继承 `UIWidget` 或已有 Widget 基类
- UI 绑定优先使用 `AutoBindComponent`
- 列表/滚动视图优先复用 `SuperScrollView` 相关封装

## Resource Style

- 加载和释放成对考虑
- 能复用资源模块就不要直接写裸 `Resources.Load`
- 改资源定位字符串时确认包名、地址、Prefab 资源路径是否一致

## Editing Strategy

- 先做最小改动，不要为了“更优雅”顺手重构整层框架
- 改公共 API 时搜索调用方
- 改编辑器工具时注意生成物、菜单路径和构建输出
- 改 UI 或场景结构时尽量同步验证 Prefab/场景而不是只改脚本

## Review Priorities

评审当前项目代码时优先关注：

1. Runtime/HotFix 分层是否被破坏
2. 资源是否泄漏或缺失释放
3. UI 生命周期是否和窗口栈冲突
4. 构建链路是否只改了一半
5. Fantasy 网络初始化和重连状态是否可能错乱

