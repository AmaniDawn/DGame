using System;
using Fantasy.Entitas.Interface;

namespace GameBattle
{
    /// <summary>
    /// 单位事件中心销毁系统。
    /// </summary>
    public sealed class UnitEventHubComponentDestroySystem : DestroySystem<UnitEventHubComponent>
    {
        /// <summary>
        /// 销毁单位事件中心。
        /// </summary>
        /// <param name="self">单位事件中心实例。</param>
        protected override void Destroy(UnitEventHubComponent self)
        {
            self.Clear();
        }
    }
    
    /// <summary>
    /// 单位事件中心扩展方法。
    /// </summary>
    public static class UnitEventHubComponentSystem
    {
        /// <summary>
        /// 注册单位事件监听。
        /// </summary>
        /// <typeparam name="T">单位事件类型。</typeparam>
        /// <param name="self">逻辑单位。</param>
        /// <param name="owner">监听所属者。</param>
        /// <param name="handler">事件回调。</param>
        public static void SubscribeLogic<T>(this LogicUnit self, object owner, Action<T> handler)
            where T : struct, IUnitEvent
            => self?.UnitEventHub?.Subscribe(owner, handler);
        
        /// <summary>
        /// 取消单位事件监听。
        /// </summary>
        /// <typeparam name="T">单位事件类型。</typeparam>
        /// <param name="self">逻辑单位。</param>
        /// <param name="handler">事件回调。</param>
        public static void UnsubscribeLogic<T>(this LogicUnit self, Action<T> handler)
            where T : struct, IUnitEvent
            => self?.UnitEventHub?.Unsubscribe(handler);

        /// <summary>
        /// 移除指定所属者的全部单位事件监听。
        /// </summary>
        /// <param name="self">逻辑单位。</param>
        /// <param name="owner">监听所属者。</param>
        public static void RemoveAllLogicSubscriptions(this LogicUnit self, object owner)
            => self?.UnitEventHub?.RemoveAll(owner);
        
        /// <summary>
        /// 发布单位事件。
        /// </summary>
        /// <typeparam name="T">单位事件类型。</typeparam>
        /// <param name="self">逻辑单位。</param>
        /// <param name="eventData">事件数据。</param>
        public static void PublishLogic<T>(this LogicUnit self, T eventData)
            where T : struct, IUnitEvent
            => self?.UnitEventHub?.Publish(eventData);
    }
}