using System;
using Fantasy.Entitas.Interface;
using GameBattle;

namespace GameLogic
{
    /// <summary>
    /// 单位显示组件销毁系统。
    /// </summary>
    public sealed class UnitDisplayComponentDestroySystem : DestroySystem<UnitDisplayComponent>
    {
        /// <summary>
        /// 销毁单位显示组件。
        /// </summary>
        /// <param name="self">单位显示组件实例。</param>
        protected override void Destroy(UnitDisplayComponent self)
        {
            self.Destroy();
        }
    }
    
    public static class UnitDisplayComponentSystem
    {
        /// <summary>
        /// 注册一个跟随当前渲染单位默认作用域自动释放的渲染单位事件监听。
        /// <para>
        /// <see cref="UnitDisplayComponent"/> 作为 <see cref="RenderUnit"/> 的内部显示组件使用，
        /// 因此事件所属者和默认释放作用域都直接复用当前 <see cref="RenderUnit"/>。
        /// </para>
        /// </summary>
        /// <typeparam name="T">渲染单位事件类型。</typeparam>
        /// <param name="self">渲染单位实例。</param>
        /// <param name="handler">事件回调。</param>
        public static void SubscribeRenderScoped<T>(this UnitDisplayComponent self, Action<T> handler)
            where T : struct, IUnitEvent
            => self.OwnerUnit?.SubscribeRenderScoped(handler);

        /// <summary>
        /// 注册渲染单位事件监听。
        /// </summary>
        /// <typeparam name="T">渲染单位事件类型。</typeparam>
        /// <param name="self">渲染单位实例。</param>
        /// <param name="owner">监听所属者。</param>
        /// <param name="handler">事件回调。</param>
        public static void SubscribeRender<T>(this UnitDisplayComponent self, object owner, Action<T> handler)
            where T : struct, IUnitEvent
            => self.OwnerUnit?.SubscribeRender(owner, handler);

        /// <summary>
        /// 取消渲染单位事件监听。
        /// </summary>
        /// <typeparam name="T">渲染单位事件类型。</typeparam>
        /// <param name="self">渲染单位实例。</param>
        /// <param name="handler">事件回调。</param>
        public static void UnsubscribeRender<T>(this UnitDisplayComponent self, Action<T> handler)
            where T : struct, IUnitEvent
            => self.OwnerUnit?.UnsubscribeRender(handler);

        /// <summary>
        /// 移除指定所属者注册的全部渲染单位事件监听。
        /// </summary>
        /// <param name="self">渲染单位实例。</param>
        /// <param name="owner">监听所属者。</param>
        public static void RemoveAllRenderSubscriptions(this UnitDisplayComponent self, object owner)
            => self.OwnerUnit?.RemoveAllRenderSubscriptions(owner);

        /// <summary>
        /// 发布渲染单位事件。
        /// </summary>
        /// <typeparam name="T">渲染单位事件类型。</typeparam>
        /// <param name="self">渲染单位实例。</param>
        /// <param name="eventData">事件数据。</param>
        public static void PublishRender<T>(this UnitDisplayComponent self, T eventData)
            where T : struct, IUnitEvent
            => self.OwnerUnit?.PublishRender(eventData);
    }
}