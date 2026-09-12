# DGame Luban 表规则

## 推荐策略

- 默认推荐在 `GameConfig/Datas/__tables__.xlsx` 显式注册表。
- Luban 支持 `#<value_type>-<comment>.xlsx` 文件级自动导入，DGame 可用但不默认推荐。
- DGame 扩展支持 Sheet 级拆表：sheet 名为 `#<value_type>-<comment>` 时，可手动运行 `GameConfig/Tools/split_sheets.py` 拆成临时 `#` 文件再交给 Luban 自动导入；当前默认导表脚本不会自动调用该步骤。
- 需要 `mode`、`index`、`group_by`、复杂 `tags` 或明确双端分组时，优先使用 `__tables__.xlsx`。

## 显式注册

`__tables__.xlsx` 当前字段从实际表头确认：

- `full_name`：表类名，如 `TbItemConfig`
- `value_type`：行类型，如 `ItemConfig`
- `read_schema_from_file`：是否从 Excel 表头读取字段定义
- `input`：业务 Excel 文件
- `index`：主键字段
- `mode`：`map`、`one`、`list`
- `group`：`c`、`s`、`e`
- `comment`：表注释
- `tags`：如 `group_by:GroupID`

## 自动导入

文件级：

```text
GameConfig/Datas/#SkillCfg-技能表.xlsx -> value_type=SkillCfg -> TbSkillCfg
```

Sheet 级：

```text
任意业务 Excel 的 sheet "#SkillCfg-技能表"
-> 手动运行 split_sheets.py，临时生成 GameConfig/Datas/#SkillCfg-技能表.xlsx
-> Luban 自动导入 TbSkillCfg
```

`split_sheets.py` 规则：

- 跳过 `__tables__`、`__beans__`、`__enums__`。
- 如果 Datas 根目录已有同名 `#value_type*.xlsx`，跳过同名 sheet，避免重复成表。
- 生成文件记录在 `_split_manifest.txt`，导表后清理。
- 文件名中的 `.` 会被替换，避免 Luban 误判 namespace。

## Excel 数据表

普通横表前几行：

```text
##var    字段名
##type   字段类型
##group  c/s/e 分组
##       字段说明
```

当前 DGame 业务表约定保持原始行序：第 1 行 `##var` 字段名，第 2 行 `##type` 类型，第 3 行 `##group` 分组，第 4 行 `##` 中文字段注释，随后才是数据行。新增字段必须复用第 4 行注释，不得把注释写入第 3 行或另造第五行；追加前先删除字段区间内和末尾的空列。

部分表使用多行 `##var` 或多行 `##type` 表达复合字段。字段工具必须保留所有这些行及其相对顺序：新增顶层字段写入第一组 `##var`/`##type`，压缩列时按所有定义行的并集移动整列，不能只扫描第一行，也不能把第二个 `##type` 当成普通数据行。

多行 `##type` 必须和对应的 `##var` 续行逐列对齐；续行只补充复合字段列的类型，不能被当成数据行或合并为新的字段名。修改字段时只更新该字段所在列的第一组类型定义，其他续行原样保留。

数据从第 5 行之后开始。当前项目使用过的扩展类型包括 `vector2`、`vector3`、`vector4`、`vector2int`、`vector3int`，定义在 `GameConfig/Defines/builtin.xml`。


## GroupId 分组索引

需要按组查询时，在 `__tables__.xlsx` 对应表的 `tags` 字段填写 `group_by:GroupId`。字段名必须与业务表字段完全一致。导表后会生成 `_groupedDataMap`、`GroupedDataMap` 和 `GetListByGroupId(int)`，例如：

```csharp
var steps = TbGuideStepConfig.GetListByGroupId(groupId);
```

不要手动修改 `GameProto` 下的生成代码。

## `read_schema_from_file`

配置 `__tables__.xlsx` 时确认 `read_schema_from_file` 使用正确的布尔值：`true` 从业务 Excel 表头读取 schema，`false` 使用 Defines/schema 中的定义。值配置错误会导致 schema 解析或导表失败。
