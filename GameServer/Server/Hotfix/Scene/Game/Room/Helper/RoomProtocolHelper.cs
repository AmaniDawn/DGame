using Fantasy;
using GameProto;

namespace Hotfix;

/// <summary>
/// 房间协议对象转换辅助方法。
/// </summary>
public static class RoomProtocolHelper
{
    /// <summary>
    /// 将房间组件转换为客户端房间信息。
    /// </summary>
    /// <param name="roomComponent">房间组件。</param>
    /// <returns>客户端房间信息。</returns>
    public static CSRoomInfo ToCSRoomInfo(this RoomComponent roomComponent)
        => new()
        {
            RoomId = roomComponent.RoomId,
            RoomSeq = roomComponent.RoomSeq,
        };

    /// <summary>
    /// 将房间玩家快照转换为客户端协议对象。
    /// </summary>
    /// <param name="playerInfo">玩家快照。</param>
    /// <returns>客户端玩家信息。</returns>
    public static CSRoomPlayerInfo ToCSRoomPlayerInfo(this RoomPlayerInfo playerInfo)
        => new()
        {
            RoleId = (ulong)playerInfo.RoleId,
            RoleName = playerInfo.RoleName,
            Level = playerInfo.Level,
            FightValue = playerInfo.FightValue,
        };
}