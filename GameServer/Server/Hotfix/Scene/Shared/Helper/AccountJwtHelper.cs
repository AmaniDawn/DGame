using Fantasy;

namespace Hotfix;

public static class AccountJwtHelper
{
    /// <summary>
    /// 生成包含账号信息的Jwt Token
    /// <remarks>直接构造 JwtHeader + JwtPayload 携带 accountId 和 accountName</remarks>
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="roleId">账号ID</param>
    /// <param name="accountName">账号名称</param>
    /// <returns>签名后的JWT Token</returns>
    public static string GenerateJwtToken(this Scene scene, long roleId, string accountName)
        => scene.GetComponent<AccountJwtComponent>().GenerateJwtToken(roleId, accountName);

    /// <summary>
    /// 验证 Jwt Token 的合法性
    /// <remarks>验证签名、发证者、受众及过期时间</remarks>
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="jwtToken">待验证的Token</param>
    /// <param name="roleId">验证成功的账号ID</param>
    /// <param name="accountName">验证成功的账号名</param>
    /// <returns></returns>
    public static bool ValidationToken(this Scene scene, string jwtToken, out long roleId, out string accountName)
        => scene.GetComponent<AccountJwtComponent>().ValidationToken(jwtToken, out roleId, out accountName);
}