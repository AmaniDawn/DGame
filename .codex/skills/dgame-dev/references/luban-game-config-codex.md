# DGame Luban 配置表指引（精简版）

当需求涉及配置表、Luban、Excel 源数据、生成脚本、模板、转表操作或配置消费链路时，先读本文件。

目标：让 Codex 以最短阅读成本理解 DGame 当前配置工程结构、生成链路、运行时加载方式、默认修改入口与禁止事项。原始细节和补充说明仍以 `references/originals/luban-game-config.md` 为准。

## 核心结论

- 源头在 `GameConfig/`，不要直接改生成产物。
- 客户端默认重点关注 `cs-bin + bin`，无特殊说明优先二进制和 LazyLoad 流程。
- 客户端配置代码生成到 `GameUnity/Assets/Scripts/HotFix/GameProto/LubanConfig/`，属于 `GameProto` 热更程序集。
- 客户端二进制数据生成到 `GameUnity/Assets/BundleAssets/Configs/Binary/`。
- `ConfigSystem.cs`、`ExternalTypeUtil.cs` 会额外复制到 `GameUnity/Assets/Scripts/HotFix/GameProto/`。
- 若生成结果不对，优先检查 Excel、`__tables__.xlsx`、`__beans__.xlsx`、`__enums__.xlsx`、`CustomTemplate`、生成脚本，不要改 `LubanConfig/` 下的 `.cs`。

## 技术栈与输出

- Luban：Excel/JSON/YAML -> C# 代码 + 数据产物
- 当前客户端二进制生成参数：`-c cs-bin -d bin`
- 当前客户端 Json 生成参数：`-c cs-simple-json -d json2`
- 客户端二进制生成脚本：`GameConfig/GenerateTool_Binary/gen_bin_client.bat`
- 客户端 Json 生成脚本：`GameConfig/GenerateTool_Json/gen_json_client.bat`
- 顶层模块/命名空间：`GameProto`
- 表管理器名称：`Tables`

## 配置工程结构

```text
GameConfig/
├── Datas/                  # Excel 源数据、表定义、Bean、枚举
│   ├── __tables__.xlsx     # 表定义
│   ├── __beans__.xlsx      # 复合类型/Bean 定义
│   ├── __enums__.xlsx      # 枚举定义
│   └── *.xlsx              # 业务配置表
├── Defines/                # Luban schema/扩展定义
├── CustomTemplate/         # 客户端/服务端模板，含 Bin/Json/LazyLoad
├── GenerateTool_Binary/    # bin 导出脚本
├── GenerateTool_Json/      # json 导出脚本
├── Tools/                  # Luban 工具本体与说明
└── luban.conf              # 主配置
```

### 目录职责

- `Datas/`：改配置源数据时优先改这里。
- `Defines/`：扩展类型或 schema 定义。
- `CustomTemplate/`：改生成代码格式、导出内容、LazyLoad 行为时优先看这里。
- `GenerateTool_Binary/`：bin 导出脚本。
- `GenerateTool_Json/`：json 导出脚本。
- `Tools/`：本地 Luban 工具环境依赖。

## `luban.conf` 当前关键信息

- `groups`：`c`、`s`、`e`
- `c/s/e` 当前都为默认组
- `schemaFiles`：`Defines`、`Datas/__tables__.xlsx`、`Datas/__beans__.xlsx`、`Datas/__enums__.xlsx`
- `dataDir`：`Datas`
- `targets`：`server`、`client`、`all`
- `manager`：`Tables`
- `topModule`：`GameProto`

结论：`groups`、`schemaFiles`、`targets` 是最核心的三类配置；客户端/服务端导出都建立在它们与脚本、模板的组合上。

## 类型支持

### Luban 基础类型

常见内建类型包括：

- `bool`
- `byte`
- `short`
- `int`
- `long`
- `float`
- `double`
- `string`
- `datetime`
- `enum`
- `bean`
- `array`
- `list`
- `set`
- `map`

### 当前项目已确认使用/扩展类型

- `long?`
- `string[]`
- `vector2`
- `vector3`
- `vector4`
- `vector2int`
- `vector3int`

说明：

