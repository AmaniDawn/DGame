# Luban 配置表操作指南

本文档是 `luban_helper.py` 脚本的完整命令参考，包含所有命令的参数详解、Excel 结构说明、数据填写格式和注释约定。

## 基础参数

**执行方式**：
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas <command>
```

**注意**：`--data-dir` 必须放在子命令之前。PowerShell 中使用分号 `;` 作为命令分隔符，不要使用 `&&`。

### 参数类型说明

本工具参数分为两类：

| 类型 | 格式 | 说明 | 示例 |
|------|------|------|------|
| **位置参数** | 大写显示（如 `NAME`、`TABLE`） | 不带 `--` 前缀，按位置顺序传值 | `table get TbItemConfig` |
| **可选参数** | `--参数名 值` | 带 `--` 前缀，可省略 | `--comment "道具表"` |

**常见错误**：
```bash
# 错误 — 位置参数不能加 -- 前缀
python luban_helper.py table get --name TbItemConfig

# 正确 — 位置参数直接传值
python luban_helper.py table get TbItemConfig
```

---

## 枚举操作

### enum list - 列出所有枚举
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas enum list
```

### enum get - 查询枚举详情
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas enum get test.ETestQuality
```

**参数**：`name`（位置参数）— 枚举名称

### enum add - 新增枚举
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas enum add test.EWeaponType --values "SWORD=1:剑,BOW=2:弓,STAFF=3:法杖" --comment "武器类型"
```

**参数**：
- `name`（位置参数）— 枚举全名（包含模块，如 `test.EWeaponType`）
- `--values`: 枚举值，格式 `name=value:alias,name2=value2:alias2`
- `--comment`: 枚举注释
- `--flags`: 是否为标志枚举（可选）

### enum delete - 删除枚举
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas enum delete test.EWeaponType
python scripts/luban_helper.py --data-dir GameConfig/Datas enum delete test.EWeaponType --force  # 强制删除
```

**参数**：
- `name`（位置参数）— 枚举名称
- `--force`: 强制删除，忽略引用检查

### enum update - 更新枚举属性
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas enum update test.EWeaponType --comment "武器类型枚举"
python scripts/luban_helper.py --data-dir GameConfig/Datas enum update test.EWeaponType --flags
```

**参数**：
- `name`（位置参数）— 枚举名称
- `--comment`: 注释
- `--flags`: 是否为标志枚举

---

## Bean 操作

### bean list - 列出所有 Bean
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas bean list
```

### bean get - 查询 Bean 详情
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas bean get test.TestBean1
```

**参数**：`name`（位置参数）— Bean 名称

### bean add - 新增 Bean
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas bean add test.Weapon --fields "attack:int:攻击力,speed:float:攻击速度" --parent Item --comment "武器"
```

**参数**：
- `name`（位置参数）— Bean 全名（包含模块）
- `--fields`: 字段定义，格式 `name:type:comment,name2:type2:comment2` 或 JSON 数组
- `--file`: 从 JSON 文件读取字段定义
- `--parent`: 父类名称（可选）
- `--comment`: Bean 注释（可选）
- `--value-type`: 是否为值类型（0=普通类，1=值类型/struct）
- `--sep`: 分隔符（用于 list 类型元素分隔）

### bean delete - 删除 Bean
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas bean delete test.Weapon
python scripts/luban_helper.py --data-dir GameConfig/Datas bean delete test.Weapon --force  # 强制删除
```

**参数**：
- `name`（位置参数）— Bean 名称
- `--force`: 强制删除，忽略引用检查

