# Luban Schema 定义详解

## XML Schema 定义

### 文件基本结构

```xml
<?xml version="1.0" encoding="utf-8"?>
<module name="模块名">

    <!-- 模块导入（可选） -->
    <import name="other_module"/>

    <!-- 枚举定义 -->
    <enum name="EItemType" comment="物品类型">
        <var name="Weapon" alias="武器" value="0"/>
        <var name="Armor" alias="护甲" value="1"/>
    </enum>

    <!-- 结构体定义 -->
    <bean name="ItemBase" comment="物品基础">
        <var name="id" type="string" comment="ID"/>
        <var name="name" type="string" comment="名称"/>
    </bean>

    <!-- 表定义 -->
    <table name="TbItemConfig" value="ItemBase"
           input="道具配置表.xlsx" mode="map" index="id"/>

</module>
```

## 枚举定义 (enum)

### 基本语法

```xml
<enum name="EQuality" comment="品质" flags="false">
    <var name="White" alias="白" value="0"/>
    <var name="Green" alias="绿" value="1"/>
    <var name="Blue" alias="蓝" value="2"/>
</enum>

<!-- Flags 枚举（位运算） -->
<enum name="EEquipSlot" flags="true">
    <var name="Head" value="1"/>      <!-- 0b0001 -->
    <var name="Body" value="2"/>      <!-- 0b0010 -->
    <var name="Hand" value="4"/>      <!-- 0b0100 -->
</enum>
```

> **规范约束：flags 枚举的基础标志值通常使用 2 的幂次**（1, 2, 4, 8, 16, ...）。组合别名可以使用多个基础标志按位或后的结果，例如 `READ_WRITE = READ | WRITE`；只有把未定义组合值误当作独立基础标志时才会产生歧义。
>
> 反面教材：`WHITE=1, RED=2, GREEN=3` — `GREEN(3)` 等于 `WHITE|RED`，按位组合时产生歧义。

### 属性说明

| 属性 | 说明 |
|------|------|
| `name` | 枚举名 |
| `flags` | 是否为标志位枚举（对应 C# FlagsAttribute）|
| `unique` | 值是否唯一（用于 ID 类枚举）|
| `comment` | 注释 |
| `alias` | 别名（用于 Excel 显示）|

### 枚举值自动递增

```xml
<enum name="AutoIncrement">
    <var name="A" value="1"/>   <!-- 显式指定 1 -->
    <var name="B"/>             <!-- 自动 2 -->
    <var name="C"/>             <!-- 自动 3 -->
</enum>
```

## 结构体定义 (bean)

### 基础 bean

```xml
<bean name="ItemConfig" comment="物品配置">
    <var name="id" type="string" comment="ID"/>
    <var name="name" type="string" comment="名称"/>
    <var name="price" type="int#range=[0,)" comment="价格"/>
</bean>
```

### 继承（父 bean 自动为抽象类）

```xml
<bean name="WeaponConfig" parent="ItemConfig" comment="武器配置">
    <var name="damage" type="int" comment="伤害"/>
    <var name="attackSpeed" type="float" comment="攻速"/>
</bean>
```

### 多态 bean

Luban 原生多态通过 `abstract` + `parent` 继承实现。源数据的子类型标记由 loader 决定；DGame 的 `cs-bin` 运行时按二进制类型 ID 分发。

> **不要**手动添加 `type` 字段模拟多态——这无法利用 Luban 的自动分发和校验能力。

```xml
<!-- 抽象基类：不可直接实例化 -->
<bean name="Skill" abstract="true">
    <var name="skillId" type="int" comment="技能ID"/>
    <var name="skillName" type="string" comment="技能名称"/>
</bean>

<!-- 子类通过 parent 继承 -->
<bean name="AttackSkill" parent="Skill">
    <var name="skillHp" type="int" comment="伤害"/>
</bean>

<bean name="BuffSkill" parent="Skill">
    <var name="buffId" type="int" comment="Buff ID"/>
</bean>
```

`abstract="true"` 使基类在 C# 中生成为 `abstract class`，子类继承基类并添加自有字段。上述 Skill/AttackSkill/BuffSkill 是结构示例，并非当前项目已有配置类型。

#### 代码生成对照

当前客户端 `cs-bin/bean.sbn` 对抽象 Bean 生成以下分发逻辑。以下仅展示方法片段，类型 ID 常量及构造函数由导表生成：

```csharp
public static Skill DeserializeSkill(ByteBuf _buf)
{
    switch (_buf.ReadInt())
    {
        case AttackSkill.__ID__: return new AttackSkill(_buf);
        case BuffSkill.__ID__: return new BuffSkill(_buf);
        default: throw new SerializationException();
    }
}
```

JSON 源数据的多态标记须单独核对当前 loader；它与运行时 `ByteBuf.ReadInt()` 类型 ID 分发不是同一层。

### bean 属性说明

| 属性 | 说明 |
|------|------|
| `name` | bean 名 |
| `parent` | 父 bean 名（支持继承和多态）|
| `abstract` | 是否为抽象基类（不可实例化，仅作为多态父类）|
| `valueType` | 是否为值类型（C# struct）|
| `sep` | 分隔符，用于紧凑格式数据 |
| `group` | 分组控制 |

