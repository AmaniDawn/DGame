namespace GameLogic
{
    /// <summary>
    /// 怪物渲染单位。
    /// </summary>
    public class MonsterRender : RenderUnit
    {
        /// <summary>
        /// 获取怪物当前使用的模型配置 ID。
        /// </summary>
        /// <returns>模型配置 ID；当前默认返回 0。</returns>
        public override int GetModelID()
        {
            return 0;
        }
    }
}