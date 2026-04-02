using Fantasy.Entitas.Interface;

namespace GameBattle
{
    public sealed class UnitIdGeneratorComponentDestroySystem : DestroySystem<UnitIdGeneratorComponent>
    {
        protected override void Destroy(UnitIdGeneratorComponent self)
        {
            self.Reset();
        }
    }

    /// <summary>
    /// 逻辑单位 ID 生成组件扩展方法。
    /// </summary>
    public static class UnitIdGeneratorComponentSystem
    {
        /// <summary>
        /// 分配一个新的 UnitID。
        /// </summary>
        /// <param name="self">逻辑单位 ID 生成组件。</param>
        /// <returns>分配得到的 UnitID；组件为空时返回 0。</returns>
        public static ulong Allocate(this UnitIdGeneratorComponent self)
        {
            if (self == null)
            {
                return 0;
            }

            return self.NextUnitId++;
        }

        /// <summary>
        /// 使用指定 UnitID，并确保后续自增游标不回退。
        /// </summary>
        /// <param name="self">逻辑单位 ID 生成组件。</param>
        /// <param name="unitId">外部指定的 UnitID；为 0 时自动分配。</param>
        /// <returns>最终使用的 UnitID；组件为空时返回 0。</returns>
        public static ulong AllocateOrUse(this UnitIdGeneratorComponent self, ulong unitId)
        {
            if (self == null)
            {
                return 0;
            }

            if (unitId == 0)
            {
                return self.Allocate();
            }

            if (unitId >= self.NextUnitId)
            {
                self.NextUnitId = unitId + 1;
            }

            return unitId;
        }

        /// <summary>
        /// 重置 UnitID 生成器状态。
        /// </summary>
        /// <param name="self">逻辑单位 ID 生成组件。</param>
        public static void Reset(this UnitIdGeneratorComponent self)
        {
            if (self == null)
            {
                return;
            }

            self.NextUnitId = 1;
        }
    }
}