### bean update - 更新 Bean 属性
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas bean update test.ItemList --sep "|"
python scripts/luban_helper.py --data-dir GameConfig/Datas bean update test.ItemList --comment "道具列表"
```

**参数**：
- `name`（位置参数）— Bean 名称
- `--sep`: 分隔符（用于 list 类型元素分隔）
- `--comment`: 注释
- `--alias`: 别名
- `--parent`: 父类名称
- `--value-type`: 是否为值类型

**分隔符说明**：

| 分隔符 | 数据格式示例 | 说明 |
|--------|-------------|------|
| 默认 `;` | `1001,10;2003,50;5003,10` | 默认分隔符 |
| `|` | `1001,10|2003,50|5003,10` | 更清晰（推荐） |
| `#` | `1001,10#2003,50#5003,10` | 自定义分隔符 |

---

## 表操作

### table list - 列出所有表
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas table list
```

### table get - 查询表详情
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas table get TbItemConfig
```

**参数**：`name`（位置参数）— 表名称

### table add - 新增配置表
```bash
# 默认在 __tables__.xlsx 中显式注册（DGame 推荐）
python scripts/luban_helper.py --data-dir GameConfig/Datas table add TbItemConfig --fields "id:int:道具ID,name:string:道具名称" --comment "道具表"

# 使用 # 前缀自动导入格式（DGame 支持但不默认推荐）
python scripts/luban_helper.py --data-dir GameConfig/Datas table add TbItemConfig --fields "id:int:道具ID,name:string:道具名称" --auto-import
```

**参数**：
- `name`（位置参数）— 表全名（包含模块，如 `TbItemConfig`）
- `--fields`: 字段定义，格式 `name:type:comment:group`（group 可选）
- `--value-type`: 值类型
- `--input`: 数据文件名
- `--sheet`: Sheet名称
- `--mode`: 模式
- `--comment`: 表注释
- `--index`: 主键定义（如 `id` 或 `id1+id2`）
- `--groups`: 分组列表（如 `c,s`），启用 `##group` 行
- `--auto-import`: 使用 `#` 前缀自动导入格式（DGame 支持但不默认推荐，默认在 `__tables__.xlsx` 显式注册）
- `--vertical`: 使用纵表模式

**显式注册方式**（默认，DGame 推荐）：
- 在 `__tables__.xlsx` 中添加表定义行
- 创建对应的数据 xlsx 文件
- 所有表统一管理，结构清晰

**自动导入格式**（`--auto-import`，支持但不默认推荐）：
- 文件名格式：`#表名-注释.xlsx`
- Luban 自动识别，无需在 `__tables__.xlsx` 中声明
- DGame 还支持 sheet 名 `#表名-注释`，需要手动运行 `GameConfig/Tools/split_sheets.py` 拆成临时 `#` 文件后再导表

### table delete - 删除配置表
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas table delete TbItemConfig
python scripts/luban_helper.py --data-dir GameConfig/Datas table delete TbItemConfig --delete-data  # 同时删除数据文件
```

**参数**：
- `name`（位置参数）— 表名称
- `--delete-data`: 同时删除数据文件

### table update - 更新表属性
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas table update TbItemConfig --comment "道具配置表"
python scripts/luban_helper.py --data-dir GameConfig/Datas table update TbItemConfig --input "道具配置表_v2.xlsx"
```

**参数**：
- `name`（位置参数）— 表名称
- `--comment`: 注释
- `--input`: 输入文件名
- `--mode`: 模式
- `--value-type`: 值类型

### table check-legacy - 列出显式注册表及可选自动导入文件名
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas table check-legacy
```

### table migrate-auto - 禁用：DGame 默认不迁移到自动导入格式
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas table migrate-auto
python scripts/luban_helper.py --data-dir GameConfig/Datas table migrate-auto TbItemConfig  # 迁移指定表
```

**参数**：`name`（位置参数，可选）— 表名称，不指定则迁移所有

### 纵表（单例表）
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas table add test.TbGlobalConfig --fields "guild_open_level:int:公会开启等级,bag_init_size:int:初始格子数" --comment "全局配置" --vertical
```

**纵表结构**：
```
| ##column |          |          |         |
| ##var    | ##type   | ##       | ##group |
| guild_open_level | int | 公会开启等级 | c |
| bag_init_size    | int | 初始格子数   | c |
```

---

## 字段操作

### field list - 列出表的所有字段
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas field list TbItemConfig
```

