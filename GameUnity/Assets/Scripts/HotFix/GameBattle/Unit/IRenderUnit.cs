namespace GameBattle
{
    /// <summary>
    /// 渲染层单位接口。
    /// </summary>
    public interface IRenderUnit
    {
        /// <summary>
        /// 向渲染层单位派发事件通知。
        /// </summary>
        /// <param name="eventId">事件标识。</param>
        void OnUnitEvent(int eventId);
    }
}