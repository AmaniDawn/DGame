# DGame 客户端红点开发指南（精简版）

当需求涉及红点节点定义、红点刷新、`RedDotItem` UI 接入、`RedDotModule`、`RedDotNode` 或 `RedDotPathDefine` 时，先读本文件。

目标：只保留 Codex 做红点接入和修改必须知道的信息。原始细节以 `references/originals/client-reddot-development.md` 为准。

## 核心规则

- 红点体系统一走 `RedDotModule + RedDotPathDefine + RedDotItem`
- 红点值变化统一通过 `GameModule.RedDotModule`
- UI 上显示红点统一使用 `RedDotItem`
- UI 创建红点优先用 `UIBase.CreateRedDot(...)` / `CreateRedDotAsync(...)`
- 节点 ID / 路径优先使用生成的 `RedDotPathDefine`
- 不要手写路径字符串、数字 ID、另一套 UI 提示系统

## 一图理解

```text
RedDotPathDefine
  生成的节点 ID / Path 定义

RedDotModule
  注册节点、查找节点、更新值、管理监听

RedDotNode
  单个节点，负责值、父子聚合、监听通知

RedDotItem
  UI 红点控件

UIBase
  提供 CreateRedDot / CreateRedDotAsync
```

## 刷新方式

常用入口：

```csharp
GameModule.RedDotModule.SetValue(nodeIdOrPath, value);
GameModule.RedDotModule.AddValue(nodeIdOrPath, delta);
GameModule.RedDotModule.ClearNodeValue(nodeIdOrPath);
```

推荐：

```csharp
GameModule.RedDotModule.SetValue(RedDotPathDefine.Main.Mail.System, 1);
GameModule.RedDotModule.ClearNodeValue(RedDotPathDefine.Main.Mail.System);
```

结论：

- 业务只更新节点值
- `RedDotNode` 变化后会通知监听者
- `RedDotItem` 会自动刷新
- 不需要自己额外发 UI 刷新消息

## UI 接入

`RedDotItem` 已集成在 `UIBase` 的创建辅助里。

直接用：

```csharp
CreateRedDot(RedDotPathDefine.Main.Mail.System, m_tfRedDotRoot);
```

异步：

```csharp
await CreateRedDotAsync(RedDotPathDefine.Main.Mail.System, m_tfRedDotRoot);
```

不要自己写：

```csharp
CreateWidgetByType<RedDotItem>(...).Init(...);
```

## 关键对象

### `RedDotModule`

- 维护红点树
- 注册节点
- 通过 ID 或路径获取节点
- 设置值 / 增量更新 / 清空值
- 添加和移除监听
- 初始化时会调用 `RedDotPathDefine.RegisterAll()`

结论：业务通常不需要自己补基础注册逻辑。

### `RedDotNode`

- 保存 `Id` / `Path` / `Type` / `Value`
- 维护父子关系
- 聚合子节点值
- 值变化时通知监听者

结论：

- 叶子节点可直接 `SetValue`
- 非叶子节点会根据聚合自动计算
- 不要再手动重复给父节点补同类值

### `RedDotPathDefine`

- 位于生成文件 `RedDotPathDefine_Gen.g.cs`
- 提供节点 `Id` / `Path` / `Segments` / `RegisterAll()`
- 来自红点编辑器配置生成

结论：

- 新增或修改红点树：先改编辑器配置，再生成代码
- 不要手改生成文件

## 典型流程

### 业务刷新红点

```csharp
GameModule.RedDotModule.SetValue(RedDotPathDefine.Main.Mail.System, 1);
GameModule.RedDotModule.SetValue(RedDotPathDefine.Main.Bag.Item, 12);
GameModule.RedDotModule.ClearNodeValue(RedDotPathDefine.Main.Mail.System);
```

### UI 接入红点

```csharp
protected override void OnCreate()
{
    base.OnCreate();
    CreateRedDot(RedDotPathDefine.Main.Mail.System, m_tfMailRedDotRoot);
}
```

### 手动监听节点

只有在不需要 `RedDotItem`、而是要自己处理节点变化时才直接监听：

```csharp
GameModule.RedDotModule.AddListener(RedDotPathDefine.Main.Mail.System, OnMailRedDotChanged);
GameModule.RedDotModule.RemoveListener(RedDotPathDefine.Main.Mail.System, OnMailRedDotChanged);
```

## 常见错误

- 手写路径字符串或数字 ID
- 手改 `RedDotPathDefine_Gen.g.cs`
- 自己手动创建 `RedDotItem`
- 只改 UI 显隐，不更新红点节点值
- 页面销毁后忘记移除手动监听
- 对非叶子节点重复手动补值，破坏父子聚合语义