**参数**：`table`（位置参数）— 表名称

### field add - 添加字段
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas field add TbItemConfig desc --type "string" --comment "道具描述"
```

**参数**：
- `table`（位置参数）— 表名称
- `name`（位置参数）— 字段名
- `--type`: 字段类型
- `--comment`: 字段注释（支持多行，用 `|` 分隔）
- `--group`: 字段分组（可选，不指定时自动推断）
- `--sheet`: Sheet名称
- `--position`: 插入位置（从0开始，-1表示末尾）

**分组自动推断规则**：
- `c` (客户端): name, desc, icon, image, model, effect, sound, ui 等显示相关
- `s` (服务器): server, logic, damage, hp, mp, exp, level, rate 等逻辑相关
- `cs` (两者): id, 其他无法明确判断的字段

### field update - 修改字段
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas field update TbItemConfig desc --new-name "description" --comment "详细描述"
```

**参数**：
- `table`（位置参数）— 表名称
- `name`（位置参数）— 原字段名
- `--new-name`: 新字段名
- `--type`: 新类型
- `--comment`: 新注释
- `--group`: 新分组
- `--sheet`: Sheet名称

### field delete - 删除字段（危险操作）
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas field delete TbItemConfig desc
```

**参数**：
- `table`（位置参数）— 表名称
- `name`（位置参数）— 字段名
- `--sheet`: Sheet名称
- `--force`: 强制删除，跳过确认

**警告**：删除字段会同时删除该字段的所有数据。

### field disable / enable - 禁用/启用字段
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas field disable TbItemConfig desc
python scripts/luban_helper.py --data-dir GameConfig/Datas field enable TbItemConfig desc
```

**参数**：
- `table`（位置参数）— 表名称
- `name`（位置参数）— 字段名
- `--sheet`: Sheet名称

禁用字段通过在字段名前添加 `##` 前缀实现，Luban 导表时会忽略该字段，但数据保留。

---

## 数据行操作

### row list - 列出数据行
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas row list TbItemConfig
python scripts/luban_helper.py --data-dir GameConfig/Datas row list TbItemConfig --start 10 --limit 20
```

**参数**：`table`（位置参数）— 表名称

### row get - 按字段值查询数据行
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas row get TbItemConfig --field id --value 1004
python scripts/luban_helper.py --data-dir GameConfig/Datas row get TbItemConfig --field name --value "屠龙刀"
```

**参数**：
- `table`（位置参数）— 表名称
- `--field`: 字段名
- `--value`: 字段值
- `--sheet`: Sheet名称

**返回示例**：
```json
{"id": 1004, "name": "烈焰剑", "type": "Weapon", "quality": 4}
```

### row query - 多条件查询
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas row query TbItemConfig --conditions '{"type":"Weapon","quality":5}'
python scripts/luban_helper.py --data-dir GameConfig/Datas row query TbItemConfig --conditions '{"type":"Consumable"}' --limit 10
```

**参数**：
- `table`（位置参数）— 表名称
- `--conditions`: 查询条件 JSON
- `--sheet`: Sheet名称
- `--limit`: 返回行数限制

### row add - 添加数据行
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas row add TbItemConfig --data '{"id":1001,"name":"宝剑","count":1}'
```

**参数**：
- `table`（位置参数）— 表名称
- `--data`: 数据 JSON 格式
- `--file`: 从 JSON 文件读取数据（推荐用于 PowerShell）
- `--sheet`: Sheet名称

**智能插入**：添加数据行时必须紧接最后一条实际数据，不能使用工作表 `max_row` 把数据放到预格式化空白区；存在数值 `id` 主键时按升序插入到合适位置，并检查重复 ID。操作遵循最小改动原则，只修改目标表和目标行。
- ID 最大 → 追加到末尾
- ID 在中间 → 插入到合适位置

