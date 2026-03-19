using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Fantasy;
using Fantasy.Entitas.Interface;
using Microsoft.IdentityModel.Tokens;

namespace Hotfix;

public sealed class AuthenticationJwtComponentAwakeSystem : AwakeSystem<AuthenticationJwtComponent>
{
    protected override void Awake(AuthenticationJwtComponent self)
    {
        self.Awake();
    }
}

public static class AuthenticationJwtComponentSystem
{
    public static void Awake(this AuthenticationJwtComponent self)
    {
        var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(Convert.FromBase64String(self.PublicKeyPem), out _);
        rsa.ImportRSAPrivateKey(Convert.FromBase64String(self.PrivateKeyPem), out _);
        self.SigningCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);
        // 创建 TokenValidationParameters 对象 用于配置验证参数
        self.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateLifetime = false,                   // 禁止令牌验证时间是否过期
            ValidateIssuer = true,                      // 验证发行者
            ValidateAudience = true,                    // 验证受众
            ValidateIssuerSigningKey = true,            // 验证签名密钥
            ValidIssuer = "Fantasy",                    // 有效的发行者
            ValidAudience = "Fantasy",                  // 有效的受众
            IssuerSigningKey = new RsaSecurityKey(rsa)  // RSA公钥作为签名密钥
        };
    }

    /// <summary>
    /// 获取登录 Token
    /// </summary>
    /// <param name="self"></param>
    /// <param name="uid">账号ID</param>
    /// <param name="address">登录的目标服务器地址</param>
    /// <param name="port">登录的目标服务器地址端口号</param>
    /// <param name="sceneId">分配的Scene的Id</param>
    /// <returns></returns>
    public static string GetToken(this AuthenticationJwtComponent self, long uid, string address, int port, uint sceneId)
    {
        var jwtPayload = new JwtPayload()
        {
            { "uid", uid },
            { "address", address },
            { "port", port },
            { "sceneId", sceneId},
        };
        var jwtSecurityToken = new JwtSecurityToken
            (
            issuer: "Fantasy",
            audience: "Fantasy",
            claims: jwtPayload.Claims,
            expires: DateTime.UtcNow.AddMilliseconds(3000),
            signingCredentials: self.SigningCredentials
            );
        return new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
    }
}