---
name: dgame-dev
description: DGame-Fantasy 商业级 Unity 客户端 + C# 服务器框架全栈开发指导。在该框架中编写或修改任何代码时触发，包括：(1) 模块系统使用（**Module），(2) 事件中心系统（事件、接口事件、UIEvent、GameEvent），(3) 资源管理（YooAsset 加载、释放、热更），(4) 热更代码开发（HybridCLR 程序集划分、GameStart 入口），(5) Luban 配置表集成与访问，(6) 异步编程规范（UniTask），(7) 代码规范与框架设计审查，(8) 通过 Unity-MCP（AI Game Developer-MCP / Ivan Murzak）操作 Unity Editor（拼 UI Prefab、操作场景、创建脚本、材质、动画、测试自动化等），(9) UI 开发（UIWindow、UIWidget、UIModule、RitchTextItem、Super Scroll View、无限滚动列表、UILoopGridWidget、UILoopItemWidget、UISpineWidget、UIEventItem、UIParticleWidget、UILoopListWidget、SetUISafeFitHelper、窗口模态、SwitchPageMgr、SwitchTabItem、BaseChildPage、UIButton、UIImage、UIText）。关键词：DGame、Fantasy、Luban、unity-mcp、HybridCLR、UniTask、YooAsset、ResoueceModule、GameModule、GameEvent、EventInterface、ProcedureBase、ModuleSystem、GameStart、InputModule、UIWindow、UIWidget、UIModule、RitchTextItem、Super Scroll View、无限滚动列表、UILoopGridWidget、UILoopItemWidget、UISpineWidget、UIEventItem、UIParticleWidget、UILoopListWidget、SetUISafeFitHelper、窗口模态、SwitchPageMgr、SwitchTabItem、BaseChildPage、UIButton、UIImage、UIText。
---

# DGame 开发导航

将这个 skill 作为 DGame-Fantasy 仓库内的默认开发导航层使用。只要任务涉及该框架中的代码编写、代码修改、目录落点判断、架构审查、Unity Editor 自动化操作或 UI 业务实现，就先按本 skill 的约束判断落点、依赖和实现方式，不要退回泛化的 Unity/C# 项目习惯。

## 快速入口

优先按任务类型选择参考文件：

| 任务方向 | 优先查看 |
| --- | --- |
| 项目整体结构、模块边界、程序集归属、代码落点 | [architecture.md](references/architecture.md) |
| Unity 客户端热更、模块访问、UI、资源消费、程序集依赖 | [client-dev.md](references/client-dev.md) |
| 服务端启动、Entity/Hotfix 分层、Handler 落点、配置消费 | [server-dev.md](references/server-dev.md) |
| 配置表源头、Luban 生成链路、共享协议定义、导出工具 | [protocol-config.md](references/protocol-config.md) |

## 工作规则

1. 先判断任务属于客户端、服务端、共享协议、配置表源头、生成结果，还是 Unity Editor 操作，不要一上来直接改结果文件。
2. 先找源头目录，再确认是否需要同步修改生成产物、程序集引用或注册入口。
3. 涉及客户端业务层时，优先沿用既有 `**Module`、`GameModule`、`UIModule`、`InputModule` 等访问路径，不随意新增绕过模块系统的入口。
4. 涉及事件交互时，优先确认应该落在普通事件、接口事件、`UIEvent` 还是 `GameEvent`，不要混用同一语义的多套派发方式。
5. 涉及资源管理时，明确资源由谁加载、谁持有、谁释放，确认是否属于 YooAsset 热更资源链路的一部分。
6. 涉及热更代码时，明确代码所属程序集、是否进入 HybridCLR 热更域、是否需要接入 `GameStart` 或相关启动链路。
7. 涉及配置表时，优先确认 Luban 源文件、生成目标、访问入口和缓存方式，避免直接手改生成代码。
8. 涉及异步逻辑时，优先遵循现有 UniTask 写法、生命周期约束和取消时机，不混入风格不一致的异步模式。
9. 涉及 UI 开发时，优先复用框架既有基类和组件，包括 `UIWindow`、`UIWidget`、`UILoopGridWidget`、`UILoopItemWidget`、`UILoopListWidget`、`UISpineWidget`、`UIParticleWidget`、`UIEventItem`、`SwitchPageMgr`、`SwitchTabItem`、`BaseChildPage` 等。
10. 需要通过 Unity-MCP 操作 Unity Editor 时，优先使用 AI Game Developer-MCP / Ivan Murzak 提供的能力处理 Prefab、Scene、脚本、材质、动画与自动化测试，避免先假设手工编辑。
11. 涉及程序集时，明确说明代码应该落在哪个 asmdef 下，以及是否会引入新的依赖关系或破坏现有分层。
12. 涉及框架设计或代码审查时，优先指出是否偏离模块系统、事件中心、资源链路、热更边界、异步规范和 UI 框架约束。

## 输出要求

输出结论时优先包含以下信息：

1. 应该改哪里。
2. 为什么放在那里。
3. 是否影响模块系统、事件系统、资源链路、热更入口、Luban 生成链路、程序集依赖或 Unity Editor 自动化流程。
