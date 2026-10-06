# DGame Agent Skills

`.agents/` 是仓库内唯一的 Agent Skills 与验证工具目录。两个工具共用根目录的 `AGENTS.md`，再按任务读取这里的技能：

- `skills/dgame-dev/SKILL.md`：DGame Unity 业务与架构
- `skills/luban-dev/SKILL.md`：Luban 配置、校验与导表
- `skills/unity-cli/SKILL.md`：Unity CLI、Workflow 与验证

技能目录遵循 Agent Skills 结构：入口为 `SKILL.md`，详细资料放在 `references/`，可执行辅助脚本放在 `scripts/`。`agents/openai.yaml` 只提供 Codex 的界面元数据，不影响 Claude Code 读取 `SKILL.md`。

从仓库根目录运行 Workflow：

```powershell
python .agents/scripts/workflow.py doctor
python .agents/scripts/workflow.py check
```

## 工作流程

读取[仓库规则](../AGENTS.md)和[Unity 工程规则](../GameUnity/AGENTS.md)，检查已有修改，按任务读取源码与相关技能，再实施、验证和交付证据。API、程序集和实际工具结果优先于参考文档。

小改按影响选择检查；跨模块、高风险或多阶段任务使用[任务模板](templates/task.md)记录目标、验收、决策、授权和证据。技能路由与授权边界以仓库规则为准。

## 环境与版本

- Unity 工程是 `GameUnity/`，版本以 [ProjectVersion.txt](../GameUnity/ProjectSettings/ProjectVersion.txt) 为准。
- CLI 指定版本以 [tool-versions.json](scripts/tool-versions.json) 的 `unityCli` 字段为准。`doctor` 精确检查实际版本；项目 CLI 缺失时，PATH 回退也必须匹配。
- Pipeline 版本取 [package.json](../GameUnity/Packages/com.unity.pipeline/package.json)，能力以本工程实时注册命令为准。
- Workflow 需要 Python 3.11+；按需准备 Git、.NET SDK、Unity 生成的 `GameUnity.sln` 和相关领域依赖，见 [Workflow 依赖](scripts/requirements.txt)及 [Luban 依赖](skills/luban-dev/scripts/requirements.txt)。脚本不会自动安装工具。

CLI 优先使用 `GameUnity/Tools/unity.exe`，与 Unity Editor 的同名程序不同。安装与验收见 [unity-cli 操作规程](skills/unity-cli/references/operations.md)；动态 beta/latest 渠道不能保证指定版本。

`doctor` 是独立的环境预检，下面的验证 profile 不会自动调用它。使用 Unity 前先执行 doctor；直接启动 CLI 或用户级 MCP 不经过 Workflow 的版本检查。

## 验证选择

以下命令从仓库根目录执行：

```powershell
python .agents/scripts/workflow.py verify --profile docs
python .agents/scripts/workflow.py verify --profile code
python .agents/scripts/workflow.py unity list
python .agents/scripts/workflow.py verify --profile unity --test-filter GameLogic --filter-type assembly
python .agents/scripts/workflow.py verify --profile full --test-filter GameLogic --filter-type assembly
```

| Profile | 实际阶段 |
|---|---|
| docs | 三个项目技能的结构、引用与元数据检查，Python 回归 |
| code | docs 阶段加完整解决方案构建，检查项目源码是否已纳入生成工程 |
| unity | 当前工程的能力发现、Editor 编译、指定筛选器的 EditMode 和 PlayMode 测试 |
| full | docs、代码构建、Unity 阶段，加隔离的 standard/lazyload 导表回归；每种模式导出两次并比较产物清单与哈希 |

默认测试筛选器是 `DGame`，不代表全部游戏测试。业务变更应显式选择相关程序集、分类或测试名；两个测试模式均需有可执行用例。零用例、全跳过、失败、取消或结果身份与数量不符不能通过。

新增 C# 或 asmdef 后需通过 Unity 导入并更新生成工程，旧解决方案构建成功不能证明新文件已编译。当前 Workflow 没有 DGame 专用资源校验阶段；场景、Prefab、序列化与视觉任务需另行取得实际证据。

Editor 操作绑定当前工程的绝对路径，连接、编译和测试使用工程锁串行；新操作要求唯一实例且 ready，已审核的状态轮询可以观察 busy。断开、多实例或缺少能力时保持 blocked，不使用其他工程代替验证。

## 写入与配置导表

Unity 写入和 Luban 生产导表按各技能的 preview、approve、apply 流程执行。预览需展示具体工程、命令、输入和影响路径；审批只能记录真实用户确认，不能由代理自行批准。范围或输入变化后重新预览，超时先核对实际状态，不自动重放。

Luban 的 validate/test 使用隔离目录，生产导表见 [luban-dev](skills/luban-dev/SKILL.md)。preview 先记录隔离产物清单，apply 导表后核对生产产物，再验证构建和 Editor 编译；需要测试时在 preview 中显式指定两个模式的程序集过滤器。lazyload 与 standard 不自动互相回退。

删除、覆盖、包依赖、ProjectSettings、平台切换、真实构建与发布的授权要求见仓库规则；完成验证不等于获得发布授权。

## 结果与证据

报告位于 `.agents/runs/<run-id>/`，包含脱敏 `report.json`、`report.md` 和命令证据，目录已忽略提交。通过以下命令重排已有报告：

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

Claude Code 原生会读取仓库级 `AGENTS.md`。Claude 的技能菜单自动发现仍由其自身的 `.claude/skills` 目录控制；在只保留 `.agents` 的布局下，应通过 `AGENTS.md` 的任务路由读取对应 `SKILL.md`。Codex 使用同一入口和 `.agents/skills` 内容。
