# DGame 帧同步定点基础（精简版）

当需求涉及 `GameBattle` 中的定点数学、定点物理、确定性随机数或 `FixedPointPhysics` 的使用方式时，先读本文件。

目标：只保留 Codex 在实现和评审时必须先守住的规则。完整说明以 `references/originals/frame-sync-fixedpoint-foundation.md` 为准。

范围：本文件讨论的是定点数学、定点物理和确定性随机数基础，不讨论输入缓存、回滚、预测纠正或联网锁步流程本身。

## 核心结论

- 进入 `GameBattle` 逻辑真值链路的帧同步相关逻辑，必须优先使用 `DGame.FixedPointPhysics`
- 不要把 `float`、`Vector3`、`Quaternion`、Unity `Transform`、Unity `Physics` 当作逻辑真值
- 表现层只消费逻辑结果，不反向写入逻辑状态
- 先做定点逻辑建模，再由逻辑结果驱动表现

## 当前代码锚点

- `FixedPoint64` 的实际实现文件是 `GameUnity/Assets/DGame/FixedPointPhysics/Math/FixedPointInt64.cs`
- 定点基础入口主要落在 `FPTransform`、`FPPhysics`、`FixedPointRandom`
- 当前 `GameBattle` 更贴近的逻辑/表现边界是 `LogicUnit -> UnitStateSyncComponent / UnitStateSnapshot -> IRenderUnit.SyncFromLogic()`

## 优先使用的类型

- 标量：`FixedPoint64`
- 数学：`FixedPointMath`
- 向量：`FixedPointVector2`、`FixedPointVector3`
- 旋转：`FixedPointQuaternion`
- 矩阵：`FixedPointMatrix`
- 逻辑空间：`FPTransform`
- 碰撞与查询：`FPCollider`、`FPPhysics`、`FixedPointIntersection`
- 动态体能力：`FPCharacterController`、`FPRigidbody`
- 连续状态与表现同步：`UnitStateSyncComponent`、`UnitStateSnapshot`、`IRenderUnit.SyncFromLogic()`
- 随机数：`FixedPointRandom`

## 三条基础规则

### 定点数学

- 逻辑层不要长期保存 `float` / `double`
- 会影响结论的数值尽量统一收口到 `FixedPoint64`
- 各类数学运算优先走 `FixedPointMath`

### 定点物理

- 逻辑位置和旋转优先挂在 `FPTransform`
- 命中、阻挡、范围、地面检测优先走 `FPPhysics`
- 当前最直接可复用的公共查询入口主要是 `Raycast`、`OverlapSphere`、`OverlaySphereCollision`、`OverlayBoxCollision`
- 更特殊的查询封装先核对当前实现和参数含义，再决定是否直接复用
- 若需要直接复用定点动态体能力，可优先评估 `FPCharacterController`
- 更偏球体刚体场景时再评估 `FPRigidbody`
- 不要默认当前 `GameBattle` 单位主线已经直接接好了 `FPCharacterController` / `FPRigidbody`
- `FPPhysics.Raycast(...)` 目前还缺 Mesh Raycast 检测，不要默认它和 Unity `Physics.Raycast` 完全等价
- 当前 `GameBattle` 更直接的逻辑/表现边界是 `LogicUnit + UnitStateSyncComponent + IRenderUnit`

### 定点随机

- 随机数必须使用 `FixedPointRandom`
- 必须显式传入同步种子
- 不要用 `UnityEngine.Random`、本地时间或临时随机源

## 约束边界

- 上述约束针对 `GameBattle` 的逻辑真值链路
- `FixedPointPhysics` 目录下的编辑器、Gizmo、表现辅助脚本可以使用 Unity 浮点 API，但不能反向成为战斗逻辑真值来源
- `AsFloat()`、`ToVector3()` 这类转换只应出现在渲染、调试、编辑器边界，不要把转换后的浮点值再写回逻辑真值

## 不该做的事

- 在逻辑层维护另一套浮点位置、旋转或碰撞结果
- 用 Unity `Transform.position` 作为逻辑源
- 用 Unity 物理查询做命中判断，再补回逻辑层
- 让 Unity 刚体或角色控制器决定最终逻辑位移
- 新造一套和 `FixedPointPhysics` 并存的定点工具

## 落地顺序

1. 先把真值建模成定点类型
2. 位置/旋转统一收口到 `FPTransform`
3. 碰撞与范围统一收口到 `FPPhysics`
4. 随机统一收口到 `FixedPointRandom(seed)`
5. 连续状态写入 `UnitStateSyncComponent` / `UnitStateSnapshot`
6. 表现层通过 `IRenderUnit.SyncFromLogic()` 等入口只读逻辑结果，不改逻辑真值