using Fantasy.Entitas;
using Microsoft.IdentityModel.Tokens;

namespace Fantasy;

public class AuthenticationJwtComponent : Entity
{
    /// <summary>
    /// RSA公钥PEM格式字符串，用于JWT令牌验证
    /// </summary>
    public string PublicKeyPem =
        "MIIBCgKCAQEA0PxKWKHSqRbF5wMgwOEX2BFu+8tg5Ml0MnZr4ZnfajoPemXRD1eN/rzfFRVkr+uHttwr/pCeJqIB0I2ZbHQ9Hs8t/c+MqrC2/MCmqWn5uLFpFASvPid8qGtoBCAWyVMkoJbQR3t05tfHmRQHpclKjJkv9BqhkNk5NeDRoQxU/4kSMVj2DSGLXF0RFrcPrp8ijR9oP/TMdUbUMbXH1NIal1kwHBYyFkXk1FxBgiWfWWyx2O/CLvJHaNpcXvv/JQ96gXMJ2QSoJt3I4mkxA/0WJO603wLg02NlqrwFMorJ7fTWqZcTLcsUjxCWIFPYoHknjX6NJEgpS+1n13sLA+GaUQIDAQAB";

    /// <summary>
    /// RSA私钥PEM格式字符串，用于JWT令牌签名
    /// </summary>
    public string PrivateKeyPem =
        "MIIEowIBAAKCAQEA0PxKWKHSqRbF5wMgwOEX2BFu+8tg5Ml0MnZr4ZnfajoPemXRD1eN/rzfFRVkr+uHttwr/pCeJqIB0I2ZbHQ9Hs8t/c+MqrC2/MCmqWn5uLFpFASvPid8qGtoBCAWyVMkoJbQR3t05tfHmRQHpclKjJkv9BqhkNk5NeDRoQxU/4kSMVj2DSGLXF0RFrcPrp8ijR9oP/TMdUbUMbXH1NIal1kwHBYyFkXk1FxBgiWfWWyx2O/CLvJHaNpcXvv/JQ96gXMJ2QSoJt3I4mkxA/0WJO603wLg02NlqrwFMorJ7fTWqZcTLcsUjxCWIFPYoHknjX6NJEgpS+1n13sLA+GaUQIDAQABAoIBAGCdYlmbZmKZjqCAB7Jj3bwcQyzRF1ht8fQqXzGLC4h2kxVI4N+w4Ip2EsQSgdv6jWFyZDxp61N87k3WSKmlC2Sk72Q5gZSf4djzz5jez34dNrD0gXfAlZbfINVXaHFmqLY9QsjpQGBAPZx9cBOq/XYGk+7MKQloA1TvPLqxktIXXBmxokMtLnxSbRoI1jueHYhKDVvLO2UYXxq9IdjoKKaMLNVsrmf2U8rDG7XZfduopicffogY6TeX3cdxyg8Uzkm1nLWO9nJtHL62KnFyXHTXjumLKAaIf+WcbvfJ+s9cY18pFL8sPeq4xbRW2w06Zp230QBe9uuFusbmezrLbSkCgYEA18SW8o/7oVvjzJGLE0184omdcD4wL7ZXJXnjdPR0vasCOnUmHWkiL4u+wWR/BA8GsfDT+805xfgmMis/3zLuLDul0rYlAvMa2MLb+IdpIwT+TlAH5b4dE8hOWKcocyEVzNfZQQYf4ziWQnrXEqxMYYaQe7v8blONgWT5WUf8rGcCgYEA9/PzV3PBzvN4h2sulgT/oa904AuzFAPNBXeWU/l57PrDqG2NttlF4S1zhRNzXzSlODt5KR/44QQAYj6Nwje6nfaMDM/NRrnkGbZzRW80RP1DOUb9q903h58rGs6qy8vY4YlPM75QLva9g0ZXptengskk7q3EYH1p1YsMyYiZ0IcCgYBwz5dAYSll9x5GQb5eLEBkTSEko08cUxCDRpQ2/Ozgkb2LhN6Vt/cotr7YbEvAen68oDalS2quaAzIZDZz4zQFqnYLkjINtb9On6rU6S9+IMk5drx6UQjw4+Sak2MhtqWoQR6U0bfwXBCr14AFglI5F1sJZoMXx9WPVpTMKkggdwKBgQDQXA+AzaVvYulF4qujJVArbmWoYCx8BTWkAnow1tO+cHs6bdVIcgxmzOrmSRIKTxMHzfJivJtHezVXWXmGW45Wb3gAzB6T8GHduZPkJS6nSqvS1fUVFzAyp25xeHnOB96Yp+oGcUawMGfQiKvfaBk7rgt7BkqfSsREzjRQppmawQKBgDPS3KrmXgNViKbhYxt9dt4O95cfLw34fNOwpg33G6N0iEonykpn3JomkMG0YVgEsS0u/5/6qsMHFOvpDWSI2QrpZZPVH7pxbvAMED0UpD9CThRUGrvQ+MWHitKt1SjaesY2SSIRcrgu2TjwzM3VfF29anXmW3mXw2titgUVJZkG";

    /// <summary>
    /// JWT签名凭据，用于生成JWT令牌
    /// </summary>
    public SigningCredentials SigningCredentials;

    /// <summary>
    /// JWT令牌验证参数，用于验证传入的JWT令牌
    /// </summary>
    public TokenValidationParameters TokenValidationParameters;
}