using System;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace Hotfix;

public sealed class C2S_StartBattleRequestHandler : MessageRPC<C2S_StartBattleRequest, S2C_StartBattleResponse>
{
    protected override async FTask Run(Session session, C2S_StartBattleRequest request, S2C_StartBattleResponse response, Action reply)
    {
        var playerDataFlagComponent = session.GetComponent<PlayerDataFlagComponent>();
        if (playerDataFlagComponent == null || !playerDataFlagComponent.HasRoom())
        {
            response.ErrorCode = ErrorCode.ROOM_NOT_FOUND;
            return;
        }

        var roomId = playerDataFlagComponent.CurrentRoomId;
        if (!RoomIdHelper.IsValid(roomId))
        {
            response.ErrorCode = ErrorCode.ROOM_NOT_FOUND;
            return;
        }

        PlayerData playerData = playerDataFlagComponent.playerData;
        if (playerData == null || playerData.IsDisposed)
        {
            response.ErrorCode = ErrorCode.ROOM_INVALID_PARAMETER;
            return;
        }

        var gameSceneConfig = RoomGateHelper.GetGameSceneConfigByRoomId(roomId);
        if (gameSceneConfig == null)
        {
            response.ErrorCode = ErrorCode.ROOM_NOT_FOUND;
            return;
        }

        var innerResponse = await session.Scene.Call(gameSceneConfig.Address, new G2Game_StartBattleRequest
        {
            RoomId = roomId,
            RoleId = playerData.Id,
        });

        if (innerResponse is not G2Game_StartBattleResponse startBattleResponse)
        {
            response.ErrorCode = ErrorCode.ROOM_CREATE_FAILED;
            return;
        }

        response.ErrorCode = startBattleResponse.ErrorCode;
        if (startBattleResponse.ErrorCode != ErrorCode.SUCCESS || startBattleResponse.SessionRuntimeIds.Count == 0)
        {
            return;
        }

        session.Scene.BroadcastBattleLoading(startBattleResponse.SessionRuntimeIds);
    }
}
