# Luban 官方 AI 工具

## 版本与目录

项目正式生成器、Agent CLI 和 MCP 统一固定为官方 `v5.1.0`，提交 `df34215d2e22035073d1af83d44e3ac0744dc3b3`。来源由 [版本锁](../scripts/ai-tools.lock.json) 维护，不自动跟随 `main` 或最新发布。

| 路径 | 用途 |
| --- | --- |
| `.agents/skills/luban-dev/scripts/` | 本技能的 CLI/MCP 启动器、安装脚本与版本锁 |
| `GameConfig/Tools/LubanTools/AI/Luban.Agent/` | 官方查询和无产物校验 CLI |
| `GameConfig/Tools/LubanTools/AI/Luban.Mcp/` | 官方 stdio MCP Server |
| `GameConfig/Tools/LubanTools/AI/Luban/` | 与 AI 工具同版本的主生成器 |
| `GameConfig/Tools/LubanTools/Luban/` | 现有正式导表脚本使用的 5.1.0 主生成器 |
| `GameConfig/Tools/LubanTools/AI/install.json` | 来源与安装记录 |

工具目录被 Git 忽略。导出生成器切换时，安装脚本会先保留旧目录到同级 `Backups/`，再安装匹配版本；不会执行生产导表。

AI 工具和正式生成器使用官方 `v5.1.0`。官方 Agent 对缺失 `luban.conf` 返回非成功退出码 `4`；调用方应通过 `ok`、`exitCode` 和 `errors` 判断失败。

## 本项目兼容范围

AI 工具 5.1.0 已通过当前 `client` 配置的查询和校验。客户端 LazyLoad / standard 隔离导出与 4.5.0 的 Binary、JSON 完全一致，C# 差异仅为新增 XML 注释。

正式导表已按用户要求更新到 5.1.0。音效和引导共四张客户端专用表已将表级 `group` 设为 `c`，client / server 均通过校验及原一键脚本的隔离导表，客户端产物保持不变。

`all` 目标已修正为 `groups=["c", "s", "e"]`，并通过 CLI、Agent 与 MCP 查询/校验。

## 部署

前置条件：Python 3.11+、Git、可构建 net8.0 的 .NET SDK 与 .NET 8 运行时。

```powershell
python .agents/skills/luban-dev/scripts/install_ai_tools.py --activate-generator
python .agents/skills/luban-dev/scripts/luban_ai.py capabilities
python .agents/skills/luban-dev/scripts/luban_ai.py validate
```

安装器只构建固定提交，使用临时目录顺序构建三个官方项目，完整成功后安装。已有匹配版本可重复执行；来源或安装摘要不匹配时报告差异，不能静默覆盖。也可用 `--source <干净的固定提交检出目录>` 避免重复下载。

`--activate-generator` 会将同一固定版本同步到正式导表工具目录，并保留旧工具备份；省略该参数只部署 AI 工具。安装或升级按用户授权范围执行，之后通过隔离导表验证，不把安装成功等同于所有业务目标兼容。维护 `ai-tools.lock.json` 时同样重新验证。

## Agent CLI

项目入口绑定当前检出的绝对配置路径，不依赖调用者工作目录：

```powershell
python .agents/skills/luban-dev/scripts/luban_ai.py list-tables
python .agents/skills/luban-dev/scripts/luban_ai.py describe --name TbModelConfig
python .agents/skills/luban-dev/scripts/luban_ai.py schema --name TbModelConfig
python .agents/skills/luban-dev/scripts/luban_ai.py validate --target client
```

`--target` 支持 `client/server/all`，默认 `client`。`describe` 面向表、Bean、Enum；`schema --name` 过滤结构。也可直接调用官方 `Luban.Agent.dll` 并显式传 `--conf` / `-t`。

stdout 是一份 `AgentResult` JSON；日志走 stderr。读取 `ok`、`exitCode`、`errors` 与 `result`，同时检查进程退出码。官方退出码为 0 成功、1 数据/校验、2 Schema/配置、3 用法、4 内部错误；5.1.0 的缺失配置场景按官方实现返回 4。

官方 Agent 的校验加载真实配置但不生成代码/数据；helper 校验不替代该结果，完整模板回归继续运行 Workflow `luban test`。

## MCP

```powershell
python .agents/skills/luban-dev/scripts/luban_ai.py configure-mcp
```

该操作维护仓库已有的 `.codex/config.toml`，保留其他 MCP 设置。Luban 条目使用 `python` 与仓库相对路径，可随项目共享；客户端工作目录应为仓库根。已有不同的 `mcp_servers.luban` 时报告冲突，不覆盖它。

项目配置启用 `list_tables`、`get_schema`、`describe`、`validate`，参数 `conf` 传本项目 `GameConfig/luban.conf` 的绝对路径，`target` 传 `client`。新会话加载配置；当前工具列表未出现 Luban 时，使用等价 CLI。

其他 MCP 客户端可使用相同启动方式：`python <本检出绝对路径>/.agents/skills/luban-dev/scripts/luban_ai.py mcp`。项目启动器设置官方所需的 `LUBAN_AGENT_DLL`、`LUBAN_DLL`；不修改用户级客户端配置。

官方服务还提供 `generate`、`search_docs`，本项目 Codex 配置未启用它们。`generate` 直接运行主 CLI，不包含 DGame 的生产审批和成对产物检查；`search_docs` 需要另行提供 `LUBAN_DOC`，当前使用技能参考和官方文档即可。

## 官方指导的落点

| 官方技能 | 本项目维护位置 |
| --- | --- |
| `luban-add-table` | 现有表注册流程，补充同名 Bean 检查 |
| `luban-excel-fill` | 表头、分组和复杂字段填表规则 |
| `luban-schema-design` | Schema 选型及类型约束 |
| `luban-validator` | 校验定位与保留校验强度 |
| `luban-generate-debug` | 独立排错参考 |
| `luban-runtime-load` | 格式匹配、目标分组和项目运行时检查 |

上游示例的目录、命名和资源加载方式需按 DGame 适配；业务字段不因示例采用 `snake_case` 而改名。继续维护一个 `luban-dev` 入口，不重复安装同类技能。

来源核对日期：2026-11-07。

- [官方 AI 目录与 Skills](https://github.com/focus-creative-games/luban/tree/df34215d2e22035073d1af83d44e3ac0744dc3b3/ai)
- [Agent CLI](https://github.com/focus-creative-games/luban/tree/df34215d2e22035073d1af83d44e3ac0744dc3b3/src/Luban.Agent)
- [MCP Server](https://github.com/focus-creative-games/luban/tree/df34215d2e22035073d1af83d44e3ac0744dc3b3/src/Luban.Mcp)
- [官方文档](https://www.datable.cn/docs/intro)
- 本技能采纳的官方指导遵循 [MIT 许可](../assets/luban-LICENSE.txt)，复制或再分发实质内容时保留版权和许可。
