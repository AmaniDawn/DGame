namespace Fantasy;

public sealed class AuthenticationComponent : Entitas.Entity
{
    /// <summary>
    /// 当前服务器在鉴权服务器中的位置
    /// </summary>
    public int Position;

    /// <summary>
    /// 当前鉴权组的数量
    /// </summary>
    public int AuthenticationCount;

    public readonly Dictionary<string, Account> CacheAccountList = new Dictionary<string, Account>();

    public readonly Dictionary<string, AccountCacheInfo> CacheLoginAccountList = new Dictionary<string, AccountCacheInfo>();
}