### row update - 更新数据行
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas row update TbItemConfig 0 --data '{"name":"神剑"}'
```

**参数**：
- `table`（位置参数）— 表名称
- `index`（位置参数）— 行索引（从0开始）
- `--data`: 更新数据 JSON 格式
- `--file`: 从 JSON 文件读取数据
- `--sheet`: Sheet名称

### row delete - 删除数据行
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas row delete TbItemConfig 0
python scripts/luban_helper.py --data-dir GameConfig/Datas row delete TbItemConfig 0 --force
```

**参数**：
- `table`（位置参数）— 表名称
- `index`（位置参数）— 行索引（从0开始）
- `--sheet`: Sheet名称
- `--force`: 强制删除，跳过确认

---

## 批量操作

### batch fields - 批量添加字段
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas batch fields TbItemConfig --data '[{"name":"price","type":"int","comment":"价格"},{"name":"quality","type":"int","comment":"品质"}]'
```

### batch rows - 批量添加数据行
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas batch rows TbItemConfig --data '[{"id":1001,"name":"宝剑"},{"id":1002,"name":"铁剑"}]'
```

**参数**：
- `table`（位置参数）— 表名称
- `--data`: JSON 数组数据
- `--sheet`: Sheet名称

---

## 导入导出

### export - 导出表数据为 JSON
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas export TbItemConfig
python scripts/luban_helper.py --data-dir GameConfig/Datas export TbItemConfig --output item_backup.json
```

**参数**：
- `table`（位置参数）— 表名称
- `--output`: 输出文件路径（默认打印到控制台）
- `--sheet`: Sheet名称

### import - 从 JSON 导入数据
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas import TbItemConfig item_backup.json
```

**参数**：
- `table`（位置参数）— 表名称
- `file`（位置参数）— 输入 JSON 文件路径
- `--sheet`: Sheet名称
- `--mode`: 导入模式（`append` 追加，`replace` 替换）

---

## 验证功能

### validate - 验证表数据
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas validate TbItemConfig
python scripts/luban_helper.py --data-dir GameConfig/Datas validate --all
```

**参数**：
- `table`（位置参数，可选）— 表名称，不指定则验证所有表
- `--sheet`: Sheet名称
- `--all`: 验证所有表

**验证内容**：
- 表结构完整性（##var、##type 行）
- 字段定义检查
- 数据类型验证

---

## 其他命令

### ref - 引用完整性检查
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas ref test.RewardItem
```

**参数**：`type`（位置参数）— 类型名称（枚举或 Bean）

### template - 配置模板
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas template list
python scripts/luban_helper.py --data-dir GameConfig/Datas template create item TbEquip --module test
```

**参数**（template create）：
- `template`（位置参数）— 模板名称
- `table`（位置参数）— 表名称（不含模块）
- `--module`: 模块名
- `--input`: 输入文件名

### rename - 重命名表
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas rename TbItemConfig TbItemConfigNew
python scripts/luban_helper.py --data-dir GameConfig/Datas rename TbItemConfig TbItemConfigNew --migrate-data
```

**参数**：
- `old_name`（位置参数）— 原表名
- `new_name`（位置参数）— 新表名
- `--migrate-data`: 迁移数据文件

### copy - 复制表
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas copy TbItemConfig TbItemConfigCopy
```

**参数**：
- `source`（位置参数）— 源表名
- `target`（位置参数）— 目标表名
- `--copy-data`: 复制数据文件

### diff - 差异对比
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas diff TbItemConfig TbItemConfigV2
```

**参数**：
- `table1`（位置参数）— 表1名称
- `table2`（位置参数）— 表2名称或 JSON 文件
- `--json`: table2 是 JSON 文件路径

### auto - 自动导入表操作（支持但不默认推荐）
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas auto list
python scripts/luban_helper.py --data-dir GameConfig/Datas auto create #ItemConfig --fields "id:int:ID,name:string:名称"
```

**参数**（auto create）：
- `name`（位置参数）— 表名（如 `#ItemConfig` 或 `#ItemConfig-道具表`）
- `--fields`: 字段定义，格式 `name:type:comment`

