using Fantasy.Entitas;
// ReSharper disable InconsistentNaming

namespace Fantasy;

/// <summary>
/// 账号异常下线组件
/// </summary>
public sealed class PlayerDataFlagComponent : Entity
{
    public EntityReference<PlayerData> playerData { get; set; }
}