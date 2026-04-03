using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas.Interface;
using GameProto;

// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。

namespace Hotfix;

/// <summary>
/// 会话标记组件销毁时的房间清理与玩家下线逻辑。
/// </summary>
public sealed class PlayerDataFlagComponentDestroySystem : DestroySystem<PlayerDataFlagComponent>
{
    protected override void Destroy(PlayerDataFlagComponent self)
    {
        if (self.CurrentRoomId > 0)
        {
            self.LeaveCurrentRoom().Coroutine();
        }

        PlayerData playerData = self.playerData;
        if (playerData == null)
        {
            return;
        }

        // 执行下线操作 延迟5秒下线
        playerData.Offline(TbFuncParamConfig.DelayOfflineTime).Coroutine();
    }
}

/// <summary>
/// PlayerDataFlagComponent 的会话房间状态扩展方法。
/// </summary>
public static class PlayerDataFlagComponentSystem
{
    /// <summary>
    /// 设置标记组件的账号数据实体。
    /// </summary>
    /// <param name="self">会话标记组件。</param>
    /// <param name="playerData">账号数据实体。</param>
    public static void SetPlayerData(this PlayerDataFlagComponent self, PlayerData playerData)
    {
        self.playerData = playerData;
    }

    /// <summary>
    /// 设置当前会话所在房间。
    /// </summary>
    /// <param name="self">会话标记组件。</param>
    /// <param name="roomId">房间 ID。</param>
    public static void SetRoom(this PlayerDataFlagComponent self, int roomId)
    {
        self.CurrentRoomId = roomId;
    }

    /// <summary>
    /// 清空当前会话所在房间。
    /// </summary>
    /// <param name="self">会话标记组件。</param>
    public static void ClearRoom(this PlayerDataFlagComponent self)
    {
        self.CurrentRoomId = 0;
    }

    /// <summary>
    /// 判断当前会话是否已经在房间中。
    /// </summary>
    /// <param name="self">会话标记组件。</param>
    /// <returns>已在房间中时返回 true。</returns>
    public static bool HasRoom(this PlayerDataFlagComponent self)
        => self.CurrentRoomId > 0;

    /// <summary>
    /// 复制旧会话的房间状态到当前会话。
    /// </summary>
    /// <param name="self">当前会话标记组件。</param>
    /// <param name="other">旧会话标记组件。</param>
    public static void CopyRoomState(this PlayerDataFlagComponent self, PlayerDataFlagComponent? other)
    {
        self.CurrentRoomId = other?.CurrentRoomId ?? 0;
    }

    /// <summary>
    /// 通知目标 GameScene 将当前玩家从房间中移除。
    /// </summary>
    /// <param name="self">会话标记组件。</param>
    /// <returns>异步任务。</returns>
    public static async FTask LeaveCurrentRoom(this PlayerDataFlagComponent self)
    {
        var roomId = self.CurrentRoomId;
        if (!RoomIdHelper.IsValid(roomId))
        {
            self.ClearRoom();
            return;
        }

        PlayerData playerData = self.playerData;
        if (playerData == null || playerData.IsDisposed)
        {
            self.ClearRoom();
            return;
        }

        var gameSceneConfig = RoomGateHelper.GetGameSceneConfigByRoomId(roomId);
        if (gameSceneConfig == null)
        {
            self.ClearRoom();
            return;
        }

        var response = await self.Scene.Call(gameSceneConfig.Address, new G2Game_LeaveRoomRequest
        {
            RoomId = roomId,
            RoleId = playerData.Id,
        });

        if (response is not G2Game_LeaveRoomResponse leaveResponse || leaveResponse.ErrorCode != ErrorCode.SUCCESS)
        {
            return;
        }

        self.ClearRoom();
    }
}
