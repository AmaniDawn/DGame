using System;
using Fantasy.Entitas.Interface;

namespace GameBattle
{
    /// <summary>
    /// 订阅作用域销毁系统。
    /// </summary>
    public sealed class SubscriptionScopeComponentDestroySystem : DestroySystem<SubscriptionScopeComponent>
    {
        /// <summary>
        /// 销毁订阅作用域组件。
        /// </summary>
        /// <param name="self">订阅作用域组件实例。</param>
        protected override void Destroy(SubscriptionScopeComponent self)
        {
            self.Destroy();
        }
    }
    
    /// <summary>
    /// 订阅作用域扩展方法。
    /// </summary>
    public static class SubscriptionScopeComponentSystem
    {
        /// <summary>
        /// 注册一个跟随作用域自动释放的单位事件监听。
        /// </summary>
        /// <typeparam name="T">单位事件类型。</typeparam>
        /// <param name="self">逻辑单位。</param>
        /// <param name="owner">监听所属者。</param>
        /// <param name="scope">订阅作用域。</param>
        /// <param name="handler">事件回调。</param>
        public static void SubscribeLogicScoped<T>(this LogicUnit self, object owner, SubscriptionScopeComponent scope,
            Action<T> handler) where T : struct, IUnitEvent
        {
            self.SubscribeLogic(owner, handler);
            scope.Add(() => self.UnsubscribeLogic(handler));
        }

        /// <summary>
        /// 注册一个跟随作用域自动释放的战斗事件监听。
        /// </summary>
        /// <typeparam name="T">战斗事件类型。</typeparam>
        /// <param name="self">战斗上下文。</param>
        /// <param name="owner">监听所属者。</param>
        /// <param name="scope">订阅作用域。</param>
        /// <param name="handler">事件回调。</param>
        public static void SubscribeBattleScoped<T>(this BattleContextComponent self, object owner, SubscriptionScopeComponent scope,
            Action<T> handler) where T : struct, IBattleEvent
        {
            self.SubscribeBattle(owner, handler);
            scope.Add(() => self.UnsubscribeBattle(handler));
        }
    }
}