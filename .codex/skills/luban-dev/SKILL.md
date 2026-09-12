---
name: luban-dev
description: DGame Luban 游戏配置全栈工具，支持枚举/Bean/数据表/字段/数据行的增删改查、配置校验、引用检查、导表脚本调用、ConfigMgr 封装与运行时消费链路排查。触发场景：(1) 编辑 GameConfig/Datas 下的游戏配置数据（道具表、文本表、模型表、音效表、特效表、GM 表等），(2) 新增/修改/删除配置表结构、字段、枚举或 Bean，(3) 处理 __tables__.xlsx/__beans__.xlsx/__enums__.xlsx、# 自动导入表或 DGame Sheet 拆表规则，(4) 修改 luban.conf、Defines、CustomTemplate 或 GenerateTool_Binary 导表脚本，(5) 生成/校验客户端 GameProto 配置代码和二进制配置数据，(6) 排查 ConfigSystem、Tables、GameLogic/ConfigMgr 配置访问问题。即使用户未明确说 “Luban”，只要是在编辑或排查游戏配置数据，也应使用此技能。
---

# DGame Luban 配置开发

## 核心约定

### 现有 Excel 表结构保护

- 修改已有业务表字段前，先按逻辑字段名压缩并删除字段区间内、字段末尾的空列，再追加字段。
- DGame 常见四行表头固定为：第 1 行 `##var` 字段名，第 2 行 `##type` 类型，第 3 行 `##group` 分组，第 4 行 `##` 中文注释；字段工具必须复用第 4 行，不得改写第 3 行或创建第五行注释。
- 追加/删除字段后必须检查 `field list` 的列号连续、`validate --all` 通过，再执行导表。

- 源头：`GameConfig/Datas/`、`GameConfig/Defines/`、`GameConfig/CustomTemplate/`，不要手改生成产物。
- 默认生成：客户端 `cs-bin` 代码 + `bin/json` 数据，优先走 LazyLoad。
- 默认脚本：`GameConfig/GenerateTool_Binary/gen_bin_client_lazyload.bat`。
- 代码输出：`GameUnity/Assets/Scripts/HotFix/GameProto/LubanConfig/`。
- 桥接文件：`ConfigSystem.cs`、`ExternalTypeUtil.cs` 由模板复制到 `GameUnity/Assets/Scripts/HotFix/GameProto/`。
- 数据输出：二进制到 `GameUnity/Assets/BundleAssets/Configs/Binary/`；Json 路径以 `GenerateTool_Binary/path_define.*` 当前配置为准。
- 命名空间/顶层模块：`GameProto`；总表管理器：`Tables`。
- 业务访问：优先在 `GameUnity/Assets/Scripts/HotFix/GameLogic/ConfigMgr/` 封装 `XxxConfigMgr`，不要把 `TbXXX` 访问散落到业务层。

### 导出脚本

所有脚本位于 `GameConfig/GenerateTool_Binary/`，使用相对路径调用。

| 脚本 | 用途 | 说明 |
|:---|:---|:---|
| `gen_bin_client_lazyload` | 客户端（**推荐**，LazyLoad 模板） | AI 默认调用此脚本 |
| `gen_bin_client` | 客户端（标准模板） | 非懒加载 |
| `gen_bin_server_lazyload` | 服务端（LazyLoad 模板） | 需要服务端配置时使用 |
| `gen_bin_server` | 服务端（标准模板） | 非懒加载 |
| `gen_bin_all_lazyload` | 客户端 + 服务端（LazyLoad 模板） | 一键导出两端 |
| `gen_bin_all` | 客户端 + 服务端（标准模板） | 一键导出两端，非懒加载 |

### AI 调用导表命令

根据操作系统选择对应扩展名。默认只导客户端 LazyLoad：

**Windows：**

```powershell
cmd /c "set AI_MODE=1 && GameConfig\GenerateTool_Binary\gen_bin_client_lazyload.bat"
```

**macOS/Linux：**

```bash
bash GameConfig/GenerateTool_Binary/gen_bin_client_lazyload.sh
```

## 导表与发布成对约束

