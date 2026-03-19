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
    public static FTask<uint> Login(this Scene scene, string username, string password)
        => scene.GetComponent<AccountManagerComponent>().Login(username, password);
}