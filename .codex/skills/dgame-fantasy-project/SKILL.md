---
name: dgame-fantasy-project
description: DGame Fantasy Unity 项目开发技能。在当前仓库中编写、修改或审查与 DGame 框架、HotFix 业务层、GameLogic/UI、Fantasy 网络接入、HybridCLR 热更、YooAsset 资源管理、Luban 配置、Unity 编辑器工具或场景/Prefab 自动化相关的内容时使用。适用于 `GameUnity/Assets/DGame/Runtime`、`GameUnity/Assets/Scripts/HotFix/GameLogic`、`GameUnity/Assets/Scripts/HotFix/GameProto`、`GameUnity/Assets/Scripts/HotFix/Fantasy.Unity`、`GameUnity/Assets/Editor` 与 `GameUnity/Assets/Scenes` 下的任务。
---

# DGame Fantasy Project

适用于当前仓库的 Unity 客户端开发。该项目基于 DGame 运行时框架，业务逻辑主要位于 HotFix 层，并接入 Fantasy 网络库。

## Quick Start

先按任务类型选择参考文档：

| 需求 | 参考文件 |
| --- | --- |
| 仓库分层、程序集、启动资源、场景入口 | [architecture.md](references/architecture.md) |
| 常用模块入口与目录位置 | [module-map.md](references/module-map.md) |
| UIWindow、UIWidget、自动绑定、红点、列表/控件扩展 | [ui-development.md](references/ui-development.md) |
| HotFix、Fantasy 网络、资源加载/释放、HybridCLR、GameProto | [hotfix-and-resources.md](references/hotfix-and-resources.md) |
| Unity 编辑器菜单、构建、Luban、HybridCLR、Prefab/Scene 操作 | [editor-workflow.md](references/editor-workflow.md) |
| 编码约定与修改边界 | [conventions.md](references/conventions.md) |

## Core Rules

1. 先确认改动发生在哪一层：`DGame/Runtime` 是基础框架层，`Scripts/HotFix/GameLogic` 是业务层，`GameProto` 是配置/协议层，`Fantasy.Unity` 是网络运行时接入层。
2. 在 HotFix 业务代码中优先通过 `GameLogic.GameModule` 访问模块，不要无差别散落 `ModuleSystem.GetModule<T>()`。
3. 涉及 UI 时优先沿用 `UIModule`、`UIWindow`、`UIWidget`、`UIBindComponent` 现有模式，不要绕开现有窗口栈和资源加载器自建一套。
4. 涉及资源加载时要成对考虑释放，尤其是 `LoadAssetAsync`、`LoadGameObjectAsync` 和缓存/对象池交互。
5. 涉及 HotFix 或 Fantasy 网络初始化时，先确认改动属于业务逻辑、热更程序集加载，还是 Fantasy 场景/会话驱动，不要混淆层级。
6. 涉及编辑器自动化、场景、Prefab、组件操作时，优先使用当前环境可用的 Unity MCP 工具，而不是只停留在静态代码修改。

## Working Pattern

1. 先定位任务所属目录与程序集。
2. 再读取对应 reference，而不是一次性加载全部文档。
3. 做代码修改前确认是否会跨越 Runtime/HotFix/Fantasy 三层边界。
4. 完成后尽量做最小验证：编译、查找引用、必要时跑 Unity 测试或检查 Editor 日志。

## When To Be Careful

- 改 `GameUnity/Assets/DGame/Runtime` 时，要假设这是基础框架，优先保持 API 兼容。
- 改 `GameUnity/Assets/Scripts/HotFix/GameLogic/Module/UIModule` 时，要检查窗口层级、资源定位、生命周期和输入/关闭行为。
- 改 `GameUnity/Assets/Scripts/HotFix/GameLogic/DataCenter` 或 `Fantasy.Unity` 时，要检查初始化顺序和线程/异步模型。
- 改构建、打包、HybridCLR、Luban 相关编辑器脚本时，要确认输出目录和资源复制逻辑，不要只修改一半流程。
