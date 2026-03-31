using DGame;

namespace GameBattle
{
    /// <summary>
    /// 单位类型定义。
    /// </summary>
    public enum UnitType
    {
        /// <summary>
        /// 未区分具体类型。
        /// </summary>
        [DisplayName("不区分")]
        None,

        /// <summary>
        /// 玩家单位。
        /// </summary>
        [DisplayName("玩家")]
        GamePlayer,

        /// <summary>
        /// 怪物单位。
        /// </summary>
        [DisplayName("怪物")]
        Monster,
    }
}