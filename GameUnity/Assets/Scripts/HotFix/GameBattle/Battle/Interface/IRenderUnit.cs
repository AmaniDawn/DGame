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
        /// 从 LogicUnit 同步快照数据。
        /// </summary>
        void SyncFromLogic();
    }
}