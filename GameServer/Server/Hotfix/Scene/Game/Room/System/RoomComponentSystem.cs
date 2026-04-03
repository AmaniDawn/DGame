using Fantasy;
using Fantasy.Entitas.Interface;
using Fantasy.Helper;
using System.Diagnostics.CodeAnalysis;

namespace Hotfix;

/// <summary>
/// RoomComponent 销毁时负责清空房间内缓存的玩家快照。
/// </summary>
public sealed class RoomComponentDestroySystem : DestroySystem<RoomComponent>
{
    protected override void Destroy(RoomComponent self)
    {
        self.PlayerInfos.Clear();
        self.RoomId = 0;
        self.RoomSeq = 0;
        self.MaxPlayerCount = 0;
        self.CreateTime = 0;
    }
}

/// <summary>
/// RoomComponent 的房间成员管理逻辑。
/// </summary>
public static class RoomComponentSystem
{
    /// <summary>
    /// 获取当前房间玩家数量。
    /// </summary>
    /// <param name="self">房间组件。</param>
    /// <returns>当前房间玩家数量。</returns>
    public static int GetPlayerCount(this RoomComponent self)
        => self.PlayerInfos.Count;

    /// <summary>
    /// 检查指定玩家是否已在房间中。
    /// </summary>
    /// <param name="self">房间组件。</param>
    /// <param name="roleId">角色 ID。</param>
    /// <returns>已在房间中时返回 true。</returns>
    public static bool ContainsPlayer(this RoomComponent self, long roleId)
        => roleId > 0 && self.PlayerInfos.ContainsKey(roleId);

    /// <summary>
    /// 尝试获取房间中的玩家快照。
    /// </summary>
    /// <param name="self">房间组件。</param>
    /// <param name="roleId">角色 ID。</param>
    /// <param name="playerInfo">输出的玩家快照。</param>
    /// <returns>查找到玩家时返回 true。</returns>
    public static bool TryGetPlayer(this RoomComponent self, long roleId, [NotNullWhen(true)] out RoomPlayerInfo? playerInfo)
        => self.PlayerInfos.TryGetValue(roleId, out playerInfo);

    /// <summary>
    /// 添加或更新一个房间玩家快照。
    /// </summary>
    /// <param name="self">房间组件。</param>
    /// <param name="playerData">在线玩家数据。</param>
    /// <returns>添加或更新后的玩家快照。</returns>
    /// <exception cref="ArgumentNullException">playerData 为空时抛出。</exception>
    /// <exception cref="ArgumentException">玩家角色 ID 非法时抛出。</exception>
    public static RoomPlayerInfo AddPlayer(this RoomComponent self, PlayerData playerData)
    {
        ArgumentNullException.ThrowIfNull(playerData);

        var roleId = playerData.Id;
        if (roleId <= 0)
        {
            throw new ArgumentException("playerData.Id must be greater than zero.", nameof(playerData));
        }

        var playerInfo = new RoomPlayerInfo
        {
            RoleId = roleId,
            AccountId = playerData.AccountID,
            ServerId = playerData.ServerID,
            SessionRuntimeId = playerData.SessionRuntimeId,
            RoleName = playerData.RoleName,
            HeadId = playerData.HeadID,
            Sex = playerData.Sex,
            Level = playerData.Level,
            FightValue = playerData.FightValue,
            JoinTime = TimeHelper.Now,
        };

        self.PlayerInfos[roleId] = playerInfo;
        self.SyncPlayerCount();
        return playerInfo;
    }

    /// <summary>
    /// 移除指定角色的房间快照。
    /// </summary>
    /// <param name="self">房间组件。</param>
    /// <param name="roleId">角色 ID。</param>
    /// <returns>移除成功时返回 true。</returns>
    public static bool RemovePlayer(this RoomComponent self, long roleId)
    {
        if (roleId <= 0)
        {
            return false;
        }

        if (!self.PlayerInfos.Remove(roleId))
        {
            return false;
        }

        self.SyncPlayerCount();
        return true;
    }

    /// <summary>
    /// 移除指定玩家的房间快照。
    /// </summary>
    /// <param name="self">房间组件。</param>
    /// <param name="playerData">在线玩家数据。</param>
    /// <returns>移除成功时返回 true。</returns>
    public static bool RemovePlayer(this RoomComponent self, PlayerData playerData)
        => playerData != null && self.RemovePlayer(playerData.Id);

    /// <summary>
    /// 获取当前房间全部玩家快照。
    /// </summary>
    /// <param name="self">房间组件。</param>
    /// <returns>房间玩家快照列表。</returns>
    public static IReadOnlyCollection<RoomPlayerInfo> GetPlayers(this RoomComponent self)
        => self.PlayerInfos.Values;

    /// <summary>
    /// 将房间人数同步到 FrameSyncComponent。
    /// </summary>
    /// <param name="self">房间组件。</param>
    private static void SyncPlayerCount(this RoomComponent self)
    {
        var frameSyncComponent = self.GetComponent<FrameSyncComponent>();
        if (frameSyncComponent != null)
        {
            frameSyncComponent.PlayerCount = self.PlayerInfos.Count;
        }
    }
}
