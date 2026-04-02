using DGame;
using Fantasy.Entitas.Interface;

namespace GameBattle
{
    /// <summary>
    /// 单位状态同步组件销毁系统。
    /// </summary>
    public sealed class UnitStateSyncComponentDestroySystem : DestroySystem<UnitStateSyncComponent>
    {
        /// <summary>
        /// 销毁单位状态同步组件。
        /// </summary>
        /// <param name="self">单位状态同步组件实例。</param>
        protected override void Destroy(UnitStateSyncComponent self)
        {
            self?.Clear();
        }
    }
    
    /// <summary>
    /// 单位状态同步扩展方法。
    /// </summary>
    public static class UnitStateSyncComponentSystem
    {
        /// <summary>
        /// 标记并写入单位位置。
        /// </summary>
        /// <param name="self">逻辑单位。</param>
        /// <param name="pos">目标位置。</param>
        public static void MarkPosition(this LogicUnit self, FixedPointVector3 pos)
        {
            self.transform.position = pos;
            var snapshot = self.StateSync.Snapshot;
            snapshot.Position = pos;
            self.StateSync.Snapshot = snapshot;
            self.StateSync.MarkDirty(UnitStateDirtyFlags.Transform);
        }

        /// <summary>
        /// 标记并写入单位朝向。
        /// </summary>
        /// <param name="self">逻辑单位。</param>
        /// <param name="forward">目标朝向向量。</param>
        public static void MarkForward(this LogicUnit self, FixedPointVector3 forward)
        {
            var snapshot = self.StateSync.Snapshot;
            snapshot.MoveForward = forward;
            snapshot.Rotation = self.transform.rotation;
            self.StateSync.Snapshot = snapshot;
            self.StateSync.MarkDirty(UnitStateDirtyFlags.Transform);
        }

        /// <summary>
        /// 标记并写入单位生命值。
        /// </summary>
        /// <param name="self">逻辑单位。</param>
        /// <param name="hp">生命值。</param>
        public static void MarkHp(this LogicUnit self, int hp)
        {
            var snapshot = self.StateSync.Snapshot;
            var attrSnapshot = snapshot.Attr;
            attrSnapshot.Hp = hp;
            snapshot.Attr = attrSnapshot;
            self.StateSync.Snapshot = snapshot;
            self.StateSync.MarkDirty(UnitStateDirtyFlags.Attr);
        }

        /// <summary>
        /// 批量标记并写入单位属性快照。
        /// </summary>
        /// <param name="self">逻辑单位。</param>
        /// <param name="atk">当前攻击力。</param>
        /// <param name="hp">当前生命值。</param>
        /// <param name="maxHp">当前最大生命值。</param>
        /// <param name="moveSpeed">当前移动速度。</param>
        public static void MarkAttrSnapshot(this LogicUnit self, int atk, int hp, int maxHp, FixedPoint64 moveSpeed)
        {
            var snapshot = self.StateSync.Snapshot;
            snapshot.Attr = new UnitAttrSnapshot
            {
                Atk = atk,
                Hp = hp,
                MaxHp = maxHp,
                MoveSpeed = moveSpeed
            };
            self.StateSync.Snapshot = snapshot;
            self.StateSync.MarkDirty(UnitStateDirtyFlags.Attr);
        }
    }
}
