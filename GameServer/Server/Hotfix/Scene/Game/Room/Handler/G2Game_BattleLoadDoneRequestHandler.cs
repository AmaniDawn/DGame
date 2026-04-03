using System;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network.Interface;

namespace Hotfix;

public sealed class G2Game_BattleLoadDoneRequestHandler : AddressRPC<Scene, G2Game_BattleLoadDoneRequest, G2Game_StartBattleResponse>
{
    protected override async FTask Run(Scene entity, G2Game_BattleLoadDoneRequest request, G2Game_StartBattleResponse response, Action reply)
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

        if (!roomComponent.IsBattleStarted)
        {
            response.ErrorCode = ErrorCode.ROOM_INVALID_PARAMETER;
            return;
        }

        response.ErrorCode = ErrorCode.SUCCESS;

        if (roomComponent.IsBattleEntered)
        {
            return;
        }

        if (!roomComponent.MarkBattleLoaded(request.RoleId))
        {
            response.ErrorCode = ErrorCode.ROOM_NOT_FOUND;
            return;
        }

        if (!roomComponent.AreAllPlayersBattleLoaded())
        {
            return;
        }

        if (!roomComponent.TryGetBattleChapterConfig(out var chapterConfig) || chapterConfig == null)
        {
            roomComponent.ClearBattleProgress();
            response.ErrorCode = ErrorCode.ROOM_CREATE_FAILED;
            return;
        }

        roomComponent.IsBattleEntered = true;
        response.FillBattleStart(roomComponent, chapterConfig);
        await FTask.CompletedTask;
    }
}
