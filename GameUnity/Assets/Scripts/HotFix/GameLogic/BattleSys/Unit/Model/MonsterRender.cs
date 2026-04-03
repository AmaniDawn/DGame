using GameProto;

namespace GameLogic
{
    /// <summary>
    /// 怪物渲染单位。
    /// </summary>
    public sealed class MonsterRender : RenderUnit
    {
        private MonsterConfig m_monsterConfig;

        public MonsterConfig MonsterConfig => m_monsterConfig ??= MonsterConfigMgr.Instance.GetOrDefault((int)GetConfigId());

        /// <summary>
        /// 获取怪物当前使用的模型配置 ID。
        /// </summary>
        /// <returns>模型配置 ID；当前默认返回 0。</returns>
        public override int GetModelID() => MonsterConfig?.ModelID ?? 0;
    }
}