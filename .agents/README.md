# DGame Agent Skills

`.agents/` 是仓库内唯一的 Agent Skills 与验证工具目录。两个工具共用根目录的 `AGENTS.md`，再按任务读取这里的技能：

- `skills/dgame-dev/SKILL.md`：DGame Unity 业务与架构
- `skills/luban-dev/SKILL.md`：Luban 配置、校验与导表
- `skills/unity-cli/SKILL.md`：原生 MCP / Unity CLI 操作、连接与验证
- `skills/fantasy-net/SKILL.md`：Fantasy 服务端与 Unity 客户端、ECS、协议、Roaming、事件、数据库与分布式运行时

技能目录遵循 Agent Skills 结构：入口为 `SKILL.md`，详细资料放在 `references/`，可执行辅助脚本放在 `scripts/`。`agents/openai.yaml` 只提供 Codex 的界面元数据，不影响 Claude Code 读取 `SKILL.md`。

需要仓库结构检查或环境报告时，从仓库根目录运行 Workflow：

```powershell
python .agents/scripts/workflow.py doctor
python .agents/scripts/workflow.py check
```

## 工作流程

读取[仓库规则](../AGENTS.md)和对应的[Unity 工程规则](../GameUnity/AGENTS.md)或[服务端工程规则](../GameServer/AGENTS.md)，检查已有修改，按任务读取源码与相关技能，再实施、验证和交付证据。API、程序集和实际工具结果优先于参考文档。

Unity 日常查询、对象/资源操作、编译和测试使用已加载的原生 MCP 工具或直接 CLI，按 [unity-cli 技能](skills/unity-cli/SKILL.md) 核对当前工程。Workflow 用于组合验证、CI 和统一报告，Luban 生产导表继续使用其审批流程。

小改按影响选择检查；跨模块、高风险或多阶段任务使用[任务模板](templates/task.md)记录目标、验收、决策、授权和证据。技能路由与授权边界以仓库规则为准。

## 环境与版本

- Unity 工程是 `GameUnity/`，版本以 [ProjectVersion.txt](../GameUnity/ProjectSettings/ProjectVersion.txt) 为准。
- CLI 指定版本以 [tool-versions.json](scripts/tool-versions.json) 的 `unityCli` 字段为准。直接 CLI 使用 `GameUnity/Tools/unity.exe`；项目 CLI 缺失时按该版本准备项目副本。MCP 可执行文件的版本核对用于接入或排障，普通已加载工具调用不重复查询客户端配置。`doctor` 可记录完整环境报告。
- Pipeline 版本取 [package.json](../GameUnity/Packages/com.unity.pipeline/package.json)，能力以本工程实时注册命令为准。
- Workflow 需要 Python 3.11+；按需准备 Git、.NET SDK、Unity 生成的 `GameUnity.sln` 和相关领域依赖，见 [Workflow 依赖](scripts/requirements.txt)及 [Luban 依赖](skills/luban-dev/scripts/requirements.txt)。脚本不会自动安装工具。

CLI 优先使用 `GameUnity/Tools/unity.exe`，与 Unity Editor 的同名程序不同。安装与验收见 [unity-cli 操作规程](skills/unity-cli/references/operations.md)的对应章节；动态 beta/latest 渠道不能保证指定版本。

`doctor` 是可选的独立环境预检，下面的验证 profile 不会自动调用它。已加载 MCP 的工程身份由 `editor_status.projectPath` 核对，服务名称不作为身份；日常调用不依赖项目 CLI 文件、Python、.NET 或生成解决方案。

## 验证选择

日常 Unity 验证直接调用原生命令：

| 验证 | MCP / CLI 命令与证据 |
|---|---|
| 工程状态和查询 | `editor_status` 核对工程路径与状态，使用对应场景/对象/资源查询工具 |
| Editor 编译 | `test_status` 确认无活动测试 → `recompile` → `recompile_status`，检查本次终态与编译错误 |
| 测试 | `list_tests` → 选择模式/筛选器 → `test_status` 确认无活动测试 → `run_tests` → `test_status`，检查用例身份、执行数和业务结果 |
| 场景、Prefab、序列化、视觉 | 使用对应原生工具取得目标状态或截图证据；截图本身不等于视觉验收 |

直接 CLI 的路径、参数和授权示例见 [操作规程](skills/unity-cli/references/operations.md)。需要组合验证或 CI 报告时，以下 Workflow 命令从仓库根目录执行：

直接编译/测试和等待已有测试默认每阶段设 300 秒总体期限，成功查询不重置计时；到期记录结果未知并保留现场，不自动取消或重放。

