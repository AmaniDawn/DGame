using DGame;

namespace GameBattle
{
    public enum UnitType
    {
        [DisplayName("不区分")]
        None,
        [DisplayName("玩家")]
        GamePlayer,
        [DisplayName("怪物")]
        Monster,
    }
}