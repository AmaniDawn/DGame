using Fantasy.Entitas;

namespace Fantasy;

public class PlayerManagerComponent : Entity
{
    /// <summary>
    /// 玩家数据缓存字典 RoleID => key
    /// </summary>
    public readonly Dictionary<long, PlayerData> PlayerDataDict = new Dictionary<long, PlayerData>();
}