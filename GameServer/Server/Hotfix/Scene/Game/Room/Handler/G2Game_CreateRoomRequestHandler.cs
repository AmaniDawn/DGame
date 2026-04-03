using System;
using Fantasy;
using Fantasy.Async;
using Fantasy.Helper;
using Fantasy.Network.Interface;

namespace Hotfix;

/// <summary>
/// 处理 Gate 转发到 GameScene 的创建房间请求。
/// </summary>
public sealed class G2Game_CreateRoomRequestHandler : AddressRPC<Scene, G2Game_CreateRoomRequest, G2Game_CreateRoomResponse>
{
    protected override async FTask Run(Scene entity, G2Game_CreateRoomRequest request, G2Game_CreateRoomResponse response, Action reply)
    {
        if (request.PlayerCount <= 0 || request.RoleId <= 0)
        {
            response.ErrorCode = ErrorCode.ROOM_INVALID_PARAMETER;
            return;
        }

        var roomScene = await entity.CreateRoom(request.PlayerCount);
        var roomComponent = roomScene.GetComponent<RoomComponent>();
        if (roomComponent == null)
        {
            response.ErrorCode = ErrorCode.ROOM_CREATE_FAILED;
            return;
        }

        var playerInfo = request.ToRoomPlayerInfo();
        playerInfo.JoinTime = TimeHelper.Now;
        roomComponent.AddOrUpdatePlayer(playerInfo);

        response.ErrorCode = ErrorCode.SUCCESS;
        response.Fill(roomComponent);
        entity.BroadcastRoomPlayerInfos(roomComponent);
    }
}
