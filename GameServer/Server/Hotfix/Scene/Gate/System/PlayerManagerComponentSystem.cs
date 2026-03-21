using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using Fantasy.Helper;
#pragma warning disable CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。

// ReSharper disable InconsistentNaming
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8601 // 引用类型赋值可能为 null。
#pragma warning disable CS8620 // 由于引用类型的可为 null 性差异，实参不能用于形参。

namespace Hotfix;

public sealed class PlayerManagerComponentDestroySystem : DestroySystem<PlayerManagerComponent>
{
    protected override void Destroy(PlayerManagerComponent self)
    {
        self.PlayerDataDict.Clear();
    }
}

public static class PlayerManagerComponentSystem
{
    private static (long AccountID, int ServerID) GetCacheKey(long accountID, int serverID)
        => (accountID, serverID);

    private static long GetCreateLockKey(long accountID, int serverID)
        => HashCodeHelper.ComputeHash64($"{accountID}_{serverID}");

    /// <summary>
    /// 将玩家账号数据手动添加到管理器组件缓存中
    /// </summary>
    /// <param name="self"></param>
    /// <param name="playerData"></param>
    public static void Add(this PlayerManagerComponent self, PlayerData playerData)
        => self.PlayerDataDict.TryAdd(GetCacheKey(playerData.AccountID, playerData.ServerID), playerData);

    /// <summary>
    /// 获取玩家账号数据
    /// <remarks>不涉及数据库</remarks>
    /// </summary>
    /// <param name="self"></param>
    /// <param name="accountID"></param>
    /// <param name="serverID"></param>
    public static PlayerData? Get(this PlayerManagerComponent self, long accountID, int serverID)
        => self.PlayerDataDict.GetValueOrDefault(GetCacheKey(accountID, serverID), null);

    /// <summary>
    /// 获取玩家账号数据
    /// </summary>
    /// <param name="self"></param>
    /// <param name="accountID"></param>
    /// <param name="serverID"></param>
    /// <param name="playerData"></param>
    public static bool TryGet(this PlayerManagerComponent self, long accountID, int serverID, out PlayerData playerData)
        => self.PlayerDataDict.TryGetValue(GetCacheKey(accountID, serverID), out playerData);

    /// <summary>
    /// 从数据库创建或加载一个游戏账号
    /// <remarks>先检查缓存 再检查数据库</remarks>
    /// </summary>
    /// <param name="self"></param>
    /// <param name="accountID"></param>
    /// <param name="serverID"></param>
    /// <returns></returns>
    public static async FTask<PlayerData> Create(this PlayerManagerComponent self, long accountID, int serverID)
    {
        if (self.TryGet(accountID, serverID, out PlayerData playerData))
        {
            return playerData;
        }

        var scene = self.Scene;
        var createLockKey = GetCreateLockKey(accountID, serverID);

        using (await scene.CoroutineLockComponent.Wait(CoroutineLockType.PlayerDataCreateLock, createLockKey, "PlayerManagerComponentSystem.Create", 10000))
        {
            if (self.TryGet(accountID, serverID, out playerData))
            {
                return playerData;
            }

            // 尝试从数据库查询
            var worldDataBase = scene.World.Database;
            playerData = await worldDataBase.First<PlayerData>(d => d.AccountID == accountID && d.ServerID == serverID);

            // 数据库不存在 则创建账号
            if (playerData == null)
            {
                playerData = Entity.Create<PlayerData>(scene, true, true);
                playerData.AccountID = accountID;
                playerData.ServerID = serverID;
                playerData.CreateTime = TimeHelper.Now;
                // 新账号插入到数据库
                await worldDataBase.Insert(playerData);
            }

            // 加入到账号缓存中
            self.Add(playerData);
            return playerData;
        }
    }

    /// <summary>
    /// 删除玩家账号缓存数据
    /// </summary>
    /// <param name="self"></param>
    /// <param name="accountID"></param>
    /// <param name="serverID"></param>
    /// <param name="isDispose"></param>
    public static bool Remove(this PlayerManagerComponent self, long accountID, int serverID, bool isDispose = true)
    {
        if (!self.PlayerDataDict.Remove(GetCacheKey(accountID, serverID), out var playerData))
        {
            return false;
        }

        if (isDispose)
        {
            playerData.Dispose();
        }
        
        return true;
    }

    /// <summary>
    /// 删除玩家账号缓存数据
    /// </summary>
    /// <param name="self"></param>
    /// <param name="playerData"></param>
    /// <param name="isDispose"></param>
    public static bool Remove(this PlayerManagerComponent self, PlayerData playerData, bool isDispose = true)
    {
        if (!self.PlayerDataDict.Remove(GetCacheKey(playerData.AccountID, playerData.ServerID)))
        {
            return false;
        }

        if (isDispose)
        {
            playerData.Dispose();
        }
        
        return true;
    }
}
