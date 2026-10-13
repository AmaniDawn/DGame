# DGame Unity CLI 操作规程

从 DGame 仓库根目录执行以下 CLI 示例。已提供对应 MCP 工具时直接调用工具。按任务读取查询、编译与测试、写入、Player 或安装与 MCP 接入章节。

## MCP 查询

使用客户端已加载的 Unity CLI 工具，调用 `editor_status` 确认 `projectPath` 是当前 DGame/GameUnity，再直接调用 `list_open_scenes` 等工具。现有通用 MCP 服务可复用，服务名与是否写有 `--project-path` 不作为工程身份；没有路径信息、目标不唯一或返回其他工程时使用显式绑定本工程的 CLI，或报告 blocked。

已加载工具的普通调用不需要先查客户端配置、检查项目 CLI 文件或运行 Workflow。任务需要 CLI 但项目副本缺失时，先运行 `python .agents/scripts/workflow.py doctor`；仅安装、接入、版本或连接排障时核对服务使用的可执行文件和配置。域重载、重连或目标变化后重新核对工程路径。

## 直接 CLI

项目副本存在时：

```powershell
$taskCliPath = (Resolve-Path -LiteralPath 'GameUnity/Tools/unity.exe').Path
$taskProjectPath = (Resolve-Path -LiteralPath 'GameUnity').Path
$taskExpectedVersion = (Get-Content -LiteralPath '.agents/scripts/tool-versions.json' -Raw | ConvertFrom-Json).unityCli
& $taskCliPath --version
& $taskCliPath status --project-path $taskProjectPath --format json --no-log-proxy
& $taskCliPath command --project-path $taskProjectPath --format json --no-log-proxy
& $taskCliPath command --project-path $taskProjectPath list_open_scenes --format json --no-log-proxy
```

实际版本必须等于 `$taskExpectedVersion`；项目副本缺少时按安装流程准备 `GameUnity/Tools/unity.exe`。`status` 核对本工程唯一实例，无命令名的 `command` 发现注册能力；后续 Editor 命令继续显式传入工程绝对路径。

复杂参数按当前工具描述传入；MCP 使用结构化输入，CLI 使用终端能可靠传递的 JSON 参数。`--params-file` 属于 Workflow，不是原生 CLI 的统一参数。

## 编译与测试

MCP 使用对应工具；CLI 复用上面的绝对路径，按阶段执行以下命令，每次检查响应后才继续：

```powershell
& $taskCliPath command --project-path $taskProjectPath test_status --format json --no-log-proxy
& $taskCliPath command --project-path $taskProjectPath recompile --format json --no-log-proxy
& $taskCliPath command --project-path $taskProjectPath recompile_status --format json --no-log-proxy
& $taskCliPath command --project-path $taskProjectPath list_tests --mode editor --format json --no-log-proxy
& $taskCliPath command --project-path $taskProjectPath test_status --format json --no-log-proxy
& $taskCliPath command --project-path $taskProjectPath run_tests --mode editor --filter GameLogic --filter_type assembly --async_tests true --format json --no-log-proxy
& $taskCliPath command --project-path $taskProjectPath test_status --format json --no-log-proxy
```

`recompile` 和 `run_tests` 前分别检查 `test_status`。返回 `running` 时只轮询已有测试，不发送新编译或测试，不调用 `cancel_tests`；已有测试结束后重新核对工程身份和 Editor 状态，再继续自己的验证。无法确定测试状态时报告 blocked。`editor_status=ready` 不表示测试空闲：当前 Pipeline 在 EditMode 测试期间可能仍返回 ready，新 `run_tests` 会使旧收集器失效。

每个阶段完成后才开始下一阶段。调用方记录总体截止时间：编译、单轮测试或等待已有测试默认各 300 秒，从触发阶段开始（等待已有测试从首次观察到 running 开始）计时；任务明确需要更长时间时提前确定期限。每次查询成功或收到相同状态不重置期限，轮询间隔 1–5 秒。编译需 `completed` / `up_to_date` 且无编译错误；明确失败、取消或错误时立即记录对应结果并停止依赖阶段。域重载期间仅重试查询，最多连续 5 次连接失败且仍受总体期限约束，重连后核对工程身份，不重发触发命令。

当前 Pipeline 异步测试分支不使用传入的 `timeoutSeconds`，CLI 单次请求超时也不限制后台测试总时长，不能依赖它们结束轮询。总体期限到期后报告 blocked/结果未知，记录最后状态与耗时，保留现场；不自动取消测试、删除状态文件或换入口重放。无需为此新增执行脚本。

`GameLogic` 是筛选示例，必须先从 `list_tests` 找到真实用例。按任务选择 editor/playmode 及程序集、分类或名称，需要两个模式时分别串行执行。异步启动须返回本次执行的成功响应与 `result=running`；未确认启动时不能使用保留的完成结果。核对终态、用例身份和 total/passed/failed/skipped，不能接受零用例、全跳过、失败、取消或旧结果。命令不保证通用操作 ID，结合模式、筛选器、起始响应与结果判断本次执行。

## 写入与结果

先读取目标，明确工程、参数及影响范围；支持 `dry_run` 时直接预览，例如：

```powershell
& $taskCliPath command --project-path $taskProjectPath delete_asset --asset 'Assets/Example.asset' --dry_run true --format json --no-log-proxy
```

