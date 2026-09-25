# Luban 配置

> **适用场景**：DGame 业务代码读取配置、ConfigSystem、ConfigMgr 封装 | **配置编辑**：Excel、Defines、导表脚本必须使用 `luban-dev`

## 目录

| 路径 | 职责 |
|------|------|
| `GameConfig` | Luban 配置工程、Excel、Defines、导表脚本 |
| `GameUnity/Assets/Scripts/HotFix/GameProto` | `ConfigSystem`、`ExternalTypeUtil`、`LubanConfig/` 生成代码 |
| `GameUnity/Assets/Scripts/HotFix/GameLogic/ConfigMgr` | 业务配置封装 |
| `GameUnity/Assets/Scripts/HotFix/GameLogic/Module/TextModule` | 文本配置消费 |

## 配置工程结构

DGame 的 Luban 工程在 `GameConfig/`，消费侧排查配置问题时至少确认这几处：

| 项 | DGame 实际取值 |
|----|----------------|
| `luban.conf` groups | `c`、`s`、`e` |
| `luban.conf` topModule | `GameProto`，生成 `GameProto.Tables`、`GameProto.TbXxx` |
| Excel 数据源 | `GameConfig/Datas/*.xlsx`，业务表使用中文名，如 `道具配置表.xlsx`、`文本配置表.xlsx` |
| 表/Bean/Enum 定义 | `GameConfig/Datas/Defines/__tables__.xlsx`、`__beans__.xlsx`、`__enums__.xlsx` |
| Schema Defines | `GameConfig/Defines/`，并由 `luban.conf` 的 `schemaFiles` 引入 |
| 客户端生成脚本 | `GameConfig/GenerateTool_Binary/gen_bin_client_lazyload.bat`（推荐），路径变量在 `path_define.bat` |
| 生成代码产物 | `GameUnity/Assets/Scripts/HotFix/GameProto/LubanConfig/` |
| 二进制配置产物 | `GameUnity/Assets/BundleAssets/Configs/Binary/` |

配置编辑、字段增删、导表脚本调用仍交给 `luban-dev`；本文件只说明 DGame 业务代码如何消费这些产物。

## 运行时入口

```csharp
var tables = GameProto.ConfigSystem.Instance.Tables;
var item = GameProto.TbItemConfig.GetOrDefault(1001);
var allItems = GameProto.TbItemConfig.DataList;
```

`ConfigSystem` 懒加载 `Tables`：

```csharp
m_tables = new Tables(LoadByteBuf);
```

当前 `LoadByteBuf` 通过 `IResourceModule.LoadAsset<TextAsset>(file)` 读取二进制配置。

`ConfigSystem` 关键点：

- 使用 `ModuleSystem.GetModule<IResourceModule>()`（非 `GameModule.Resource`），因为它在 GameProto 程序集、不依赖 GameLogic。
- `m_resourceModule` 延迟获取并缓存，避免构造时模块未就绪。

条件查询可直接在 `DataList` 上过滤；注意区分是否产生 GC：

```csharp
// 初始化/低频场景：LINQ 可读性好，一次性构建缓存可接受
var swords = TbItemConfig.DataList.Where(i => i.Type == ItemType.Sword).ToList();

// 高频/每帧路径：避免 Where/Select/ToList 的委托与中间集合分配，改手写 for
foreach (var cfg in TbItemConfig.DataList)
{
    if (cfg.Type != ItemType.Sword) continue;
    // ...
}
```

`Count`/`Contains(key)` 等不分配的 LINQ 鼓励使用；`Where/Select/OrderBy/ToList` 会产生委托和中间集合，热路径改手写循环或预先缓存结果。

## 懒加载时机

`ConfigSystem.Instance` 只创建系统对象；第一次访问 `ConfigSystem.Instance.Tables` 时才会执行 `Load()`：

```csharp
public Tables Tables
{
    get
    {
        if (!m_init)
        {
            Load();
        }

        return m_tables;
    }
}
```

这意味着：

- 首次读表前，`IResourceModule` 必须已经注册并可同步 `LoadAsset<TextAsset>(file)`。
- 不要在热更程序集静态字段初始化、`GameStart.Entrance` 之前或资源模块未就绪时读取 `TbXxxConfig.Instance`。
- `TbXxxConfig.Instance` 这类生成表单例会间接访问 `ConfigSystem.Instance.Tables.TbXxxConfig`，同样会触发懒加载。
- `Reload()` 只有在 `m_init == true` 后才会调用 `m_tables.Reload()`；未初始化时调用不会主动加载。

## 业务封装

已有封装示例：

```csharp
SoundConfigMgr.Instance.GetOrDefault(soundId);
ModelConfigMgr.Instance.TryGetValue(modelID, out var cfg);
TextConfigMgr.Instance.GetTextConfig(id);
```

新增复杂查询优先封装在 ConfigMgr 或业务模块里，不让 UI 到处写复杂表查询。

## 边界

- 表结构、字段、枚举、Bean、Excel 数据、导表：使用 `luban-dev`。
- 业务如何消费配置、封装缓存、在 UI 或模块中使用：使用本文件。
- 不手改 `GameProto/LubanConfig/*.cs` 生成代码。

## 常见错误

| 错误 | 正确做法 |
|------|---------|
| 手改 `LubanConfig/*.cs` | 修改 Excel/Defines 后导表 |
| 每次查询都 `new Tables(...)` | 使用 `ConfigSystem.Instance.Tables` 或 `TbXxxConfig` |
| 静态字段过早读取 `TbXxxConfig.Instance` | 改到模块初始化后或首次业务使用时读取 |
| UI 中散落复杂表过滤 | 放入 ConfigMgr |
| 用本 skill 改 Excel | 改配置表用 `luban-dev` |
| `.bytes` 加载失败 | 检查 YooAsset 收集器是否收集 `BundleAssets/Configs/Binary/` |
| `Tables`/资源为 null | 确认 PRELOAD 后资源模块就绪，首次访问才触发懒加载 |

## 交叉引用

- 项目结构与程序集划分见 [architecture.md](architecture.md)
- 资源加载见 [resource-api.md](resource-api.md)
- 问题排查见 [troubleshooting.md](troubleshooting.md)
