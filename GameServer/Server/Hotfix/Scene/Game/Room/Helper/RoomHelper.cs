using Fantasy;
using Fantasy.Async;

namespace Hotfix;

/// <summary>
/// 房间相关的 Scene 扩展方法。
/// </summary>
public static class RoomHelper
{
    /// <summary>
    /// 在根 GameScene 下创建一个房间子场景。
    /// </summary>
    /// <param name="scene">根 GameScene。</param>
    /// <param name="roomId">业务房间 ID。</param>
    /// <param name="playerCount">房间初始玩家数量。</param>
    /// <returns>创建完成的房间子场景。</returns>
    public static FTask<SubScene> CreateRoom(this Scene scene, int roomId, int playerCount)
        => scene.GetComponent<RoomManagerComponent>().CreateRoom(roomId, playerCount);

    /// <summary>
    /// 获取指定房间的子场景。
    /// </summary>
    /// <param name="scene">根 GameScene。</param>
    /// <param name="roomId">业务房间 ID。</param>
    /// <returns>存在则返回房间子场景，否则返回 null。</returns>
    public static SubScene? GetRoom(this Scene scene, int roomId)
        => scene.GetComponent<RoomManagerComponent>().Get(roomId);

    /// <summary>
    /// 尝试获取指定房间的子场景。
    /// </summary>
    /// <param name="scene">根 GameScene。</param>
    /// <param name="roomId">业务房间 ID。</param>
    /// <param name="roomScene">输出的房间子场景。</param>
    /// <returns>查找到房间时返回 true。</returns>
    public static bool TryGetRoom(this Scene scene, int roomId, out SubScene? roomScene)
        => scene.GetComponent<RoomManagerComponent>().TryGet(roomId, out roomScene);

    /// <summary>
    /// 删除指定房间。
    /// </summary>
    /// <param name="scene">根 GameScene。</param>
    /// <param name="roomId">业务房间 ID。</param>
    /// <param name="dispose">是否同时销毁房间子场景。</param>
    /// <returns>删除成功时返回 true。</returns>
    public static bool RemoveRoom(this Scene scene, int roomId, bool dispose = true)
        => scene.GetComponent<RoomManagerComponent>().Remove(roomId, dispose);
}
