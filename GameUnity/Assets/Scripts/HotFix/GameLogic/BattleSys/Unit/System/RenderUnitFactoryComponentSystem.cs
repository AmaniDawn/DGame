using System;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using GameBattle;

namespace GameLogic
{
    /// <summary>
    /// 渲染单位工厂组件唤醒系统。
    /// </summary>
    public sealed class RenderUnitFactoryComponentAwakeSystem : AwakeSystem<RenderUnitFactoryComponent>
    {
        /// <summary>
        /// 唤醒渲染单位工厂组件。
        /// </summary>
        /// <param name="self">渲染单位工厂组件实例。</param>
        protected override void Awake(RenderUnitFactoryComponent self)
        {
            self.Awake();
        }
    }

    /// <summary>
    /// 渲染单位工厂组件销毁系统。
    /// </summary>
    public sealed class RenderUnitFactoryComponentDestroySystem : DestroySystem<RenderUnitFactoryComponent>
    {
        /// <summary>
        /// 销毁渲染单位工厂组件。
        /// </summary>
        /// <param name="self">渲染单位工厂组件实例。</param>
        protected override void Destroy(RenderUnitFactoryComponent self)
        {
            self.Destroy();
        }
    }

    /// <summary>
    /// 渲染单位工厂组件扩展方法集合。
    /// </summary>
    public static class RenderUnitFactoryComponentSystem
    {
        /// <summary>
        /// 初始化渲染单位工厂并注册内置渲染单位类型。
        /// </summary>
        /// <param name="self">渲染单位工厂组件实例。</param>
        public static void Awake(this RenderUnitFactoryComponent self)
        {
            self.Register<PlayerUnit, PlayerRender>();
            self.Register<MonsterUnit, MonsterRender>();
        }

        /// <summary>
        /// 清理渲染单位工厂中已注册的创建器。
        /// </summary>
        /// <param name="self">渲染单位工厂组件实例。</param>
        public static void Destroy(this RenderUnitFactoryComponent self)
        {
            self.RenderUnitCreators.Clear();
        }

        /// <summary>
        /// 注册逻辑单位类型到渲染单位类型的映射。
        /// </summary>
        /// <typeparam name="TLogic">逻辑单位类型。</typeparam>
        /// <typeparam name="TRender">渲染单位类型。</typeparam>
        /// <param name="self">渲染单位工厂组件实例。</param>
        public static void Register<TLogic, TRender>(this RenderUnitFactoryComponent self)
            where TLogic : LogicUnit where TRender : RenderUnit, new()
        {
            self.RenderUnitCreators[typeof(TLogic)] = self.CreateInternal<TRender>;
        }

        /// <summary>
        /// 创建并初始化指定类型的渲染单位。
        /// </summary>
        /// <typeparam name="T">渲染单位类型。</typeparam>
        /// <param name="self">渲染单位工厂组件实例。</param>
        /// <param name="logicUnit">待绑定的逻辑单位。</param>
        /// <returns>初始化完成的渲染单位实例。</returns>
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