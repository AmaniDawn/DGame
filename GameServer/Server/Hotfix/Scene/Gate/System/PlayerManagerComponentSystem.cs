using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using Fantasy.Helper;

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
    /// <summary>
    /// 将玩家账号数据手动添加到管理器组件缓存中
    /// </summary>
    /// <param name="self"></param>
    /// <param name="playerData"></param>
    public static void Add(this PlayerManagerComponent self, PlayerData playerData)
        => self.PlayerDataDict.TryAdd(playerData.Id, playerData);

    /// <summary>
    /// 获取玩家账号数据
    /// <remarks>不涉及数据库</remarks>
    /// </summary>
    /// <param name="self"></param>
    /// <param name="roleID"></param>
    public static PlayerData? Get(this PlayerManagerComponent self, long roleID)
        => self.PlayerDataDict.GetValueOrDefault(roleID, null);

    /// <summary>
    /// 获取玩家账号数据
    /// </summary>
    /// <param name="self"></param>
    /// <param name="roleID"></param>
    /// <param name="playerData"></param>
    public static bool TryGet(this PlayerManagerComponent self, long roleID, out PlayerData playerData)
        => self.PlayerDataDict.TryGetValue(roleID, out playerData);

    /// <summary>
    /// 从数据库创建或加载一个游戏账号
    /// <remarks>先检查缓存 再检查数据库</remarks>
    /// </summary>
    /// <param name="self"></param>
    /// <param name="roleID"></param>
    /// <returns></returns>
    public static async FTask<PlayerData> Create(this PlayerManagerComponent self, long roleID)
    {
        if (self.TryGet(roleID, out PlayerData playerData))
        {
            return playerData;
        }

        var scene = self.Scene;

        using (await scene.CoroutineLockComponent.Wait(CoroutineLockType.PlayerDataCreateLock, roleID, "PlayerManagerComponentSystem.Create", 10000))
        {
            if (self.TryGet(roleID, out playerData))
            {
                return playerData;
            }

            // 尝试从数据库查询
            var worldDataBase = scene.World.Database;
            // true 让服务器框架知道有PlayerData
            playerData = await worldDataBase.Query<PlayerData>(roleID, true);

            // 数据库不存在 则创建账号
            if (playerData == null)
            {
                playerData = Entity.Create<PlayerData>(scene,true, true);
                playerData.AccountID = roleID;
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
    /// <param name="roleID"></param>
    /// <param name="isDispose"></param>
    public static bool Remove(this PlayerManagerComponent self, long roleID, bool isDispose = true)
    {
        if (!self.PlayerDataDict.Remove(roleID, out var playerData))
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
        if (!self.PlayerDataDict.Remove(playerData.Id))
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
