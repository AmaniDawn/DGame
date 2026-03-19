using Fantasy;

namespace Hotfix;

public static class AuthenticationJwtHelper
{
    /// <summary>
    /// 获取登录 Token
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="aId">账号ID</param>
    /// <param name="address">登录的目标服务器地址</param>
    /// <param name="port">登录的目标服务器地址端口号</param>
    /// <param name="sceneId">分配的Scene的Id</param>
    /// <returns></returns>
    public static string GetToken(Scene scene, long aId, string address, int port, uint sceneId)
        => scene.GetComponent<AuthenticationJwtComponent>().GetToken(aId, address, port, sceneId);
}