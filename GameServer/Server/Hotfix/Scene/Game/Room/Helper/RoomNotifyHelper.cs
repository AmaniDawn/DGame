using System.Collections.Generic;
using System.Linq;
using Fantasy;
using Fantasy.IdFactory;
using Fantasy.Platform.Net;

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

        var gateSceneSessions = new Dictionary<long, List<long>>();
        foreach (var player in players)
        {
            if (player.SessionRuntimeId <= 0)
            {
                continue;
            }

            var gateSceneId = IdFactoryHelper.RuntimeIdTool.GetSceneId(player.SessionRuntimeId);
            if (!SceneConfigData.Instance.TryGet(gateSceneId, out var gateSceneConfig))
            {
                continue;
            }

            if (!gateSceneSessions.TryGetValue(gateSceneConfig.Address, out var sessionRuntimeIds))
            {
                sessionRuntimeIds = new List<long>();
                gateSceneSessions.Add(gateSceneConfig.Address, sessionRuntimeIds);
            }

            sessionRuntimeIds.Add(player.SessionRuntimeId);
        }

        if (gateSceneSessions.Count <= 0)
        {
            return;
        }

        var roomPlayerInfos = players.Select(static player => player.ToInnerRoomPlayerInfo()).ToList();
        foreach (var gateSceneSession in gateSceneSessions)
        {
            scene.Send(gateSceneSession.Key, new G2Gate_RoomPlayerInfoChangedMessage
            {
                SessionRuntimeIds = gateSceneSession.Value,
                RoomId = roomComponent.RoomId,
                RoomSeq = roomComponent.RoomSeq,
                PlayerCount = roomComponent.GetPlayerCount(),
                PlayerInfos = roomPlayerInfos,
            });
        }
    }
}