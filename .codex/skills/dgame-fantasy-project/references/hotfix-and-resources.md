# HotFix And Resources

## HotFix Layout

HotFix 代码主要位于：

- `GameUnity/Assets/Scripts/HotFix/GameLogic`
- `GameUnity/Assets/Scripts/HotFix/GameProto`
- `GameUnity/Assets/Scripts/HotFix/Fantasy.Unity`

这三部分职责不同：

- `GameLogic`
  业务逻辑、UI、数据、模块封装、客户端网络流程。
- `GameProto`
  协议或配置生成相关程序集。
- `Fantasy.Unity`
  Fantasy 网络基础设施与 Unity 侧接入。

## Resource Rules

已确认资源系统基于 `IResourceModule + YooAsset + UniTask`。

工作原则：

1. 优先使用现有 `IResourceModule` API。
2. 加载资源时同时思考释放路径。
3. 不要在业务层偷偷缓存 `UnityEngine.Object` 却不参与回收。
4. 如果改的是资源更新、下载、清缓存、包初始化，要同步检查 Editor 构建工具与运行时设置。

## HybridCLR And Hot Update

项目已确认包含：

- `GameUnity/Assets/DGame/Editor/HybridCLR`
- `GameUnity/HybridCLRData`
- 编辑器构建命令 `BuildDllCommand`

说明：

- DLL 构建和复制流程已经由编辑器工具接管
- 修改热更程序集加载、AOT、DLL 复制逻辑时，要同时检查构建命令和资源发布流程

## Fantasy Network Integration

`GameLogic/DataCenter/GameClient.cs` 已确认：

- 使用 `Fantasy` 命名空间
- 通过 `Fantasy.Platform.Unity.Entry.Initialize()` 初始化
- 创建 `Scene`
- 管理连接、重连、心跳、连接状态

因此：

- 涉及连接流程、断线重连、消息驱动时，优先从 `GameClient` 及其 watcher/helper 入手
- 涉及框架级网络改动时，再下钻 `Fantasy.Unity`
- 不要把纯业务 UI 状态处理塞到 Fantasy 运行时基础设施

## Data And Save

与客户端持久化和数据管理相关的目录：

- `GameLogic/DataCenter`
- `GameLogic/DataCenter/ClientSaveData`
- `GameLogic/DataMgr`

修改这类代码时要检查：

- 生命周期初始化时机
- 角色切换或登录切换时的数据清理
- 本地存档结构变更后的兼容性

## Verification Checklist

完成 HotFix/资源相关修改后，至少核对：

1. 资源是否有释放路径
2. 异步方法是否沿用现有任务模型
3. Runtime 和 HotFix 是否出现错误方向依赖
4. 若改构建流程，是否影响 AB、DLL、StreamingAssets 复制