- 上述扩展依据 `GameConfig/Defines/builtin.xml` 和当前 `GameProto/LubanConfig` 生成结果确认。
- `vector2`、`vector3` 等客户端生成时会通过 `ExternalTypeUtil` 转为 Unity 类型。

## Excel 表结构规则

普通配置表第一列前四行固定为：

1. `##var`
2. `##type`
3. `##group`
4. `##`

含义：

- `##var`：字段名
- `##type`：字段类型
- `##group`：导出分组
- `##`：字段说明

从第 5 行开始才是实际数据行。

### 当前已确认分组

- `c`：client
- `s`：server
- `e`：扩展分组，具体使用范围以项目约定为准

## `__beans__.xlsx`

`__beans__.xlsx` 用于定义可复用复合类型/Bean，适合放：

- 多张表重复使用的数据结构
- 由多个字段组合成、语义明确的一组数据
- 不适合拆成多个独立字段、但也不需要独立成表的结构

Bean 可在普通表中直接作为字段类型使用，例如：

- `RewardItem`
- `RewardItem[]`
- `list<RewardItem>`

结论：`__beans__.xlsx` 定义的是复合字段结构，不是业务表本身。

## `__tables__.xlsx`

当前可确认的关键定义项：

- `name`：生成表类名，通常形如 `TbXXX`
- `value`：行数据类型
- `mode`：表组织方式
- `input`：对应 Excel 文件
- `tags`：额外生成行为，例如 `group_by:字段名`

示例：

```xml
<table name="TbItemConfig" value="ItemConfig" mode="map" input="道具配置表.xlsx" tags="group_by:GroupID" />
```

### 当前已确认生成对应关系

- `name="TbItemConfig"` -> 生成 `TbItemConfig`
- `value="ItemConfig"` -> 生成 `ItemConfig`
- `mode="map"` -> 当前生成结果表现为 `Dictionary<int, ItemConfig>` + `List<ItemConfig>`
- `mode="one"` -> 模板会生成适合单例表的静态访问字段
- `input="xxx.xlsx"` -> 数据来自该 Excel

### `group_by`

若表定义配置 `group_by:xxx`，且表中存在 `xxx` 字段，则生成代码会自动生成按组访问结构，可通过组 ID 快速获取一组数据。

结论：如果需要分组访问，应在 `__tables__.xlsx` 配 `tags=group_by:xxx`，不要在普通表或生成代码里手补结构。

## 生成链路

### 客户端二进制链路

1. 读取 `GameConfig/luban.conf`
2. 加载 `Defines`、`__tables__.xlsx`、`__beans__.xlsx`、`__enums__.xlsx`
3. 通过 `gen_bin_client.bat` 调用 Luban，使用 `-c cs-bin -d bin`
4. 复制 `ConfigSystem.cs`、`ExternalTypeUtil.cs` 到 `GameUnity/Assets/Scripts/HotFix/GameProto/`
5. 生成配置代码到 `GameUnity/Assets/Scripts/HotFix/GameProto/LubanConfig/`
6. 生成二进制数据到 `GameUnity/Assets/BundleAssets/Configs/Binary/`

### 客户端 Json 链路

1. 通过 `gen_json_client.bat` 调用 Luban，使用 `-c cs-simple-json -d json2`
2. 同样复制 `ConfigSystem.cs`、`ExternalTypeUtil.cs`
3. 代码仍输出到 `GameUnity/Assets/Scripts/HotFix/GameProto/LubanConfig/`

### 重要约束

- `GameUnity/Assets/Scripts/HotFix/GameProto/LubanConfig/` 下的 `.cs` 不要手改。
- 它们是自动生成产物，下次生成会被覆盖。
- 调整生成结果应改源 Excel、schema、模板或脚本。

## 运行时加载

配置加载入口：`GameUnity/Assets/Scripts/HotFix/GameProto/ConfigSystem.cs`

### 当前加载流程

1. 通过 `ConfigSystem.Instance` 获取单例
2. 首次访问 `Tables` 时，若未初始化则自动 `Load()`
3. `Load()` 内创建 `new Tables(LoadByteBuf)`
4. `Tables` 在访问具体表时，通过 `LoadByteBuf` 按文件名加载配置

### 初始化时机

