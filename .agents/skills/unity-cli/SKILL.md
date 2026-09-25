---
name: unity-cli
description: 安装和修复 DGame Unity CLI，使用内嵌 Pipeline 与项目级 Workflow 排查 CLI/MCP 连接，查询 Unity Editor/Development Player，执行编译、测试和受控验证。涉及 CLI 安装、连接配置或实际 Unity 状态时使用；纯源码或文档任务不连接 Editor。
---

# DGame Unity CLI

## 执行入口

从仓库根目录运行 `python .agents/scripts/workflow.py doctor`。脚本固定把 `GameUnity` 作为 Unity 工程；CLI 优先使用 `GameUnity/Tools/unity.exe`，文件缺少时从系统 `PATH` 查找 `unity`。此 CLI 与 Unity Editor 自身的 `Unity.exe` 是不同程序。

用户要求安装或修复 CLI 时，按 [操作规程](references/operations.md) 的安装与验收步骤处理。常规使用先运行 `python .agents/scripts/workflow.py doctor` 记录当前 CLI 的绝对路径、版本和 SHA-256，再运行 `python .agents/scripts/workflow.py unity list` 获取当前 DGame 工程注册命令。查询示例：`python .agents/scripts/workflow.py unity query list_open_scenes`。

用户要求 MCP 接入时，使用 CLI 自带的 `mcp` stdio 服务，固定 CLI 和工程的绝对路径；具体配置与验证见操作规程。

## 不变量

- Unity Editor 查询、编译、测试和写入串行；只绑定当前 `DGame/GameUnity` 的绝对路径。新操作前要求唯一实例且 ready；已审核的状态轮询允许观察 busy/compiling。
- 源码使用代码编辑工具修改；CLI 与 MCP 都只调用已审核的查询和验证能力，不绕过 Workflow 的写入预览与审批。
- 未知命令、任意 eval、菜单透传和跨工程实例默认阻塞。
- 写入操作先 preview，再由用户明确确认后执行；请求超时先检查实际状态，禁止自动重放。
- 测试先发现用例，分别核对模式、筛选器、执行数和终态；零用例、全跳过、失败或取消都不通过。
- 缺少工具、命令或连接时报告具体原因。安装、替换和配置按用户当前任务及已有授权处理；Workflow 本身不会自动安装、升级或启动 Editor。
- 不将 Pipeline 实例描述文件中的鉴权信息输出到日志或提交到仓库。

## 常用命令

```powershell
python .agents/scripts/workflow.py doctor
python .agents/scripts/workflow.py check
python .agents/scripts/workflow.py unity list
python .agents/scripts/workflow.py unity query list_open_scenes
python .agents/scripts/workflow.py unity runtime-status --runtime-path <player-working-directory>
python .agents/scripts/workflow.py unity preview <mutation> --params-file request.json
python .agents/scripts/workflow.py verify --profile code
python .agents/scripts/workflow.py verify --profile unity --test-filter GameLogic
```

CLI 启动参数以当前可执行文件的帮助为准；Editor 命令以实时注册描述和 `GameUnity/Packages/com.unity.pipeline/Documentation~` 为准。截图、资源写入、构建和 Player 操作需要单独验证和授权。

配置表导出交给 [luban-dev](../luban-dev/SKILL.md)，业务框架 API 交给 [dgame-dev](../dgame-dev/SKILL.md)。