### alias - 常量别名操作
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas alias list
python scripts/luban_helper.py --data-dir GameConfig/Datas alias add MAX_LEVEL 100 --comment "最大等级"
python scripts/luban_helper.py --data-dir GameConfig/Datas alias delete MAX_LEVEL
python scripts/luban_helper.py --data-dir GameConfig/Datas alias resolve MAX_LEVEL
```

**参数**（alias add）：
- `name`（位置参数）— 别名名
- `value`（位置参数）— 别名值
- `--comment`: 注释

### tag - 数据标签操作
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas tag list TbItemConfig
python scripts/luban_helper.py --data-dir GameConfig/Datas tag add TbItemConfig 0 TEST_TAG
python scripts/luban_helper.py --data-dir GameConfig/Datas tag remove TbItemConfig 0
```

**参数**（tag add）：
- `table`（位置参数）— 表名称
- `index`（位置参数）— 行索引
- `tag`（位置参数）— 标签名
- `--sheet`: Sheet名称

### variant - 字段变体操作
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas variant list TbItemConfig name
python scripts/luban_helper.py --data-dir GameConfig/Datas variant add TbItemConfig name zh
```

**参数**（variant add）：
- `table`（位置参数）— 表名称
- `field`（位置参数）— 字段名
- `variant`（位置参数）— 变体名（如 zh, en）
- `--sheet`: Sheet名称

### multirow - 多行结构列表操作
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas multirow TbItemConfig rewards
python scripts/luban_helper.py --data-dir GameConfig/Datas multirow TbItemConfig rewards --disable
```

**参数**：
- `table`（位置参数）— 表名称
- `field`（位置参数）— 字段名
- `--disable`: 禁用多行结构
- `--sheet`: Sheet名称

### cache - 缓存操作
```bash
python scripts/luban_helper.py --data-dir GameConfig/Datas cache build
python scripts/luban_helper.py --data-dir GameConfig/Datas cache clear
```

---

## Excel 结构说明

### 数据表结构
```
| ##var   | id  | name   | count   |
| ##type  | int | string | int     |
| ##      | 道具ID | 道具名称 | 堆叠数量  |
| ##group | c   | c      | c       |  ← 可选
```

### 纵表结构（单例表）
```
| ##column |          |          |         |
| ##var    | ##type   | ##       | ##group |
| key      | string   | 配置键    | c       |
| value    | int      | 配置值    | s       |
```

### __enums__.xlsx 结构
- `full_name` 有值 = 枚举定义开始
- `full_name` 为空 = 上一枚举的枚举项
- `*items` 列（H列开始）= 枚举项数据

```
| ##var | full_name          | flags | unique | group | comment | tags | *items              |
|-------|-------------------|-------|--------|-------|---------|------|---------------------|
| ##var | name              | alias | value  | comment | tags  |      |                     |
| ##    | 全名              | 是否标志| 是否唯一 |       |         |      | 枚举名              |
|       | test.ETestQuality | False | True   |       |         |      | A | 白 | 1 | 最高品质 |
|       |                   |       |        |       |         |      | B | 黑 | 2 | 黑色的   |
```

### __beans__.xlsx 结构
- `full_name` 有值 = Bean 定义开始
- `full_name` 为空 = 上一 Bean 的字段
- `*fields` 列（J列开始）= 字段数据

```
| ##var | full_name          | parent | valueType | alias | sep | comment | tags | group | *fields      |
|-------|-------------------|--------|-----------|-------|-----|---------|------|-------|--------------|
| ##var | name              | alias  | type      | group | comment | tags | variants |            |
| ##    | 全名              | 父类   | 是否值类型 | 别名  | 分隔符 | 字段名  | 别名 | 类型  | 分组 | 注释 |
|       | test.TestBean1    |        |           |       |     | 测试Bean |      | c     | x1 | int | 最高品质 |
|       |                   |        |           |       |     |         |      |       | x2 | string | 黑色的 |
```

