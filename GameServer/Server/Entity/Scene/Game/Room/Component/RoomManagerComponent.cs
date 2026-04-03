using Fantasy.Entitas;

namespace Fantasy;

/// <summary>
/// GameScene 下的房间管理组件。
/// </summary>
public sealed class RoomManagerComponent : Entity
{
    /// <summary>
    /// 下一个可用的本地房间序号。
    /// </summary>
    public int NextRoomId = 1;

    /// <summary>
    /// 房间 ID -> 房间子场景。
    /// </summary>
    public readonly Dictionary<int, SubScene> RoomScenes = new Dictionary<int, SubScene>();
}
