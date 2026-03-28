using System;
using Fantasy;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;

namespace GameBattle
{
    public sealed class LogicUnitFactoryComponentAwakeSystem : AwakeSystem<LogicUnitFactoryComponent>
    {
        protected override void Awake(LogicUnitFactoryComponent self)
        {
            self.Awake();
        }
    }
    
    public sealed class LogicUnitFactoryComponentDestroySystem : DestroySystem<LogicUnitFactoryComponent>
    {
        protected override void Destroy(LogicUnitFactoryComponent self)
        {
            self.Destroy();
        }
    }
    
    public static class LogicUnitFactoryComponentSystem
    {
        public static void Awake(this LogicUnitFactoryComponent self)
        {
            // 内置注册
            self.Register<PlayerUnit>(UnitType.GamePlayer);
            self.Register<MonsterUnit>(UnitType.Monster);
        }
        
        public static void Destroy(this LogicUnitFactoryComponent self)
        {
            self.LogicUnitCreators.Clear();
        }

        /// <summary>
        /// 外部注册新类型
        /// </summary>
        /// <param name="self"></param>
        /// <param name="unitType"></param>
        /// <typeparam name="TUnit"></typeparam>
        public static void Register<TUnit>(this LogicUnitFactoryComponent self, UnitType unitType)
            where TUnit : LogicUnit, new()
            => self.LogicUnitCreators[unitType] = self.CreateInternal<TUnit>;
        
        private static T CreateInternal<T>(this LogicUnitFactoryComponent self) where T : LogicUnit, new()
        {
            var unit = Entity.Create<T>(self.Scene, true, true);
            // unit.Init();
            // unit.Initialize(info);
            return unit;
        }

        public static LogicUnit Create(this LogicUnitFactoryComponent self, UnitType unitType)
        {
            if (self.LogicUnitCreators.TryGetValue(unitType, out var creator))
            {
                return creator();
            }

            throw new NotSupportedException($"Unsupported UnitType: {unitType}");
        }
    }
}