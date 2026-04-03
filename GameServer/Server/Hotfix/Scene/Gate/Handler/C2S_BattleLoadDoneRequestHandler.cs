using System;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace Hotfix;

public sealed class C2S_BattleLoadDoneRequestHandler : MessageRPC<C2S_BattleLoadDoneRequest, S2C_BattleLoadDoneResponse>
{
    protected override async FTask Run(Session session, C2S_BattleLoadDoneRequest request, S2C_BattleLoadDoneResponse response, Action reply)
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

        var innerResponse = await session.Scene.Call(gameSceneConfig.Address, new G2Game_BattleLoadDoneRequest
        {
            RoomId = roomId,
            RoleId = playerData.Id,
        });

        if (innerResponse is not G2Game_StartBattleResponse battleLoadDoneResponse)
        {
            response.ErrorCode = ErrorCode.ROOM_CREATE_FAILED;
            return;
        }

        response.ErrorCode = battleLoadDoneResponse.ErrorCode;
        if (battleLoadDoneResponse.ErrorCode != ErrorCode.SUCCESS || battleLoadDoneResponse.SessionRuntimeIds.Count == 0)
        {
            return;
        }

        session.Scene.BroadcastEnterBattle(battleLoadDoneResponse);
    }
}
