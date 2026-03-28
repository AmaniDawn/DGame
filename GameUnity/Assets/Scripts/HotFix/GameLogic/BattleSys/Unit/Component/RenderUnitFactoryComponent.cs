using System;
using System.Collections.Generic;
using Fantasy.Entitas;
using GameBattle;

namespace GameLogic
{
    /// <summary>
    /// 渲染单位工厂组件，负责管理和创建渲染层单位。
    /// 实现渲染层与逻辑层的解耦，通过 <see cref="IRenderUnitFactory"/> 接口提供服务。
    /// </summary>
    public class RenderUnitFactoryComponent : Entity, IRenderUnitFactory
    {
        /// <summary>
        /// 获取渲染单位创建器字典，键为逻辑单位类型，值为创建委托。
        /// </summary>
        internal readonly Dictionary<Type, Func<LogicUnit, IRenderUnit>> RenderUnitCreators = new();

        /// <summary>
        /// 根据逻辑单位创建对应的渲染单位。
        /// </summary>
        /// <param name="logicUnit">逻辑层单位实例。</param>
        /// <returns>创建的渲染层单位实例。</returns>
        /// <exception cref="NotSupportedException">当逻辑单位类型未注册时抛出。</exception>
        public IRenderUnit Create(in LogicUnit logicUnit)
        {
            var type = logicUnit.GetType();

            if (RenderUnitCreators.TryGetValue(type, out var creator))
            {
                return creator(logicUnit);
            }

            throw new NotSupportedException($"Unsupported logic unit: {type.Name}");
        }
    }
}
