using System;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using GameBattle;

namespace GameLogic
{
    public sealed class RenderUnitFactoryComponentAwakeSystem : AwakeSystem<RenderUnitFactoryComponent>
    {
        protected override void Awake(RenderUnitFactoryComponent self)
        {
            self.Awake();
        }
    }

    public sealed class RenderUnitFactoryComponentDestroySystem : DestroySystem<RenderUnitFactoryComponent>
    {
        protected override void Destroy(RenderUnitFactoryComponent self)
        {
            self.Destroy();
        }
    }

    public static class RenderUnitFactoryComponentSystem
    {
        public static void Awake(this RenderUnitFactoryComponent self)
        {
            self.Register<PlayerUnit, PlayerRender>();
            self.Register<MonsterUnit, MonsterRender>();
        }

        public static void Destroy(this RenderUnitFactoryComponent self)
        {
            self.RenderUnitCreators.Clear();
        }

        /// <summary>
        /// 外部注册新类型
        /// </summary>
        /// <typeparam name="TLogic"></typeparam>
        /// <typeparam name="TRender"></typeparam>
        public static void Register<TLogic, TRender>(this RenderUnitFactoryComponent self)
            where TLogic : LogicUnit where TRender : RenderUnit, new()
        {
            self.RenderUnitCreators[typeof(TLogic)] = self.CreateInternal<TRender>;
        }

        private static T CreateInternal<T>(this RenderUnitFactoryComponent self, LogicUnit logicUnit) where T : RenderUnit, new()
        {
            var renderUnit = Entity.Create<T>(self.Scene, true, true);

            if (!renderUnit.Init(logicUnit))
            {
                renderUnit.Dispose();
                throw new InvalidOperationException($"Init render unit failed: {typeof(T).Name}");
            }

            return renderUnit;
        }
    }
}