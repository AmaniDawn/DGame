# DGame 客户端模块指南

当需求涉及 `GameModule`、模块职责划分、应该从哪里取模块实例、某个系统该依赖哪个模块，或判断“这个能力是 Runtime 模块还是 HotFix 单例模块”时，先阅读本文件。

本文档记录当前仓库中 `GameModule` 暴露的模块入口及其职责边界。处理客户端模块时，核心规则是：模块统一从 `GameModule` 获取使用，不要在业务代码里自行保存平行入口、单独查找或长期持有另一份实例引用。

## 目录导航

- [DGame 客户端模块指南](#dgame-客户端模块指南)
  - [目录导航](#目录导航)
  - [核心规则](#核心规则)
  - [GameModule 总览](#gamemodule-总览)
  - [模块清单](#模块清单)
    - [`RootModule`](#rootmodule)
    - [`FsmModule`](#fsmmodule)
    - [`SensitiveWordModule`](#sensitivewordmodule)
    - [`AnimModule`](#animmodule)
    - [`ResourceModule`](#resourcemodule)
    - [`AudioModule`](#audiomodule)
    - [`SceneModule`](#scenemodule)
    - [`GameTimerModule`](#gametimermodule)
    - [`InputModule`（DGame）](#inputmoduledgame)
    - [`Input`（GameLogic）](#inputgamelogic)
    - [`LocalizationModule`](#localizationmodule)
    - [`GameObjectPool`](#gameobjectpool)
    - [`UIModule`](#uimodule)
    - [`RedDotModule`](#reddotmodule)
  - [不在 GameModule 里的类型](#不在-gamemodule-里的类型)
  - [使用原则](#使用原则)
  - [常见错误](#常见错误)

## 核心规则

模块统一从 `GameModule` 获取使用，不要单独持有。

这里的“不单独持有”指的是：

- 不要自己再去 `ModuleSystem.GetModule<T>()`
- 不要自己再去 `FindObjectOfType<RootModule>()`
- 不要自己维护另一套全局静态模块入口
- 不要把模块实例缓存到业务对象里长期保存，除非是当前作用域内的短时局部变量

推荐：

```csharp
GameModule.ResourceModule
GameModule.LocalizationModule
GameModule.UIModule
GameModule.RedDotModule
```

不推荐：

```csharp
private IResourceModule m_resourceModule;
private UIModule m_uiModule;
private RedDotModule m_redDotModule;
```

原因：

- `GameModule` 已经是客户端模块统一入口
- 统一入口更利于重置、替换、调试和阅读
- 业务层不应该再发明第二套模块访问方式

## GameModule 总览

当前 `GameModule` 位于 `GameUnity/Assets/Scripts/HotFix/GameLogic/GameModule.cs`。

它本质上是 HotFix 侧的模块访问门面，负责：

- 缓存模块引用
- 为业务层提供统一静态入口
- 在 `Destroy()` 中清空缓存

当前 `GameModule` 暴露的模块可分两类：

| 类型 | 获取方式 | 代表 |
| --- | --- | --- |
| Runtime / 主工程模块 | `ModuleSystem.GetModule<T>()` 的包装 | `IResourceModule`、`IAudioModule`、`ILocalizationModule` 等 |
| HotFix 单例模块 | 直接转发到 HotFix 单例 | `UIModule.Instance`、`RedDotModule.Instance` |

## 模块清单

### `RootModule`

入口：

```csharp
GameModule.RootModule
```

当前行为：

- 通过 `Object.FindObjectOfType<RootModule>()` 获取场景中的运行时根模块
- 代表 Runtime 层的 Unity 生命周期根节点

适用场景：

- 需要访问根驱动、编辑器语言或 Runtime 根行为时

使用建议：

- 只在确实需要 Runtime 根对象能力时使用
- 不要自己重复查找场景中的 `RootModule`

### `FsmModule`

入口：

```csharp
GameModule.FsmModule
```

职责：

- 有限状态机模块入口

适用场景：

- 状态机创建、状态切换和流程状态管理相关逻辑

### `SensitiveWordModule`

入口：

```csharp
GameModule.SensitiveWordModule
```

职责：

- 敏感词过滤相关能力

适用场景：

- 聊天、输入校验、文本审核前置处理

### `AnimModule`

入口：

```csharp
GameModule.AnimModule
```

职责：

- 动画机相关底层模块能力

适用场景：

- Runtime 动画播放、动画控制系统接入

### `ResourceModule`

入口：

```csharp
GameModule.ResourceModule
```

职责：

- 资源管理模块

在当前项目里，这通常意味着：

- 资源加载模式判断
- 运行时资源系统访问
- 与 YooAsset 资源链路协作

适用场景：

- 资源加载、资源模式判断、资源生命周期管理

### `AudioModule`

入口：

```csharp
GameModule.AudioModule
```

职责：

- 音频播放与音频系统访问

适用场景：

- BGM、音效、UI 音频控制

### `SceneModule`

入口：

```csharp
GameModule.SceneModule
```

职责：

- 场景管理模块

适用场景：

- 场景切换、场景加载和场景状态判断

### `GameTimerModule`

入口：

```csharp
GameModule.GameTimerModule
```

职责：

- 游戏计时器模块

当前项目中大量用于：

- 一次性延迟
- UI 延迟播放
- 非缩放时间计时

适用场景：

- 延迟执行、UI 计时、游戏内时序控制

### `InputModule`（DGame）

入口：

```csharp
GameModule.InputModule
```

类型：

- `DGame.IInputModule`

职责：

- 主工程侧输入模块入口

适用场景：

- 需要对接 Runtime 输入系统时

### `Input`（GameLogic）

入口：

```csharp
GameModule.Input
```

类型：

- `GameLogic.IInputModule`

当前实现位于：

- `GameUnity/Assets/Scripts/HotFix/GameLogic/Module/InputModule/InputModule.cs`

当前职责：

- 管理 `IInputComponent`
- 管理输入上下文层
- 缓存输入轴值
- 分发输入事件和输入轴变化
- 控制输入启用/禁用

适用场景：

- HotFix 业务输入
- 实体输入上下文
- 输入层级控制

说明：

- `GameModule.InputModule` 和 `GameModule.Input` 不是同一个接口层
- 业务侧新增输入相关逻辑时，优先先看目标代码附近到底使用哪一个入口，不要混用

### `LocalizationModule`

入口：

```csharp
GameModule.LocalizationModule
```

职责：

- 多语言模块

当前项目里的典型用法：

- 设置语言
- 获取当前语言
- 注入 `DGameLocalizationHelper`

适用场景：

- 文本语言切换
- 语言初始化
- 本地化刷新

### `GameObjectPool`

入口：

```csharp
GameModule.GameObjectPool
```

职责：

- 游戏对象池模块

适用场景：

- 可复用对象生成/回收
- 运行时对象池管理

### `UIModule`

入口：

```csharp
GameModule.UIModule
```

当前实现位于：

- `GameUnity/Assets/Scripts/HotFix/GameLogic/Module/UIModule/UIModule.cs`

职责：

- UI 根节点管理
- 窗口显示/隐藏/关闭
- UI 栈和层级排序
- Esc 关闭逻辑
- UIController 注册

适用场景：

- 打开窗口
- 关闭窗口
- 查询顶部窗口
- UI 根节点和 UI 相机访问

说明：

- `UIModule` 是 HotFix 单例模块，不走 `ModuleSystem.GetModule<T>()`
- 业务侧仍然统一通过 `GameModule.UIModule` 访问

### `RedDotModule`

入口：

```csharp
GameModule.RedDotModule
```

当前实现位于：

- `GameUnity/Assets/Scripts/HotFix/GameLogic/Module/RedDotModule/RedDotModule.cs`

职责：

- 红点树注册
- 红点节点查询
- 红点值更新
- 红点路径和 ID 管理

适用场景：

- UI 红点显示
- 红点路径注册
- 红点数值驱动更新

说明：

- `RedDotModule` 也是 HotFix 单例模块
- 业务侧仍应统一通过 `GameModule.RedDotModule` 访问

## 不在 GameModule 里的类型

并不是所有“名字里带 Module 的类型”都在 `GameModule` 里暴露。

当前可明确区分：

| 类型 | 是否通过 `GameModule` 获取 | 说明 |
| --- | --- | --- |
| `DataCenterModule<T>` | 否 | 这是数据中心基类，不是 `GameModule` 暴露的统一模块入口 |
| `SingletonSystem` 相关 | 否 | 这是 HotFix 单例体系基础设施 |
| 各种 UI Widget / Manager | 否 | UI 组件和局部管理器不是 `GameModule` 统一模块 |

因此不要看到一个类名里有 `Module` 就默认把它加进 `GameModule` 使用习惯里。

## 使用原则

处理 DGame 客户端模块时，优先遵循以下原则：

1. 模块访问统一走 `GameModule`。
2. 优先在使用点就近访问，不要把模块实例长期缓存到业务对象字段里。
3. 区分 Runtime 模块接口与 HotFix 单例模块，不要混用入口。
4. 若能力已经在 `GameModule` 中有入口，不要再手写第二套静态门面。

## 常见错误

### 1. 绕过 GameModule 直接取模块

不推荐：

```csharp
var resourceModule = ModuleSystem.GetModule<IResourceModule>();
var uiModule = UIModule.Instance;
```

推荐：

```csharp
var resourceModule = GameModule.ResourceModule;
var uiModule = GameModule.UIModule;
```

### 2. 在业务对象里长期缓存模块引用

不推荐：

```csharp
private UIModule m_uiModule;

public void Init()
{
    m_uiModule = GameModule.UIModule;
}
```

推荐：

```csharp
public void OpenMainWindow()
{
    GameModule.UIModule.ShowWindow<MainWindow>();
}
```

### 3. 混用两套输入模块入口

当前项目同时存在：

- `GameModule.InputModule`
- `GameModule.Input`

新增输入逻辑前，先确认目标代码附近使用的是哪一套，不要随手混写。
