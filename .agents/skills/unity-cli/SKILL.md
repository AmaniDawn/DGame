---
name: unity-cli
description: 使用本项目 Unity CLI 查询和验证 Editor/Development Player，操作场景、Prefab、组件和资源数据库，执行编译、测试和构建预检。涉及实际 Unity 状态、CLI 安装或连接排查时使用；纯源码或文档任务不连接 Editor。
---

# DGame Unity CLI

## 执行入口

Unity CLI 原生命令通过客户端已加载的 MCP 工具或 `unity command <name>` 调用。MCP 工具已暴露时直接使用其描述与参数，先以 `editor_status.projectPath` 核对当前工程；不要求服务名为 `unity-dgame`，也不要求每次读取客户端配置或运行本地 CLI。任务需要 CLI 且项目副本缺失时，先运行 `python .agents/scripts/workflow.py doctor` 完成准备和验收，再继续调用。未暴露对应工具时，直接 CLI 固定 `--project-path <GameUnity 绝对路径>`；Player 使用明确的 `--runtime-path`。

直接 CLI 使用 `GameUnity/Tools/unity.exe`，缺失时按 [版本配置](../../scripts/tool-versions.json) 准备项目副本。无命令名的 `unity command --project-path <绝对路径>` 可发现当前注册命令。CLI 不是 Unity Editor 的同名程序。

MCP 工具由客户端配置的 `unity mcp` 服务提供，Pipeline 为原生命令提供描述和参数 Schema；技能不注册工具，Workflow 也不负责 MCP 发现。`agents/openai.yaml` 的自动调用策略用于技能选择，不设置工具优先级。普通原生命令不需要 Workflow 白名单、包装脚本或审批文件。

查询、验证、安装与 MCP 接入统一见 [操作规程](references/operations.md)，按任务读取对应章节。组合验证、CI 和报告按需使用 [项目 Workflow](../../README.md)，Luban 生产导表使用 [luban-dev](../luban-dev/SKILL.md)。

| 操作 | 原生命令 / 对应 MCP 工具 |
|---|---|
| 工程与场景查询 | `editor_status`、`list_open_scenes` |
| 脚本编译 | `test_status` 确认无活动测试 → `recompile` → `recompile_status` |
| 测试 | `list_tests` → `test_status` 确认无活动测试 → `run_tests` → `test_status` |

## 不变量

- 只绑定当前 DGame 工程；导入、编译、测试与写入串行。新操作前核对唯一目标及状态，查询和轮询允许观察忙操作；直接 MCP/CLI 不经过 Workflow 工程锁。
- 源码用代码编辑工具修改，对象、序列化与资产用已注册命令；不通过文本写入、任意 eval、菜单或 Scene/Prefab YAML 绕过编辑和授权规则。
- 普通操作按用户任务及已有授权执行；先明确目标和影响范围，支持 `dry_run` 时直接预览，不支持时读取目标并说明变更。删除、覆盖、依赖、设置、平台和真实构建按仓库授权边界处理，`confirm` 不代替用户授权。
- 按命令真实实现核对副作用、`dry_run`/`confirm` 与 Undo 能力，执行后回读目标。超时先检查状态，禁止换入口自动重放写入。
- 编译或测试启动前查询 `test_status`；已有测试运行时等待，不依赖 `editor_status=ready` 判断测试空闲，不取消或覆盖已有测试。
- 异步命令检查触发响应，在调用方设定的总体期限内轮询终态；编译/测试默认每阶段 300 秒，超期报告 blocked/结果未知并保留现场，不自动取消或重放。测试先发现用例，按任务选择模式和筛选器，核对执行数与结果。零用例、全跳过、失败、取消或旧结果不能通过。
- 连接、命令或必需依赖缺失时报告具体 blocked 原因；任务需要项目 CLI 且副本缺失时先按安装验收流程准备，准备失败后再 blocked。不自动升级全局 CLI、启动 Editor、修改用户配置或借用其他工程。Pipeline 鉴权信息不得输出。

## 权威资料

按任务读取 `GameUnity/Packages/com.unity.pipeline/Documentation~/`：

| 场景 | 包内文档 |
|---|---|
| 连接、鉴权、Player | `connectivity.md`、`runtime-setup.md` |
| 路径、预览、Undo | `safety-and-mutations.md`、`authoring-commands.md` |
| 编译、测试、构建 | `commands/build-and-compilation.md` |
| 资源、对象、Prefab | `commands/assets-and-files.md`、`commands/gameobjects-and-components.md`、`commands/prefabs.md` |
| 截图、场景、设置 | `commands/capture.md`、`commands/scenes.md`、`commands/project-settings.md` |
| 新增项目命令 | `creating-commands.md` |

实时工具描述、CLI 帮助和源码优先于示例。截图需核对真实视图及输出路径，不等于视觉验收；业务框架 API 使用 [dgame-dev](../dgame-dev/SKILL.md)。
