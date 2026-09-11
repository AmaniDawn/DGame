# DGame Luban 集成速查

## 目录

```text
GameConfig/
├── Datas/                  # Excel 源数据、__tables__/__beans__/__enums__
├── Defines/                # Luban schema 与外部类型映射
├── CustomTemplate/         # ConfigSystem、ExternalTypeUtil、LazyLoad 模板
├── GenerateTool_Binary/    # bin/json 导表脚本
├── Tools/                  # Luban 工具与 split_sheets.py
└── luban.conf
```

Unity 输出：

```text
GameUnity/Assets/Scripts/HotFix/GameProto/
├── ConfigSystem.cs
├── ExternalTypeUtil.cs
└── LubanConfig/            # 自动生成代码，不手改

GameUnity/Assets/BundleAssets/Configs/Binary/  # 二进制配置数据
GameUnity/Configs/Json/                         # Windows 脚本 Json 输出
GameUnity/Json/                                 # 当前 shell 脚本 Json 输出
GameUnity/Assets/Scripts/HotFix/GameLogic/ConfigMgr/  # 业务配置管理器
```

## luban.conf

- `groups`：`c`、`s`、`e`，当前均为默认组。
- `schemaFiles`：`Defines`、`Datas/__tables__.xlsx`、`Datas/__beans__.xlsx`、`Datas/__enums__.xlsx`。
- `dataDir`：`Datas`。
- `targets`：`server`、`client`、`all`。
- 客户端 target：`manager=Tables`、`groups=["c"]`、`topModule=GameProto`。

## 导表脚本

优先使用客户端 LazyLoad：

```powershell
cmd /c "set AI_MODE=1 && GameConfig\GenerateTool_Binary\gen_bin_client_lazyload.bat"
```

脚本行为：

1. 复制 `CustomTemplate\Client\Bin\ConfigSystem.cs` 和 `ExternalTypeUtil.cs` 到 `GameProto/`。
2. 当前默认脚本中的 `Tools\split_sheets.py` 前置/后置调用已注释；需要使用 Sheet 级 `#xxx-xxx` 拆表时，先手动执行该脚本，导表后再执行清理。
3. 调用 Luban：`-t client -c cs-bin -d bin -d json`。
4. 使用 `CustomTemplate\Client\CustomTemplate_Client_LazyLoad`。
5. 输出代码到 `GameProto/LubanConfig/`，输出数据到 `path_define.*` 指定的 Binary/Json 目录。
6. 清理临时拆分文件。

其他脚本：

- `gen_bin_client.bat/.sh`：客户端非 LazyLoad。
- `gen_bin_server.bat/.sh`：服务端。
- `gen_bin_all.bat/.sh`：全量。
- `*_lazyload.*`：对应 LazyLoad 模板。

## 运行时加载

`ConfigSystem` 位于 `GameUnity/Assets/Scripts/HotFix/GameProto/ConfigSystem.cs`。

- `ConfigSystem.Instance.Tables` 首次访问时自动 `Load()`。
- `Load()` 构造 `new Tables(LoadByteBuf)`。
- `LoadByteBuf(string file)` 通过 `IResourceModule.LoadAsset<TextAsset>(file)` 读取 bytes，并包装为 `Luban.ByteBuf`。
- `Reload()` 在已初始化后调用 `m_tables.Reload()`。

注意：当前实现通过 `ModuleSystem.GetModule<IResourceModule>()` 取资源模块；若项目规范和实际代码不同，先信任实际代码并标注差异。

## Excel 结构

普通表前四行：

1. `##var`：字段名
2. `##type`：字段类型
3. `##group`：导出分组
4. `##`：字段说明

数据从第 5 行开始。常用分组：

- `c`：客户端
- `s`：服务端
- `e`：扩展分组，按项目约定使用

## 表定义

DGame 当前支持三种表来源；默认仍推荐 `__tables__.xlsx` 显式注册：

