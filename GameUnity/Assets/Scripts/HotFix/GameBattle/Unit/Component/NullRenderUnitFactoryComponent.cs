using Fantasy.Entitas;
// ReSharper disable InconsistentNaming

namespace GameBattle
{
    /// <summary>
    /// 空渲染单位工厂组件，用于不需要渲染的场景（如服务器端或纯逻辑模式）。
    /// 实现空对象模式，返回 <see cref="NullRenderUnit"/> 单例而非 null。
    /// </summary>
    public sealed class NullRenderUnitFactoryComponent : Entity, IRenderUnitFactory
    {
        /// <summary>
        /// 创建空渲染单位实例。
        /// </summary>
        /// <param name="logicUnit">逻辑层单位。</param>
        /// <returns>空渲染单位单例。</returns>
        public IRenderUnit Create(in LogicUnit logicUnit) => NullRenderUnit.Instance;
    }

    /// <summary>
    /// 空渲染单位，用于不需要渲染的场景（如服务器端）。
    /// 实现空对象模式，提供 <see cref="IRenderUnit"/> 的空实现，避免调用方判空。
    /// </summary>
    public sealed class NullRenderUnit : IRenderUnit
    {
        /// <summary>
        /// 获取空渲染单位单例实例。
        /// </summary>
        public static readonly NullRenderUnit Instance = new NullRenderUnit();

        private NullRenderUnit() { }

        /// <summary>
        /// 接收逻辑层事件通知，空实现不做任何处理。
        /// </summary>
        /// <param name="eventId">事件标识。</param>
        public void OnUnitEvent(int eventId) { }

        public void SyncFromLogic() { }
    }
}