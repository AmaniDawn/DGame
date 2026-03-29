# DGame 帧同步定点基础指南

当需求涉及 `GameBattle` 中的定点数学、定点物理、确定性随机数或 `FixedPointPhysics` 的使用方式时，先阅读本文件。

这份文档只讨论帧同步开发会用到的基础构件，不讨论输入缓存、回滚、预测纠正或联网锁步流程本身。目标很明确：先把逻辑真值世界收紧，再谈帧同步系统如何组织。

## 目录导航

- [DGame 帧同步定点基础指南](#dgame-帧同步定点基础指南)
  - [目录导航](#目录导航)
  - [当前仓库里的直接锚点](#当前仓库里的直接锚点)
  - [一张图先看全景](#一张图先看全景)
  - [定点数学](#定点数学)
    - [`FixedPoint64` 标量](#fixedpoint64-标量)
    - [向量、旋转与矩阵](#向量旋转与矩阵)
    - [建模原则](#建模原则)
  - [定点物理](#定点物理)
    - [逻辑空间入口 `FPTransform`](#逻辑空间入口-fptransform)
    - [碰撞体体系 `FPCollider`](#碰撞体体系-fpcollider)
    - [物理查询 `FPPhysics`](#物理查询-fpphysics)
    - [动态体 `FPCharacterController` 与 `FPRigidbody`](#动态体-fpcharactercontroller-与-fprigidbody)
    - [逻辑层与表现层的边界](#逻辑层与表现层的边界)
  - [确定性随机数](#确定性随机数)
  - [推荐用法速查](#推荐用法速查)
  - [不要这样做](#不要这样做)
  - [开发前自检](#开发前自检)

## 当前仓库里的直接锚点

如果想快速确认这份文档是不是贴合当前仓库，可以先看下面这些文件：

- `GameUnity/Assets/DGame/FixedPointPhysics/Math/FixedPointInt64.cs`
  `FixedPoint64` 的实际实现文件在这里，文件名不是 `FixedPoint64.cs`。
- `GameUnity/Assets/DGame/FixedPointPhysics/Math/FixedPointMath.cs`
  定点数学公共入口在这里。
- `GameUnity/Assets/DGame/FixedPointPhysics/Core/FPTransform.cs`
  逻辑空间入口在这里，位置、旋转、父子层级坐标换算都围绕它展开。
- `GameUnity/Assets/DGame/FixedPointPhysics/Core/FPPhysics.cs`
  公共查询入口在这里，同时也能直接看到 `Raycast(...)` 的当前实现边界。
- `GameUnity/Assets/DGame/FixedPointPhysics/Math/FixedPointRandom.cs`
  确定性随机数在这里，同时也能看到无参构造和 `New()` 会使用时间种子。
- `GameUnity/Assets/Scripts/HotFix/GameBattle/Unit/Model/LogicUnit.cs`
  当前 `GameBattle` 的单位逻辑真值在这里。
- `GameUnity/Assets/Scripts/HotFix/GameBattle/Unit/Component/UnitStateSyncComponent.cs`
  连续状态同步和版本号边界在这里。
- `GameUnity/Assets/Scripts/HotFix/GameBattle/Battle/Interface/IRenderUnit.cs`
  表现层消费逻辑结果的接口边界在这里。

## 一张图先看全景

在 DGame 的帧同步语境里，推荐把基础能力按下面顺序理解：

```text
FixedPoint64 / FixedPointMath
    ↓
FixedPointVector2 / FixedPointVector3 / FixedPointQuaternion / FixedPointMatrix
    ↓
FPTransform / FPCollider / FPPhysics / FPCharacterController / FPRigidbody
    ↓
Battle / Unit / Skill / Buff 等战斗逻辑
    ↓
RenderUnit / UI / Animator / VFX / Audio 等表现层
```

这里有一个必须始终成立的前提：

- 逻辑真值只存在于上半部分的定点世界。
- 下半部分的表现层只负责显示，不负责产出逻辑结论。

如果一段战斗逻辑会影响：

- 位置
- 旋转
- 朝向
- 速度
- 碰撞
- 距离
- 随机结果

那么它应该先在 `FixedPointPhysics` 中完成逻辑建模，再由逻辑结果驱动表现；不要让 Unity 的浮点 API 反向决定逻辑结论。

这里也要补一句边界说明：

- 这套约束针对 `GameBattle` 的逻辑真值链路。
- `FixedPointPhysics` 目录里仍然存在编辑器、Gizmo、表现辅助、相机辅助等脚本，它们可以使用 Unity 浮点 API，但不能反向成为战斗逻辑真值来源。
- 诸如 `AsFloat()`、`ToVector3()` 这类转换只应出现在渲染、调试、编辑器边界，不要把转换后的浮点值再写回逻辑真值。

## 定点数学

### `FixedPoint64` 标量

帧同步逻辑中的基础标量，优先使用：

- `FixedPoint64`

它适合表达：

- 位置坐标
- 速度与加速度
- 技能距离
- 判定半径
- 伤害系数
- 持续时间
- 旋转速度
- 任意会进入逻辑比较与逻辑结算的数值

配套数学入口是：

- `FixedPointMath`

常见能力包括：

- `Clamp`
- `Min` / `Max`
- `Abs`
- `Sqrt`
- `Sin` / `Cos` / `Tan`
- `Asin` / `Acos` / `Atan` / `Atan2`
- `Ceiling`
- `Round`
- `Floor`
- `Lerp`

推荐把它们理解成一句话：

> 逻辑层所有会影响结论的数值运算，优先在 `FixedPoint64 + FixedPointMath` 里闭环完成。

常见写法：

```csharp
FixedPoint64 moveSpeed = 5;
FixedPoint64 delta = moveSpeed * tickDelta;
FixedPoint64 clamped = FixedPointMath.Clamp(delta, 0, 1);
```

不要把下面这些值长期保存为逻辑真值：

- `float`
- `double`
- `decimal`

配置、协议或编辑器输入如果天然不是定点类型，也应该在进入战斗逻辑边界时尽快转换，而不是一层层把浮点值往下传。

### 向量、旋转与矩阵

定点空间表达优先使用：

- `FixedPointVector2`
- `FixedPointVector3`
- `FixedPointQuaternion`
- `FixedPointMatrix`

可以把它们的职责先这样记：

| 类型 | 主要用途 |
| --- | --- |
| `FixedPointVector2` | 2D 平面方向、输入向量、偏移 |
| `FixedPointVector3` | 3D 位置、方向、速度、位移 |
| `FixedPointQuaternion` | 朝向、旋转、转向 |
| `FixedPointMatrix` | 方向基、局部空间变换、碰撞朝向计算 |

推荐用法：

- 用 `FixedPointVector3` 保存逻辑位置、方向、速度
- 用 `FixedPointQuaternion` 保存逻辑朝向
- 用 `FixedPointQuaternion.LookRotation(...)` 生成目标朝向
- 用 `FixedPointQuaternion.RotateTowards(...)` 做固定角速度转向

示意写法：

```csharp
FixedPointVector3 dir = (targetPos - selfPos).normalized;
FixedPointQuaternion targetRot = FixedPointQuaternion.LookRotation(dir);
self.transform.rotation = FixedPointQuaternion.RotateTowards(
    self.transform.rotation,
    targetRot,
    maxDegreesPerTick
);
```

不要这样做：

- 用 `Vector3` 保存逻辑位置
- 用 `Quaternion` 保存逻辑旋转
- 先在 Unity 浮点空间里算完，再把结果“补回”定点类型

### 建模原则

定点数学建模时，建议遵守三条简单规则：

1. 逻辑真值只保留一份。
2. 会进入比较、同步、回放、回滚的数据都用定点类型。
3. 表现层换算放在边界做，不要把换算后的浮点值再传回逻辑层。

可以用下面这张表快速判断：

| 场景 | 推荐 | 不推荐 |
| --- | --- | --- |
| 保存逻辑位置 | `FixedPointVector3` | `Vector3` |
| 保存逻辑角度 | `FixedPointQuaternion` / `FixedPoint64` | `Quaternion` / `float` |
| 范围、半径、距离 | `FixedPoint64` | `float` |
| 逻辑插值 | `FixedPointVector3.Lerp(...)` 等定点路径 | Unity 浮点插值后回写 |

## 定点物理

### 逻辑空间入口 `FPTransform`

逻辑对象的空间状态，优先挂在：

- `FPTransform`

它适合承载：

- `position`
- `rotation`
- `localPosition`
- `localRotation`
- `localScale`
- `forward`
- `up`
- `right`

推荐理解为：

- `FPTransform` 是逻辑空间的入口。
- Unity `Transform` 是表现空间的入口。
- 前者产出逻辑真值，后者消费逻辑真值。

凡是涉及：

- 位移推进
- 转向
- 面朝方向
- 局部偏移
- 父子层级下的逻辑坐标

都应该先看 `FPTransform` 是否已经能承接。

### 碰撞体体系 `FPCollider`

定点碰撞体统一建立在：

- `FPCollider`

之上，再扩展出不同形状：

- `FPSphereCollider`
- `FPAABBCollider`
- `FPBoxCollider`
- `FPCapsuleCollider`
- `FPAACapsuleCollider`
- `FPCylinderCollider`
- `FPMeshCollider`
- `FPCharacterController`

推荐理解方式：

- 逻辑碰撞体使用定点坐标
- 逻辑碰撞体跟随 `FPTransform`
- 逻辑碰撞判定优先使用定点几何和定点相交算法

如果某个战斗规则依赖：

- 进入范围
- 命中检测
- 障碍阻挡
- 地面判断
- 穿透修正

那么优先考虑的是“用什么定点碰撞体表达”，而不是“Unity 里挂什么 Collider”。

### 物理查询 `FPPhysics`

逻辑层的物理查询，优先走：

- `FPPhysics`

它适合承接：

- 射线检测
- 球形范围检测
- 盒体检测
- 角色胶囊相关检测
- 逻辑层命中与阻挡判断

结合当前代码，更准确地说，直接暴露出来、最值得优先复用的公共查询入口主要集中在：

- `Raycast(...)`
- `OverlapSphere(...)`
- `OverlaySphereCollision(...)`
- `OverlayBoxCollision(...)`

推荐原则：

- 查询结果直接服务于逻辑结论
- 查询输入和输出都尽量保持定点表达
- 逻辑层不要混用 Unity `Physics.Raycast`、`Physics.OverlapSphere` 等浮点查询作为主判定

一条很实用的判断标准是：

> 如果这次查询结果会影响技能命中、角色位置、状态机切换或伤害结算，它就应该优先走定点物理。

这里还要补一个当前实现边界：

- `FPPhysics.Raycast(...)` 当前代码里明确写了 `TODO 缺少Mesh的Raycast检测`。
- 所以不要默认它已经和 Unity `Physics.Raycast` 完全等价；如果需求依赖 Mesh 级射线检测，应先确认现有库是否已补齐。
- 当前代码里还存在 `OverlayCharacterWithCapsule(...)` 这类更特殊的封装；这类 API 在直接复用前，先核对实现与参数含义，再决定是否作为主入口。

### 动态体 `FPCharacterController` 与 `FPRigidbody`

涉及动态体时，常见入口有两类：

- `FPCharacterController`
- `FPRigidbody`

推荐分工：

| 类型 | 更适合处理什么 |
| --- | --- |
| `FPCharacterController` | 可控角色、地面检测、移动约束、跳跃、击退、角色交互 |
| `FPRigidbody` | 当前更偏球体刚体式受力、速度、冲量与约束求解 |

经验上可以这样选：

- 如果你准备直接复用 `FixedPointPhysics` 的定点动态体能力，可先评估 `FPCharacterController`
- 如果需求更偏球体刚体式受力、冲量或约束求解，再评估 `FPRigidbody`
- 不要把上面两条理解成“当前 `GameBattle` 单位主线已经默认接好了这两套”

无论选哪一种，都应保持一个原则：

- 运动结论来自定点物理对象本身
- 不要先让 Unity 刚体跑完，再把结果同步回逻辑层

当前还要注意一个实现事实：

- `FPRigidbody` 的构造函数直接接收 `FPSphereCollider`。
- 因此在现阶段，不要把它理解成“Unity Rigidbody 的定点等价全量替身”，而更应该把它看成一个偏球体刚体场景的能力点。

### 逻辑层与表现层的边界

定点物理层只负责逻辑结论，例如：

- 这一步能不能走
- 是否发生命中
- 最终停在哪个位置
- 当前朝向是多少
- 当前速度是多少

表现层负责的是：

- 模型怎么插值
- 动画怎么播
- 特效什么时候挂
- 镜头如何跟随
- UI 如何显示

在当前 DGame 的战斗语境里，更贴近现有实现的理解方式是：

- `LogicUnit` 持有单位逻辑真值与 `FPTransform`
- 连续状态通过 `UnitStateSyncComponent` / `UnitStateSnapshot` 保存
- 表现侧通过 `IRenderUnit.SyncFromLogic()` 消费逻辑结果

推荐边界：

| 逻辑层负责 | 表现层负责 |
| --- | --- |
| 位置、旋转、速度、碰撞、命中 | 动画、特效、镜头、音频、界面反馈 |
| 连续状态快照 | 读取快照并展示 |
| 一次性逻辑事件 | 接收事件并播放表现 |

这条链路可以直接记成：

```text
LogicUnit / FPTransform
    -> UnitStateSyncComponent / UnitStateSnapshot
    -> IRenderUnit.SyncFromLogic()
    -> Unity 表现层
```

## 确定性随机数

帧同步逻辑中的随机数，必须使用：

- `FixedPointRandom`

这里最重要的不是“随机算法名字”，而是三条纪律：

1. 必须显式使用同步种子。
2. 同一场逻辑结算中的随机消费顺序必须稳定。
3. 不允许把本地时间、本地帧率或本地事件顺序混入随机路径。

推荐做法：

```csharp
FixedPointRandom random = new FixedPointRandom(seed);
FixedPoint64 angle = random.Next(0, 360);
```

不要这样做：

- `new FixedPointRandom()` 然后让它自己取时间种子
- `UnityEngine.Random.Range(...)`
- 客户端 A 走一条随机分支，客户端 B 少调一次随机函数

如果随机数会影响：

- 技能散布
- 暴击判定
- 掉落判定
- 目标选择
- 随机位移

那么它必须被当成逻辑系统的一部分，而不是表现细节。

## 推荐用法速查

可以先按下面这张表判断：

| 需求 | 推荐使用 | 不要直接使用 |
| --- | --- | --- |
| 逻辑标量 | `FixedPoint64` | `float` / `double` |
| 逻辑位置 | `FixedPointVector3` + `FPTransform` | `Transform.position` |
| 逻辑朝向 | `FixedPointQuaternion` | `Transform.rotation` |
| 范围检测 | `FPPhysics` / `FixedPointIntersection` | Unity `Physics` |
| 角色运动 | `FPCharacterController` | Unity `CharacterController` |
| 刚体式运动 | `FPRigidbody` | Unity `Rigidbody` |
| 随机数 | `FixedPointRandom(seed)` | `UnityEngine.Random` |

## 不要这样做

下面这些写法在帧同步逻辑里都应该默认避免：

- 用 `float` / `double` / `Vector3` / `Quaternion` 保存逻辑真值
- 用 Unity `Transform` 作为逻辑位置、旋转、朝向的来源
- 用 Unity `Physics` 生成命中、阻挡、地面检测等核心逻辑结论
- 用 Unity 刚体或角色控制器产出最终逻辑移动结果
- 用 `UnityEngine.Random`、本地时间或临时随机源驱动同步逻辑
- 在逻辑层和表现层之间维护两套彼此独立的位置/旋转状态
- 为了一个局部需求再造一套和 `FixedPointPhysics` 并存的定点工具

## 开发前自检

开始编码前，至少先回答这几个问题：

1. 这段逻辑的真值是不是全部落在定点类型里？
2. 位置、旋转、朝向是不是都通过 `FPTransform` 或其它定点类型维护？
3. 命中、阻挡、范围判断是不是都走定点物理？
4. 随机数是不是显式传入了同步种子？
5. 我是不是把表现层对象误当成逻辑输入源了？

只要有一项答案不明确，就先回到本文件，把定点基础收紧，再继续实现。