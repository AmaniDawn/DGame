using Fantasy.Entitas;

namespace Fantasy;

/// <summary>
/// 玩家功能开放运行时缓存组件。
/// 持久化数据存放在 PlayerData.OpenFuncList 中。
/// </summary>
public sealed class PlayerFuncOpenComponent : Entity
{
    public EntityReference<PlayerData> PlayerData { get; set; }

    public readonly HashSet<int> OpenFuncSet = new HashSet<int>();

    public bool IsInitialized { get; set; }
}
