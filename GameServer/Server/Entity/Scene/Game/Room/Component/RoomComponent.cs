using Fantasy.Entitas;

namespace Fantasy;

/// <summary>
/// 房间子场景元数据组件，挂载在房间 SubScene 上。
/// </summary>
public sealed class RoomComponent : Entity
{
    /// <summary>
    /// 业务房间 ID。
    /// </summary>
    public int RoomId;

    /// <summary>
    /// 房间创建时间。
    /// </summary>
    public long CreateTime;
}