1. `__tables__.xlsx` 显式注册：推荐方式，适合需要 `mode`、`index`、`tags` 等明确控制的表。
2. 文件级自动导入：`GameConfig/Datas/#<value_type>-<comment>.xlsx`，Luban 自动生成 `Tb<value_type>`，不需要注册；DGame 支持但不默认推荐。
3. Sheet 级拆表扩展：任意业务 Excel 中的 sheet 命名为 `#<value_type>-<comment>`，手动执行 `GameConfig/Tools/split_sheets.py` 后会生成临时 `#<value_type>-<comment>.xlsx`，再交给 Luban 自动导入；当前默认导表脚本不会自动调用该拆表步骤，DGame 支持但仍按自动导入场景谨慎使用。

`__tables__.xlsx` 常用字段：

- `full_name`：表类名，如 `TbItemConfig`
- `value_type`：行类型，如 `ItemConfig`
- `input`：业务 Excel 文件
- `index`：主键字段
- `mode`：`map`、`one`、`list` 等
- `tags`：额外行为，如 `group_by:GroupID`

`group_by` 优先写在 `__tables__.xlsx` 的 `tags` 里，不要手改生成代码补索引。

### 按 GroupId 快速访问

在需要按组查询的表对应的 `__tables__.xlsx` 行，将 `tags` 设置为 `group_by:GroupId`（字段名必须与业务表中的字段完全一致）。导表后会生成按组索引，例如：

```csharp
var steps = TbGuideStepConfig.GetListByGroupId(groupId);
var groups = TbGuideStepConfig.GroupedDataMap;
```

生成代码中对应 `_groupedDataMap`、`GroupedDataMap` 和 `GetListByGroupId(int)`，不要手动修改 `GameProto` 生成文件。

### `read_schema_from_file` 注意事项

`__tables__.xlsx` 的 `read_schema_from_file` 必须根据表结构配置为正确的 `true` 或 `false`：`true` 从业务 Excel 的 `##var/##type/##group` 表头读取字段定义，`false` 使用项目 Defines/schema 中的定义。配置错误会导致 schema 读取失败或导表失败；修改后应先校验 `__tables__.xlsx`，再运行导表脚本。

自动导入命名规则：

- 文件：`#SkillCfg-技能表.xlsx` -> `value_type=SkillCfg` -> 生成 `TbSkillCfg`
- Sheet：`#SkillCfg-技能表` -> 导表前拆成临时 `#SkillCfg-技能表.xlsx`
- 若 Datas 根目录已有同名 `#value_type*.xlsx`，Sheet 拆分会跳过，避免重复成表。

## 业务封装

业务层优先通过 `GameLogic/ConfigMgr/XxxConfigMgr` 访问配置；`TbXXX` 只放在管理器内部封装：

```csharp
using GameProto;

namespace GameLogic
{
    public sealed class ItemConfigMgr : Singleton<ItemConfigMgr>
    {
        public ItemConfig GetOrDefault(int itemId) => TbItemConfig.GetOrDefault(itemId);

        public bool TryGetValue(int itemId, out ItemConfig cfg) => TbItemConfig.TryGetValue(itemId, out cfg);

        public bool ContainsKey(int itemId) => TbItemConfig.ContainsKey(itemId);
    }
}
```

这样可以收口默认值、索引、兼容字段和表结构变化。

## 兼容性注意事项

配置结构改动会同时影响热更代码（`GameProto/LubanConfig/` 由 HybridCLR 热更程序集加载）与热更资源（`Binary/` 二进制数据由 YooAsset 分发），改字段前先评估兼容性：

- **新增字段**：前向兼容，旧数据缺列时取类型默认值，旧代码忽略新字段。
- **删除字段**：不兼容，仍引用该字段的代码会编译报错，旧二进制数据需重新导出。
- **修改字段类型**：不兼容，需同步改数据、重新导表、更新引用代码。
- **重命名字段/表**：不兼容，等价于删除加新增，会影响热更包，需代码与数据同版本更新。
- **代码与数据必须同版本发布**：`GameProto/LubanConfig/` 生成代码与 `Binary/` 数据是一对，只热更其一会导致反序列化错乱。
- 生成代码（`GameProto/LubanConfig/`）不要手改，下次导表会覆盖；`ConfigSystem.cs`、`ExternalTypeUtil.cs` 由 `CustomTemplate/` 模板复制而来，改动应落在模板而非 `GameProto/` 目录。
