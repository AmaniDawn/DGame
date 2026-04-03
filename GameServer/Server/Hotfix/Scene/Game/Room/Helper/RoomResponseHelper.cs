using System.Collections.Generic;
using Fantasy;
using GameProto;

namespace Hotfix;

/// <summary>
/// 房间响应填充辅助方法。
/// </summary>
public static class RoomResponseHelper
{
    /// <summary>
    /// 用房间组件填充创建房间服内响应。
    /// </summary>
    /// <param name="response">服内创建房间响应。</param>
    /// <param name="roomComponent">房间组件。</param>
    public static void Fill(this G2Game_CreateRoomResponse response, RoomComponent roomComponent)
    {
        response.RoomId = roomComponent.RoomId;
        response.RoomSeq = roomComponent.RoomSeq;
        response.PlayerCount = roomComponent.GetPlayerCount();
        response.CaptainRoleId = roomComponent.CaptainRoleId;
        response.PlayerInfos = new List<InnerRoomPlayerInfo>();

        foreach (var playerInfo in roomComponent.GetOrderedPlayers())
        {
            response.PlayerInfos.Add(playerInfo.ToInnerRoomPlayerInfo());
        }
    }

    /// <summary>
    /// 用房间组件填充加入房间服内响应。
    /// </summary>
    /// <param name="response">服内加入房间响应。</param>
    /// <param name="roomComponent">房间组件。</param>
    public static void Fill(this G2Game_JoinRoomResponse response, RoomComponent roomComponent)
    {
        response.RoomId = roomComponent.RoomId;
        response.RoomSeq = roomComponent.RoomSeq;
        response.PlayerCount = roomComponent.GetPlayerCount();
        response.CaptainRoleId = roomComponent.CaptainRoleId;
        response.PlayerInfos = new List<InnerRoomPlayerInfo>();

        foreach (var playerInfo in roomComponent.GetOrderedPlayers())
        {
            response.PlayerInfos.Add(playerInfo.ToInnerRoomPlayerInfo());
        }
    }

    /// <summary>
    /// 将服内房间玩家信息转换为客户端协议对象列表。
    /// </summary>
    /// <param name="playerInfos">服内房间玩家列表。</param>
    /// <returns>客户端房间玩家列表。</returns>
    public static List<CSRoomPlayerInfo> ToCSRoomPlayerInfos(this List<InnerRoomPlayerInfo>? playerInfos)
    {
        var result = new List<CSRoomPlayerInfo>();
        if (playerInfos == null)
        {
            return result;
        }

        foreach (var playerInfo in playerInfos)
        {
            result.Add(new CSRoomPlayerInfo
            {
                RoleId = (ulong)playerInfo.RoleId,
                RoleName = playerInfo.RoleName,
                Level = playerInfo.Level,
                FightValue = playerInfo.FightValue,
            });
        }

        return result;
    }
}
