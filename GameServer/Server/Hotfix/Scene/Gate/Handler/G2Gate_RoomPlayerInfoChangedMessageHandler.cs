using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace Hotfix;

/// <summary>
/// 将 GameScene 下发的房间成员变化转发给 Gate 内的在线会话。
/// </summary>
public sealed class G2Gate_RoomPlayerInfoChangedMessageHandler : Address<Scene, G2Gate_RoomPlayerInfoChangedMessage>
{
    protected override async FTask Run(Scene entity, G2Gate_RoomPlayerInfoChangedMessage message)
    {
        if (message.SessionRuntimeIds == null || message.SessionRuntimeIds.Count <= 0)
        {
            return;
        }

        for (int i = 0; i < message.SessionRuntimeIds.Count; i++)
        {
            if (!entity.TryGetEntity<Session>(message.SessionRuntimeIds[i], out var session))
            {
                continue;
            }

            session.Send(new G2C_RoomPlayerInfoChangedNotify
            {
                RoomInfo = new CSRoomInfo
                {
                    RoomId = message.RoomId,
                    RoomSeq = message.RoomSeq,
                },
                PlayerCount = message.PlayerCount,
                PlayerInfos = message.PlayerInfos.ToCSRoomPlayerInfos(),
            });
        }

        await FTask.CompletedTask;
    }
}
