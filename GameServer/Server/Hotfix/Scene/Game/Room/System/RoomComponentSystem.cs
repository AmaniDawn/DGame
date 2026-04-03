using System;
using Fantasy;
using Fantasy.Entitas.Interface;
using Fantasy.Helper;
using System.Diagnostics.CodeAnalysis;

namespace Hotfix;

public sealed class RoomComponentDestroySystem : DestroySystem<RoomComponent>
{
    protected override void Destroy(RoomComponent self)
    {
        self.PlayerInfos.Clear();
        self.RoomId = 0;
        self.RoomSeq = 0;
        self.CaptainRoleId = 0;
        self.MaxPlayerCount = 0;
        self.CreateTime = 0;
        self.IsBattleStarted = false;
        self.IsBattleEntered = false;
    }
}

public static class RoomComponentSystem
{
    public static int GetPlayerCount(this RoomComponent self)
        => self.PlayerInfos.Count;

    public static bool ContainsPlayer(this RoomComponent self, long roleId)
        => roleId > 0 && self.PlayerInfos.ContainsKey(roleId);

    public static bool TryGetPlayer(this RoomComponent self, long roleId, [NotNullWhen(true)] out RoomPlayerInfo? playerInfo)
        => self.PlayerInfos.TryGetValue(roleId, out playerInfo);

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
        self.ClearBattleProgress();
        self.SyncPlayerCount();
        return playerInfo;
    }

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

        self.ClearBattleProgress();
        self.SyncPlayerCount();
        return true;
    }

    public static bool RemovePlayer(this RoomComponent self, PlayerData playerData)
        => playerData != null && self.RemovePlayer(playerData.Id);

    public static IReadOnlyCollection<RoomPlayerInfo> GetPlayers(this RoomComponent self)
        => self.PlayerInfos.Values;

    private static void SyncPlayerCount(this RoomComponent self)
    {
        var frameSyncComponent = self.GetComponent<FrameSyncComponent>();
        if (frameSyncComponent != null)
        {
            frameSyncComponent.PlayerCount = self.PlayerInfos.Count;
        }
    }
}
