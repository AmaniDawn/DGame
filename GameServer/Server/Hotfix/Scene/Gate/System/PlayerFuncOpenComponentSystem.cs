using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas.Interface;
using Fantasy.Helper;
using Fantasy.Network;
using GameProto;

namespace Hotfix;

public sealed class PlayerFuncOpenComponentDestroySystem : DestroySystem<PlayerFuncOpenComponent>
{
    protected override void Destroy(PlayerFuncOpenComponent self)
    {
        self.PlayerData = null;
        self.OpenFuncSet.Clear();
        self.IsInitialized = false;
    }
}

public static class PlayerFuncOpenComponentSystem
{
    public static PlayerFuncOpenComponent EnsureFuncOpenComponent(this PlayerData self)
    {
        var component = self.GetComponent<PlayerFuncOpenComponent>();
        if (component != null)
        {
            PlayerData playerData = component.PlayerData;
            if (playerData== null)
            {
                component.PlayerData = self;
            }

            return component;
        }

        component = self.AddComponent<PlayerFuncOpenComponent>();
        component.PlayerData = self;
        return component;
    }

    public static void InitializeFromPersistence(this PlayerData self)
    {
        var component = self.EnsureFuncOpenComponent();
        component.OpenFuncSet.Clear();

        if (self.OpenFuncList != null)
        {
            foreach (var funcId in self.OpenFuncList)
            {
                component.OpenFuncSet.Add(funcId);
            }
        }

        component.IsInitialized = true;
    }

    public static List<int> GetOpenFuncList(this PlayerData self)
    {
        var component = self.EnsureFuncOpenComponent();
        if (!component.IsInitialized)
        {
            self.InitializeFromPersistence();
        }

        return component.OpenFuncSet.OrderBy(static id => id).ToList();
    }

    public static List<int> RefreshOpenFuncState(this PlayerData self)
    {
        var component = self.EnsureFuncOpenComponent();
        if (!component.IsInitialized)
        {
            self.InitializeFromPersistence();
        }

        var newOpenFuncList = new List<int>();
        foreach (var config in TbFuncOpenConfig.DataList)
        {
            if (config == null)
            {
                continue;
            }

            if (config.FuncID <= 0)
            {
                Log.Warning($"[FuncOpen] Ignore invalid func id: {config.FuncID}");
                continue;
            }

            if (component.OpenFuncSet.Contains(config.FuncID))
            {
                continue;
            }

            if (!self.CanOpenFunc(config, component.OpenFuncSet))
            {
                continue;
            }

            component.OpenFuncSet.Add(config.FuncID);
            newOpenFuncList.Add(config.FuncID);
        }

        if (newOpenFuncList.Count > 0)
        {
            self.OpenFuncList = component.OpenFuncSet.OrderBy(static id => id).ToList();
        }

        return newOpenFuncList;
    }

    public static async FTask NotifyNewOpenFuncs(this PlayerData self, IEnumerable<int> newOpenFuncList)
    {
        if (!self.Scene.TryGetEntity<Session>(self.SessionRuntimeId, out var session))
        {
            await FTask.CompletedTask;
            return;
        }

        var funcIds = newOpenFuncList?.ToList();
        if (funcIds == null || funcIds.Count == 0)
        {
            await FTask.CompletedTask;
            return;
        }

        var notify = G2C_FuncOpenNotify.Create();
        notify.NewOpenFuncList.AddRange(funcIds);
        session.Send(notify);
        await FTask.CompletedTask;
    }

    private static bool CanOpenFunc(this PlayerData self, FuncOpenConfig config, IReadOnlySet<int> currentOpenFuncSet)
    {
        if (config.ParentFuncID > 0 && config.ParentFuncID != config.FuncID && !currentOpenFuncSet.Contains(config.ParentFuncID))
        {
            return false;
        }

        if (config.OpenLevel > 0 && self.Level < config.OpenLevel)
        {
            return false;
        }

        if (config.CreateRoleDay > 0)
        {
            var createDate = DateTimeOffset.FromUnixTimeSeconds(self.CreateTime).UtcDateTime.Date;
            var nowDate = DateTimeOffset.FromUnixTimeSeconds(TimeHelper.Now).UtcDateTime.Date;
            var createRoleDay = (nowDate - createDate).Days + 1;
            if (createRoleDay < config.CreateRoleDay)
            {
                return false;
            }
        }

        if (config.OpenChapter > 0)
        {
            // 当前 PlayerData 尚未承载章节进度字段，先保持保守策略。
            return false;
        }

        return true;
    }
}
