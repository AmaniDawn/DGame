namespace Fantasy;

/// <summary>
/// 协程锁常量表
/// </summary>
public static class CoroutineLockType
{
    /// <summary>
    /// 注册账号协程锁类型
    /// </summary>
    public const long AccountRegisterLock = 1;

    /// <summary>
    /// 玩家数据加载/创建协程锁类型
    /// </summary>
    public const long PlayerDataCreateLock = 2;
}
