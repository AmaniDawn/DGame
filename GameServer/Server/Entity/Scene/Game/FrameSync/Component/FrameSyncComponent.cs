using Fantasy.Entitas;

namespace Fantasy;

public class FrameSyncComponent : Entity
{
    /// <summary>
    /// 当前帧ID
    /// </summary>
    public int FrameID;

    /// <summary>
    /// 当前帧玩家数量
    /// </summary>
    public int PlayerCount;
}