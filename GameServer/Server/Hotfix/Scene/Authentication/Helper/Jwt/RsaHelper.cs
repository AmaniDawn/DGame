using System.Security.Cryptography;
using System.Text;
using Fantasy;

namespace Hotfix;

/// <summary>
/// RSA 密钥辅助工具。
/// </summary>
public static class RsaHelper
{
    /// <summary>
    /// 生成 RSA 公钥和私钥，并以 Base64 字符串形式输出。
    /// </summary>
    public static void GenerateKey()
    {
        using RSA rsa = RSA.Create();
        // 一般长度通常为2048和4096 密钥越长 安全性越高 性能越差
        rsa.KeySize = 2048;
        var exportRsaPublicKey = rsa.ExportRSAPublicKey();
        var exportRsaPrivateKey = rsa.ExportRSAPrivateKey();
        var base64PublicString = Convert.ToBase64String(exportRsaPublicKey);
        var base64PrivateString = Convert.ToBase64String(exportRsaPrivateKey);
        Log.Info($"RSA Public Key: {base64PublicString}");
        Log.Info($"RSA Private Key: {base64PrivateString}");
    }
    
    /// <summary>
    /// 生成带密码保护的 PKCS#8 RSA 密钥，并以 Base64 字符串形式输出。
    /// </summary>
    /// <param name="password">用于保护私钥的密码。</param>
    public static void GeneratePkcs8Key(string password)
    {
        using RSA rsa = RSA.Create();
        // 一般长度通常为2048和4096 密钥越长 安全性越高 性能越差
        rsa.KeySize = 2048;
        // 设置加密算法的参数 使用 AES256-CBC 进行加密
        var pbeParameters = new PbeParameters(PbeEncryptionAlgorithm.Aes128Cbc, HashAlgorithmName.SHA256, 10000);
        // 需要保护私钥的密码
        var exportRsaPublicKey = rsa.ExportSubjectPublicKeyInfo();
        var exportRsaPrivateKey = rsa.ExportEncryptedPkcs8PrivateKey(Encoding.UTF8.GetBytes(password), pbeParameters);
        var base64PublicString = Convert.ToBase64String(exportRsaPublicKey);
        var base64PrivateString = Convert.ToBase64String(exportRsaPrivateKey);
        Log.Info($"RSA Public Key: {base64PublicString}");
        Log.Info($"RSA Private Key: {base64PrivateString}");
    }
}
