---
name: dgame-dev
description: DGame Unity 项目开发指导。触发词：DGame, GameModule, UIWindow, UIWidget, UIModule, AddUIEvent, GameEvent, GameEventDriver, EEventGroup, LoadAssetAsync, LoadGameObjectAsync, SetSprite, ConfigSystem, HybridCLR, YooAsset, Luban, RedDotModule, CoplayDev unity-mcp, MCP, 热更, 资源加载, UI开发, 事件系统, 模块架构
---

# DGame 开发指导

DGame 使用 HybridCLR + YooAsset + UniTask + Luban 构建。
本 skill 提供 DGame 专属的 AI 精炼参考文档，优先描述本仓库实际 API、目录和落位规则。

## 核心红线

1. **分层落位**：框架运行时放 `GameUnity/Assets/DGame/Runtime`，编辑器工具放 `GameUnity/Assets/DGame/Editor`，热更玩法放 `GameUnity/Assets/Scripts/HotFix/GameLogic`。热更边界：`DGame.Runtime`/`DGame.AOT` 不热更，`Scripts/HotFix/*` 全部热更。
2. **模块访问**：业务代码优先通过 `GameLogic.GameModule.XXX` 访问模块，不在业务层散落 `ModuleSystem.GetModule<T>()`。
3. **异步优先**：IO、资源、场景等耗时操作优先使用 `UniTask`，不要新增 Coroutine 工作流。
4. **资源生命周期成对**：非实例化资源用 `LoadAssetAsync/LoadAsset` 后按持有关系 `UnloadAsset`；GameObject 实例用 `LoadGameObjectAsync/LoadGameObject`，销毁实例时资源模块自动回收引用。
5. **事件解耦**：UI 内部监听用 `UIBase.AddUIEvent` 自动清理；跨模块事件用 `[EventInterface(EEventGroup...)]`、`GameEvent.Get<T>()` 或 `GameEvent.AddEventListener`。
6. **配置表边界**：配置表结构、Excel、导表脚本优先使用 `luban-dev`；本 skill 只补充 DGame 业务代码如何消费配置。

## 防止误用的例外与优先级

- **模块访问分层**：`GameLogic` 业务代码优先通过 `GameModule.XXX`；`DGame.Runtime` 框架内部、编辑器代码、`GameModule` 的初始化桥接代码允许直接使用 `ModuleSystem.GetModule<T>()`，不要为了机械遵守规则改动这些底层代码。
- **事件监听**：UIWindow/UIWidget 在 `RegisterEvent` 中使用 `AddUIEvent`；非 UI 模块对带 `[EventInterface]` 的接口优先使用生成的 `GameEventHelper.AddEvent.Xxx.Xxx` / `GameEventHelper.RemoveEvent.Xxx.Xxx`。只有没有生成门面或框架底层场景才直接调用 `GameEvent.AddEventListener/RemoveEventListener`。 `IUIController` 不是 `UIBase`，也必须按非 UI 生命周期处理，成对注册和移除。
- **资源例外**：业务热更资源优先走 `GameModule.ResourceModule`；`UIWindow.FromResources`、AOT 的 Obfuz 密钥、框架音频混音器等已存在的 `Resources.Load` 路径属于框架例外，修改前先核对调用层，不要全局替换。
- **生成产物**：Luban 的 `GameProto/LubanConfig`、事件/模块/输入等 Source Generator 产物不要手改；先修改 schema、接口或生成器源码，再重新生成并检查 Unity 编译。

## 文档路由

根据任务类型，读取对应的 reference 文档：

| 任务类型 | 必读文档 | 进阶文档 | 优先级 |
|---------|---------|---------|--------|
| UI 开发 | [ui-lifecycle.md](references/ui-lifecycle.md) | [ui-patterns.md](references/ui-patterns.md) | P0 |
| 事件系统 | [event-system.md](references/event-system.md) | [event-antipatterns.md](references/event-antipatterns.md) | P0 |
| 资源加载 | [resource-api.md](references/resource-api.md) | [resource-patterns.md](references/resource-patterns.md) | P0 |
| 热更资源包 | [hotpatch-workflow.md](references/hotpatch-workflow.md) | [resource-api.md](references/resource-api.md) | P0 |
| 构建打包 | [build-pipeline.md](references/build-pipeline.md) | [hotfix-workflow.md](references/hotfix-workflow.md) | P1 |
| 模块使用 | [modules.md](references/modules.md) | — | P0 |
| 热更代码 | [hotfix-workflow.md](references/hotfix-workflow.md) | — | P1 |
| 代码规范 | [naming-rules.md](references/naming-rules.md) | — | P1 |
| Luban 配置 | [luban-config.md](references/luban-config.md) | `luban-dev` | P1 |
| 红点系统 | [reddot-system.md](references/reddot-system.md) | [ui-patterns.md](references/ui-patterns.md) | P1 |
| 项目结构 | [architecture.md](references/architecture.md) | — | P2 |
| 问题排查 | [troubleshooting.md](references/troubleshooting.md) | — | P2 |
| MCP 场景/GO/UI/脚本/Editor | [mcp-tools.md](references/mcp-tools.md) | — | P1 |
| MCP 材质/Shader/动画/VFX | [mcp-visual.md](references/mcp-visual.md) | — | P2 |

## 使用原则

- 先读与任务直接相关的 reference，不要一次性加载全部文档。
- 如果 reference 与源码冲突，使用 `rg` 搜索实际签名，优先信任源码，并在回复中标注冲突点。
- 修改配置表、`__tables__.xlsx`、`__beans__.xlsx`、`__enums__.xlsx`、导表脚本或 `GameConfig/` 数据时，先使用 `luban-dev`。
- 生成 UI 代码时匹配本仓库实际生命周期：`ScriptGenerator`、`BindMemberProperty`、`RegisterEvent`、`OnCreate`、`OnRefresh`、`OnDestroy`。
- 资源地址通常等于 `GameUnity/Assets/BundleAssets` 下资源文件名，不含路径和扩展名；改动前用实际资源和 YooAsset 设置验证。
