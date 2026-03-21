using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Fantasy;
using Fantasy.Entitas.Interface;
using GameProto;
using Microsoft.IdentityModel.Tokens;
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。

namespace Hotfix;

/// <summary>
/// AccountManagerComponent 的 Awake生命周期
/// </summary>
public sealed class AccountJwtComponentAwakeSystem : AwakeSystem<AccountJwtComponent>
{
    protected override void Awake(AccountJwtComponent self)
    {
        self.Awake();
    }
}

/// <summary>
/// AccountManagerComponent 的 Destroy
/// </summary>
public sealed class AccountJwtComponentDestroySystem : DestroySystem<AccountJwtComponent>
{
    protected override void Destroy(AccountJwtComponent self)
    {
        self.SigningCredentials = null;
        self.TokenValidationParameters = null;
    }
}

/// <summary>
/// AccountManagerComponent 逻辑
/// </summary>
public static class AccountJwtComponentSystem
{
    public static void Awake(this AccountJwtComponent self)
    {
        var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(Convert.FromBase64String(TbFuncParamConfig.AccountTokenPublicKeyPem), out _);
        rsa.ImportRSAPrivateKey(Convert.FromBase64String(TbFuncParamConfig.AccountTokenPrivateKeyPem), out _);
        self.SigningCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);
        self.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateLifetime = true,                                    // 验证令牌是否过期
            ClockSkew = TimeSpan.Zero,                                  // 取消默认5分钟 始终偏差零容忍 严格按照过期时间验证
            ValidateIssuer = true,                                      // 验证发证者
            ValidateAudience = true,                                    // 验证受众
            ValidateIssuerSigningKey = true,                            // 验证签名密钥
            ValidIssuer = TbFuncParamConfig.AccountValidIssuer,         // 有效的发证者
            ValidAudience = TbFuncParamConfig.AccountValidAudience,     // 有效的受众
            IssuerSigningKey = new RsaSecurityKey(rsa)                  // RSA 公钥作为签名验证的密钥
        };
    }

    /// <summary>
    /// 生成包含账号信息的Jwt Token
    /// <remarks>直接构造 JwtHeader + JwtPayload 携带 RoleId 和 accountName</remarks>
    /// </summary>
    /// <param name="self"></param>
    /// <param name="roleId">账号ID</param>
    /// <param name="roleName">账号名称</param>
    /// <returns>签名后的JWT Token</returns>
    public static string GenerateJwtToken(this AccountJwtComponent self, long roleId, string roleName)
    {
        var now = DateTime.UtcNow;
        var jwtPayload = new JwtPayload()
        {
            ["iss"] = TbFuncParamConfig.AccountValidIssuer,                                                             // 发证者
            ["aud"] = TbFuncParamConfig.AccountValidAudience,                                                           // 受众
            ["nbf"] = EpochTime.GetIntDate(now),                                                                        // 生效时间
            ["exp"] = EpochTime.GetIntDate(now.AddMilliseconds(TbFuncParamConfig.AccountTokenExpireTime)),      // 过期时间
            ["aid"] = roleId,                                        
            ["aName"] = roleName,
        };
        var jwtSecurityToken = new JwtSecurityToken(new JwtHeader(self.SigningCredentials), jwtPayload);
        return new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
    }

    /// <summary>
    /// 验证 Jwt Token 的合法性
    /// <remarks>验证签名、发证者、受众及过期时间</remarks>
    /// </summary>
    /// <param name="self"></param>
    /// <param name="jwtToken">待验证的Token</param>
    /// <param name="roleId">验证成功的账号ID</param>
    /// <param name="roleName">验证成功的账号名</param>
    /// <returns></returns>
    public static bool ValidationToken(this AccountJwtComponent self, string jwtToken, out long roleId, out string roleName)
    {
        roleId = 0;
        roleName = string.Empty;

        try
        {
            var claimsPrincipal = new JwtSecurityTokenHandler().ValidateToken(jwtToken, self.TokenValidationParameters, out _);
            var accountIdClaim = claimsPrincipal.FindFirst("aid");
            var accountNameClaim = claimsPrincipal.FindFirst("aName");

            if (accountIdClaim == null || accountNameClaim == null)
            {
                return false;
            }
            roleId = long.Parse(accountIdClaim.Value);
            roleName = accountNameClaim.Value;
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }
}