---
name: luban-dev
description: DGame Luban 配置开发与排错。用于 GameConfig 数据、表结构、Bean/Enum、校验规则、自定义模板、导表脚本及 ConfigSystem/ConfigMgr 消费链路，也用于本项目官方 Agent CLI 和 MCP 的部署与查询。
---

# DGame Luban 配置开发

## 先选择入口

| 任务 | 入口 |
| --- | --- |
| 查询编译后的表、Bean、Enum、Schema，执行 Luban 校验 | 官方 Agent CLI；已加载时也可用 Luban MCP |
| 修改 Excel、字段、数据行，检查引用 | `scripts/luban_helper.py`；写入前读取 [操作指南](references/operating-guide.md) |
| 校验现有导表流程、隔离导表、生产导表 | 仓库 Workflow，见下方导表流程 |
| 工具安装、恢复、版本、MCP 配置 | [工具恢复指南](references/tool-recovery.md)；来源与兼容范围见 [官方工具接入](references/official-ai.md) |

所有命令从仓库根执行。查询示例：

```powershell
python .agents/skills/luban-dev/scripts/luban_ai.py list-tables
python .agents/skills/luban-dev/scripts/luban_ai.py describe --name TbModelConfig
python .agents/skills/luban-dev/scripts/luban_ai.py schema --name TbModelConfig
python .agents/skills/luban-dev/scripts/luban_ai.py validate
python .agents/skills/luban-dev/scripts/luban_helper.py table get TbModelConfig
```

Agent CLI 默认目标为 `client`，可显式使用 `--target server/all`。MCP 的 `list_tables`、`get_schema`、`describe`、`validate` 调用应传本工程 `GameConfig/luban.conf` 的绝对路径和目标 `client`。工具未暴露时直接用 CLI，不假定技能加载就代表 MCP 可用。

## 项目约定

- 数据源：`GameConfig/Datas/`；表、Bean、Enum 注册位于 `GameConfig/Datas/Defines/`。具体文件以 `GameConfig/luban.conf` 为准。
- XML 类型定义：`GameConfig/Defines/`；生成模板：`GameConfig/CustomTemplate/`。
- 默认导表：客户端 `cs-bin` + `bin/json`，使用 `GenerateTool_Binary/gen_bin_client_lazyload.*`。
- 生成代码：`GameUnity/Assets/Scripts/HotFix/GameProto/LubanConfig/`；Binary：`GameUnity/Assets/BundleAssets/Configs/Binary/`；JSON：`GameUnity/Configs/Json/`。输出路径以 `path_define.*` 为准。
- `ConfigSystem.cs`、`ExternalTypeUtil.cs` 来源于 `CustomTemplate/Client/Bin/`，改动应落在模板；生成代码不手改。
- 命名空间为 `GameProto`，总表为 `Tables`。业务访问通过 `GameLogic/ConfigMgr/XxxConfigMgr` 封装；运行时沿用 ConfigSystem、LazyLoad 和 DGame 资源加载规则。
- 运行时消费遵守项目文本规范，不将玩家可见文本硬编码进业务代码。
- 字段分组以现有定义和需求为准，helper 自动推断只作辅助；联合导表分别校验 client / server，保证各目标的表主键可见。
- 正式导表、Agent CLI 和 MCP 统一固定 5.1.0；兼容范围和部署方式见 [官方工具接入](references/official-ai.md)。不能将上游 `main` 的示例视为本地命令保证。

## 配置修改流程

