using System;
using Fantasy;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;

namespace GameBattle
{
    /// <summary>
    /// 逻辑单位工厂组件唤醒系统。
    /// </summary>
    public sealed class LogicUnitFactoryComponentAwakeSystem : AwakeSystem<LogicUnitFactoryComponent>
    {
        /// <summary>
        /// 唤醒逻辑单位工厂组件。
        /// <remarks>注册内置单位类型。</remarks>
        /// </summary>
        /// <param name="self">逻辑单位工厂组件实例。</param>
        protected override void Awake(LogicUnitFactoryComponent self)
        {
            self.Awake();
        }
    }

    /// <summary>
    /// 逻辑单位工厂组件销毁系统。
    /// </summary>
    public sealed class LogicUnitFactoryComponentDestroySystem : DestroySystem<LogicUnitFactoryComponent>
    {
        /// <summary>
        /// 销毁逻辑单位工厂组件，清理所有注册的创建器。
        /// </summary>
        /// <param name="self">逻辑单位工厂组件实例。</param>
        protected override void Destroy(LogicUnitFactoryComponent self)
        {
            self.Destroy();
        }
    }

    /// <summary>
    /// 逻辑单位工厂组件扩展方法系统。
    /// </summary>
    public static class LogicUnitFactoryComponentSystem
    {
        /// <summary>
        /// 唤醒逻辑单位工厂组件，注册内置单位类型。
        /// </summary>
        /// <param name="self">逻辑单位工厂组件实例。</param>
        public static void Awake(this LogicUnitFactoryComponent self)
        {
            // 内置注册
            self.Register<PlayerUnit>(UnitType.GamePlayer);
            self.Register<MonsterUnit>(UnitType.Monster);
        }

        /// <summary>
        /// 销毁逻辑单位工厂组件，清理所有注册的创建器。
        /// </summary>
        /// <param name="self">逻辑单位工厂组件实例。</param>
        public static void Destroy(this LogicUnitFactoryComponent self)
        {
            self.LogicUnitCreators.Clear();
        }

        /// <summary>
        /// 注册新的单位类型到工厂。
        /// </summary>
        /// <typeparam name="TUnit">单位类型，必须继承自 <see cref="LogicUnit"/> 且有无参构造函数。</typeparam>
        /// <param name="self">逻辑单位工厂组件实例。</param>
        /// <param name="unitType">单位类型枚举。</param>
        public static void Register<TUnit>(this LogicUnitFactoryComponent self, UnitType unitType)
            where TUnit : LogicUnit, new()
            => self.LogicUnitCreators[unitType] = self.CreateInternal<TUnit>;

        /// <summary>
        /// 创建指定类型的逻辑单位。
        /// </summary>
        /// <param name="self">逻辑单位工厂组件实例。</param>
        /// <param name="unitType">单位类型枚举。</param>
        /// <returns>创建的逻辑单位实例。</returns>
        /// <exception cref="NotSupportedException">当单位类型未注册时抛出。</exception>
        public static LogicUnit Create(this LogicUnitFactoryComponent self, UnitType unitType)
        {
            if (self.LogicUnitCreators.TryGetValue(unitType, out var creator))
            {
                return creator();
            }

            throw new NotSupportedException($"Unsupported UnitType: {unitType}");
        }

        /// <summary>
        /// 内部创建逻辑单位实例。
        /// </summary>
        /// <typeparam name="T">单位类型，必须继承自 <see cref="LogicUnit"/> 且有无参构造函数。</typeparam>
        /// <param name="self">逻辑单位工厂组件实例。</param>
        /// <returns>创建的逻辑单位实例。</returns>
        private static T CreateInternal<T>(this LogicUnitFactoryComponent self) where T : LogicUnit, new()
        {
            var unit = Entity.Create<T>(self.Scene, true, true);
            // unit.Init();
            // unit.Initialize(info);
            return unit;
        }
    }
}