实际删除须获得对应授权后才传 `confirm=true`。普通对象/组件编辑按任务和已有授权执行；`set_transform` 无 `dry_run`，先核对对象身份和当前属性，再传目标值，`position` 表示 localPosition，执行后回读确认。

删除、覆盖、依赖、设置、平台和真实构建遵守 [仓库授权边界](../../../../AGENTS.md)。全局保存可能影响用户其他脏资源，应核对真实保存范围。任务记录用于保存目标、授权和证据，不是 Workflow 执行计划。

超时或断连视作结果未知：通过对应状态命令或目标回读确认，禁止换 MCP/CLI/Workflow 自动重放写入。异步构建、包、平台和烘焙按实际契约轮询并核对业务结果。截图保存到临时证据路径，核对视图与文件；不得为了截图擅自修改场景或相机。

## Development Player

Player 使用明确工作目录，macOS 使用 `.app` 路径：

```powershell
& $taskCliPath command --runtime-path '<player-working-directory>' runtime_status --format json --no-log-proxy
```

核对该目录实例描述的 `workingDirectory`，必要时以 `buildGuid` 核对构建；只保留身份字段，不输出鉴权信息或完整运行时响应。业务响应不保证包含路径。MCP 须单独绑定目标 Player，不能把 Editor 服务或其他构建结果代替当前 DGame 验证。

## 可选 Workflow

离线检查、组合验证与 CI 报告参阅 [项目验证矩阵](../../../README.md)。只有选择 Workflow 写入入口时才使用其 preview、approve、apply 协议；白名单只限定该脚本支持的契约。Luban 生产导表仍走 [luban-dev](../../luban-dev/SKILL.md) 的审批流程。

## 安装与验收

用户要求安装或修复 CLI 时执行；任务需要 CLI 且项目副本缺失时，先由 Workflow 执行这套流程：

1. 读取 [版本配置](../../../scripts/tool-versions.json) 的 `unityCli`，检查 `GameUnity/Tools/unity.exe`。已有项目 CLI 的 `--version` 精确匹配配置且满足所需能力时直接复用。
2. 从 Unity 官方发行渠道获取指定版本的 CLI，先下载到临时目录检查 `--version` 与顶层 `--help`，确认有 `status`、`command`、`list`、`pipeline`；任务涉及 MCP 时还须有 `mcp`。包内 README 的 beta 安装命令读取动态 `latest-beta.json`，不能用它保证配置指定的版本。
3. 核对指定版本的下载来源和对应校验信息；不能只给通用安装脚本传版本参数便假定其校验信息也属于该版本。候选包版本不符、缺少所需命令或无法核实来源时报告 blocked，不改装其他版本。仅凭下载完成、安装脚本退出成功或能输出版本号，不算安装验收通过。
4. DGame 项目安装必须把验收通过的 CLI 放到 `GameUnity/Tools/unity.exe`，缺少目录时创建目录；已有项目副本满足版本和能力要求时直接复用。复制后核对源文件与项目副本的 SHA-256 一致，不以仅安装到全局 PATH 作为项目安装完成。替换已有文件按授权边界确认；用户要求终端直接运行 `unity` 时，再按授权范围配置 PATH 并验收该入口。
5. 安装后从 `GameUnity/Tools/unity.exe` 检查版本与顶层帮助，并核对项目副本路径与 SHA-256；`doctor` 可用于记录完整环境报告，不是日常调用前置。其他使用入口也分别检查。Editor 已打开时，使用直接 CLI 的 `status`、无命令名的 `command` 和场景查询；涉及 MCP 接入时继续完成工具调用验证。Editor 未打开时区分“CLI 安装已验收”和“Editor 连接尚未验证”。

## MCP 接入

用户要求接入 Codex 时，使用 CLI 的 `mcp` stdio 服务连接 Pipeline。先通过 `codex mcp list --json` 发现实际服务名，选择执行 `unity mcp` 的条目，再用 `codex mcp get <实际服务名> --json` 核对 CLI 路径、版本及参数；只保留非敏感字段。已有通用服务或项目服务可连接当前 DGame 时复用，不因名称不是 `unity-dgame` 或未指定工程参数而重复创建。

需要新增项目服务且用户已授权配置写入时，使用以下示例；服务名按现有配置选择，CLI 与工程使用绝对路径：

```powershell
$taskCliPath = (Resolve-Path -LiteralPath 'GameUnity/Tools/unity.exe').Path
$taskProjectPath = (Resolve-Path -LiteralPath 'GameUnity').Path
codex mcp add unity-dgame -- $taskCliPath mcp --project-path $taskProjectPath --no-log-proxy
codex mcp get unity-dgame --json
```

该例写入用户级 Codex 设置并固定绑定 DGame，不是普通 Unity 操作的前置步骤。使用 PATH 中的 CLI 时，也写入其解析后的绝对路径。

通过工具发现、`editor_status` 工程路径核对和 `list_open_scenes` 实际调用验证连接。若 MCP 因项目 CLI 缺失而未启动，先运行 `python .agents/scripts/workflow.py doctor`，再重新加载 MCP 或重启客户端。当前会话尚未加载工具时，可直接使用 CLI 完成当前任务。MCP 调用与授权按本文前述操作规程执行；连接鉴权由 CLI 管理，不输出实例描述文件内容。
