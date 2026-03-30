using System;
using Fantasy;
using Fantasy.Entitas;
using GameBattle;

namespace GameLogic
{
    /// <summary>
    /// 战斗管理器，作为战斗系统的上层入口，负责创建和管理战斗实例。
    /// 此为渲染层的战斗管理器，负责初始化渲染相关的工厂组件。
    /// </summary>
    public static class BattleManager
    {
        /// <summary>
        /// 创建一个新的战斗实例。
        /// </summary>
        /// <param name="scene">父场景，默认使用 GameClient.Instance.Scene。</param>
        /// <returns>创建的战斗上下文组件。</returns>
        public static BattleContextComponent CreateBattle(Scene scene = null)
        {
            scene ??= GameClient.Instance.Scene;
            var battleScene = Entity.Create<SubScene>(scene);
            var battleContext = GameBattle.BattleManager.CreateBattle(battleScene);
            BattleSystem.Instance.Init(battleContext);
            return battleContext;
        }

        /// <summary>
        /// 获取当前战斗上下文组件。
        /// </summary>
        /// <returns>当前战斗上下文组件，如果没有则返回 null。</returns>
        public static BattleContextComponent CurBattleContext => GameBattle.BattleManager.CurBattleContextComponent;

        /// <summary>
        /// 获取当前战斗场景。
        /// </summary>
        /// <returns>当前战斗场景，如果没有则返回 null。</returns>
        public static SubScene CurBattleScene => GameBattle.BattleManager.CurScene;

        /// <summary>
        /// 销毁当前战斗实例，释放所有相关资源。
        /// </summary>
        public static void DestroyBattle()
        {
            GameBattle.BattleManager.DestroyBattle();
            BattleSystem.Instance.Clear();
        }

        #region Battle级事件相关

        /// <summary>
        /// (推荐)注册一个跟随作用域自动释放的战斗事件监听。
        /// <remarks>此方法用于在战斗上下文中注册事件监听，当作用域销毁时自动取消监听。</remarks>
        /// </summary>
        /// <typeparam name="T">战斗事件类型。</typeparam>
        /// <param name="owner">监听所属者。</param>
        /// <param name="scope">订阅作用域。</param>
        /// <param name="handler">事件回调。</param>
        public static void SubscribeScoped<T>(object owner, SubscriptionScopeComponent scope,
            Action<T> handler) where T : struct, IBattleEvent
            => CurBattleContext.SubscribeScoped(owner, scope, handler);

        /// <summary>
        /// 注册战斗级事件监听。不推荐直接使用，请优先使用自动释放的 <see cref="SubscribeScoped{T}"/>。
        /// <remarks>此方法用于在战斗上下文中注册事件监听，需要手动调用Unsubscribe取消监听/RemoveAll取消所有监听。</remarks>
        /// </summary>
        /// <typeparam name="T">战斗事件类型。</typeparam>
        /// <param name="owner">监听所属者。</param>
        /// <param name="handler">事件回调。</param>
        public static void Subscribe<T>(object owner, Action<T> handler) where T : struct, IBattleEvent
            => CurBattleContext.Subscribe(owner, handler);

        /// <summary>
        /// 取消战斗级事件监听。
        /// </summary>
        /// <typeparam name="T">战斗事件类型。</typeparam>
        /// <param name="handler">事件回调。</param>
        public static void Unsubscribe<T>(Action<T> handler) where T : struct, IBattleEvent
            => CurBattleContext.Unsubscribe(handler);

        /// <summary>
        /// 发布战斗级事件。
        /// </summary>
        /// <typeparam name="T">战斗事件类型。</typeparam>
        /// <param name="eventData">事件数据。</param>
        public static void Publish<T>(T eventData) where T : struct, IBattleEvent
            => CurBattleContext.Publish(eventData);

        /// <summary>
        /// 移除指定所属者的全部战斗级事件监听。
        /// </summary>
        /// <param name="owner">监听所属者。</param>
        public static void RemoveAll(object owner) => CurBattleContext.RemoveAll(owner);

        #endregion
    }
}