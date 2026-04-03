namespace Fantasy;

/// <summary>
/// 房间 ID 编解码辅助工具。
/// </summary>
public static class RoomIdHelper
{
    /// <summary>
    /// 单个 GameScene 可分配的本地房间序号容量。
    /// </summary>
    public const int RoomIdFactor = 100000;

    /// <summary>
    /// 使用 GameScene 配置 ID 和本地房间序号生成全局房间 ID。
    /// </summary>
    /// <param name="sceneConfigId">GameScene 配置 ID。</param>
    /// <param name="roomSeq">本地房间序号。</param>
    /// <returns>全局唯一房间 ID。</returns>
    public static int CreateRoomId(uint sceneConfigId, int roomSeq)
        => CreateRoomId((int)sceneConfigId, roomSeq);

    /// <summary>
    /// 使用 GameScene 配置 ID 和本地房间序号生成全局房间 ID。
    /// </summary>
    /// <param name="sceneConfigId">GameScene 配置 ID。</param>
    /// <param name="roomSeq">本地房间序号。</param>
    /// <returns>全局唯一房间 ID。</returns>
    public static int CreateRoomId(int sceneConfigId, int roomSeq)
        => checked(sceneConfigId * RoomIdFactor + roomSeq);

    /// <summary>
    /// 从全局房间 ID 中解析所属 GameScene 配置 ID。
    /// </summary>
    /// <param name="roomId">全局房间 ID。</param>
    /// <returns>GameScene 配置 ID。</returns>
    public static int GetSceneConfigId(int roomId)
        => roomId / RoomIdFactor;

    /// <summary>
    /// 从全局房间 ID 中解析本地房间序号。
    /// </summary>
    /// <param name="roomId">全局房间 ID。</param>
    /// <returns>本地房间序号。</returns>
    public static int GetRoomSeq(int roomId)
        => roomId % RoomIdFactor;

    /// <summary>
    /// 校验房间 ID 是否合法。
    /// </summary>
    /// <param name="roomId">全局房间 ID。</param>
    /// <returns>合法时返回 true。</returns>
    public static bool IsValid(int roomId)
        => roomId > RoomIdFactor;
}
