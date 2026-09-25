# DGame Luban 运行时加载

## 当前链路

DGame 客户端配置运行时入口：

```text
GameUnity/Assets/Scripts/HotFix/GameProto/ConfigSystem.cs
```

实际流程：

1. 业务通过 `GameLogic/ConfigMgr/XxxConfigMgr` 访问配置。
2. `XxxConfigMgr` 内部封装 `GameProto.TbXXX`。
3. `TbXXX.Instance` 首次访问 `ConfigSystem.Instance.Tables`。
4. `ConfigSystem.Load()` 构造 `new Tables(LoadByteBuf)`。
5. `Tables` 按需加载具体表，调用 `LoadByteBuf(file)`。
6. `LoadByteBuf` 通过 `IResourceModule.LoadAsset<TextAsset>(file)` 读取 bytes。
7. bytes 包装为 `Luban.ByteBuf` 后反序列化。

当前实现要点：

```csharp
using Luban;
using DGame;
using UnityEngine;

namespace GameProto
{
    public class ConfigSystem
    {
        public static ConfigSystem Instance => ...;
        public Tables Tables { get { if (!m_init) Load(); return m_tables; } }

        public void Load()
        {
            m_tables = new Tables(LoadByteBuf);
            m_init = true;
        }

        private ByteBuf LoadByteBuf(string file)
        {
            if (m_resourceModule == null)
            {
                m_resourceModule = ModuleSystem.GetModule<IResourceModule>();
            }
            TextAsset textAsset = m_resourceModule.LoadAsset<TextAsset>(file);
            byte[] bytes = textAsset.bytes;
            m_resourceModule.UnloadAsset(textAsset);
            return new ByteBuf(bytes);
        }
    }
}
```

## 业务访问方式

业务层不要散落访问 `TbXXX`，优先走 `GameLogic/ConfigMgr/`：

```csharp
using GameLogic;

var modelCfg = ModelConfigMgr.Instance.GetOrDefault(modelId);
var hasModel = ModelConfigMgr.Instance.ContainsKey(modelId);
var ok = SoundConfigMgr.Instance.TryGetValue(soundId, out var soundCfg);
```

`TbXXX` 只在 `XxxConfigMgr` 内部封装：

```csharp
using GameProto;

namespace GameLogic
{
    public class ModelConfigMgr : Singleton<ModelConfigMgr>
    {
        public ModelConfig GetOrDefault(int modelID) => TbModelConfig.GetOrDefault(modelID);
        public bool TryGetValue(int modelID, out ModelConfig cfg) => TbModelConfig.TryGetValue(modelID, out cfg);
        public bool ContainsKey(int modelID) => TbModelConfig.ContainsKey(modelID);
    }
}
```

## 重载

当前支持：

```csharp
ConfigSystem.Instance.Reload();
```

`Reload()` 在已初始化后调用 `m_tables.Reload()`。

## 类型映射

DGame 的外部类型映射位于：

```text
GameConfig/Defines/builtin.xml
GameUnity/Assets/Scripts/HotFix/GameProto/ExternalTypeUtil.cs
```

运行时类型以 `GameConfig/Defines/builtin.xml` 的 mapper 和最终生成代码为准，不能只看 `ExternalTypeUtil` 中是否存在同名方法。

当前确认类型：

- `vector2` -> `UnityEngine.Vector2`
- `vector3` -> `UnityEngine.Vector3`
- `vector4` -> `UnityEngine.Vector4`
- `vector2int` -> `UnityEngine.Vector2Int`
- `vector3int` -> `UnityEngine.Vector3Int`，使用 `ExternalTypeUtil.NewVector3Int`。

示例：

```xml
<bean name="vector3" valueType="1" sep=",">
    <var name="x" type="float" />
    <var name="y" type="float" />
    <var name="z" type="float" />
    <mapper target="client" codeTarget="cs-bin,cs-simple-json,cs-newtonsoft-json">
        <option name="type" value="UnityEngine.Vector3" />
        <option name="constructor" value="ExternalTypeUtil.NewVector3" />
    </mapper>
</bean>
```

## 资源约束

- 运行时配置读取走 `IResourceModule`，不要新增 `Resources.Load` 或文件直读。
- 自动生成代码位于 `GameUnity/Assets/Scripts/HotFix/GameProto/LubanConfig/`，不要手改。
- 调整运行时加载逻辑时，优先改 `GameConfig/CustomTemplate/`，再重新导表。
