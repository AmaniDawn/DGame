# Luban 验证器完整说明

## 1. notDefaultValue (!)

防止字段使用默认值。

### 语法

在类型后添加 `!`：

```xml
<var name="id" type="int!"/>
<var name="name" type="string!"/>
<var name="optional" type="int?!"/>
```

### 验证规则

| 类型 | 禁止值 |
|------|--------|
| `bool` | - |
| `int`/`long`/`short`/`byte` | `0` |
| `float`/`double` | `0.0` |
| `string` | `""` |
| 可空类型 `T?` | `null` |
| 容器类型 | 空容器 |

### 容器元素验证

```xml
<var name="ids" type="list,int!"/>      <!-- 列表元素不能为 0 -->
<var name="names" type="list,string!"/>  <!-- 列表元素不能为空字符串 -->
```

---

## 2. ref (外键引用)

验证表引用合法性，是 Luban 最强大的功能之一。

### 基础语法

```xml
<!-- Map 表引用 -->
<var name="itemId" type="string#ref=TbItemConfig"/>

<!-- List 表引用（需要指定 key 字段） -->
<var name="dropId" type="int#ref=id@drop.TbDropTable"/>

<!-- 可选引用 -->
<var name="iconId" type="string#ref=gen.TbResourceCfg?"/>
```

### 跨模块引用

```xml
<var name="monsterId" type="string#ref=monster.TbMonsterConfig"/>
```

### 多表引用

```xml
<var name="targetId" type="string#ref=TbItemConfig,item.TbEquip,item.TbMaterial"/>
```

### refgroup 引用组

先在 luban.conf 或 XML 中定义引用组：

```xml
<refgroup name="AllItems">
    item.TbWeapon,item.TbArmor,item.TbMaterial
</refgroup>
```

然后使用：

```xml
<var name="itemId" type="string#ref=AllItems"/>
```

### 代码生成

- 单值 ref：字段 `xxx` → 生成 `xxx_Ref`（如 `itemId` → `itemId_Ref`）
- 数组/列表 ref：字段 `xxx` → 生成 `xxx_Ref` 数组（如 `testIds:int[]` → `testIds_Ref:TestA[]`）
- 多表 ref 不生成 Ref 字段

### ResolveRef 生命周期

标准模式在 `Tables.Init()` 中先加载所有表，再逐表调用 `ResolveRef(this)`。默认 LazyLoad 模式则在表属性首次访问、完成该表实例赋值后调用；不能把两种模式都描述成构造函数加载全表。

```csharp
// 当前 LazyLoad Tables 中的属性片段。
public TbItemConfig TbItemConfig
{
    get
    {
        if (m_TbItemConfig == null)
        {
            m_TbItemConfig = new TbItemConfig(m_defaultLoader("tbitemconfig"));
            m_TbItemConfig.ResolveRef(this);
        }
        return m_TbItemConfig;
    }
}
```

这是 DGame 当前 `cs-bin` 运行时示意；JSON loader 的 `JsonElement` 签名只适用于单独配置的 JSON 目标，不属于默认客户端消费链路。

### 找不到引用时的行为

- **导出时**：Luban 校验 ref 合法性，找不到目标记录会报错
- **运行时**：`GetOrDefault` 返回 null（不抛异常），Ref 字段为 null

---

## 3. path (资源路径)

验证文件或资源路径存在性。

### 子类型

| 类型 | 说明 | 额外参数 |
|------|------|----------|
| `normal` | 普通路径验证 | `pattern` |
| `unity` | Unity Addressable | - |
| `ue` | UE4/UE5 资源 | - |
| `godot` | Godot 资源 | - |

### 基础用法

```xml
<var name="iconPath" type="string#path=normal;Sprites/Icons/*.png"/>
```

`*` 会被替换为字段值。

### Unity 路径

```xml
<var name="prefabPath" type="string#path=unity"/>
```

验证 Addressable 系统中的资源。

### UE 路径

```xml
<var name="modelPath" type="string#path=ue"/>
```

自动检查 `.uasset` 或 `.umap` 扩展名。

### 命令行配置

```bash
-x pathValidator.rootDir=GameUnity/Assets/BundleAssets
```

---

## 4. range (数值范围)

验证数值在指定范围内。

### 语法

```xml
<var name="level" type="int#range=[1,99]"/>
<var name="rate" type="float#range=[0,1]"/>
```

### 区间类型

| 语法 | 数学表示 | 示例 |
|------|----------|------|
| `[a,b]` | a ≤ x ≤ b | `[1,10]` = 1 到 10 |
| `(a,b)` | a < x < b | `(0,1)` = 0 到 1 之间 |
| `[a,b)` | a ≤ x < b | `[0,100)` = 0 到 99 |
| `(a,b]` | a < x ≤ b | `(0,100]` = 1 到 100 |
| `[a,)` | x ≥ a | `[0,)` = 非负数 |
| `(,b]` | x ≤ b | `(,100]` = 最大 100 |

### 固定值

```xml
<var name="fixed" type="int#range=10"/>  <!-- 必须是 10 -->
```

---

## 5. size (容器大小)

验证容器元素数量。

### 语法

必须将容器用括号包裹：

```xml
<!-- 固定大小 -->
<var name="slots" type="(list#size=4),string"/>

<!-- 范围限制 -->
<var name="skills" type="(list#size=[1,4]),int"/>

<!-- Map 大小 -->
<var name="attrs" type="(map#size=[3,6]),string,int"/>
```

### 区间语法

同 `range`：`=n`、 `[a,b]`、`[a,)` 等

---

## 6. set (值集合)

验证值在指定集合中。

### 基础用法

```xml
<var name="rarity" type="int#set=1;2;3"/>
<var name="slot" type="EEquipSlot#set=Head;Body"/>
```

### 包含逗号的值

使用括号包裹：

```xml
<var name="coord" type="(set=1,2,3),int"/>
```

### 支持的类型

- `int`、`long`、`float`、`double`
- `string`
- `enum`
- 以上类型的容器类型

---

## 7. index (列表索引)

要求列表按指定字段唯一索引，生成辅助 Dictionary。

### 语法

```xml
<var name="items" type="(list#index=id),ItemData"/>
```

### 代码生成

生成 `Items_id` Dictionary（C#）：

```csharp
public List<ItemData> Items { get; }
public Dictionary<string, ItemData> Items_id { get; }  // 额外生成
```

---

## 组合使用

多个验证器可以组合：

```xml
<!-- 范围 + 非默认 -->
<var name="level" type="int#range=[1,99]!"/>

<!-- 列表大小 + 元素非默认 -->
<var name="ids" type="(list#size=[1,4]),int!"/>

<!-- 引用 + 非默认 -->
<var name="itemId" type="string#ref=TbItemConfig!"/>
```

## 跳过验证

使用 `unchecked` tag 跳过记录验证：

```csv
tag,id,name
unchecked,test_001,临时数据
```
