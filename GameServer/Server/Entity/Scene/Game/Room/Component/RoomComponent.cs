using Fantasy.Entitas;

namespace Fantasy;

/// <summary>
/// 房间子场景元数据组件，挂载在房间 SubScene 上。
/// </summary>
public sealed class RoomComponent : Entity
{
    /// <summary>
    /// 业务房间 ID。
    /// </summary>
    public int RoomId;

    /// <summary>
    /// 房间序号。
    /// </summary>
    public int RoomSeq;

    /// <summary>
    /// 房间最大玩家数量。
    /// </summary>
    public int MaxPlayerCount;

    /// <summary>
    /// 房间创建时间。
    /// </summary>
    public long CreateTime;

    /// <summary>
    /// 当前房间玩家信息。
    /// Key: RoleId。
    /// </summary>
    public readonly Dictionary<long, RoomPlayerInfo> PlayerInfos = new Dictionary<long, RoomPlayerInfo>();
}
