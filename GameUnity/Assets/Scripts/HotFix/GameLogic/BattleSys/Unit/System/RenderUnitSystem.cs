using System;
using Fantasy.Entitas.Interface;
using GameBattle;

namespace GameLogic
{
    public sealed class RenderUnitAwakeSystem : AwakeSystem<RenderUnit>
    {
        protected override void Awake(RenderUnit self)
        {
            self.Awake();
        }
    }
    
    /// <summary>
    /// 渲染层单位销毁触发系统。
    /// </summary>
    public sealed class RenderUnitDestroySystem : DestroySystem<RenderUnit>
    {
        protected override void Destroy(RenderUnit self)
        {
            self.Destroy();
        }
    }

    /// <summary>
    /// 渲染层单位扩展系统入口。
    /// </summary>
    public static class RenderUnitSystem
    {
        /// <summary>
        /// 获取当前绑定逻辑单位的标识。
        /// </summary>
        /// <returns>逻辑单位存在时返回其标识，否则返回 0。</returns>
        public static ulong GetPlayerID(this RenderUnit self)
        {
            if (self.LogicUnit != null)
            {
                return self.LogicUnit.UnitID;
            }

            return 0;
        }
        
        /// <summary>
        /// 判断两个渲染单位是否表示同一个运行时实例。
        /// </summary>
        /// <param name="self">渲染单位。</param>
        /// <param name="other">待比较的渲染单位。</param>
        /// <returns>是同一个有效运行时实例时返回 <see langword="true"/>。</returns>
        public static bool IsSameUnit(this RenderUnit self, RenderUnit other)
            => other != null && self.RuntimeId != 0 && other.RuntimeId != 0 && self.RuntimeId == other.RuntimeId;

        public static void SubscribeScoped<T>(this RenderUnit self, Action<T> handler) where T : struct, IUnitEvent 
            => self.LogicUnit.SubscribeScoped(self, self.Subscriptions, handler);

        /// <summary>
        /// 注册一个跟随作用域自动释放的单位事件监听。
        /// </summary>
        /// <typeparam name="T">单位事件类型。</typeparam>
        /// <param name="self">渲染单位。</param>
        /// <param name="owner">监听所属者。</param>
        /// <param name="scope">订阅作用域。</param>
        /// <param name="handler">事件回调。</param>
        public static void SubscribeScoped<T>(this RenderUnit self, object owner, SubscriptionScopeComponent scope,
            Action<T> handler) where T : struct, IUnitEvent
        {
            self.Subscribe(owner, handler);
            scope.Add(() => self.Unsubscribe(handler));
        }

        /// <summary>
        /// 注册单位事件监听。
        /// </summary>
        /// <typeparam name="T">单位事件类型。</typeparam>
        /// <param name="self">渲染单位。</param>
        /// <param name="owner">监听所属者。</param>
        /// <param name="handler">事件回调。</param>
        public static void Subscribe<T>(this RenderUnit self, object owner, Action<T> handler)
            where T : struct, IUnitEvent
            => self.UnitEventHub.Subscribe(owner, handler);

        /// <summary>
        /// 取消单位事件监听。
        /// </summary>
        /// <typeparam name="T">单位事件类型。</typeparam>
        /// <param name="self">渲染单位。</param>
        /// <param name="handler">事件回调。</param>
        public static void Unsubscribe<T>(this RenderUnit self, Action<T> handler)
            where T : struct, IUnitEvent
            => self.UnitEventHub.Unsubscribe(handler);

        /// <summary>
        /// 移除指定所属者的全部单位事件监听。
        /// </summary>
        /// <param name="self">渲染单位。</param>
        /// <param name="owner">监听所属者。</param>
        public static void RemoveAll(this RenderUnit self, object owner)
            => self.UnitEventHub.RemoveAll(owner);

        /// <summary>
        /// 发布单位事件。
        /// </summary>
        /// <typeparam name="T">单位事件类型。</typeparam>
        /// <param name="self">渲染单位。</param>
        /// <param name="eventData">事件数据。</param>
        public static void Publish<T>(this RenderUnit self, T eventData)
            where T : struct, IUnitEvent
            => self.UnitEventHub.Publish(eventData);
    }
}