using System;

namespace GameBattle
{
    /// <summary>
    /// 事件订阅记录。
    /// </summary>
    internal sealed class EventSubscription
    {
        /// <summary>
        /// 获取订阅所属者。
        /// </summary>
        public object Owner { get; }
        /// <summary>
        /// 获取事件回调。
        /// </summary>
        public Delegate Handler { get; }
        /// <summary>
        /// 获取或设置是否已标记为移除。
        /// </summary>
        public bool Removed { get; set; }

        /// <summary>
        /// 初始化事件订阅记录。
        /// </summary>
        /// <param name="owner">订阅所属者。</param>
        /// <param name="handler">事件回调。</param>
        public EventSubscription(object owner, Delegate handler)
        {
            Owner = owner;
            Handler = handler;
        }
    }
}