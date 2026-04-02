using DGame;

namespace GameBattle
{
    /// <summary>
    /// 逻辑单位类型专属创建数据基类。
    /// </summary>
    public abstract class LogicUnitCreatePayload : MemoryObject
    {
        /// <summary>
        /// 当前创建数据对应的逻辑单位类型。
        /// </summary>
        public abstract UnitType UnitType { get; }
    }
}
