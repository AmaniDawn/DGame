using Fantasy.Entitas;

namespace GameBattle
{
    /// <summary>
    /// 渲染单位工厂接口。
    /// <remarks>定义创建渲染层单位的能力。</remarks>
    /// </summary>
    public interface IRenderUnitFactory : IEntity
    {
        /// <summary>
        /// 根据逻辑层单位创建对应的渲染层单位。
        /// </summary>
        /// <param name="logicUnit">逻辑层单位实例。</param>
        /// <returns>创建的渲染层单位实例。</returns>
        IRenderUnit Create(in LogicUnit logicUnit);
    }
}