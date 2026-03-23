# DGame Luban配置表指引

当需求涉及配置表、Luban、Excel 源数据、生成脚本、模板或配置消费链路时，先阅读本文件。

本文档先记录当前已确认的仓库事实，后续再逐步补充业务规则、命名规范、生成流程细节和注意事项。

## 目录导航

- [DGame Luban配置表指引](#dgame-luban配置表指引)
  - [目录导航](#目录导航)
  - [技术栈说明](#技术栈说明)
  - [配置工程目录结构](#配置工程目录结构)
  - [类型支持](#类型支持)
    - [Luban 基础类型](#luban-基础类型)
    - [当前项目已确认使用或扩展的类型](#当前项目已确认使用或扩展的类型)
  - [Excel数据表结构](#excel数据表结构)
    - [示例](#示例)
  - [__beans__.xlsx 复合类型](#beansxlsx-复合类型)
    - [简要例子](#简要例子)
  - [数据表格定义](#数据表格定义)
    - [当前项目已确认的表定义示例](#当前项目已确认的表定义示例)
    - [当前项目生成结果对应关系](#当前项目生成结果对应关系)
    - [`tags` 中 `group_by` 的作用](#tags-中-group_by-的作用)
  - [luban.conf 当前信息](#lubanconf-当前信息)
  - [代码生成](#代码生成)
    - [代码产物示例](#代码产物示例)
    - [数据产物示例](#数据产物示例)
    - [重要提示](#重要提示)
  - [ConfigSystem 加载器](#configsystem-加载器)
    - [当前加载流程](#当前加载流程)
    - [初始化时机](#初始化时机)
    - [二进制加载方式](#二进制加载方式)
    - [当前职责理解](#当前职责理解)
  - [配置表数据访问示例](#配置表数据访问示例)
    - [常规配置表访问](#常规配置表访问)
    - [`group_by:xxx` 分组访问](#group_byxxx-分组访问)
    - [单例表访问](#单例表访问)
    - [推荐使用方式](#推荐使用方式)
  - [目录职责理解](#目录职责理解)
    - [`Datas/`](#datas)
    - [`Defines/`](#defines)
    - [`CustomTemplate/`](#customtemplate)
    - [`GenerateTool_Binary/`](#generatetool_binary)
    - [`GenerateTool_Json/`](#generatetool_json)
    - [`Tools/`](#tools)
  - [使用原则](#使用原则)
  - [添加新配置表的流程](#添加新配置表的流程)

## 技术栈说明

- Luban：高性能游戏配置表工具，支持 Excel/JSON/YAML → C# 代码 + 二进制数据。
- 生成格式：`cs-bin`（C# 代码）+ `bin`（二进制数据）。
- 客户端二进制数据输出目录：`GameUnity/Assets/BundleAssets/Configs/Binary/`。
- 客户端配置代码输出目录：`GameUnity/Assets/Scripts/HotFix/GameProto/LubanConfig/`，属于 `GameProto` 热更程序集。
- 客户端公共配置辅助代码会额外复制到：`GameUnity/Assets/Scripts/HotFix/GameProto/`，当前包括 `ConfigSystem.cs` 和 `ExternalTypeUtil.cs`。
- 当前仓库中的客户端二进制生成脚本是 `GameConfig/GenerateTool_Binary/gen_bin_client.bat`，其生成参数为 `-c cs-bin -d bin`。
- 当前仓库中的客户端 Json 生成脚本是 `GameConfig/GenerateTool_Json/gen_json_client.bat`，其生成参数为 `-c cs-simple-json -d json2`。

## 配置工程目录结构

```text
GameConfig/
├── CustomTemplate/         # 自定义模板根目录，内部按客户端和服务端区分不同配置代码模板
│   ├── Client/             # 客户端模板目录，内部按生成格式和加载模式拆分不同模板内容
│   │   ├── Bin/            # bin 模式模板目录，内部文件用于生成 bin 配置配套的客户端代码和辅助类型
│   │   ├── Json/           # json 模式模板目录，内部文件用于生成 json 配置配套的客户端代码和辅助类型
│   │   └── CustomTemplate_Client_LazyLoad/ # LazyLoad 模式模板目录，内部文件用于生成按需加载配置时使用的模板代码
│   └── Server/             # 服务端模板目录，内部文件用于生成服务端配置代码和 LazyLoad 相关模板
├── Datas/                  # 配置数据目录，内部文件用于维护业务配置表、表结构定义、Bean 定义和枚举定义
│   ├── __tables__.xlsx     # 配置表结构定义文件，用于声明表信息和基础结构
│   ├── __beans__.xlsx      # Bean 定义文件，用于声明可复用的数据结构
│   ├── __enums__.xlsx      # 枚举定义文件，用于声明配置系统使用的枚举类型
│   ├── 道具配置表.xlsx      # 业务配置表示例，说明具体功能配置表会在此目录维护
│   └── ...                 # 其他业务配置表，例如文本、模型、特效、音效、GM 等
├── Defines/                # Luban 定义目录，内部文件用于补充内建类型与 schema 相关定义
├── GenerateTool_Binary/    # 二进制生成脚本目录，内部脚本用于导出 bin 数据以及 cs-bin 配套代码
│   ├── gen_bin_client.bat          # 生成客户端 bin 数据和 cs-bin 配置代码
│   ├── gen_bin_server.bat          # 生成服务端 bin 数据和配置代码
│   ├── gen_bin_all.bat             # 一次性生成 client + server + enum 相关配置内容
│   ├── gen_bin_client_lazyload.bat # 生成客户端 LazyLoad 模式下使用的 bin 数据和配套代码
│   ├── gen_bin_server_lazyload.bat # 生成服务端 LazyLoad 模式下使用的 bin 数据和配套代码
│   ├── gen_bin_all_lazyload.bat    # 一次性生成 LazyLoad 模式下的全量配置内容
│   └── path_define.bat             # 定义生成脚本共用的输入、输出和工具路径
├── GenerateTool_Json/      # Json 生成脚本目录，内部脚本用于导出 json 数据以及对应配置代码
├── Tools/                  # 配置工具目录，内部包含 Luban 工具本体、默认模板资源和工具说明
└── luban.conf              # Luban 主配置文件，用于声明分组、输入源、target 和生成规则
```

## 类型支持

### Luban 基础类型

| Excel类型 | C#类型 | 示例 |
| --- | --- | --- |
| bool | bool | `true` |
| byte | byte | `1` |
| short | short | `100` |
| int | int | `1001` |
| long | long | `10000001` |
| float | float | `1.5` |
| double | double | `3.1415926` |
| string | string | `"道具名称"` |
| datetime | DateTime | `"2024-01-01 00:00:00"` |
| enum | 枚举类型 | `ColorQuality.High` |
| bean | 自定义 Bean/结构体 | `vector2` |
| array | `T[]` | `int[]` |
| list | `List<T>` | `List<ItemConfig>` |
| set | `HashSet<T>` | `HashSet<int>` |
| map | `Dictionary<TKey, TValue>` | `Dictionary<int, ItemConfig>` |

### 当前项目已确认使用或扩展的类型

| Excel类型 | C#类型 | 示例 |
| --- | --- | --- |
| long? | `long?` | `ExpireTime = 1710000000` |
| string[] | `string[]` | `["简体中文","English"]` |
| vector2 | `GameProto.vector2` | `"1.5,2.5"` |
| vector3 | `GameProto.vector3` | `"1,2,3"` |
| vector4 | `GameProto.vector4` | `"1,2,3,4"` |
| vector2int | `GameProto.vector2int` | `"100,200"` |
| vector3int | `GameProto.vector3int` | `"1,2,3"` |

说明：

- “Luban 基础类型”依据当前仓库自带 Luban 工具包中的内建类型线索整理，已在本地 `Luban.L10N.dll` 字符串中确认存在 `DByte`、`DShort`、`DInt`、`DLong`、`DFloat`、`DDouble`、`DBool`、`DString`、`DDateTime`、`DArray`、`DList`、`DSet`、`DMap`、`DBean`、`DEnum`。
- “当前项目已确认使用或扩展的类型”依据 `GameConfig/Defines/builtin.xml` 和当前生成到 `GameProto/LubanConfig` 的代码整理。
- Unity 类型说明：`vector2`、`vector3` 在客户端生成时会通过 `ExternalTypeUtil` 转换为 `UnityEngine.Vector2`、`UnityEngine.Vector3`。
- `datetime` 的 Excel 写法这里只给出常见时间字符串示例，若后续项目内有固定填写规范，再单独补充。

## Excel数据表结构

当前项目的 Excel 数据表，第一列前四行固定为：

- `##var`
- `##type`
- `##group`
- `##`

每一行的作用如下：

| 行标记 | 作用 |
| --- | --- |
| `##var` | 字段名定义行。从第二列开始填写字段名，生成代码时会据此生成对应的数据字段。 |
| `##type` | 字段类型定义行。从第二列开始填写字段类型，类型需要符合 Luban 支持的类型系统。 |
| `##group` | 字段分组定义行。从第二列开始填写字段所属分组，用于控制客户端、服务端或其他分组的导出范围。 |
| `##` | 字段说明行。从第二列开始填写字段注释，便于策划和程序理解字段含义。 |

当前项目中已确认的基础分组含义如下：

| 分组 | 作用 |
| --- | --- |
| `c` | client，表示该字段或数据会参与客户端配置导出。 |
| `s` | server，表示该字段或数据会参与服务端配置导出。 |
| `e` | 扩展分组，当前仓库 `luban.conf` 中存在该分组，具体使用范围以项目实际约定为准。 |

### 示例

```text
| A       | B      | C        | D       | E          |
| ------- | ------ | -------- | ------- | ---------- |
| ##var   | ItemID | ItemName | Quality | ExpireTime |
| ##type  | int    | string   | int     | long?      |
| ##group | c/s    | c/s      | c/s     | c          |
| ##      | 道具ID | 道具名称 | 道具品质 | 过期时间   |
|         | 1001   | 红药水   | 1       | 1710000000 |
|         | 1002   | 蓝药水   | 2       |            |
```

说明：

- 第 5 行开始才是实际数据行。
- 第一列前四行是结构定义标记，不属于业务字段内容。
- `##group` 的具体填写规则以项目分组约定为准，当前仓库中可见基础分组为 `c`、`s`、`e`。

## __beans__.xlsx 复合类型

`__beans__.xlsx` 用于定义可复用的复合类型。复合类型可以理解为“由多个基础字段组成的结构体/Bean”，后续可以在普通配置表里直接作为字段类型使用。

适合放在 `__beans__.xlsx` 中定义的内容包括：

- 需要在多个配置表中重复使用的数据结构。
- 由多个字段组合而成、语义明确的一组数据。
- 不适合拆成多个独立字段、但又不需要单独做一张表的数据结构。

### 简要例子

例如定义一个奖励结构：

```text
Bean名：RewardItem

| A       | B      | C   |
| ------- | ------ | --- |
| ##var   | Id     | Num |
| ##type  | int    | int |
| ##group | c/s    | c/s |
| ##      | 道具ID | 数量 |
```

定义完成后，就可以在普通配置表中把某个字段类型写成 `RewardItem`，也可以配合数组或列表使用，例如：

- `RewardItem`
- `RewardItem[]`
- `list<RewardItem>`

说明：

- `__beans__.xlsx` 关注的是“复合字段结构定义”，不是完整业务表定义。
- Bean 生成后通常会生成对应的数据类型代码，供 `TbXXX` 表中的行数据引用。

## 数据表格定义

当前项目的数据表格定义来源于 `Datas/__tables__.xlsx`。结合仓库内 `builtin.xml` 的示例和生成结果，可以先确认表定义至少包含以下关键信息：

| 定义项 | 说明 | 示例 |
| --- | --- | --- |
| `name` | 生成后的表类型名，通常会生成 `TbXXX` 表类 | `TbItemConfig` |
| `value` | 表中每一行对应的数据结构类型 | `ItemConfig` |
| `mode` | 表的组织方式，决定生成后的访问结构 | `map` |
| `input` | 对应的 Excel 数据源文件名 | `道具配置表.xlsx` |
| `tags` | 额外标签配置，用于扩展生成行为；例如 `group_by:字段名` 可用于生成按组访问能力 | `group_by:GroupID` |

### 当前项目已确认的表定义示例

项目内 `builtin.xml` 注释示例中可见如下定义形式：

```xml
<table name="TbItemConfig" value="ItemConfig" mode="map" input="道具配置表.xlsx" tags="group_by:GroupID" />
```

### 当前项目生成结果对应关系

- `name="TbItemConfig"` 会生成表类 `TbItemConfig`。
- `value="ItemConfig"` 会生成行数据类型 `ItemConfig`。
- `mode="map"` 在当前生成结果中体现为 `Dictionary<int, ItemConfig>` + `List<ItemConfig>` 的组合访问结构。
- `input="道具配置表.xlsx"` 表示该表的数据来自对应 Excel 文件。
- 如果 `mode` 是 `one`，模板代码会自动生成静态属性字段，方便快速访问单例表数据。

### `tags` 中 `group_by` 的作用

当表定义中配置 `group_by:xxx`，并且配置表中存在 `xxx` 这个字段时，生成的代码会自动根据组 ID 生成 `Dictionary<GroupID, List<xxx>>` 这样的分组数据结构，可以通过组 ID 快速访问到该分组下的所有数据。

如果某张表需要支持按组 ID 分组访问，应在 `GameConfig/Datas/__tables__.xlsx` 的表定义中配置 `tags=group_by:xxx`，而不是去普通业务表或生成代码中手动补数据结构。分组访问所需的数据结构由表定义和模板自动生成。

说明：

- 这一节当前只记录仓库内已经能确认的表定义项，不提前扩展到未验证的列规则或更多 mode 类型。
- 如果后续需要，可以继续补充 `mode=one`、`mode=list` 等模式在 DGame 项目中的实际使用方式。

## luban.conf 当前信息

当前 `GameConfig/luban.conf` 已确认：

| 配置项 | 当前值 | 作用 |
| --- | --- | --- |
| `groups` | `c`、`s`、`e` | 定义可用的分组集合，用于控制字段、表和导出目标的生效范围。 |
| `default: true` | `c`、`s`、`e` 都为默认 | 表示这些分组在默认情况下会参与处理和导出。 |
| `schemaFiles` | `Defines` | 声明 schema 输入来源之一，用于加载定义目录下的类型或扩展定义。 |
| `schemaFiles` | `Datas/__tables__.xlsx` | 声明表结构定义文件输入，用于定义有哪些配置表以及表的组织方式。 |
| `schemaFiles` | `Datas/__beans__.xlsx` | 声明 Bean 定义文件输入，用于定义可复用复合类型。 |
| `schemaFiles` | `Datas/__enums__.xlsx` | 声明枚举定义文件输入，用于定义配置系统中使用的枚举。 |
| `dataDir` | `Datas` | 声明数据目录根路径，配置表数据文件默认从该目录下读取。 |
| `targets` | `server` | 定义服务端导出目标，控制服务端代码和数据的生成。 |
| `targets` | `client` | 定义客户端导出目标，控制客户端代码和数据的生成。 |
| `targets` | `all` | 定义全量导出目标，用于统一生成多个分组的数据。 |
| `manager` | `Tables` | 指定生成后的表管理器名称，当前会生成 `Tables` 作为总入口。 |
| `topModule` | `GameProto` | 指定生成代码所属的顶层模块或命名空间，当前为 `GameProto`。 |

说明：

- `groups`、`schemaFiles`、`targets` 是 `luban.conf` 中最核心的三类配置。
- 当前项目的客户端和服务端导出能力，都是在这些基础配置之上，再配合生成脚本与自定义模板完成的。

## 代码生成

当前项目的配置代码生成，主要由 `GenerateTool_Binary` 和 `GenerateTool_Json` 目录下的脚本驱动完成。

客户端二进制代码生成链路当前已确认如下：

1. 读取 `GameConfig/luban.conf`，加载 `Defines`、`__tables__.xlsx`、`__beans__.xlsx`、`__enums__.xlsx`。
2. 通过 `gen_bin_client.bat` 调用 Luban，使用 `-c cs-bin -d bin` 生成客户端配置代码和二进制数据。
3. 将客户端辅助代码复制到 `GameUnity/Assets/Scripts/HotFix/GameProto/`，当前包括：
   - `ConfigSystem.cs`
   - `ExternalTypeUtil.cs`
4. 将生成的配置代码输出到 `GameUnity/Assets/Scripts/HotFix/GameProto/LubanConfig/`。
5. 将生成的二进制数据输出到 `GameUnity/Assets/BundleAssets/Configs/Binary/`。

客户端 Json 代码生成链路当前已确认如下：

1. 通过 `gen_json_client.bat` 调用 Luban，使用 `-c cs-simple-json -d json2` 生成客户端 Json 配套代码和数据。
2. 同样会复制 `ConfigSystem.cs` 和 `ExternalTypeUtil.cs` 到 `GameUnity/Assets/Scripts/HotFix/GameProto/`。
3. 配置代码输出目录仍为 `GameUnity/Assets/Scripts/HotFix/GameProto/LubanConfig/`。

说明：

- 代码生成的顶层模块当前为 `GameProto`。
- 表管理入口会生成 `Tables`。
- 具体生成行为还会受 `CustomTemplate` 中客户端或服务端模板影响。

### 代码产物示例

```text
GameUnity/Assets/Scripts/HotFix/GameProto/
├── ConfigSystem.cs                 # 配置系统入口辅助代码
├── ExternalTypeUtil.cs             # 外部类型转换辅助代码
└── LubanConfig/
    ├── Tables.cs                   # 配置总表入口
    ├── TbItemConfig.cs             # 配置表访问类
    ├── ItemConfig.cs               # 行数据类型
    ├── ColorQuality.cs             # 枚举类型
    ├── vector2.cs                  # Bean/复合类型
    └── ...                         # 其他自动生成配置类型
```

### 数据产物示例

```text
GameUnity/Assets/BundleAssets/Configs/Binary/
├── tbitemconfig.bytes              # 道具配置表二进制数据示例
├── tbtextconfig.bytes              # 文本配置表二进制数据示例
├── tbmodelconfig.bytes             # 模型配置表二进制数据示例
└── ...                             # 其他配置表生成的二进制数据
```

### 重要提示

- `GameUnity/Assets/Scripts/HotFix/GameProto/LubanConfig/` 目录下的 `.cs` 文件不要手动修改。
- 该目录内容属于自动生成产物，下次重新执行生成脚本时会被覆盖。
- 如果需要调整生成结果，应优先修改 Excel、`__tables__.xlsx`、`__beans__.xlsx`、`__enums__.xlsx`、`CustomTemplate` 或生成脚本，而不是直接改生成文件。

## ConfigSystem 加载器

当前项目的配置加载入口位于 `GameUnity/Assets/Scripts/HotFix/GameProto/ConfigSystem.cs`。

### 当前加载流程

1. 通过 `ConfigSystem.Instance` 获取单例。
2. 首次访问 `Tables` 属性时，如果尚未初始化，会自动调用 `Load()`。
3. `Load()` 内部会创建 `new Tables(LoadByteBuf)`，把二进制加载函数传给 Luban 生成的 `Tables`。
4. `Tables` 在实际访问具体表时，再通过传入的 `LoadByteBuf` 按文件名加载对应配置。

### 初始化时机

- 在 `PreloadProcedure` 中，配置数据会被标记为 `PRELOAD`。
- 框架会在预加载阶段自动将这部分配置资源加载到内存中。
- `ConfigSystem` 在实际取表数据时，基于已经预加载到内存中的配置资源完成读取。

### 二进制加载方式

`LoadByteBuf(string file)` 的当前实现逻辑如下：

- 首次使用时，通过 `ModuleSystem.GetModule<IResourceModule>()` 获取资源模块。
- 调用 `m_resourceModule.LoadAsset<TextAsset>(file)` 加载对应配置资源。
- 取出 `TextAsset.bytes`。
- 使用 `new ByteBuf(bytes)` 包装成 Luban 读取所需的二进制缓冲对象。

### 当前职责理解

- `ConfigSystem`：配置系统总入口，负责初始化和提供 `Tables` 访问。
- `Tables`：Luban 自动生成的总表管理器，负责分发表加载和访问入口。
- `IResourceModule`：DGame 资源模块，负责按名称加载配置资源。
- `ByteBuf`：Luban 的二进制读取容器，负责将 `.bytes` 数据反序列化为配置对象。

## 配置表数据访问示例

### 常规配置表访问

```csharp
using GameProto;

// 通过静态入口按主键获取配置
ItemConfig itemCfg = TbItemConfig.GetOrDefault(1001);
if (itemCfg != null)
{
    DGameLog.Info($"道具名: {itemCfg.ItemName}");
}

// 通过 Tables 入口访问具体表
Tables tables = ConfigSystem.Instance.Tables;
ItemConfig itemCfg2 = tables.TbItemConfig.GetOrDefaultNoStatic(1001);
if (itemCfg2 != null)
{
    DGameLog.Info($"道具价格: {itemCfg2.Price}");
}

// 遍历整张表
foreach (var cfg in TbItemConfig.DataList)
{
    DGameLog.Info($"ItemID={cfg.ItemID}, Name={cfg.ItemName}");
}
```

### `group_by:xxx` 分组访问

当表定义中配置了 `group_by:xxx` 后，生成代码会额外提供按组访问能力，可以通过组 ID 快速获取该分组下的全部数据。

```csharp
using GameProto;

// 例：表定义中配置了 group_by:GroupID
int groupId = 1;

// 可通过组 ID 快速获取当前组的全部配置
var groupList = TbItemConfig.Instance.GetListByGroupID(groupId);
if (groupList != null)
{
    foreach (var cfg in groupList)
    {
        DGameLog.Info($"Group={groupId}, ItemID={cfg.ItemID}, Name={cfg.ItemName}");
    }
}
```

### 单例表访问

当表定义的 `mode` 为 `one` 时，模板代码会自动生成静态属性字段，适合快速访问全局唯一的一份配置数据。

```csharp
using GameProto;

// 假设 TbGameConfig 是一个 mode=one 的单例表
// 推荐直接通过模板生成的静态属性访问字段
DGameLog.Info($"默认区域ID: {TbGameConfig.DefaultAreaId}");

// 如果需要整份数据对象，则通过 Instance.Data 访问
var gameConfig = TbGameConfig.Instance.Data;
```

说明：

- 常规 `map` 表通常提供 `GetOrDefault`、`TryGetValue`、`DataMap`、`DataList` 等访问方式。
- `group_by:xxx` 表会额外补充按组访问能力，适合批量读取同组数据。
- `mode=one` 表适合全局唯一配置，访问方式通常比普通表更直接。

### 推荐使用方式

推荐将具体配置表再封装一层配置管理器，并将其设计为单例，例如 `ItemConfigMgr`，统一在管理器内部处理和 `ConfigSystem` 的交互，而不是在业务代码中直接到处访问 `TbItemConfig` 或 `ConfigSystem.Instance.Tables`。

这样做的主要目的包括：

- 将业务代码和底层配置结构解耦。
- 当配置字段改名、配置结构调整、表访问方式变化时，尽量只需要修改管理器内部代码，而不是在业务层改很多地方。
- 可以在管理器中集中做缓存、预处理、分组索引、默认值兜底、数据校验等逻辑。
- 便于后续扩展更稳定的业务接口，例如 `GetItemName(itemId)`、`GetItemPrice(itemId)`、`GetItemsByGroup(groupId)`。

示例：

```csharp
using GameProto;

public sealed class ItemConfigMgr
{
    private static ItemConfigMgr s_instance;
    public static ItemConfigMgr Instance => s_instance ??= new ItemConfigMgr();

    private ItemConfigMgr()
    {
    }

    public ItemConfig Get(int itemId)
    {
        return TbItemConfig.GetOrDefault(itemId);
    }

    public string GetItemName(int itemId)
    {
        return Get(itemId)?.ItemName ?? string.Empty;
    }

    public int GetItemPrice(int itemId)
    {
        return Get(itemId)?.Price ?? 0;
    }
}
```

业务层推荐直接依赖管理器：

```csharp
string itemName = ItemConfigMgr.Instance.GetItemName(1001);
int itemPrice = ItemConfigMgr.Instance.GetItemPrice(1001);
```

## 目录职责理解

基于当前仓库结构，可以先按下面方式理解职责：

### `Datas/`

存放配置 Excel 源文件。修改数值、文本、枚举映射、表内容时，优先在这里处理，而不是直接改生成结果。

### `Defines/`

存放 Luban 识别的定义输入。若要新增表结构、字段定义、Bean 或枚举相关 schema，需要先确认这里与 `__tables__`、`__beans__`、`__enums__` 的配合方式。

### `CustomTemplate/`

存放客户端和服务端模板。当前如果需求主要落在客户端配置生成，可优先关注：

- `Client/Bin`：bin 模式下的客户端模板。
- `Client/Json`：json 模式下的客户端模板。
- `Client/CustomTemplate_Client_LazyLoad`：LazyLoad 模式下的客户端模板。

如果需求涉及生成代码格式、自定义导出内容或 LazyLoad 模式，优先检查这里。

### `GenerateTool_Binary/`

存放二进制配置导出脚本。详细树形说明见上方“配置工程目录结构”。

### `GenerateTool_Json/`

存放 Json 配置导出脚本，组织方式与 Binary 目录类似，也同时覆盖：

-   `client`
-   `server`
-   `all`
-   `lazyload`
-   Windows `.bat`
-   Shell `.sh`

### `Tools/`

当前包含 `LubanTools`，并有说明文件提示需要自行下载 Luban 插件或从 `Tools` 目录拷贝。这表示配置生成流程依赖本地 Luban 工具环境。

## 使用原则

处理 GameConfig 相关任务时，优先遵循以下原则：

1.  先判断修改的是“源数据”还是“生成产物”。
2.  改 Excel、schema、模板或生成脚本，不要直接改生成结果。
3.  涉及客户端或服务端差异时，先确认目标是 `client`、`server` 还是 `all`。
4.  涉及加载性能或按需加载时，优先检查 `lazyload` 相关模板与脚本。
5.  任何生成链路改动，都要说明是否需要重新跑 Luban。

## 添加新配置表的流程

新增一张配置表时，推荐按下面顺序处理：

1. 在 `GameConfig/Datas/` 中新增业务 Excel 配置表。
2. 按项目约定填写 Excel 表结构，第一列前四行固定为：
   - `##var`
   - `##type`
   - `##group`
   - `##`
3. 如果表中需要用到新的复合类型，先在 `GameConfig/Datas/__beans__.xlsx` 中补充 Bean 定义。
4. 如果表中需要用到新的枚举类型，先在 `GameConfig/Datas/__enums__.xlsx` 中补充枚举定义。
5. 在 `GameConfig/Datas/__tables__.xlsx` 中新增表定义，至少补齐：
   - `name`
   - `value`
   - `mode`
   - `input`
   - `tags`（如有需要）
6. 如果需要按组访问能力，可以在表定义中配置 `group_by:xxx`。
7. 如果该表是全局唯一配置，可根据设计将 `mode` 设为 `one`。
8. 执行对应生成脚本，生成配置代码和数据：
   - 二进制流程使用 `GenerateTool_Binary`
   - Json 流程使用 `GenerateTool_Json`
9. 确认生成结果已输出到正确目录：
   - 代码输出到 `GameUnity/Assets/Scripts/HotFix/GameProto/LubanConfig/`
   - 数据输出到 `GameUnity/Assets/BundleAssets/Configs/Binary/`
10. 不要直接在业务代码中到处访问生成表，优先补一层配置管理器，例如 `ItemConfigMgr`，统一处理配置读取和业务封装。
11. 在对应流程或业务入口中验证配置是否可正常加载、访问、分组读取或单例读取。

重要提示：

- 自动生成目录下的 `.cs` 文件不要手动修改，重新生成时会被覆盖。
- 如果生成结果不符合预期，应优先检查 Excel、`__tables__.xlsx`、`__beans__.xlsx`、`__enums__.xlsx`、`CustomTemplate` 和生成脚本。
- 字段名和字段类型一旦确定后不要轻易修改，这会影响热更包兼容性和历史数据兼容性。
- 新增字段通常可以前向兼容，旧客户端会忽略新字段；删除字段通常不能前向兼容，容易导致旧版本读取异常。
- 测试时需要先在 Editor 模式下完成加载验证和格式验证，再决定是否发布热更包。
