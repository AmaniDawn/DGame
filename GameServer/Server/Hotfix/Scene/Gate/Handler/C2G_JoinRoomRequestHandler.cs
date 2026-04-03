using System;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace Hotfix;

/// <summary>
/// 处理客户端加入房间请求。
/// </summary>
public sealed class C2G_JoinRoomRequestHandler : MessageRPC<C2G_JoinRoomRequest, G2C_JoinRoomResponse>
{
    protected override async FTask Run(Session session, C2G_JoinRoomRequest request, G2C_JoinRoomResponse response, Action reply)
    {
        if (!RoomIdHelper.IsValid(request.RoomId))
        {
            response.ErrorCode = ErrorCode.ROOM_INVALID_PARAMETER;
            return;
        }

        var playerDataFlagComponent = session.GetComponent<PlayerDataFlagComponent>();
        if (playerDataFlagComponent == null)
        {
            response.ErrorCode = ErrorCode.ROOM_INVALID_PARAMETER;
            return;
        }

        PlayerData playerData = playerDataFlagComponent?.playerData;
        if (playerData == null || playerData.IsDisposed)
        {
            response.ErrorCode = ErrorCode.ROOM_INVALID_PARAMETER;
            return;
        }

        if (playerDataFlagComponent.HasRoom())
        {
            response.ErrorCode = ErrorCode.ROOM_ALREADY_JOINED;
            return;
        }

        var gameSceneConfig = RoomGateHelper.GetGameSceneConfigByRoomId(request.RoomId);
        if (gameSceneConfig == null)
        {
            response.ErrorCode = ErrorCode.ROOM_NOT_FOUND;
            return;
        }

        var innerResponse = await session.Scene.Call(gameSceneConfig.Address, playerData.ToJoinRoomRequest(request.RoomId));
        if (innerResponse is not G2Game_JoinRoomResponse joinRoomResponse)
        {
            response.ErrorCode = ErrorCode.ROOM_NOT_FOUND;
            return;
        }

        if (joinRoomResponse.ErrorCode != ErrorCode.SUCCESS)
        {
            response.ErrorCode = joinRoomResponse.ErrorCode;
            return;
        }

        response.ErrorCode = ErrorCode.SUCCESS;
        response.RoomInfo = new CSRoomInfo
        {
            RoomId = joinRoomResponse.RoomId,
            RoomSeq = joinRoomResponse.RoomSeq,
            CaptainRoleId = (ulong)joinRoomResponse.CaptainRoleId,
        };
        response.PlayerCount = joinRoomResponse.PlayerCount;
        response.PlayerInfos = joinRoomResponse.PlayerInfos.ToCSRoomPlayerInfos();
        playerDataFlagComponent.SetRoom(joinRoomResponse.RoomId);
    }
}
