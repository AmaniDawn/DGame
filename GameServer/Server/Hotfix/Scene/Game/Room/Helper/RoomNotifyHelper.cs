using System.Collections.Generic;
using System.Linq;
using Fantasy;

namespace Hotfix;

public static class RoomNotifyHelper
{
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
                CaptainRoleId = roomComponent.CaptainRoleId,
            });
        }
    }

    public static void BroadcastRoomDismissed(this Scene scene, RoomComponent roomComponent)
    {
        var players = roomComponent.GetOrderedPlayers();
        foreach (var player in players)
        {
            if (player.SessionRuntimeId == 0)
            {
                continue;
            }

            scene.Send(player.SessionRuntimeId, new G2Gate_RoomPlayerInfoChangedMessage
            {
                SessionRuntimeIds = new List<long> { player.SessionRuntimeId },
                RoomId = 0,
                RoomSeq = 0,
                PlayerCount = 0,
                PlayerInfos = new List<InnerRoomPlayerInfo>(),
                CaptainRoleId = 0,
            });
        }
    }
}
