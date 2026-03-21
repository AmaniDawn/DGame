using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas.Interface;
using Fantasy.Helper;
using Fantasy.Network;

namespace Hotfix;

public sealed class PlayerDataDestroySystem : DestroySystem<PlayerData>
{
    protected override void Destroy(PlayerData self)
    {
        self.SessionRuntimeId = 0;
        self.AccountID = 0;
        self.RoleName = string.Empty;
        self.HeadID = 0;
        self.Sex = 0;
        self.Level = 0;
        self.Exp = 0;
        self.FightValue = 0;
        self.Diamond = 0;
        self.Gold = 0;
        self.Stam = 0;
        self.IsFinGuide = 0;
        self.Sign = string.Empty;
        self.WorldID = 0;
        self.TotalRmb = 0;
        self.LastAddStamTime = 0;
        self.DailyBuyStamCount = 0;
        self.CreateTime = 0;
        self.LastLoginTime = 0;
    }
}

public static class PlayerDataSystem
{
    /// <summary>
    /// 记录客户端的Session
    /// </summary>
    /// <param name="self"></param>
    /// <param name="sessionRuntimeId"></param>
    public static void RecordSession(this PlayerData self, long sessionRuntimeId)
    {
        self.SessionRuntimeId = sessionRuntimeId;
    }
    
    /// <summary>
    /// 账号上线逻辑
    /// </summary>
    /// <param name="self"></param>
    public static async FTask Online(this PlayerData self)
    {
        await FTask.CompletedTask;
    }
    
    /// <summary>
    /// 账号下线逻辑
    /// </summary>
    /// <param name="self"></param>
    /// <param name="timeOut">延迟下线时间</param>
    public static async FTask Offline(this PlayerData self, int timeOut = 0)
    {
        var scene = self.Scene;
        var roleId = self.Id;
        var playerManagerComponent = scene.GetComponent<PlayerManagerComponent>();
        if (!playerManagerComponent.TryGet(roleId, out var playerData))
        {
            // 如果缓存中没有 表示已经下线或根本不存在账号
            Log.Warning($"PlayerDataSystem Offline fail roleID: {roleId} not found");
            return;
        }

        if (!scene.TryGetEntity<Session>(self.SessionRuntimeId, out var oldSession))
        {
            Log.Warning($"PlayerDataSystem Offline fail Session: {self.SessionRuntimeId} not found");
            return;
        }

        if (timeOut <= 0)
        {
            // 直接执行下线操作
            await self.InternalOffline();
        }
        // 延迟下线
        playerData.SetDestroyTimeout(timeOut, self.InternalOffline);
        await FTask.CompletedTask;
    }
    
    /// <summary>
    /// 内部下线方法
    /// </summary>
    /// <param name="self"></param>
    private static async FTask InternalOffline(this PlayerData self)
    {
        // 保存当前账号数据到数据库
        await self.Scene.World.Database.Save(self);
        // 在缓存中移除自己 并执行自己的Dispose销毁方法
        self.Scene.GetComponent<PlayerManagerComponent>().Remove(self.Id);
    }
}
