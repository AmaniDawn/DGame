namespace GameBattle
{
    public enum UnitState
    {
        /// <summary>
        /// 无状态
        /// </summary>
        None,
        
        /// <summary>
        /// 待机
        /// </summary>
        Idle,
        
        /// <summary>
        /// 移动
        /// </summary>
        Move,
        
        /// <summary>
        /// 释放技能
        /// </summary>
        Skill,
        
        /// <summary>
        /// 死亡
        /// </summary>
        Die,
        
        /// <summary>
        /// 击晕
        /// </summary>
        Stun,
        
        /// <summary>
        /// 出现
        /// </summary>
        Appear,
    }
}