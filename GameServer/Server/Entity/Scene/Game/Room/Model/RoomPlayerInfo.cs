namespace Fantasy;

/// <summary>
/// 房间内玩家的运行时快照信息。
/// </summary>
public sealed class RoomPlayerInfo
{
    /// <summary>
    /// 角色 ID。
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// 账号 ID。
    /// </summary>
    public long AccountId { get; set; }

    /// <summary>
    /// 服务器 ID。
    /// </summary>
    public int ServerId { get; set; }

    /// <summary>
    /// 玩家当前 SessionRuntimeId。
    /// </summary>
    public long SessionRuntimeId { get; set; }

    /// <summary>
    /// 角色名称。
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// 头像 ID。
    /// </summary>
    public int HeadId { get; set; }

    /// <summary>
    /// 性别。
    /// </summary>
    public byte Sex { get; set; }

    /// <summary>
    /// 等级。
    /// </summary>
    public uint Level { get; set; }

    /// <summary>
    /// 战斗力。
    /// </summary>
    public uint FightValue { get; set; }

    /// <summary>
    /// 加入房间时间。
    /// </summary>
    public long JoinTime { get; set; }
}
