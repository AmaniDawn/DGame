using DGame;

namespace GameBattle
{
    /// <summary>
    /// 逻辑单位属性修正项。
    /// </summary>
    public sealed class LogicUnitAttrModifier : MemoryObject
    {
        /// <summary>
        /// 修正来源标识。
        /// </summary>
        public ulong SourceId { get; private set; }

        /// <summary>
        /// 目标属性类型。
        /// </summary>
        public UnitAttrType AttrType { get; private set; }

        /// <summary>
        /// 修正模式。
        /// </summary>
        public UnitAttrModifyMode ModifyMode { get; private set; }

        /// <summary>
        /// 修正值。
        /// </summary>
        public FixedPoint64 Value { get; private set; }

        /// <summary>
        /// 从内存池创建一条属性修正项。
        /// </summary>
        public static LogicUnitAttrModifier Create(ulong sourceId, UnitAttrType attrType, UnitAttrModifyMode modifyMode,
            FixedPoint64 value)
        {
            var modifier = Spawn<LogicUnitAttrModifier>();
            modifier.SourceId = sourceId;
            modifier.AttrType = attrType;
            modifier.ModifyMode = modifyMode;
            modifier.Value = value;
            return modifier;
        }

        public override void OnRelease()
        {
            SourceId = 0;
            AttrType = UnitAttrType.None;
            ModifyMode = UnitAttrModifyMode.Flat;
            Value = FixedPoint64.Zero;
        }
    }
}
