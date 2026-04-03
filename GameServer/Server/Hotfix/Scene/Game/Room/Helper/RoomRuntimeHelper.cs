using System;
using System.Collections.Generic;
using System.Linq;
using Fantasy;
using GameProto;

namespace Hotfix;

/// <summary>
/// 房间运行时辅助方法。
/// </summary>
public static class RoomRuntimeHelper
{
    /// <summary>
    /// 将服内创建房间请求转换为房间玩家快照。
    /// </summary>
    /// <param name="request">服内创建房间请求。</param>
    /// <returns>房间玩家快照。</returns>
    public static RoomPlayerInfo ToRoomPlayerInfo(this G2Game_CreateRoomRequest request)
        => new()
        {
            RoleId = request.RoleId,
            AccountId = request.AccountId,
            ServerId = request.ServerId,
            SessionRuntimeId = request.SessionRuntimeId,
            RoleName = request.RoleName,
            HeadId = request.HeadId,
            Sex = request.Sex,
            Level = request.Level,
            FightValue = request.FightValue,
        };

    /// <summary>
    /// 将服内加入房间请求转换为房间玩家快照。
    /// </summary>
    /// <param name="request">服内加入房间请求。</param>
    /// <returns>房间玩家快照。</returns>
    public static RoomPlayerInfo ToRoomPlayerInfo(this G2Game_JoinRoomRequest request)
        => new()
        {
            RoleId = request.RoleId,
            AccountId = request.AccountId,
            ServerId = request.ServerId,
            SessionRuntimeId = request.SessionRuntimeId,
            RoleName = request.RoleName,
            HeadId = request.HeadId,
            Sex = request.Sex,
            Level = request.Level,
            FightValue = request.FightValue,
        };

    /// <summary>
    /// 向房间中添加或更新玩家快照。
    /// </summary>
    /// <param name="roomComponent">房间组件。</param>
    /// <param name="playerInfo">玩家快照。</param>
    public static void AddOrUpdatePlayer(this RoomComponent roomComponent, RoomPlayerInfo playerInfo)
    {
        ArgumentNullException.ThrowIfNull(playerInfo);
        playerInfo.IsBattleReady = false;
        playerInfo.IsBattleLoaded = false;
        roomComponent.PlayerInfos[playerInfo.RoleId] = playerInfo;
        roomComponent.ClearBattleProgress();
        roomComponent.SyncFramePlayerCount();
    }

    /// <summary>
    /// 获取按加入时间排序后的房间玩家快照。
    /// </summary>
    /// <param name="roomComponent">房间组件。</param>
    /// <returns>有序的房间玩家快照列表。</returns>
    public static List<RoomPlayerInfo> GetOrderedPlayers(this RoomComponent roomComponent)
        => roomComponent.PlayerInfos.Values
            .OrderBy(static player => player.JoinTime)
            .ThenBy(static player => player.RoleId)
            .ToList();

    /// <summary>
    /// 同步房间当前玩家数量到帧同步组件。
    /// </summary>
    /// <param name="roomComponent">房间组件。</param>
    public static void SyncFramePlayerCount(this RoomComponent roomComponent)
    {
        var frameSyncComponent = roomComponent.GetComponent<FrameSyncComponent>();
        if (frameSyncComponent != null)
        {
            frameSyncComponent.PlayerCount = roomComponent.PlayerInfos.Count;
        }
    }

    /// <summary>
    /// 将房间玩家快照转换为服内返回结构。
    /// </summary>
    /// <param name="playerInfo">房间玩家快照。</param>
    /// <returns>服内玩家信息。</returns>
    public static InnerRoomPlayerInfo ToInnerRoomPlayerInfo(this RoomPlayerInfo playerInfo)
        => new()
        {
            RoleId = playerInfo.RoleId,
            RoleName = playerInfo.RoleName,
            Level = playerInfo.Level,
            FightValue = playerInfo.FightValue,
        };
}
