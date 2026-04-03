using System;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network.Interface;

namespace Hotfix;

/// <summary>
/// 处理 Gate 转发到 GameScene 的离开房间请求。
/// </summary>
public sealed class G2Game_LeaveRoomRequestHandler : AddressRPC<Scene, G2Game_LeaveRoomRequest, G2Game_LeaveRoomResponse>
{
    protected override async FTask Run(Scene entity, G2Game_LeaveRoomRequest request, G2Game_LeaveRoomResponse response, Action reply)
    {
        if (!RoomIdHelper.IsValid(request.RoomId) || request.RoleId <= 0)
        {
            response.ErrorCode = ErrorCode.SUCCESS;
            return;
        }

        if (!entity.TryGetRoom(request.RoomId, out var roomScene) || roomScene == null)
        {
            response.ErrorCode = ErrorCode.SUCCESS;
            return;
        }

        var roomComponent = roomScene.GetComponent<RoomComponent>();
        if (roomComponent == null)
        {
            response.ErrorCode = ErrorCode.SUCCESS;
            return;
        }

        roomComponent.RemovePlayer(request.RoleId);
        if (roomComponent.GetPlayerCount() <= 0)
        {
            entity.RemoveRoom(request.RoomId);
        }
        else
        {
            entity.BroadcastRoomPlayerInfos(roomComponent);
        }

        response.ErrorCode = ErrorCode.SUCCESS;
        await FTask.CompletedTask;
    }
}