---

## 数据填写格式

### 基本类型
| 类型 | 格式示例 |
|------|---------|
| int | `100` |
| string | `道具名称` |
| bool | `true` 或 `false` |
| float | `1.5` |

### 枚举类型
```
Weapon      # 枚举名
1           # 数值
```

### List 类型
```
# list<int>
1;2;3;4;5

# list<RewardItem> 简写格式（推荐）
1001,100;1002,20
```

### Map 类型
```
# map<int,string>
1,金币;2,钻石;3,体力
```

---

## 注释约定规范

在 Luban Excel 表格中，`##` 开头的行是注释行，会被 Luban 忽略。约定：

**`##var` 行和数据起始行之间的所有 `##` 开头的行，作为字段注释。**

```
行号 | A列   | B列      | C列       | D列
-----|------|----------|----------|-----------
1    | ##var| id       | name     | count      ← 字段名行
2    | ##type| int     | string   | int        ← 类型行
3    | ##   | 道具ID   | 道具名称  | 堆叠数量    ← 注释行1
4    | ##   | 唯一标识 |          |            ← 注释行2（可多行）
5    | ##group| c      | c        | c          ← 分组行（可选）
6    |      | 1        | 道具1    | 10         ← 数据起始行
```

**优点**：
- **零改动**：完全兼容现有 Luban，`##` 本来就是注释
- **向后兼容**：现有表格无需修改
- **灵活**：支持多行注释


## 带宏 `.xlsm` 配置表

带宏工作簿不能使用普通 `openpyxl` 写入流程。`keep_vba=True` 只能保留宏二进制，保存时仍可能重建工作簿并改变空字符串、格式或其他内容。

在 Windows 且安装 Excel 时，使用安全副本命令：

```powershell
python scripts/luban_helper.py xlsm edit GameConfig/Datas/业务配置表.xlsm `
  --output GameConfig/Datas/业务配置表__edit_test.xlsm `
  --file changes.json
```

`changes.json` 格式：

```json
[
  {"sheet":"业务数据", "cell":"C10", "value":"示例值"},
  {"sheet":"业务数据", "cell":"F10", "value":1001}
]
```

命令会复制源文件、禁用宏自动执行、通过 Excel COM 写入、保存后重新打开并校验目标单元格。源文件不会被覆盖；确认输出文件和差异报告安全后，再人工替换正式文件。该流程依赖 Windows Excel，Linux/macOS 不支持。 示例中的文件名、工作表名和单元格仅用于说明格式，必须替换为实际配置表内容；`Card_卡牌总表.xlsm` 仅是本次安全性测试样例。


### 配置表修改安全约束

- 修改前先定位唯一的文件、Sheet、字段和数据行，禁止凭文件名猜测位置。
- 只写入用户要求的字段；未要求的字段保持原值，不用空值覆盖。
- 新增数据行不得制造空白间隔，不得删除或重排无关数据。
- 修改后必须重新读取并校验目标值、主键唯一性、数据连续性；失败时保留原文件不动。
- `.xlsm` 必须使用 `xlsm edit` 的 Excel COM 副本流程，禁止普通 row/field 写入命令直接保存。


## 导表空白行警告

Luban 如果提示某张表存在大量表尾空白数据，先执行预览：

```powershell
python scripts/luban_helper.py --data-dir GameConfig/Datas cleanup blank-rows TbItemConfig
```

确认输出的 `last_data_row`、`max_row` 和 `trailing_blank_rows` 后，再执行：

```powershell
python scripts/luban_helper.py --data-dir GameConfig/Datas cleanup blank-rows TbItemConfig --apply
```

清理规则只删除最后一条真实数据之后、所有业务列都为空的连续行；不会删除中间空行、表头、注释行或任何含数据的行。`.xlsm` 不走此命令，必须使用 Excel COM 流程。清理后应重新运行 `validate --all` 和导表脚本。
