using System;
using Fantasy;
using Fantasy.Async;
using Fantasy.Helper;
using Fantasy.Network.Interface;

namespace Hotfix;

/// <summary>
/// 处理 Gate 转发到 GameScene 的加入房间请求。
/// </summary>
public sealed class G2Game_JoinRoomRequestHandler : AddressRPC<Scene, G2Game_JoinRoomRequest, G2Game_JoinRoomResponse>
{
    protected override async FTask Run(Scene entity, G2Game_JoinRoomRequest request, G2Game_JoinRoomResponse response, Action reply)
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
        if (roomComponent == null)
        {
            response.ErrorCode = ErrorCode.ROOM_NOT_FOUND;
            return;
        }

        if (roomComponent.ContainsPlayer(request.RoleId))
        {
            response.ErrorCode = ErrorCode.ROOM_ALREADY_JOINED;
            return;
        }

        if (roomComponent.MaxPlayerCount > 0 && roomComponent.GetPlayerCount() >= roomComponent.MaxPlayerCount)
        {
            response.ErrorCode = ErrorCode.ROOM_PLAYER_COUNT_INVALID;
            return;
        }

        var playerInfo = request.ToRoomPlayerInfo();
        playerInfo.JoinTime = TimeHelper.Now;
        roomComponent.AddOrUpdatePlayer(playerInfo);

        response.ErrorCode = ErrorCode.SUCCESS;
        response.Fill(roomComponent);
        await FTask.CompletedTask;
    }
}
