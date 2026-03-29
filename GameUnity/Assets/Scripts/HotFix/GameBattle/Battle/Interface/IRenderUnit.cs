using Fantasy.Entitas;

namespace GameBattle
{
    /// <summary>
    /// 渲染层单位接口。
    /// <remarks>定义渲染层单位接收逻辑层事件通知的能力</remarks>
    /// </summary>
    public interface IRenderUnit
    {
        /// <summary>
        /// 接收来自逻辑层单位的事件通知，进行相应的渲染处理。
        /// </summary>
        /// <param name="eventId">事件标识。</param>
        void OnUnitEvent(int eventId);
        
        void SyncFromLogic();
    }
}