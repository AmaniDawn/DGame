# DGame 客户端模块指南（精简版）

当需求涉及 `GameModule`、模块职责、模块应该从哪里取、某个能力该依赖哪个模块时，先读本文件。

目标：让 Codex 以最短阅读成本掌握 DGame 当前客户端模块访问规则。原始细节以 `references/originals/client-modules.md` 为准。

## 核心规则

- 模块统一从 `GameModule` 获取使用。
- 不要自己再去 `ModuleSystem.GetModule<T>()`、`FindObjectOfType` 或单例 `Instance` 取平行入口。
- 不要在业务对象里长期缓存模块实例。

推荐：

```csharp
GameModule.ResourceModule
GameModule.LocalizationModule
GameModule.UIModule
GameModule.RedDotModule
```

## 当前主要模块

| 入口 | 主要职责 |
| --- | --- |
| `GameModule.RootModule` | Runtime 根模块入口 |
| `GameModule.FsmModule` | 有限状态机 |
| `GameModule.SensitiveWordModule` | 敏感词 |
| `GameModule.AnimModule` | 动画机 |
| `GameModule.ResourceModule` | 资源管理 |
| `GameModule.AudioModule` | 音频 |
| `GameModule.SceneModule` | 场景管理 |
| `GameModule.GameTimerModule` | 计时器 |
| `GameModule.InputModule` | DGame 输入模块 |
| `GameModule.Input` | GameLogic 输入模块 |
| `GameModule.LocalizationModule` | 多语言 |
| `GameModule.GameObjectPool` | 游戏对象池 |
| `GameModule.UIModule` | UI 模块 |
| `GameModule.RedDotModule` | 红点模块 |

## 使用提醒

- `UIModule` 和 `RedDotModule` 虽然本身是 HotFix 单例，但业务侧仍统一从 `GameModule` 访问。
- `GameModule.InputModule` 和 `GameModule.Input` 不是同一个模块层，新增输入逻辑前先看目标区域使用哪一个。
- `DataCenterModule<T>` 不属于 `GameModule` 统一模块入口。

## 常见坑

- 不要绕过 `GameModule` 直接访问底层模块入口。
- 不要把模块引用缓存成业务对象的长期字段。
- 不要混用两套输入模块入口。