1. 先区分数据、表结构、Bean/Enum、模板、导表脚本或运行时消费，再读取实际定义和调用点。
2. 修改已有表前读取目标 Sheet 的全部标记行；保留多行 `##var/##type`、纵表、分组、注释和原始行序。表头不是固定四行。
3. 写入前用 `table get` / `field list` 确认文件、Sheet 和字段；新增数据行前用 `row get` 核对主键，避免覆盖已有数据。
4. 表/字段改名、删除或改类型前，检查配置与代码引用，读取 [兼容性说明](references/dgame-integration.md)；不能假设 `cs-bin` 新旧结构兼容。删除按仓库已授权范围执行。
5. 字段操作先按逻辑字段压缩字段区间及末尾空列，再用 `field list` 检查列号连续。字段注释复用现有注释行；不要覆盖 `##group` 或复合字段续行。
6. 只改源数据或模板。字段类型、主键、分组的变化须符合需求；不得为让生成通过而削弱原有校验。
7. helper 的 `validate --all` / `ref` 用作辅助诊断；`ref` 查询 Bean/Enum 类型引用，表/字段及业务调用还要搜索实际源码。最终以真实 Luban 校验和相关导表结果为准，见 [排错流程](references/generate-debug.md)。
8. 需要更新生产产物时按下方流程执行。代码与 Binary 必须来自同一次导表；字段/表结构及 `tags`、`group_by`、`read_schema_from_file` 变化要检查生成 API、ConfigMgr 调用和相关 Unity 编译与消费。

新增表优先在 `Datas/Defines/__tables__.xlsx` 显式注册；根据实际表头填写 `read_schema_from_file`。从表头推断字段时，不在 `__beans__` 再定义同名 Bean。文件级 `#` 自动导入可用；Sheet 级拆分需手动执行 `split_sheets.py`，默认导表脚本已注释该步骤。完整规则见 [表注册](references/dgame-table-rules.md)。

## 导表流程

```powershell
python .agents/scripts/workflow.py luban validate
python .agents/scripts/workflow.py luban test
python .agents/scripts/workflow.py luban preview --mode lazyload
```

`validate` 和 `test` 使用隔离目录；`preview` 记录隔离产物与当前输入，生产写入按仓库授权执行 `approve` / `apply`。需要 Unity 测试时在 preview 中显式指定 `--editmode-filter` / `--playmode-filter`。命令超时先核对真实状态，不自动重放。

已明确授权直接导表时，Windows 非交互调用必须设置 `AUTO_CONTINUE`：

```powershell
cmd /c "set AI_MODE=1 && set AUTO_CONTINUE=1 && GameConfig\GenerateTool_Binary\gen_bin_client_lazyload.bat"
```

MCP 配置默认只启用查询和校验；生产生成沿用 Workflow。工具安装与升级本身不授权生产导表、提交或发布。

## 按主题读取

按实际操作读取对应参考；表/字段结构修改先读表规则和 Excel 格式，运行时消费修改先读运行时加载说明。命令清单和示例按需读取，入口中的核心约束始终适用。

| 场景 | 文档 |
| --- | --- |
| 工具缺失、安装恢复、版本切换、MCP 配置 | [tool-recovery.md](references/tool-recovery.md) |
| 固定版本、CLI/MCP、官方来源与许可 | [official-ai.md](references/official-ai.md) |
| 生成失败、结构化错误、校验失败 | [generate-debug.md](references/generate-debug.md) |
| DGame 目录、脚本、运行时链路 | [dgame-integration.md](references/dgame-integration.md) |
| 新增表、注册、自动导入、分组索引 | [dgame-table-rules.md](references/dgame-table-rules.md) |
| Excel CRUD、字段与行、空白行清理 | [operating-guide.md](references/operating-guide.md) |
| 类型与容器约束 | [type-system.md](references/type-system.md) |
| Schema、Bean、Enum、继承与多态 | [schema.md](references/schema.md) |
| 配置文件与目标分组 | [luban-conf.md](references/luban-conf.md) |
| 引用、范围、路径等校验器 | [validators.md](references/validators.md) |
| 标记行、复杂结构与填表 | [excel-format.md](references/excel-format.md) |
| 主生成器参数 | [command-reference.md](references/command-reference.md) |
| JSON/XML/YAML/Lua 数据源 | [data-sources.md](references/data-sources.md) |
| 运行时加载、ConfigMgr、类型映射 | [runtime.md](references/runtime.md) |
