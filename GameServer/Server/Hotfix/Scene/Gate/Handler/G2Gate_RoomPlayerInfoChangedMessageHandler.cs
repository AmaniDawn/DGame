using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace Hotfix;

/// <summary>
/// 将 GameScene 下发的房间成员变化转发给 Gate 内的在线会话。
/// </summary>
public sealed class G2Gate_RoomPlayerInfoChangedMessageHandler : Address<Session, G2Gate_RoomPlayerInfoChangedMessage>
{
    protected override async FTask Run(Session entity, G2Gate_RoomPlayerInfoChangedMessage message)
    {
        entity.Send(new G2C_RoomPlayerInfoChangedNotify
        {
            RoomInfo = new CSRoomInfo
            {
                RoomId = message.RoomId,
                RoomSeq = message.RoomSeq,
            },
            PlayerCount = message.PlayerCount,
            PlayerInfos = message.PlayerInfos.ToCSRoomPlayerInfos(),
        });

        await FTask.CompletedTask;
    }
}
