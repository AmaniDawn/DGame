using System.Collections.Generic;
using System.Linq;
using Fantasy;

namespace Hotfix;

/// <summary>
/// 房间成员变化通知辅助方法。
/// </summary>
public static class RoomNotifyHelper
{
    /// <summary>
    /// 将当前房间的玩家列表广播给房间内在线玩家。
    /// </summary>
    /// <param name="scene">GameScene。</param>
    /// <param name="roomComponent">房间组件。</param>
    public static void BroadcastRoomPlayerInfos(this Scene scene, RoomComponent roomComponent)
    {
        var players = roomComponent.GetOrderedPlayers();
        if (players.Count <= 0)
        {
            return;
        }

        var roomPlayerInfos = players.Select(static player => player.ToInnerRoomPlayerInfo()).ToList();
        foreach (var player in players)
        {
            if (player.SessionRuntimeId == 0)
            {
                continue;
            }

            scene.Send(player.SessionRuntimeId, new G2Gate_RoomPlayerInfoChangedMessage
            {
                SessionRuntimeIds = new List<long> { player.SessionRuntimeId },
                RoomId = roomComponent.RoomId,
                RoomSeq = roomComponent.RoomSeq,
                PlayerCount = roomComponent.GetPlayerCount(),
                PlayerInfos = roomPlayerInfos,
            });
        }
    }
}