- `PreloadProcedure` 会将配置数据标记为 `PRELOAD`
- 预加载阶段会先将这些配置资源载入内存
- `ConfigSystem` 再基于已预加载资源完成读取

### 二进制加载方式

`LoadByteBuf(string file)` 当前逻辑：

- 通过 `ModuleSystem.GetModule<IResourceModule>()` 获取资源模块
- 调用 `m_resourceModule.LoadAsset<TextAsset>(file)` 加载资源
- 取 `TextAsset.bytes`
- 包装成 `new ByteBuf(bytes)`

### 运行时重载

当前支持：

```csharp
ConfigSystem.Instance.Reload();
```

已确认 `Reload()` 会在已初始化后调用 `m_tables.Reload()`，可统一触发配置表重载。

### 当前职责理解

- `ConfigSystem`：配置系统总入口
- `Tables`：Luban 自动生成的总表管理器
- `IResourceModule`：按名称加载配置资源
- `ByteBuf`：Luban 二进制读取容器

## 配置访问方式

### 常规访问

```csharp
using GameProto;

ItemConfig itemCfg = TbItemConfig.GetOrDefault(1001);
Tables tables = ConfigSystem.Instance.Tables;
ItemConfig itemCfg2 = tables.TbItemConfig.GetOrDefaultNoStatic(1001);

foreach (var cfg in TbItemConfig.DataList)
{
    DGameLog.Info(cfg.ItemName);
}
```

### `group_by`

```csharp
using GameProto;

var groupList = TbItemConfig.Instance.GetListByGroupID(groupId);
```

### `mode=one`

```csharp
using GameProto;

DGameLog.Info($"{TbGameConfig.DefaultAreaId}");
var gameConfig = TbGameConfig.Instance.Data;
```

### 推荐方式

业务层不要到处直接访问 `TbXXX` 或 `ConfigSystem.Instance.Tables`，优先再封一层配置管理器，例如：

- `ItemConfigMgr`
- `ModelConfigMgr`
- `SoundConfigMgr`
- `TextConfigMgr`

原因：

- 解耦业务代码和底层表结构
- 字段改名/结构调整时收敛修改面
- 便于做缓存、预处理、索引、默认值兜底和校验

## 使用原则

1. 先判断改的是源数据还是生成产物。
2. 优先改 Excel、schema、模板、生成脚本，不要直接改生成结果。
3. 涉及差异时先确认目标是 `client`、`server` 还是 `all`。
4. 涉及加载性能或按需加载时，先检查 `lazyload` 模板与脚本。
5. 任何生成链路改动都要说明是否需要重新跑 Luban。
6. 自动生成目录下的 `.cs` 不要手改。
7. 字段名和字段类型确定后不要轻易修改，这会影响热更包兼容性和历史数据兼容性。
8. 新增字段通常可前向兼容；删除字段通常风险更高，容易影响旧版本读取。
9. 测试时先在 Editor 模式完成加载验证和格式验证，再决定是否发布热更包。

## 新增配置表流程

1. 在 `GameConfig/Datas/` 新增业务 Excel。
2. 按约定填写前四行：`##var`、`##type`、`##group`、`##`。
3. 如需新复合类型，先补 `GameConfig/Datas/__beans__.xlsx`。
4. 如需新枚举，先补 `GameConfig/Datas/__enums__.xlsx`。
5. 在 `GameConfig/Datas/__tables__.xlsx` 新增表定义，至少补齐：
   - `name`
   - `value`
   - `mode`
   - `input`
   - `tags`（按需）
6. 如需按组访问，加 `group_by:xxx`。
7. 如是全局唯一配置，可考虑 `mode=one`。
8. 执行对应生成脚本：
   - 二进制流程：`GenerateTool_Binary`
   - Json 流程：`GenerateTool_Json`
   - 无特殊说明时优先二进制和 `lazyload`
9. 确认输出目录正确：
   - 代码：`GameUnity/Assets/Scripts/HotFix/GameProto/LubanConfig/`
   - 数据：`GameUnity/Assets/BundleAssets/Configs/Binary/`
10. 业务层优先补配置管理器，不要把 `TbXXX` 访问散落到各处。
11. 在对应流程或业务入口验证加载、常规读取、分组读取或单例读取是否正常。
