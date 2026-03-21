using Fantasy;
using Fantasy.Entitas.Interface;
using GameProto;

// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。

namespace Hotfix;

public sealed class PlayerDataFlagComponentDestroySystem: DestroySystem<PlayerDataFlagComponent>
{
    protected override void Destroy(PlayerDataFlagComponent self)
    {
        PlayerData playerData = self.playerData;
        if (playerData == null)
        {
            return;
        }
        // 执行下线操作 延迟5秒下线
        playerData.Offline(TbFuncParamConfig.DelayOfflineTime).Coroutine();
        self.playerData = null;
    }
}

public static class PlayerDataFlagComponentSystem
{
    /// <summary>
    /// 设置标记组件的账号数据实体
    /// </summary>
    /// <param name="self"></param>
    /// <param name="playerData">账号数据实体</param>
    public static void SetPlayerData(this PlayerDataFlagComponent self, PlayerData playerData)
    {
        self.playerData = playerData;
    }
}