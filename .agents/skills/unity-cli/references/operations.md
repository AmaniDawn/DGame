# DGame Unity CLI 操作规程

所有命令从 DGame 仓库根目录运行，工程固定为 `GameUnity`。工作流不自动启动 Editor、不安装依赖、不切换到其他工程。

## 安装与验收

用户要求安装或修复 CLI 时执行：

1. 读取 [版本配置](../../../scripts/tool-versions.json) 的 `unityCli`，检查项目 CLI 和 `Get-Command unity -All` / `where.exe unity` 的实际路径。已有 CLI 的 `--version` 精确匹配配置且满足所需能力时直接复用。
2. 从 Unity 官方发行渠道获取指定版本的 CLI，先下载到临时目录检查 `--version` 与顶层 `--help`，确认有 `status`、`command`、`list`、`pipeline`；任务涉及 MCP 时还须有 `mcp`。包内 README 的 beta 安装命令读取动态 `latest-beta.json`，不能用它保证配置指定的版本。
3. 核对指定版本的下载来源和对应校验信息；不能只给通用安装脚本传版本参数便假定其校验信息也属于该版本。候选包版本不符、缺少所需命令或无法核实来源时报告 blocked，不改装其他版本。仅凭下载完成、安装脚本退出成功或能输出版本号，不算安装验收通过。
4. DGame 项目安装必须把验收通过的 CLI 放到 `GameUnity/Tools/unity.exe`，缺少目录时创建目录；已有项目副本满足版本和能力要求时直接复用。复制后核对源文件与项目副本的 SHA-256 一致，不以仅安装到全局 PATH 作为项目安装完成。替换已有文件按授权边界确认；用户要求终端直接运行 `unity` 时，再按授权范围配置 PATH 并验收该入口。
5. 安装后从 `GameUnity/Tools/unity.exe` 检查版本与顶层帮助，并运行 `python .agents/scripts/workflow.py doctor`，确认报告中的 `cliPath` 指向该项目副本、实际版本匹配版本配置。其他使用入口也分别检查。Editor 已打开时，再运行 `unity list` 和场景查询；涉及 MCP 接入时继续完成工具调用验证。Editor 未打开时区分“CLI 安装已验收”和“Editor 连接尚未验证”。

## 只读查询

```powershell
python .agents/scripts/workflow.py doctor
python .agents/scripts/workflow.py unity list
python .agents/scripts/workflow.py unity query list_open_scenes
python .agents/scripts/workflow.py unity query get_player_settings
python .agents/scripts/workflow.py unity runtime-status --runtime-path <player-working-directory>
```

`doctor` 记录基础环境并校验 CLI 版本约束；项目副本缺失时，PATH 中的 CLI 也必须通过同一版本检查。`unity list` 检查指定工程唯一且 ready，并发现注册命令。参数通过 JSON 文件和 `--params-file` 传入。

## 验证

```powershell
python .agents/scripts/workflow.py verify --profile code
python .agents/scripts/workflow.py verify --profile unity --test-filter GameLogic --filter-type assembly
```

`verify --profile unity/full` 在锁内依次执行 Editor 能力发现、脚本重编译、EditMode 测试和 PlayMode 测试；连接或编译失败时会跳过依赖阶段并保持 blocked/failed。当前 Pipeline 未提供 DGame 专用资源校验命令，不能把资源检查结果纳入验证报告。

Editor 未连接、不是 ready、筛选器没有可执行用例、CLI 返回错误或结果数量不一致时，工作流保持 blocked/failed，不把传输成功当成业务验证成功。

`runtime-status` 用 `--runtime-path` 指定 Development Player 的工作目录（macOS 使用 `.app` 包路径）。工作流先读取该目录下的 `.unity-pipeline-runtime-port` 实例描述文件，校验其中的 `workingDirectory`，再调用 `runtime_status`；业务响应本身不要求包含路径字段。

## 变更边界

涉及资源、场景、Prefab、PlayerSettings、构建和包的操作必须先取得明确目标和预览结果。预览后审批绑定当前工程路径、命令、参数和工作树；文件、输入或目标变化后重新预览。超时或断连先查询实际状态，再决定是否重新发起。

## 预览、审批与执行

```powershell
python .agents/scripts/workflow.py unity preview set_player_settings --params-file request.json
python .agents/scripts/workflow.py approve --plan <preview.json> --by <approver> --reason <confirmation-reference> --high-risk
python .agents/scripts/workflow.py unity apply --plan <preview.json> --approval <approval.json>
```

`preview` 只生成计划，不写入工程；`approve` 记录实际确认；`apply` 会重新检查当前预览、审批、工程路径和工作树后才执行。只对当前 `MUTATIONS` 白名单中的命令开放写入。

不要用任意 C# eval、菜单透传或外部工程结果替代已审核命令。运行时 Player 验证必须显式指定路径，并由 Workflow 核对实例描述文件中的 `workingDirectory`。

## MCP 接入

用户要求接入 Codex 时，使用 CLI 的 `mcp` stdio 服务连接本工程 Pipeline。先用 `codex mcp get unity-dgame --json` 检查已有条目，相同配置直接复用：

```powershell
$taskCliPath = (Resolve-Path -LiteralPath 'GameUnity/Tools/unity.exe').Path
$taskProjectPath = (Resolve-Path -LiteralPath 'GameUnity').Path
codex mcp add unity-dgame -- $taskCliPath mcp --project-path $taskProjectPath --no-log-proxy
codex mcp get unity-dgame --json
```

配置写入用户级 Codex 设置，服务固定绑定 DGame。使用 PATH 中的 CLI 时，也写入其解析后的绝对路径。

通过工具发现和 `list_open_scenes` 实际调用验证连接。当前会话尚未加载工具时重新加载 MCP 或重启 Codex。MCP 遵守相同的查询和写入边界；连接鉴权由 CLI 管理，不输出实例描述文件内容。