- Luban 生成的 `GameUnity/Assets/Scripts/HotFix/GameProto/LubanConfig/` 代码与 `GameUnity/Assets/BundleAssets/Configs/Binary/` 二进制必须来自同一次导表；只更新其中一侧会造成运行时反序列化或字段错位。
- 修改 `__tables__.xlsx` 的 `tags`、`group_by` 或 `read_schema_from_file` 后，必须重新执行对应客户端/全量导表脚本，并检查生成代码、Binary 和 Unity 编译结果。
- `read_schema_from_file` 是表级开关：业务 Excel 已带 `##var/##type/##group` 表头时通常设为 `true`；schema 已在 Defines 中维护、业务文件只提供数据时设为 `false`。不要凭表名猜值。

## 默认工作流

1. 先判断改动属于数据、表结构、Bean/Enum、schema、模板、导表脚本还是运行时消费。
2. 读现有定义：`__tables__.xlsx`、`__beans__.xlsx`、`__enums__.xlsx`、目标业务表和相关 `ConfigMgr`。
3. 改源文件，不改 `LubanConfig/` 下生成代码。
4. 运行或提示运行默认导表脚本。
5. 验证生成目录、Unity 编译、`ConfigSystem.Instance.Tables` 访问和业务管理器封装。

## 新增配置表

1. 在 `GameConfig/Datas/` 新增业务 Excel，前四行固定为 `##var`、`##type`、`##group`、`##`。
2. 需要复合字段时补 `__beans__.xlsx`，需要枚举时补 `__enums__.xlsx`。
3. 默认推荐在 `__tables__.xlsx` 显式注册 `full_name`、`value_type`、`input`、`mode`、`tags`。
4. Luban 支持文件名 `#<value_type>-<comment>.xlsx` 自动导入，不需要在 `__tables__.xlsx` 注册；DGame 可用但不作为默认推荐。
5. DGame 扩展支持 Sheet 名 `#<value_type>-<comment>` 自动拆表；导表前会拆成临时 `#` 文件，同样不需要注册，但仍按自动导入场景谨慎使用。
6. 需要按字段分组访问时，优先在 `__tables__.xlsx` 的 `tags` 写 `group_by:字段名`。例如 `group_by:GroupId` 会为表生成 `GroupedDataMap` 和 `GetListByGroupId(int)`，按组快速取得数据；自动导入表如需特殊 tags，先确认当前 Luban 支持方式。
7. 配置 `__tables__.xlsx` 时必须确认 `read_schema_from_file` 为正确的 `true` 或 `false`：`true` 表示从业务 Excel 表头读取字段定义，`false` 表示使用 Defines/schema 中的定义。该值错误会导致 schema 解析或导表失败。
7. 单例配置可用 `mode=one`。
8. 导表后在 `GameLogic/ConfigMgr/` 补 `XxxConfigMgr`。

业务层常用访问：

```csharp
using GameLogic;

var modelCfg = ModelConfigMgr.Instance.GetOrDefault(modelId);
var hasModel = ModelConfigMgr.Instance.ContainsKey(modelId);
var ok = SoundConfigMgr.Instance.TryGetValue(soundId, out var soundCfg);
```

只有在 `XxxConfigMgr` 内部封装时才直接碰 `TbXXX`：

```csharp
using GameProto;

public ModelConfig GetOrDefault(int modelID) => TbModelConfig.GetOrDefault(modelID);
```

## 操作工具

可用 `scripts/luban_helper.py` 读写 Excel 配置，适合批量增删字段、查引用、校验数据。

### 前置条件

Python 3.8+，`pip install -r .codex/skills/luban-dev/scripts/requirements.txt`

### 执行方式

```powershell
python .codex/skills/luban-dev/scripts/luban_helper.py --data-dir GameConfig/Datas <command>
```

`--data-dir` 默认就是 `GameConfig/Datas`，可省略；PowerShell 中 JSON 参数推荐用 `--file` 从文件读取。

**参数类型**：位置参数直接传值，不加 `--` 前缀。例如 `table get TbItemConfig`，不是 `table get --name TbItemConfig`。

### 命令速查

