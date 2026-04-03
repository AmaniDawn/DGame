using Fantasy.Entitas;
// ReSharper disable InconsistentNaming

namespace Fantasy;

/// <summary>
/// 账号异常下线组件
/// </summary>
public sealed class PlayerDataFlagComponent : Entity
{
    /// <summary>
    /// 当前会话关联的在线玩家实体。
    /// </summary>
    public EntityReference<PlayerData> playerData { get; set; }

    /// <summary>
    /// 当前所在房间 ID。
    /// </summary>
    public int CurrentRoomId { get; set; }
}
