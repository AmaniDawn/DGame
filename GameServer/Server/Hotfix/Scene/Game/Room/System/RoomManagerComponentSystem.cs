using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas.Interface;
using Fantasy.Helper;
using System.Diagnostics.CodeAnalysis;

namespace Hotfix;

/// <summary>
/// RoomManagerComponent 销毁时负责清理托管的房间子场景。
/// </summary>
public sealed class RoomManagerComponentDestroySystem : DestroySystem<RoomManagerComponent>
{
    protected override void Destroy(RoomManagerComponent self)
    {
        foreach (var roomScene in self.RoomScenes.Values.ToList())
        {
            roomScene.Dispose();
        }

        self.RoomScenes.Clear();
    }
}

public static class RoomManagerComponentSystem
{
    /// <summary>
    /// 获取指定房间的子场景。
    /// </summary>
    /// <param name="self">房间管理组件。</param>
    /// <param name="roomId">业务房间 ID。</param>
    /// <returns>存在则返回房间子场景，否则返回 null。</returns>
    public static SubScene? Get(this RoomManagerComponent self, int roomId)
        => self.RoomScenes.TryGetValue(roomId, out var roomScene) ? roomScene : null;

    /// <summary>
    /// 尝试获取指定房间的子场景。
    /// </summary>
    /// <param name="self">房间管理组件。</param>
    /// <param name="roomId">业务房间 ID。</param>
    /// <param name="roomScene">输出的房间子场景。</param>
    /// <returns>查找到房间时返回 true。</returns>
    public static bool TryGet(this RoomManagerComponent self, int roomId, [NotNullWhen(true)] out SubScene? roomScene)
        => self.RoomScenes.TryGetValue(roomId, out roomScene);

    /// <summary>
    /// 创建一个房间子场景，并在其上挂载房间组件和帧同步组件。
    /// </summary>
    /// <param name="self">房间管理组件。</param>
    /// <param name="roomId">业务房间 ID。</param>
    /// <param name="playerCount">房间初始玩家数量。</param>
    /// <returns>已存在则返回已有房间，否则返回新创建的房间子场景。</returns>
    /// <exception cref="ArgumentOutOfRangeException">roomId 或 playerCount 非法时抛出。</exception>
    public static async FTask<SubScene> CreateRoom(this RoomManagerComponent self, int roomId, int playerCount)
    {
        if (roomId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(roomId), roomId, "roomId must be greater than zero.");
        }

        if (playerCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(playerCount), playerCount, "playerCount must be greater than zero.");
        }

        if (self.TryGet(roomId, out var roomScene))
        {
            return roomScene;
        }

        var scene = self.Scene;
        using (await scene.CoroutineLockComponent.Wait(CoroutineLockType.RoomCreateLock, roomId, "RoomManagerComponentSystem.CreateRoom", 10000))
        {
            if (self.TryGet(roomId, out roomScene))
            {
                return roomScene;
            }

            roomScene = await Scene.CreateSubScene(scene, SceneType.Game, async (subScene, parentScene) =>
            {
                var roomComponent = subScene.AddComponent<RoomComponent>();
                roomComponent.RoomId = roomId;
                roomComponent.CreateTime = TimeHelper.Now;

                var frameSyncComponent = subScene.AddComponent<FrameSyncComponent>();
                frameSyncComponent.FrameID = 0;
                frameSyncComponent.PlayerCount = playerCount;

                await FTask.CompletedTask;
            });

            self.RoomScenes.Add(roomId, roomScene);
            return roomScene;
        }
    }

    /// <summary>
    /// 从管理器中移除指定房间。
    /// </summary>
    /// <param name="self">房间管理组件。</param>
    /// <param name="roomId">业务房间 ID。</param>
    /// <param name="dispose">是否同时销毁房间子场景。</param>
    /// <returns>删除成功时返回 true。</returns>
    public static bool Remove(this RoomManagerComponent self, int roomId, bool dispose = true)
    {
        if (!self.RoomScenes.Remove(roomId, out var roomScene))
        {
            return false;
        }

        if (dispose)
        {
            roomScene.Dispose();
        }

        return true;
    }
}