> **valueType 使用指导：** 小型不可变数据（坐标 vector2/3/4、颜色、矩形等）使用 `valueType="1"` 生成 C# `struct`；有引用语义、需要继承或多态的 bean 使用默认 class。

## 字段定义 (var)

### 基本字段

```xml
<var name="id" type="int" comment="ID"/>
<var name="name" type="string" comment="名称"/>
<var name="quality" type="Quality" comment="品质"/>
```

### 带校验器的字段

```xml
<var name="level" type="int#range=[1,100]" comment="等级"/>
<var name="itemId" type="int#ref=item.TbItemConfig" comment="物品ID"/>
<var name="count" type="int#!" comment="数量（不能为0）"/>
```

### 字段属性

| 属性 | 说明 |
|------|------|
| `name` | 字段名 |
| `type` | 字段类型 |
| `comment` | 注释 |
| `groups` | 导出分组 |
| `variants` | 变体列表 |

## 表定义 (table)

### Map 表

```xml
<table name="TbItemConfig" value="ItemConfig"
       input="道具配置表.xlsx" mode="map" index="id"/>
```

### List 表

```xml
<table name="TbDropTable" value="DropItem"
       input="drop/*.csv" mode="list"/>
```

### 单例表

```xml
<table name="TbGlobalConfig" value="GlobalConfig"
       input="GlobalConfig.json" mode="one"/>
```

### 表属性说明

| 属性 | 必填 | 说明 |
|------|------|------|
| `name` | 是 | 生成的表类名，推荐 `TbXxx` 格式 |
| `value` | 是 | value_type，即 bean 名 |
| `input` | 是 | 数据文件路径，支持通配符 `*` |
| `mode` | 否 | 容器类型：`map`/`list`/`one`，默认 `map` |
| `index` | 否 | map 模式的主键字段名 |
| `output` | 否 | 输出文件名 |
| `readSchemaFromFile` | 否 | 从数据文件读取 schema |

### Mode 说明

| 模式 | 容器与索引语义 |
|------|----------------|
| `map` | 多条记录，按主键索引 |
| `list` | 多条记录组成列表，可按表定义配置索引 |
| `one` | 单条配置记录 |

`mode` 不规定源文件的 JSON 根结构，也不规定导出数据的格式：

- **输入形状**由数据源 loader 和 `input` 选择器决定。当前 JSON loader 使用 `*@rows.json` 读取多记录数组，普通 `rows.json` 按单记录读取；`mode=map` 不意味着输入必须是“主键到记录”的 JSON 对象。具体说明见 [数据源格式](data-sources.md)。
- **Excel 输入**按当前 Sheet 的结构标记和记录组织方式解析，不能套用 JSON 根节点示例。
- **生成与导出**由代码目标、数据目标及模板决定。DGame 默认使用 `cs-bin` 生成通过 `ByteBuf` 读取二进制的客户端代码，同时导出 `bin/json` 数据；额外导出的 JSON 不会把客户端消费链路变成 JSON loader，也不能直接作为 JSON 源输入形状的依据。

### 索引类型

```xml
<!-- 单字段索引 -->
<table index="id" mode="map"/>

<!-- 联合索引（组合键） -->
<table index="key1+key2+key3" mode="list"/>

<!-- 独立索引（多个索引） -->
<table index="key1,key2,key3" mode="list"/>
```

## 类型引用

### 同模块引用

```xml
<var name="type" type="EItemType"/>
<var name="weapon" type="WeaponConfig"/>
```

### 跨模块引用

```xml
<var name="monsterId" type="string#ref=monster.TbMonster"/>
<var name="weapon" type="item.WeaponConfig"/>
```

## refgroup 定义

```xml
<refgroup name="EquipTables">
    item.TbWeapon,item.TbArmor,item.TbAccessory
</refgroup>

<var name="equipId" type="string#ref=EquipTables"/>
```

## constalias 定义

`<constalias>` 是 `<module>` 的直接子元素，与 `<enum>`/`<bean>`/`<table>` 同级：

```xml
<module name="">
    <constalias name="MAX_LEVEL" value="99"/>
    <constalias name="BOSS_TAG" value="1001"/>
</module>
```

数据文件中使用别名代替数值：`MAX_LEVEL` → `99`。仅 Excel/lite 数据源支持。

## Excel Schema 定义

### __enums__.xlsx

```
enum_name | isFlags | comment
Quality   | false   | 品质

enum_name | item_name | item_value | item_alias
Quality   | WHITE     | 0          | 白
Quality   | GREEN     | 1          | 绿
```

### __beans__.xlsx

```
bean_name | parent | comment
Item      |        | 物品

bean_name | field_name | field_type | field_comment
Item      | id         | int        | ID
Item      | name       | string     | 名称
```

### __tables__.xlsx

```
table_name | value_type | input          | mode | index
TbItemConfig     | Item       | 道具配置表.xlsx      | map  | id
TbQuest    | Quest      | Quest.xlsx     | list |
```
