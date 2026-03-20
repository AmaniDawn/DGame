using Fantasy.Entitas;
using Microsoft.IdentityModel.Tokens;

namespace Fantasy;

/// <summary>
/// 账号 JWT 组件。
/// </summary>
public class AccountJwtComponent : Entity
{
    /// <summary>
    /// JWT 签名凭据。
    /// </summary>
    public SigningCredentials SigningCredentials { get; set; }
    
    /// <summary>
    /// JWT 令牌校验参数。
    /// </summary>
    public TokenValidationParameters TokenValidationParameters { get; set; }
}
