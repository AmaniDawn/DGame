using System;
using Fantasy.Entitas.Interface;
using GameBattle;

namespace GameLogic
{
    /// <summary>
    /// 渲染单位唤醒系统。
    /// </summary>
    public sealed class RenderUnitAwakeSystem : AwakeSystem<RenderUnit>
    {
        /// <summary>
        /// 唤醒渲染单位。
        /// </summary>
        /// <param name="self">渲染单位实例。</param>
        protected override void Awake(RenderUnit self)
        {
            self.Awake();
        }
    }

    /// <summary>
    /// 渲染单位销毁系统。
    /// </summary>
    public sealed class RenderUnitDestroySystem : DestroySystem<RenderUnit>
    {
        /// <summary>
        /// 销毁渲染单位。
        /// </summary>
        /// <param name="self">渲染单位实例。</param>
        protected override void Destroy(RenderUnit self)
        {
            self.Destroy();
        }
    }

    /// <summary>
    /// 渲染单位扩展方法集合。
    /// </summary>
    public static class RenderUnitSystem
    {
        /// <summary>
        /// 获取当前绑定逻辑单位的唯一标识。
        /// </summary>
        /// <param name="self">渲染单位实例。</param>
        /// <returns>逻辑单位存在时返回其 UnitID，否则返回 0。</returns>
        public static ulong GetPlayerID(this RenderUnit self)
        {
            if (self.LogicUnit != null)
            {
                return self.LogicUnit.UnitID;
            }

            return 0;
        }

        /// <summary>
        /// 判断两个渲染单位是否表示同一运行时实例。
        /// </summary>
        /// <param name="self">当前渲染单位。</param>
        /// <param name="other">待比较的渲染单位。</param>
        /// <returns>两者均有效且 RuntimeId 相同时返回 true。</returns>
        public static bool IsSameUnit(this RenderUnit self, RenderUnit other)
            => other != null && self.RuntimeId != 0 && other.RuntimeId != 0 && self.RuntimeId == other.RuntimeId;

        /// <summary>
        /// 注册一个跟随渲染单位作用域自动释放的逻辑单位事件监听。
        /// </summary>
        /// <typeparam name="T">逻辑单位事件类型。</typeparam>
        /// <param name="self">渲染单位实例。</param>
        /// <param name="handler">事件回调。</param>
        public static void SubscribeLogicScoped<T>(this RenderUnit self, Action<T> handler) where T : struct, IUnitEvent
            => self.LogicUnit.SubscribeLogicScoped(self, self.Subscriptions, handler);

        /// <summary>
        /// 注册一个跟随指定作用域自动释放的渲染单位事件监听。
        /// </summary>
        /// <typeparam name="T">渲染单位事件类型。</typeparam>
        /// <param name="self">渲染单位实例。</param>
        /// <param name="owner">监听所属者。</param>
        /// <param name="scope">订阅作用域。</param>
        /// <param name="handler">事件回调。</param>
        public static void SubscribeRenderScoped<T>(this RenderUnit self, object owner, SubscriptionScopeComponent scope,
            Action<T> handler) where T : struct, IUnitEvent
        {
            self.SubscribeRender(owner, handler);
            scope.Add(() => self.UnsubscribeRender(handler));
        }

        /// <summary>
        /// 注册一个跟随当前渲染单位默认作用域自动释放的渲染单位事件监听。
        /// </summary>
        /// <typeparam name="T">渲染单位事件类型。</typeparam>
        /// <param name="self">渲染单位实例。</param>
        /// <param name="owner">监听所属者。</param>
        /// <param name="handler">事件回调。</param>
        public static void SubscribeRenderScoped<T>(this RenderUnit self, object owner, Action<T> handler) where T : struct, IUnitEvent
        {
            self.SubscribeRender(owner, handler);
            self.Subscriptions.Add(() => self.UnsubscribeRender(handler));
        }

        /// <summary>
        /// 注册一个以当前渲染单位为所属者并自动释放的渲染单位事件监听。
        /// </summary>
        /// <typeparam name="T">渲染单位事件类型。</typeparam>
        /// <param name="self">渲染单位实例。</param>
        /// <param name="handler">事件回调。</param>
        public static void SubscribeRenderScoped<T>(this RenderUnit self, Action<T> handler) where T : struct, IUnitEvent
        {
            self.SubscribeRender(self, handler);
            self.Subscriptions.Add(() => self.UnsubscribeRender(handler));
        }

        /// <summary>
        /// 注册渲染单位事件监听。
        /// </summary>
        /// <typeparam name="T">渲染单位事件类型。</typeparam>
        /// <param name="self">渲染单位实例。</param>
        /// <param name="owner">监听所属者。</param>
        /// <param name="handler">事件回调。</param>
        public static void SubscribeRender<T>(this RenderUnit self, object owner, Action<T> handler)
            where T : struct, IUnitEvent
            => self.UnitEventHub.Subscribe(owner, handler);

        /// <summary>
        /// 取消渲染单位事件监听。
        /// </summary>
        /// <typeparam name="T">渲染单位事件类型。</typeparam>
        /// <param name="self">渲染单位实例。</param>
        /// <param name="handler">事件回调。</param>
        public static void UnsubscribeRender<T>(this RenderUnit self, Action<T> handler)
            where T : struct, IUnitEvent
            => self.UnitEventHub.Unsubscribe(handler);

        /// <summary>
        /// 移除指定所属者注册的全部渲染单位事件监听。
        /// </summary>
        /// <param name="self">渲染单位实例。</param>
        /// <param name="owner">监听所属者。</param>
        public static void RemoveAllRenderSubscriptions(this RenderUnit self, object owner)
            => self.UnitEventHub.RemoveAll(owner);

        /// <summary>
        /// 发布渲染单位事件。
        /// </summary>
        /// <typeparam name="T">渲染单位事件类型。</typeparam>
        /// <param name="self">渲染单位实例。</param>
        /// <param name="eventData">事件数据。</param>
        public static void PublishRender<T>(this RenderUnit self, T eventData)
            where T : struct, IUnitEvent
            => self.UnitEventHub.Publish(eventData);
    }
}