using Fantasy.Entitas;

namespace Fantasy;

/// <summary>
/// 账号实体
/// </summary>
public sealed class Account : Entity
{
    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// 登录密码
    /// <remarks>MD5方式存储</remarks>
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// 账号创建时间
    /// </summary>
    public long CreateTime { get; set; }

    /// <summary>
    /// 账号上次登陆时间
    /// </summary>
    public long LastLoginTime { get; set; }

    /// <summary>
    /// 最近登录的服务器列表ID
    /// </summary>
    public List<int> RecentServerList { get; set; } = new List<int>();
}