```powershell
python .agents/scripts/workflow.py verify --profile docs
python .agents/scripts/workflow.py verify --profile code
python .agents/scripts/workflow.py verify --profile unity --test-filter GameLogic --filter-type assembly
python .agents/scripts/workflow.py verify --profile full --test-filter GameLogic --filter-type assembly
```

| Profile | 实际阶段 |
|---|---|
| docs | 四个项目技能的结构、引用与元数据检查，Python 回归 |
| code | docs 阶段加完整解决方案构建，检查项目源码是否已纳入生成工程 |
| unity | 当前工程的能力发现、Editor 编译、指定筛选器的 EditMode 和 PlayMode 测试 |
| full | docs、代码构建、Unity 阶段，加隔离的 standard/lazyload 导表回归；每种模式导出两次并比较产物清单与哈希 |

Workflow 默认测试筛选器是 `DGame`，不代表全部游戏测试；unity/full profile 要求两个模式均有可执行用例。直接 MCP/CLI 验证按任务影响选择模式和筛选器，不强制运行无关模式。零用例、全跳过、失败、取消或结果身份与数量不符不能通过。

新增 C# 或 asmdef 后需通过 Unity 导入并更新生成工程，旧解决方案构建成功不能证明新文件已编译。当前 Workflow 没有 DGame 专用资源校验阶段；场景、Prefab、序列化与视觉任务需另行取得实际证据。

Editor 操作绑定当前工程的绝对路径，MCP、CLI 与 Workflow 操作串行；编译、测试和写入前确认唯一目标且 ready，查询与轮询可观察 busy。Workflow 内部使用工程锁，直接工具不经过该锁，不得同时启动另一条 Editor 操作。断开、目标不唯一或缺少能力时报告具体 blocked 原因，不使用其他工程代替验证。

## 写入与配置导表

原生 Unity 写入先读取目标，明确工程、命令、输入和影响路径；支持 `dry_run` 时直接预览，无该参数时展示具体变更范围。普通任务内操作使用已有授权，删除、覆盖、设置、依赖、平台和真实构建遵守仓库授权要求。选择 Workflow 写入入口时才使用其 preview、approve、apply；READ_ONLY/MUTATIONS 白名单只限制 Workflow 本身，不作为原生工具的能力边界。

Luban 生产导表继续使用 preview、approve、apply，审批记录只能来自真实用户确认。范围或输入变化后重新核对授权；所有入口超时后先查询实际状态，不自动重放。

Luban 的 validate/test 使用隔离目录，生产导表见 [luban-dev](skills/luban-dev/SKILL.md)。preview 先记录隔离产物清单，apply 导表后核对生产产物，再验证构建和 Editor 编译；需要测试时在 preview 中显式指定两个模式的程序集过滤器。lazyload 与 standard 不自动互相回退。

删除、覆盖、包依赖、ProjectSettings、平台切换、真实构建与发布的授权要求见仓库规则；完成验证不等于获得发布授权。

## 结果与证据

直接 MCP/CLI 交付记录实际入口、目标工程、关键输入、返回结果和未验证项；日常查询无需生成 Workflow 报告。多阶段任务使用任务记录保存证据。Workflow 报告位于 `.agents/runs/<run-id>/`，包含脱敏 `report.json`、`report.md` 和命令证据，目录已忽略提交。通过以下命令重排已有报告：

```powershell
python .agents/scripts/workflow.py report --run <run-directory>
```

| 状态 | 含义 | 整体退出码 |
|---|---|---|
| passed | 本次选定必需阶段均有成功证据 | 0 |
| failed | 命令、业务、测试或结构检查失败 | 1 |
| blocked | 环境、授权、连接或必需阶段缺失 | 2 |
| skipped | 未执行步骤并记录原因 | 必需步骤跳过且无失败时为 2 |

report 不执行缺失阶段，中断报告保持 blocked。CI 保留实际退出码与所需运行报告，不用空测试集使检查通过。交付说明实际检查、未运行项、剩余风险及已有修改是否保留；Pipeline 鉴权信息不能写入报告或分享。

## 客户端读取入口

技能提供任务知识与原生命令用法；MCP 工具由客户端配置的 Unity CLI 服务独立暴露。Pipeline 注册命令的名称、描述和参数 Schema 同时供 CLI 与 MCP 使用，Workflow 的目录不参与 MCP 工具发现。技能的 `allow_implicit_invocation` 只允许自动选择技能，不注册 MCP 服务、不设置工具优先级。已有通用服务可用时核对目标工程后复用，不要求另建项目服务。

Claude Code 原生会读取仓库级 `AGENTS.md`。Claude 的技能菜单自动发现仍由其自身的 `.claude/skills` 目录控制；在只保留 `.agents` 的布局下，应通过 `AGENTS.md` 的任务路由读取对应 `SKILL.md`。Codex 使用同一入口和 `.agents/skills` 内容。
