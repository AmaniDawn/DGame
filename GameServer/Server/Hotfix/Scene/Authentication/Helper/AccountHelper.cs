using Fantasy;
using Fantasy.Async;

namespace Hotfix;

public static class AccountHelper
{
    /// <summary>
    /// 注册新账号
    /// </summary>
    /// <param name="scene">当前Scene</param>
    /// <param name="username">用户名</param>
    /// <param name="password">密码</param>
    /// <returns></returns>
    public static FTask<uint> Register(this Scene scene, string username, string password)
        => scene.GetComponent<AccountManagerComponent>().Register(username, password);

    /// <summary>
    /// 登录账号
    /// </summary>
    /// <param name="scene">当前Scene</param>
    /// <param name="username">用户名</param>
    /// <param name="password">密码</param>
    /// <returns></returns>
    public static FTask<AccountLoginResult> Login(this Scene scene, string username, string password)
        => scene.GetComponent<AccountManagerComponent>().Login(username, password);

    /// <summary>
    /// 记录最近登录的服务器列表
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="roleId">账户ID</param>
    /// <param name="serverId">服务器ID</param>
    public static FTask RecordRecentServer(this Scene scene, long roleId, int serverId)
        => scene.GetComponent<AccountManagerComponent>().RecordRecentServer(roleId, serverId);
}