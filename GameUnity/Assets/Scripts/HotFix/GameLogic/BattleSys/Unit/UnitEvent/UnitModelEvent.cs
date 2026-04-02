using GameBattle;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 单位模型创建完成事件。
    /// <remarks>当某个模型部位实例化并完成基础挂载后，由渲染层对外发布。</remarks>
    /// </summary>
    public readonly struct UnitModelCreatedEvent : IUnitEvent
    {
        /// <summary>
        /// 创建完成的模型对象。
        /// </summary>
        public GameObject ModelGo { get; }

        /// <summary>
        /// 创建完成的模型部位类型。
        /// </summary>
        public UnitModelType UnitModelType { get; }

        /// <summary>
        /// 构造一个模型创建完成事件。
        /// </summary>
        /// <param name="modelGo">创建出的模型对象。</param>
        /// <param name="unitModelType">模型部位类型。</param>
        public UnitModelCreatedEvent(GameObject modelGo, UnitModelType unitModelType)
        {
            ModelGo = modelGo;
            UnitModelType = unitModelType;
        }
    }

    /// <summary>
    /// 单位模型即将销毁事件。
    /// <remarks>当某个模型部位准备销毁，但销毁动作尚未真正执行完成时发布。</remarks>
    /// </summary>
    public readonly struct BeforeUnitModelDestroyEvent : IUnitEvent
    {
        /// <summary>
        /// 即将销毁的模型部位类型。
        /// </summary>
        public UnitModelType UnitModelType { get; }

        /// <summary>
        /// 构造一个模型销毁前事件。
        /// </summary>
        /// <param name="unitModelType">即将销毁的模型部位类型。</param>
        public BeforeUnitModelDestroyEvent(UnitModelType unitModelType)
        {
            UnitModelType = unitModelType;
        }
    }

    /// <summary>
    /// 单位模型销毁完成事件。
    /// <remarks>当某个模型部位已经完成销毁后，由渲染层对外发布。</remarks>
    /// </summary>
    public readonly struct UnitModelDestroyedEvent : IUnitEvent
    {
        /// <summary>
        /// 已销毁的模型部位类型。
        /// </summary>
        public UnitModelType UnitModelType { get; }

        /// <summary>
        /// 构造一个模型销毁完成事件。
        /// </summary>
        /// <param name="unitModelType">已销毁的模型部位类型。</param>
        public UnitModelDestroyedEvent(UnitModelType unitModelType)
        {
            UnitModelType = unitModelType;
        }
    }
}