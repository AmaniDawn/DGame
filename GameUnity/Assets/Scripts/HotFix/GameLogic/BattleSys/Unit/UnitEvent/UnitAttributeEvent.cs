using GameBattle;

namespace GameLogic
{
    /// <summary>
    /// 单位属性变化标记。
    /// <remarks>用于描述本次渲染层属性同步中，哪些属性域发生了变化。</remarks>
    /// </summary>
    [System.Flags]
    public enum UnitAttributeChangeFlags
    {
        /// <summary>
        /// 没有任何属性变化。
        /// </summary>
        None = 0,

        /// <summary>
        /// 生命值相关属性发生变化。
        /// </summary>
        Hp = 1 << 0,

        /// <summary>
        /// 当前已支持的全部属性域。
        /// </summary>
        All = Hp,
    }

    /// <summary>
    /// 单位属性同步完成事件。
    /// 由属性表现同步组件在属性快照发生变化后发布，供血条、名字板等表现模块订阅。
    /// </summary>
    public readonly struct UnitAttributeChangedEvent : IUnitEvent
    {
        /// <summary>
        /// 同步前的旧属性快照。
        /// </summary>
        public UnitAttrSnapshot Previous { get; }

        /// <summary>
        /// 同步后的新属性快照。
        /// </summary>
        public UnitAttrSnapshot Current { get; }

        /// <summary>
        /// 本次发生变化的属性域标记。
        /// </summary>
        public UnitAttributeChangeFlags ChangeFlags { get; }

        /// <summary>
        /// 构造一个单位属性变化事件。
        /// </summary>
        /// <param name="previous">旧属性快照。</param>
        /// <param name="current">新属性快照。</param>
        /// <param name="changeFlags">变化标记。</param>
        public UnitAttributeChangedEvent(UnitAttrSnapshot previous, UnitAttrSnapshot current,
            UnitAttributeChangeFlags changeFlags)
        {
            Previous = previous;
            Current = current;
            ChangeFlags = changeFlags;
        }
    }
}