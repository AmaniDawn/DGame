namespace GameLogic
{
    /// <summary>
    /// 玩家渲染单位。
    /// </summary>
    public class PlayerRender : RenderUnit
    {
        /// <summary>
        /// 获取玩家当前使用的模型配置 ID。
        /// </summary>
        /// <returns>模型配置 ID；当前默认返回 0。</returns>
        public override int GetModelID()
        {
            return 0;
        }
    }
}