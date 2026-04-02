using Fantasy.Entitas;

namespace GameBattle
{
    /// <summary>
    /// 逻辑单位 ID 生成组件。
    /// <remarks>负责分配当前战斗内稳定的 UnitID，不承担单位创建与注册职责。</remarks>
    /// </summary>
    public sealed class UnitIdGeneratorComponent : Entity
    {
        internal ulong NextUnitId = 1;
    }
}
