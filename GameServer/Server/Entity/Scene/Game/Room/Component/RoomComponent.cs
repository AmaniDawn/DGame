using System.Collections.Generic;
using Fantasy.Entitas;

namespace Fantasy;

/// <summary>
/// 房间子场景元数据组件，挂载在房间 SubScene 上。
/// </summary>
public sealed class RoomComponent : Entity
{
    public int RoomId;

    public int RoomSeq;

    public long CaptainRoleId;

    public int MaxPlayerCount;

    public long CreateTime;

    public bool IsBattleStarted;

    public bool IsBattleEntered;

    public readonly Dictionary<long, RoomPlayerInfo> PlayerInfos = new Dictionary<long, RoomPlayerInfo>();
}
