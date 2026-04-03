using System;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace Hotfix;

/// <summary>
/// 处理客户端创建房间请求。
/// </summary>
public sealed class C2G_CreateRoomRequestHandler : MessageRPC<C2G_CreateRoomRequest, G2C_CreateRoomResponse>
{
    protected override async FTask Run(Session session, C2G_CreateRoomRequest request, G2C_CreateRoomResponse response, Action reply)
    {
        if (request.PlayerCount <= 0)
        {
            response.ErrorCode = ErrorCode.ROOM_PLAYER_COUNT_INVALID;
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

        var gameSceneConfig = session.Scene.GetGameSceneConfig();
        if (gameSceneConfig == null)
        {
            response.ErrorCode = ErrorCode.ROOM_CREATE_FAILED;
            return;
        }

        var innerResponse = await session.Scene.Call(gameSceneConfig.Address, playerData.ToCreateRoomRequest(request.PlayerCount));
        if (innerResponse is not G2Game_CreateRoomResponse createRoomResponse)
        {
            response.ErrorCode = ErrorCode.ROOM_CREATE_FAILED;
            return;
        }

        if (createRoomResponse.ErrorCode != ErrorCode.SUCCESS)
        {
            response.ErrorCode = createRoomResponse.ErrorCode;
            return;
        }

        response.ErrorCode = ErrorCode.SUCCESS;
        response.RoomInfo = new CSRoomInfo
        {
            RoomId = createRoomResponse.RoomId,
            RoomSeq = createRoomResponse.RoomSeq,
        };
        response.PlayerCount = createRoomResponse.PlayerCount;
        response.PlayerInfos = createRoomResponse.PlayerInfos.ToCSRoomPlayerInfos();
        playerDataFlagComponent.SetRoom(createRoomResponse.RoomId);
    }
}
