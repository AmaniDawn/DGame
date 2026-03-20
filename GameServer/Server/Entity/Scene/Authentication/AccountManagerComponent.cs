using Fantasy.Entitas;

namespace Fantasy;

/// <summary>
/// 账号登录结果。
/// </summary>
public struct AccountLoginResult
{
    /// <summary>
    /// 角色 ID。
    /// </summary>
    public long RoleId;

    /// <summary>
    /// 登录令牌。
    /// </summary>
    public string Token;

    /// <summary>
    /// 错误码。
    /// </summary>
    public uint ErrorCode;

    /// <summary>
    /// 最近登录的服务器列表。
    /// </summary>
    public IReadOnlyList<int> RecentServerList;

    /// <summary>
    /// 使用错误码初始化登录结果。
    /// </summary>
    /// <param name="errorCode">错误码。</param>
    public AccountLoginResult(uint errorCode)
    {
        ErrorCode  = errorCode;
    }
    
    /// <summary>
    /// 使用完整信息初始化登录结果。
    /// </summary>
    /// <param name="roleId">角色 ID。</param>
    /// <param name="token">登录令牌。</param>
    /// <param name="errorCode">错误码。</param>
    /// <param name="recentServerList">最近登录的服务器列表。</param>
    public AccountLoginResult(long roleId, string token, uint errorCode, IReadOnlyList<int> recentServerList)
    {
        RoleId = roleId;
        Token = token;
        ErrorCode  = errorCode;
        RecentServerList = recentServerList;
    }
}

/// <summary>
/// 账号管理组件
/// </summary>
public class AccountManagerComponent : Entity
{
}
