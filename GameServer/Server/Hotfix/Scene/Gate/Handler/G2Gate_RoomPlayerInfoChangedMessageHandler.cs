using System.Collections.Generic;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace Hotfix;

public sealed class G2Gate_RoomPlayerInfoChangedMessageHandler : Address<Session, G2Gate_RoomPlayerInfoChangedMessage>
{
    protected override async FTask Run(Session entity, G2Gate_RoomPlayerInfoChangedMessage message)
    {
        var playerDataFlagComponent = entity.GetComponent<PlayerDataFlagComponent>();
        if (message.RoomId <= 0)
        {
            playerDataFlagComponent?.ClearRoom();
            entity.Send(new G2C_RoomPlayerInfoChangedNotify
            {
                RoomInfo = null,
                PlayerCount = 0,
                PlayerInfos = new List<CSRoomPlayerInfo>(),
            });

            await FTask.CompletedTask;
            return;
        }

        entity.Send(new G2C_RoomPlayerInfoChangedNotify
        {
            RoomInfo = new CSRoomInfo
            {
                RoomId = message.RoomId,
                RoomSeq = message.RoomSeq,
                CaptainRoleId = (ulong)message.CaptainRoleId,
            },
            PlayerCount = message.PlayerCount,
            PlayerInfos = message.PlayerInfos.ToCSRoomPlayerInfos(),
        });

        await FTask.CompletedTask;
    }
}
