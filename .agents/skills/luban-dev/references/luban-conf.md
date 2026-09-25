# luban.conf 完整配置说明

## 概述

`luban.conf` 是 Luban 的主配置文件，使用 JSON 格式，定义项目结构、代码生成目标和数据导出配置。

## 完整配置示例

```json
{
    "groups": [
        {"names": ["c"], "default": true},
        {"names": ["s"], "default": true},
        {"names": ["e"], "default": false}
    ],
    "schemaFiles": [
        {"fileName": "Defines", "type": ""},
        {"fileName": "Datas/__tables__.xlsx", "type": "table"},
        {"fileName": "Datas/__beans__.xlsx", "type": "bean"},
        {"fileName": "Datas/__enums__.xlsx", "type": "enum"}
    ],
    "dataDir": "Datas",
    "targets": [
        {
            "name": "client",
            "manager": "Tables",
            "groups": ["c"],
            "topModule": "GameProto"
        },
        {
            "name": "server",
            "manager": "Tables",
            "groups": ["s"],
            "topModule": "GameProto"
        },
        {
            "name": "editor",
            "manager": "Tables",
            "groups": ["c", "s", "e"],
            "topModule": "GameProto"
        }
    ]
}
```

## 配置项详解

### dataDir

数据根目录，所有数据文件相对于此目录。

```json
"dataDir": "Datas"
```

### schemaFiles

Schema 定义文件列表。

```json
"schemaFiles": [
    {"fileName": "Defines", "type": ""},
    {"fileName": "Datas/__tables__.xlsx", "type": "table"},
    {"fileName": "Datas/__beans__.xlsx", "type": "bean"},
    {"fileName": "Datas/__enums__.xlsx", "type": "enum"}
]
```

**字段说明：**

| 字段 | 类型 | 必填 | 说明 |
|:---|:---|:---|:---|
| `fileName` | string | 是 | 文件或目录路径 |
| `type` | string | 是 | 文件类型： `""`（XML）、`"enum"`、`"bean"`、`"table"` |

**类型说明：**

| type | 说明 |
|:---|:---|
| `""` | XML 定义文件 |
| `"enum"` | Excel 枚举定义（`__enums__.xlsx`） |
| `"bean"` | Excel 结构定义（`__beans__.xlsx`） |
| `"table"` | Excel 表定义（`__tables__.xlsx`） |

### groups

定义配置内可用的分组。

```json
"groups": [
    {"names": ["c"], "default": true},
    {"names": ["s"], "default": true},
    {"names": ["e"], "default": false}
]
```

**字段说明：**

| 字段 | 类型 | 必填 | 说明 |
|:---|:---|:---|:---|
| `names` | string[] | 是 | 分组名列表 |
| `default` | bool | 是 | 是否为表的默认导出目标 |

**注意：** `default` 只对 `table` 生效，对 `enum`、`bean`、`field` 不生效。

### targets

定义导出目标。

```json
"targets": [
    {
        "name": "client",
        "manager": "Tables",
        "groups": ["c"],
        "topModule": "GameProto"
    }
]
```

**字段说明：**

| 字段 | 类型 | 必填 | 说明 |
|:---|:---|:---|:---|
| `name` | string | 是 | 导出目标名，命令行 `-t` 参数使用 |
| `manager` | string | 是 | 生成的表管理类名称 |
| `groups` | string[] | 是 | 此目标包含的分组 |
| `topModule` | string | 否 | 代码额外顶层命名空间 |

**效果示例：**
- `topModule=GameProto` → 生成 `GameProto.TbItemConfig`、`GameProto.Tables`（DGame 当前取值）
- `topModule=cfg` → 生成 `cfg.TbItemConfig`、`cfg.Tables`

> `topModule` 只是控制生成代码的顶层命名空间，可任意改；改动后业务层 `using` 与 `ConfigMgr` 内引用需同步更新。DGame 固定使用 `GameProto`，改前先确认没有散落引用。

### xargs

可选字段，当前 DGame 使用字符串列表。通常通过命令行 `-x key=value` 传递，也可在 luban.conf 中预定义：

```json
"xargs": [
    "key1=value1",
    "key2=value2"
]
```

不要使用旧版本文档中的对象数组；当前 Luban CLI 会将对象数组解析为错误类型并拒绝导表。

多数项目使用命令行 `-x` 传递，luban.conf 中留空数组即可。

## 常用分组策略

| 分组 | 用途 |
|:---|:---|
| `c` | 客户端（Client） |
| `s` | 服务器（Server） |
| `e` | 编辑器（Editor） |
| `c,s` | 客户端和服务器共享 |
| `c,s,e` | 全平台 |

## 级联选项

命令行参数支持命名空间层级搜索：

```bash
# 单目标
-x outputCodeDir=GameUnity/Assets/Scripts/HotFix/GameProto/LubanConfig

# 多目标（使用命名空间前缀）
-x cs-bin.outputCodeDir=GameUnity/Assets/Scripts/HotFix/GameProto/LubanConfig
-x bin.outputDataDir=GameUnity/Assets/BundleAssets/Configs/Binary
-x json.outputDataDir=GameUnity/Configs/Json
```

查找 `a.b.c.key` 时按以下优先级：
1. `a.b.c.key`
2. `a.b.key`
3. `a.key`
4. `key`

## 项目结构示例

```
GameConfig/
├── luban.conf              # 主配置文件
├── Defines/                # XML Schema 定义
│   └── builtin.xml         # DGame 扩展类型映射
├── Datas/                  # 数据文件
│   ├── __tables__.xlsx     # Excel 表定义
│   ├── __beans__.xlsx      # Excel 结构定义
│   ├── __enums__.xlsx      # Excel 枚举定义
│   ├── 道具配置表.xlsx
│   ├── 文本配置表.xlsx
│   ├── 模型配置表.xlsx
│   ├── 音效配置表.xlsx
│   └── 特效配置表.xlsx
└── GenerateTool_Binary/    # DGame 导表脚本
```

### 多模块 Defines 拆分（可选，schema 变多时）

DGame 当前 `Defines/` 只有 `builtin.xml`。当 XML schema 增多时，可按模块拆分并用 `__root__.xml` 汇总，避免单文件膨胀：

```
Defines/
├── __root__.xml            # 根模块，import 各子模块
├── builtin.xml             # 内置/扩展类型映射
├── item.xml                # 道具模块 bean/enum/table 定义
└── skill.xml               # 技能模块定义
```

`__root__.xml` 通过 `<import name="item.xml"/>` 引入子模块；各子模块用 `<module name="item">` 声明命名空间。是否拆分取决于规模，小项目保持单文件即可。

## 配置文件检查清单

- [ ] `dataDir` 指向正确的数据目录
- [ ] `schemaFiles` 包含所有定义文件
- [ ] `groups` 定义了所需的所有分组
- [ ] `targets` 配置了正确的导出目标
- [ ] `targets.*.groups` 与表的 `group` 属性匹配
- [ ] 文件路径使用正斜杠 `/`（跨平台兼容）
