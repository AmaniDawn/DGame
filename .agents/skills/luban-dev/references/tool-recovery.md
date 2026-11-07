# Luban 工具安装与恢复

工具缺失或版本接入时，以[版本锁](../scripts/ai-tools.lock.json)和[官方工具接入说明](official-ai.md)为准。

## 检查与安装

从仓库根目录运行：

```powershell
python .agents/skills/luban-dev/scripts/install_ai_tools.py --activate-generator
python .agents/skills/luban-dev/scripts/luban_ai.py capabilities
python .agents/skills/luban-dev/scripts/luban_ai.py validate
```

需要 Python 3.11+、Git、可构建 net8.0 的 .NET SDK 和 .NET 8 运行时。安装器构建固定提交的主生成器、Agent CLI、MCP；不会选择最新分支，也不会运行生产导表。已有安装与版本锁不匹配时先查看差异，不覆盖或自动升级。

## 导表生成器

既有脚本读取 `GameConfig/Tools/LubanTools/Luban/Luban.dll`，该主生成器与 Agent CLI / MCP 均固定为 5.1.0。需要安装或恢复正式工具时，在用户授权范围内执行：

```powershell
python .agents/skills/luban-dev/scripts/install_ai_tools.py --activate-generator
```

安装器会先完整准备新目录，把旧生成器保留在 `GameConfig/Tools/LubanTools/Backups/`，再切换工具。安装失败不等于配置数据错误；工具已存在而导表失败时，应按[排错流程](generate-debug.md)检查配置和模板。

客户端专用表需在注册表显式设置 `group=c`，避免服务端导出时主键被过滤；一键脚本分别执行 client / server，联合目标按实际校验结果处理。工具固定为 5.1.0，不通过降级或削弱校验来掩盖错误。

原来的 `build-luban.*` / `copy_luban.*` 依赖人工准备的源码及本地路径，不是当前固定版本接入入口。恢复工具不能用下载最新版本、运行旧脚本或直接导表来替代兼容验证。

## MCP 与后续验证

```powershell
python .agents/skills/luban-dev/scripts/luban_ai.py configure-mcp
python .agents/scripts/workflow.py luban test
```

MCP 配置在已有 `.codex/config.toml` 中增加可共享的项目条目，保留其他服务。客户端从仓库根启动；新会话加载后可查询和校验，未加载时使用 Agent CLI。

工具恢复后先运行隔离验证。写入生产代码和 Binary 时继续遵循仓库 `preview / approve / apply`；不因原命令失败而自动重放生产导表。
