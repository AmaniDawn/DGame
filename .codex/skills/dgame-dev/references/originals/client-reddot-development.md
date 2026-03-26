# DGame 客户端红点开发指南

当需求涉及红点节点定义、红点刷新通知、红点值更新、`RedDotItem` 在 UI 里的使用方式，或判断红点应该怎么接入窗口和 Widget 时，先阅读本文件。

本文档记录当前仓库中红点系统的实际接入方式。处理红点功能时，应优先沿用 `RedDotModule + RedDotPathDefine + RedDotItem` 这套现有体系，不要再发明另一套 UI 提示状态系统。

## 目录导航

- [DGame 客户端红点开发指南](#dgame-客户端红点开发指南)
  - [目录导航](#目录导航)
  - [整体结构概览](#整体结构概览)
  - [核心规则](#核心规则)
  - [红点刷新怎么通知](#红点刷新怎么通知)
  - [`RedDotItem` 与 UI 的集成方式](#reddotitem-与-ui-的集成方式)
  - [`RedDotModule`](#reddotmodule)
  - [`RedDotNode`](#reddotnode)
  - [`RedDotPathDefine`](#reddotpathdefine)
  - [典型接入流程](#典型接入流程)
    - [业务刷新红点](#业务刷新红点)
    - [UI 接入红点](#ui-接入红点)
    - [手动监听节点变化](#手动监听节点变化)
  - [常见错误](#常见错误)
    - [1. 手写路径或节点 ID](#1-手写路径或节点-id)
    - [2. 自己手动创建 `RedDotItem`](#2-自己手动创建-reddotitem)
    - [3. 只刷新 UI，不更新红点节点](#3-只刷新-ui不更新红点节点)
    - [4. 在页面销毁后忘记移除手动监听](#4-在页面销毁后忘记移除手动监听)
    - [5. 对非叶子节点做错误理解](#5-对非叶子节点做错误理解)

## 整体结构概览

当前红点体系建议先按下面几层理解：

```text
RedDotPathDefine
  生成的红点路径 / ID 定义

RedDotModule
  红点树注册、查找、数值更新、监听管理

RedDotNode
  单个红点节点，负责值、聚合和监听通知

RedDotItem
  UI 红点显示控件

UIBase
  提供 CreateRedDot / CreateRedDotAsync，方便窗口和 Widget 接入红点
```

## 核心规则

1. 红点值变化统一通过 `RedDotModule` 通知刷新。
2. UI 上显示红点统一使用 `RedDotItem`。
3. `RedDotItem` 的创建入口已经集成在 `UIBase` 里，优先通过 `CreateRedDot(...)` / `CreateRedDotAsync(...)` 使用。
4. 红点节点 ID 和路径优先使用生成的 `RedDotPathDefine`，不要手写魔法数字和字符串。

## 红点刷新怎么通知

当前红点系统的“通知刷新”不是手动发 UI 事件，而是：

1. 业务侧调用 `RedDotModule` 更新节点值
2. `RedDotNode` 内部值变化后触发监听回调
3. `RedDotItem` 监听到节点变化后，自动刷新显示

当前最直接的入口是：

```csharp
GameModule.RedDotModule.SetValue(nodeId, value);
GameModule.RedDotModule.AddValue(nodeId, delta);
GameModule.RedDotModule.ClearNodeValue(nodeId);
```

也支持路径版：

```csharp
GameModule.RedDotModule.SetValue(path, value);
GameModule.RedDotModule.AddValue(path, delta);
GameModule.RedDotModule.ClearNodeValue(path);
```

推荐优先使用 ID 版，通常配合 `RedDotPathDefine`：

```csharp
GameModule.RedDotModule.SetValue(RedDotPathDefine.Main.Mail.System, 1);
GameModule.RedDotModule.ClearNodeValue(RedDotPathDefine.Main.Mail.System);
```

## `RedDotItem` 与 UI 的集成方式

当前 `RedDotItem` 位于：

- `GameUnity/Assets/Scripts/HotFix/GameLogic/Module/RedDotModule/RedDotItem.cs`

它不是自动附加到每个窗口上的，而是已经被集成到 `UIBase` 的创建辅助里。

当前 `UIBase` 提供：

```csharp
CreateRedDot(int redDotNodeID, Transform parent)
CreateRedDotAsync(int redDotNodeID, Transform parent)
```

实际行为：

- 内部通过 `CreateWidgetByType<RedDotItem>(parent)` 创建红点控件
- 随后自动调用 `item.Init(redDotNodeID)`

因此在窗口或 Widget 里接红点，推荐直接这样做：

```csharp
var redDot = CreateRedDot(RedDotPathDefine.Main.Mail.System, m_tfRedDotRoot);
```

异步版本：

```csharp
var redDot = await CreateRedDotAsync(RedDotPathDefine.Main.Mail.System, m_tfRedDotRoot);
```

所以更准确的说法是：

- `RedDotItem` 已经集成在 `UIBase` 提供的 UI 创建流程里
- 不需要每次手动 `CreateWidget<RedDotItem>() + Init(...)`

## `RedDotModule`

当前 `RedDotModule` 位于：

- `GameUnity/Assets/Scripts/HotFix/GameLogic/Module/RedDotModule/RedDotModule.cs`

当前职责：

- 维护红点树
- 注册节点
- 通过 ID 或路径获取节点
- 设置节点值 / 增量更新 / 清空值
- 添加和移除监听

当前初始化时会：

- 创建 `Root`
- 调用 `RedDotPathDefine.RegisterAll()`

这意味着：

- 生成的红点定义会在模块初始化时统一注册到系统中
- 业务侧通常不需要自己补一套基础注册逻辑

## `RedDotNode`

当前 `RedDotNode` 负责：

- 保存 `Id` / `Path` / `Type` / `Value`
- 维护父子关系
- 聚合子节点值
- 在值变化时通知监听者

对使用者来说，最重要的是：

- 叶子节点可直接 `SetValue`
- 非叶子节点会根据聚合策略自动计算
- `RedDotItem` 就是通过 `AddListener` 订阅节点变化

## `RedDotPathDefine`

当前生成文件位于：

- `GameUnity/Assets/Scripts/HotFix/GameLogic/Module/RedDotModule/Gen/RedDotPathDefine_Gen.g.cs`

它不是手写维护文件，而是编辑器里维护红点树配置后自动生成的代码产物。

它提供：

- 节点 `Id`
- 节点 `Path`
- 节点 `Segments`
- `RegisterAll()`

推荐：

```csharp
RedDotPathDefine.Main.Mail.System
RedDotPathDefine.Main.Mail.SystemPath
RedDotPathDefine.Main.Mail.SystemSegments
```

不推荐：

```csharp
"Main/Mail/System"
```

新增业务系统红点时，正确流程是：

1. 先去红点编辑器里新增或调整红点树
2. 再生成代码
3. 最后在业务代码和 UI 里使用新的 `RedDotPathDefine`

不要直接手改：

- `RedDotPathDefine_Gen.g.cs`
- 生成后的路径常量
- 生成后的节点 ID

## 典型接入流程

### 业务刷新红点

```csharp
// 有红点
GameModule.RedDotModule.SetValue(RedDotPathDefine.Main.Mail.System, 1);

// 数量变化
GameModule.RedDotModule.SetValue(RedDotPathDefine.Main.Bag.Item, 12);

// 清除红点
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

### 手动监听节点变化

大多数 UI 场景直接用 `RedDotItem` 就够了。只有在你不需要红点控件、而是要自己处理某个节点变化时，才直接监听：

```csharp
private void OnCreate()
{
    GameModule.RedDotModule.AddListener(RedDotPathDefine.Main.Mail.System, OnMailRedDotChanged);
}

private void OnDestroy()
{
    GameModule.RedDotModule.RemoveListener(RedDotPathDefine.Main.Mail.System, OnMailRedDotChanged);
}

private void OnMailRedDotChanged(RedDotNode node)
{
    RefreshMailState(node.IsShow);
}
```

## 常见错误

### 1. 手写路径或节点 ID

不推荐：

```csharp
GameModule.RedDotModule.SetValue("Main/Mail/System", 1);
GameModule.RedDotModule.SetValue(3, 1);
```

推荐：

```csharp
GameModule.RedDotModule.SetValue(RedDotPathDefine.Main.Mail.System, 1);
```

补充：

- 新增业务红点时不要直接改 `RedDotPathDefine_Gen.g.cs`
- 应先改红点编辑器配置，再生成代码

### 2. 自己手动创建 `RedDotItem`

不推荐：

```csharp
var item = CreateWidgetByType<RedDotItem>(m_tfRedDotRoot);
item.Init(RedDotPathDefine.Main.Mail.System);
```

推荐：

```csharp
CreateRedDot(RedDotPathDefine.Main.Mail.System, m_tfRedDotRoot);
```

### 3. 只刷新 UI，不更新红点节点

不推荐：

```csharp
m_goRedDot.SetActive(true);
```

这会让 UI 状态和红点系统状态脱节。

推荐：

```csharp
GameModule.RedDotModule.SetValue(RedDotPathDefine.Main.Mail.System, 1);
```

### 4. 在页面销毁后忘记移除手动监听

若你不是用 `RedDotItem`，而是自己监听 `RedDotNode`，记得在销毁时移除监听。

### 5. 对非叶子节点做错误理解

当前红点树支持父子聚合。若你修改的是叶子节点，父节点会自动更新；不要再手动重复给父节点补一次同类值，避免逻辑重复。
