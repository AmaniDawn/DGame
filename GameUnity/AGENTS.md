# GameUnity/AGENTS.md

本文件补充仓库根目录 [AGENTS.md](../AGENTS.md)，只适用于 `GameUnity/` Unity 工程及其子目录。仓库级规则、用户优先级和通用编码准则继续有效。

## 工程事实

- Unity 工程根目录是当前目录 `GameUnity/`。
- 主启动场景是 `Assets/Scenes/GameStart/GameStart.unity`。
- 项目解决方案是 `GameUnity.sln`。
- 项目级 Unity CLI 是工程目录的 `GameUnity/Tools/unity.exe`。
- 配置源文件位于仓库根目录 `../GameConfig/`，不是 `GameUnity/Configs/`。
- Unity 版本取 `ProjectSettings/ProjectVersion.txt`；CLI 路径和版本由 `doctor` 记录，Pipeline 能力以本项目注册命令及包内 `Documentation~` 为准。
- CLI 查询、验证和 MCP 接入见 [unity-cli 技能](../.agents/skills/unity-cli/SKILL.md)。Workflow 优先使用 `GameUnity/Tools/unity.exe`，文件缺少时从系统 `PATH` 查找 `unity`。

## DGame 任务分级与技能路由

涉及 DGame API 名称、程序集边界、资源地址、UI 节点前缀、事件接口、热更流程或 Luban 表结构时，不按 L1 处理。

| 场景 | 必须读取 |
|------|----------|
| 文件落位、程序集边界、启动流程 | `../.agents/skills/dgame-dev/references/architecture.md` |
| UI 生命周期、窗口、Widget、IUIController | `../.agents/skills/dgame-dev/references/ui-lifecycle.md`、`../.agents/skills/dgame-dev/references/ui-patterns.md` |
| 资源加载、释放、场景切换资源整理 | `../.agents/skills/dgame-dev/references/resource-api.md`、`../.agents/skills/dgame-dev/references/resource-patterns.md` |
| 热更资源包、YooAsset 下载链路 | `../.agents/skills/dgame-dev/references/hotpatch-workflow.md` |
| 整包、AB、Jenkins、ReleaseTools | `../.agents/skills/dgame-dev/references/build-pipeline.md` |
| 热更代码、HybridCLR、AOT 泛型 | `../.agents/skills/dgame-dev/references/hotfix-workflow.md` |
| 模块 API、DataCenterModule、MemoryPool、Audio、Fsm | `../.agents/skills/dgame-dev/references/modules.md` |
| 事件系统和事件反模式 | `../.agents/skills/dgame-dev/references/event-system.md`、`../.agents/skills/dgame-dev/references/event-antipatterns.md` |
| Luban 配置消费 | `../.agents/skills/dgame-dev/references/luban-config.md`；编辑配置时使用 `../.agents/skills/luban-dev/` |
| 红点系统 | `../.agents/skills/dgame-dev/references/reddot-system.md` |
| 命名、UI 节点前缀、代码规范 | `../.agents/skills/dgame-dev/references/naming-rules.md` |
| 排障 | `../.agents/skills/dgame-dev/references/troubleshooting.md` |
| Unity CLI / Workflow | `../.agents/skills/unity-cli/`、`../.agents/scripts/workflow.py` |

处理 DGame 代码时优先使用 `dgame-dev`；内容以当前源码、目录和 API 为准。配置表结构、Excel、导表脚本和 `GameConfig/` 数据优先使用 `luban-dev`，再使用 `dgame-dev` 处理运行时消费。

## 核心约束

1. **分层落位**：框架运行时放 `Assets/DGame/Runtime`；编辑器工具放 `Assets/DGame/Editor`；热更业务放 `Assets/Scripts/HotFix/GameLogic`；配置生成代码放 `Assets/Scripts/HotFix/GameProto`。
2. **优先复用 DGame 封装**：新增服务前先确认 `GameModule`、`DGame/Runtime/Module` 和 `GameLogic/Module` 是否已有封装。
3. **模块访问**：业务层通过 `GameLogic.GameModule.XXX` 访问模块，不在业务层散落 `ModuleSystem.GetModule<T>()`。
4. **异步优先**：IO、资源、场景等耗时操作优先使用 `UniTask`，不要新增 Coroutine 工作流。
5. **资源必须释放**：普通资源加载与 `UnloadAsset` 成对；实例化 GameObject 由实例销毁触发资源引用回收。
6. **事件解耦**：UI 内监听用 `AddUIEvent`；跨模块事件用 `GameEvent` 或 `[EventInterface(EEventGroup...)]`。
7. **生成产物不手改**：Luban 的 `GameProto/LubanConfig`、事件和模块 Source Generator 产物应修改源定义后重新生成。
8. **资源文件边界**：不直接编辑 Scene/Prefab YAML、GUID 或 `.meta` 文件来替代 Unity 资源数据库操作；新增源文件的 `.meta` 由 Unity 生成。
9. **工程绑定**：Unity CLI、Editor、Player 和资源验证必须绑定当前 `DGame/GameUnity` 的规范化绝对路径，不使用其他 Unity 工程结果替代。
10. **敏感信息**：Pipeline 实例描述文件和运行时响应中的鉴权信息不得写入日志、报告或提交内容。

## 验证入口

从仓库根目录执行：

```powershell
python .agents/scripts/workflow.py doctor
python .agents/scripts/workflow.py check
python .agents/scripts/workflow.py unity list
python .agents/scripts/workflow.py verify --profile docs
python .agents/scripts/workflow.py verify --profile code
python .agents/scripts/workflow.py verify --profile unity --test-filter GameLogic
python .agents/scripts/workflow.py verify --profile full
```

Unity Editor 的连接、编译和测试阶段使用工程锁；`docs/code` 离线检查和解决方案构建是否纳入锁范围，以 `.agents/scripts/workflow.py` 的实现为准，不在文档中扩大声明。
Editor 断开、忙、多实例、筛选器没有可运行用例或测试结果不完整时，必须保持 `blocked`/`failed`，不能使用旧结果或其他工程结果替代。

## 冲突与文档维护

满足以下任一条件时，应在本次回答中反馈问题：

- reference 文档与当前源码 API、路径或签名不一致。
- 生成代码失败的根因来自过期文档或错误模式。
- 用户明确指出 DGame skill 或 `AGENTS.md` 描述不准确。

当 reference 与源码 API、路径或签名冲突时：

1. 用 `rg` 搜索实际实现和调用点。
2. 优先信任当前源码。
3. 在回复中记录问题现象、文档位置、正确事实和修正建议；维护 skill 时直接修正 `../.agents/skills/dgame-dev/references/` 对应文档。

只改与任务直接相关的文件；生成或修改 UI、资源、事件和配置消费代码前，先确认当前 DGame API；能验证就验证，不能验证时说明限制。
