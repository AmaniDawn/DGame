using System;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace Hotfix;

/// <summary>
/// 处理客户端离开房间请求。
/// </summary>
public sealed class C2G_LeaveRoomRequestHandler : MessageRPC<C2G_LeaveRoomRequest, G2C_LeaveRoomResponse>
{
    protected override async FTask Run(Session session, C2G_LeaveRoomRequest request, G2C_LeaveRoomResponse response, Action reply)
    {
        var playerDataFlagComponent = session.GetComponent<PlayerDataFlagComponent>();
        if (playerDataFlagComponent == null)
        {
            response.ErrorCode = ErrorCode.SUCCESS;
            return;
        }

        if (!playerDataFlagComponent.HasRoom())
        {
            response.ErrorCode = ErrorCode.SUCCESS;
            return;
        }

        var roomId = playerDataFlagComponent.CurrentRoomId;
        var gameSceneConfig = RoomGateHelper.GetGameSceneConfigByRoomId(roomId);
        if (gameSceneConfig == null)
        {
            playerDataFlagComponent.ClearRoom();
            response.ErrorCode = ErrorCode.SUCCESS;
            return;
        }

        PlayerData playerData = playerDataFlagComponent.playerData;
        if (playerData == null || playerData.IsDisposed)
        {
            playerDataFlagComponent.ClearRoom();
            response.ErrorCode = ErrorCode.SUCCESS;
            return;
        }

        var innerResponse = await session.Scene.Call(gameSceneConfig.Address, new G2Game_LeaveRoomRequest
        {
            RoomId = roomId,
            RoleId = playerData.Id,
        });

        if (innerResponse is not G2Game_LeaveRoomResponse leaveRoomResponse)
        {
            response.ErrorCode = ErrorCode.ROOM_NOT_FOUND;
            return;
        }

        response.ErrorCode = leaveRoomResponse.ErrorCode;
        if (leaveRoomResponse.ErrorCode == ErrorCode.SUCCESS)
        {
            playerDataFlagComponent.ClearRoom();
        }
    }
}