| 分类 | 命令 | 功能 |
|------|------|------|
| 枚举 | `enum list/get/add/update/delete` | 枚举 CRUD |
| 结构 | `bean list/get/add/update/delete` | Bean CRUD |
| 表 | `table list/get/add/update/delete` | 表 CRUD；默认显式注册到 `__tables__.xlsx` |
| 字段 | `field list/add/update/delete/disable/enable` | 字段操作，支持 `##` 禁用字段 |
| 数据 | `row list/get/query/add/update/delete` | 数据行操作 |
| 批量 | `batch fields/rows` | 批量字段/数据行操作 |
| 导入导出 | `export/import` | JSON 导入导出 |
| 验证 | `validate` / `ref` | 数据验证 / 引用检查 |
| 类型 | `type list/validate/suggest/search/guide/info` | 类型系统 |
| 自动 | `auto list/create` | `#` 自动导入表；DGame 支持但不默认推荐 |
| 管理 | `rename/copy/diff/template` | 表管理工具 |
| 附加 | `alias/tag/variant/multirow/cache` | 常量别名、标签、字段变体、多行结构、缓存 |

### 操作规范

- **只读操作**（`list/get/query/search/validate/ref/diff`）：可直接执行。
- **写入操作**（`add/update/delete/import/rename/copy/template/create`）：先确认目标表、字段、Sheet 和影响面。
- **删除操作**：先跑 `ref` 检查引用，提醒兼容性风险，再二次确认。
- **修改前**：先 `table get` / `field list` 确认结构，`row get` 避免主键冲突。
- **修改后**：跑 `validate --all`，再运行 DGame 导表脚本。
- **自动导入**：Luban 支持 `#` 文件，DGame 支持 `#` Sheet 拆表，但默认仍推荐 `__tables__.xlsx` 显式注册。
- **迁移自动导入**：`migrate-auto` 在 DGame 版本中保留命令入口但禁止执行，避免把推荐的显式注册表误迁移。

### 分组自动推断

添加字段时不指定 `--group`，脚本会按字段名和类型做保守推断：

- `c`（客户端）：`name`、`desc`、`icon`、`image`、`sprite`、`model`、`spine`、`effect`、`sound`、`audio`、`ui`、`location`、`path`、`prefab`、`asset`、`local` 等。
- `s`（服务端）：`server`、`backend`、`logic`、`damage`、`hp`、`mp`、`exp`、`level`、`rate`、`attack`、`defense`、`platform`、`channel` 等。
- `cs`（两端）：`id`、`*_id`、无法明确归属的字段。

这是辅助推断，不是架构决策；关键配置仍以目标表现有 `##group` 和需求为准。

## 红线

- 不直接改 `GameUnity/Assets/Scripts/HotFix/GameProto/LubanConfig/`。
- 字段改名、删字段、改类型要当作兼容性风险处理。
- 资源加载链路以当前 `ConfigSystem` 和 DGame 资源模块为准；若文档和代码冲突，优先信任代码。
- 配置表运行时中文文本仍遵守项目文本规范，不把玩家可见文本硬编码进业务代码。

## 按需参考

| 场景 | 文档 |
| --- | --- |
| DGame 集成、目录、导表、运行时加载 | [dgame-integration.md](references/dgame-integration.md) |
| DGame 表注册、自动导入、Sheet 拆表 | [dgame-table-rules.md](references/dgame-table-rules.md) |
| 工具命令详解、Excel CRUD | [operating-guide.md](references/operating-guide.md) |
| Luban 类型系统 | [type-system.md](references/type-system.md) |
| Schema / Bean / Enum / Table 定义 | [schema.md](references/schema.md) |
| `luban.conf` 配置项 | [luban-conf.md](references/luban-conf.md) |
| 校验器与引用检查 | [validators.md](references/validators.md) |
| Excel 数据格式 | [excel-format.md](references/excel-format.md) |
| CLI 参数 | [command-reference.md](references/command-reference.md) |
| JSON/XML/YAML/Lua 数据源 | [data-sources.md](references/data-sources.md) |
| DGame 运行时加载、ConfigMgr 封装、类型映射 | [runtime.md](references/runtime.md) |

示例：DGame 当前不额外内置 `examples/`，以真实项目 `GameConfig/Datas/`、`GameConfig/Defines/`、`GameConfig/CustomTemplate/`、`GameConfig/GenerateTool_Binary/` 作为示例来源。

脚本：`scripts/luban_helper.py`（DGame 化配置表操作工具）、`scripts/requirements.txt`（依赖）。

官方文档：https://www.datable.cn/docs/intro


## 导表空白行警告

Luban 提示表尾空白数据影响性能时，先执行 `cleanup blank-rows TABLE` 预览，确认 `last_data_row` 后再加 `--apply`。工具只删除表尾连续且所有业务列为空的行，不删除中间空行或含数据的行。清理后重新执行 `validate --all` 和导表脚本。
