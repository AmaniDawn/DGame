using System;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network.Interface;

namespace Hotfix;

public sealed class G2Game_StartBattleRequestHandler : AddressRPC<Scene, G2Game_StartBattleRequest, G2Game_StartBattleResponse>
{
    protected override async FTask Run(Scene entity, G2Game_StartBattleRequest request, G2Game_StartBattleResponse response, Action reply)
    {
        if (!RoomIdHelper.IsValid(request.RoomId) || request.RoleId <= 0)
        {
            response.ErrorCode = ErrorCode.ROOM_INVALID_PARAMETER;
            return;
        }

        if (!entity.TryGetRoom(request.RoomId, out var roomScene) || roomScene == null)
        {
            response.ErrorCode = ErrorCode.ROOM_NOT_FOUND;
            return;
        }

        var roomComponent = roomScene.GetComponent<RoomComponent>();
        if (roomComponent == null || !roomComponent.TryGetPlayer(request.RoleId, out _))
        {
            response.ErrorCode = ErrorCode.ROOM_NOT_FOUND;
            return;
        }

        if (roomComponent.CaptainRoleId != request.RoleId)
        {
            response.ErrorCode = ErrorCode.ROOM_INVALID_PARAMETER;
            return;
        }

        if (roomComponent.MaxPlayerCount > 0 && roomComponent.GetPlayerCount() < roomComponent.MaxPlayerCount)
        {
            response.ErrorCode = ErrorCode.ROOM_PLAYER_COUNT_INVALID;
            return;
        }

        response.ErrorCode = ErrorCode.SUCCESS;

        if (roomComponent.IsBattleEntered)
        {
            return;
        }

        if (roomComponent.IsBattleStarted)
        {
            response.FillBattleLoadingSessions(roomComponent);
            return;
        }

        roomComponent.BeginBattleLoading();
        response.FillBattleLoadingSessions(roomComponent);
        await FTask.CompletedTask;
    }
}
