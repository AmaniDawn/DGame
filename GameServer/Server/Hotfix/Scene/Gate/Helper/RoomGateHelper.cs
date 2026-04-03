using System.Linq;
using Fantasy;
using GameProto;
using Fantasy.Platform.Net;

namespace Hotfix;

/// <summary>
/// Gate 房间请求辅助方法。
/// </summary>
public static class RoomGateHelper
{
    /// <summary>
    /// 获取当前 Gate 同世界下的目标 GameScene 配置。
    /// </summary>
    /// <param name="scene">Gate 场景。</param>
    /// <returns>目标 GameScene 配置；不存在时返回 null。</returns>
    public static SceneConfig? GetGameSceneConfig(this Scene scene)
    {
        var worldConfigId = (int)SceneConfigData.Instance.Get((uint)scene.SceneConfigId).WorldConfigId;
        return SceneConfigData.Instance.GetSceneBySceneType(worldConfigId, SceneType.Game).FirstOrDefault();
    }

    /// <summary>
    /// 根据房间 ID 解析目标 GameScene 配置。
    /// </summary>
    /// <param name="roomId">房间 ID。</param>
    /// <returns>目标 GameScene 配置；不存在时返回 null。</returns>
    public static SceneConfig? GetGameSceneConfigByRoomId(int roomId)
    {
        var sceneConfigId = RoomIdHelper.GetSceneConfigId(roomId);
        return SceneConfigData.Instance.TryGet((uint)sceneConfigId, out var config) ? config : null;
    }

    /// <summary>
    /// 将在线玩家数据转换为创建房间服内请求。
    /// </summary>
    /// <param name="playerData">在线玩家数据。</param>
    /// <param name="playerCount">房间最大玩家数量。</param>
    /// <returns>服内创建房间请求。</returns>
    public static G2Game_CreateRoomRequest ToCreateRoomRequest(this PlayerData playerData, int playerCount)
        => new()
        {
            RoleId = playerData.Id,
            AccountId = playerData.AccountID,
            ServerId = playerData.ServerID,
            SessionRuntimeId = playerData.SessionRuntimeId,
            RoleName = playerData.RoleName,
            HeadId = playerData.HeadID,
            Sex = playerData.Sex,
            Level = playerData.Level,
            FightValue = playerData.FightValue,
            PlayerCount = playerCount,
        };

    /// <summary>
    /// 将在线玩家数据转换为加入房间服内请求。
    /// </summary>
    /// <param name="playerData">在线玩家数据。</param>
    /// <param name="roomId">房间 ID。</param>
    /// <returns>服内加入房间请求。</returns>
    public static G2Game_JoinRoomRequest ToJoinRoomRequest(this PlayerData playerData, int roomId)
        => new()
        {
            RoomId = roomId,
            RoleId = playerData.Id,
            AccountId = playerData.AccountID,
            ServerId = playerData.ServerID,
            SessionRuntimeId = playerData.SessionRuntimeId,
            RoleName = playerData.RoleName,
            HeadId = playerData.HeadID,
            Sex = playerData.Sex,
            Level = playerData.Level,
            FightValue = playerData.FightValue,
        };
}
